using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(260), DisallowMultipleComponent, RequireComponent(typeof(InteractableButton))]
    public sealed class ButtonPressurePlate : MonoBehaviour
    {
        [SerializeField] private InteractableButton button;
        [SerializeField] private Collider2D surfaceCollider;
        [SerializeField, Min(0.005f), Tooltip("World-space gap allowed between a body's feet and the button top.")]
        private float contactTolerance = 0.025f;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
        private readonly List<SimulatedBody2D> passengers = new List<SimulatedBody2D>(8);
        private readonly Dictionary<Collider2D, SimulatedBody2D> bodyCache = new Dictionary<Collider2D, SimulatedBody2D>();
        private readonly List<Vector2> shapePoints = new List<Vector2>(16);
        private readonly List<Vector2[]> shapePaths = new List<Vector2[]>(4);

        public Collider2D SurfaceCollider => surfaceCollider;
        public bool HasWeight { get; private set; }

        private void Awake()
        {
            if (button == null) button = GetComponent<InteractableButton>();
            if (surfaceCollider != null) surfaceCollider.isTrigger = false;
        }

        private void FixedUpdate()
        {
            if (button == null || !button.isActiveAndEnabled) return;
            CollectPassengers();
            HasWeight = false;
            foreach (SimulatedBody2D passenger in passengers)
            {
                if (!passenger.CanPressButtons) continue;
                HasWeight = true;
                break;
            }
            button.UpdateWeight(HasWeight);
        }

        private void CollectPassengers()
        {
            passengers.Clear();
            if (surfaceCollider == null || !surfaceCollider.enabled || !surfaceCollider.gameObject.activeInHierarchy) return;
            Physics2D.SyncTransforms();
            Bounds surface = surfaceCollider.bounds;
            Vector2 center = surface.center;
            Vector2 size = (Vector2)surface.size + Vector2.one * (contactTolerance * 2f);
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            Physics2D.OverlapBox(center, size, 0f, filter, overlaps);
            foreach (Collider2D candidate in overlaps)
            {
                if (candidate == surfaceCollider || candidate.transform.IsChildOf(transform)) continue;
                if (!bodyCache.TryGetValue(candidate, out SimulatedBody2D body))
                {
                    candidate.TryGetComponent(out body);
                    bodyCache.Add(candidate, body);
                }
                if (body == null || !body.isActiveAndEnabled || body.Shape != candidate) continue;
                Bounds bounds = candidate.bounds;
                float tolerance = Mathf.Max(contactTolerance, body.Skin * 2f);
                ColliderDistance2D contact = surfaceCollider.Distance(candidate);
                if (!contact.isValid || contact.distance > tolerance) continue;
                float gap = bounds.min.y - contact.pointA.y;
                float horizontalOverlap = Mathf.Min(bounds.max.x, surface.max.x) - Mathf.Max(bounds.min.x, surface.min.x);
                if (bounds.center.y <= contact.pointA.y || Mathf.Abs(gap) > tolerance || horizontalOverlap <= 0.005f) continue;
                if (!passengers.Contains(body)) passengers.Add(body);
            }
        }

        public void MatchVisual(SpriteRenderer visual)
        {
            if (surfaceCollider == null || visual == null || visual.sprite == null) return;
            CollectPassengers();
            float previousTop = surfaceCollider.bounds.max.y;
            PolygonCollider2D polygon = GetPolygonSurface();
            shapePaths.Clear();
            for (int path = 0; path < visual.sprite.GetPhysicsShapeCount(); path++)
            {
                visual.sprite.GetPhysicsShape(path, shapePoints);
                if (shapePoints.Count < 3) continue;
                Vector2[] points = new Vector2[shapePoints.Count];
                for (int index = 0; index < points.Length; index++) points[index] = ToColliderPoint(visual, shapePoints[index]);
                shapePaths.Add(points);
            }
            if (shapePaths.Count == 0)
            {
                Bounds bounds = visual.sprite.bounds;
                shapePaths.Add(new Vector2[] { ToColliderPoint(visual, new Vector2(bounds.min.x, bounds.min.y)),
                    ToColliderPoint(visual, new Vector2(bounds.max.x, bounds.min.y)),
                    ToColliderPoint(visual, new Vector2(bounds.max.x, bounds.max.y)),
                    ToColliderPoint(visual, new Vector2(bounds.min.x, bounds.max.y)) });
            }
            polygon.offset = Vector2.zero;
            polygon.pathCount = shapePaths.Count;
            for (int path = 0; path < shapePaths.Count; path++) polygon.SetPath(path, shapePaths[path]);
            surfaceCollider.isTrigger = false;
            Physics2D.SyncTransforms();
            float delta = surfaceCollider.bounds.max.y - previousTop;
            if (!Application.isPlaying || Mathf.Abs(delta) <= 0.00001f) return;
            foreach (SimulatedBody2D passenger in passengers)
            {
                // Move with the top using normal obstacle casts, ignoring only the button itself.
                passenger.Move(Vector2.up * delta, ignored: surfaceCollider);
                passenger.RefreshGround();
            }
            Physics2D.SyncTransforms();
        }

        private PolygonCollider2D GetPolygonSurface()
        {
            if (surfaceCollider is PolygonCollider2D polygon) return polygon;
            // Compatibility for older scene copies still referencing the original box surface.
            Collider2D previous = surfaceCollider;
            polygon = previous.GetComponent<PolygonCollider2D>();
            if (polygon == null) polygon = previous.gameObject.AddComponent<PolygonCollider2D>();
            polygon.sharedMaterial = previous.sharedMaterial;
            polygon.density = previous.density;
            polygon.enabled = previous.enabled;
            previous.enabled = false;
            surfaceCollider = polygon;
            return polygon;
        }

        private Vector2 ToColliderPoint(SpriteRenderer visual, Vector2 point)
        {
            if (visual.flipX) point.x = -point.x;
            if (visual.flipY) point.y = -point.y;
            return surfaceCollider.transform.InverseTransformPoint(visual.transform.TransformPoint(point));
        }

        private void OnDisable()
        {
            HasWeight = false;
            passengers.Clear();
            bodyCache.Clear();
        }

        private void OnDrawGizmos()
        {
            if (button == null || !button.ShowGizmos || surfaceCollider == null) return;
            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.color = Color.yellow;
            Gizmos.matrix = surfaceCollider.transform.localToWorldMatrix;
            if (surfaceCollider is PolygonCollider2D polygon)
            {
                for (int path = 0; path < polygon.pathCount; path++)
                {
                    polygon.GetPath(path, shapePoints);
                    for (int index = 0; index < shapePoints.Count; index++)
                        Gizmos.DrawLine(shapePoints[index] + polygon.offset, shapePoints[(index + 1) % shapePoints.Count] + polygon.offset);
                }
            }
            else
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawWireCube(surfaceCollider.bounds.center, surfaceCollider.bounds.size);
            }
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
