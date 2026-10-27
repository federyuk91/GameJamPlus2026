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
        private readonly List<SimulatedBody2D> passengers = new List<SimulatedBody2D>(8);
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
            foreach (SimulatedBody2D body in SimulatedBody2D.ActiveBodies)
            {
                if (body == null || !body.isActiveAndEnabled) continue;
                Collider2D candidate = body.Shape;
                if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy
                    || candidate == surfaceCollider || candidate.transform.IsChildOf(transform)) continue;
                Bounds bounds = candidate.bounds;
                float skin = Mathf.Max(0f, body.Skin);
                float normalGap = skin + contactTolerance;
                float minimumUpNormal = Mathf.Max(0.01f, Mathf.Cos(body.MaxSlopeAngle * Mathf.Deg2Rad));
                float probeDistance = normalGap / minimumUpNormal;
                // Physics2D ignores Z; renderer depth must not exclude a valid pressure contact.
                Rect searchBounds = Rect.MinMaxRect(surface.min.x - probeDistance, surface.min.y - probeDistance,
                    surface.max.x + probeDistance, surface.max.y + probeDistance);
                Rect bodyBounds = Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
                if (!searchBounds.Overlaps(bodyBounds)) continue;
                Collider2D support = body.Probe(Vector2.down, probeDistance,
                    out float distance, out Vector2 normal, out Vector2 point);
                if (support != surfaceCollider || normal.y < minimumUpNormal) continue;
                // Probe reports full-body travel, compensating for its inset query shape.
                // Subtract the actual resting skin gap along the surface normal.
                float effectiveGap = distance * normal.y - skin;
                if (effectiveGap > contactTolerance) continue;
                float horizontalOverlap = Mathf.Min(bounds.max.x, surface.max.x) - Mathf.Max(bounds.min.x, surface.min.x);
                if (bounds.center.y <= point.y || horizontalOverlap <= 0.005f) continue;
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
