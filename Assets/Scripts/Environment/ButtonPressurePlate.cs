using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(260), DisallowMultipleComponent, RequireComponent(typeof(InteractableButton))]
    public sealed class ButtonPressurePlate : MonoBehaviour
    {
        [SerializeField] private InteractableButton button;
        [SerializeField] private BoxCollider2D surfaceCollider;
        [SerializeField, Min(0.005f), Tooltip("World-space gap allowed between a body's feet and the button top.")]
        private float contactTolerance = 0.025f;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
        private readonly List<SimulatedBody2D> passengers = new List<SimulatedBody2D>(8);
        private readonly Dictionary<Collider2D, SimulatedBody2D> bodyCache = new Dictionary<Collider2D, SimulatedBody2D>();

        public BoxCollider2D SurfaceCollider => surfaceCollider;
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
            Vector2 center = new Vector2(surface.center.x, surface.max.y + contactTolerance * 0.5f);
            Vector2 size = new Vector2(Mathf.Max(0.001f, surface.size.x - 0.01f), contactTolerance * 3f);
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
                float gap = bounds.min.y - surface.max.y;
                float tolerance = Mathf.Max(contactTolerance, body.Skin * 2f);
                float horizontalOverlap = Mathf.Min(bounds.max.x, surface.max.x) - Mathf.Max(bounds.min.x, surface.min.x);
                if (bounds.center.y <= surface.max.y || Mathf.Abs(gap) > tolerance || horizontalOverlap <= 0.005f) continue;
                if (!passengers.Contains(body)) passengers.Add(body);
            }
        }

        public void MatchVisual(SpriteRenderer visual)
        {
            if (surfaceCollider == null || visual == null || visual.sprite == null) return;
            CollectPassengers();
            float previousTop = surfaceCollider.bounds.max.y;
            Bounds spriteBounds = visual.sprite.bounds;
            Vector3 min = surfaceCollider.transform.InverseTransformPoint(visual.transform.TransformPoint(spriteBounds.min));
            Vector3 max = surfaceCollider.transform.InverseTransformPoint(visual.transform.TransformPoint(spriteBounds.max));
            surfaceCollider.size = new Vector2(Mathf.Max(0.001f, Mathf.Abs(max.x - min.x)), Mathf.Max(0.001f, Mathf.Abs(max.y - min.y)));
            surfaceCollider.offset = (min + max) * 0.5f;
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
            Gizmos.DrawWireCube(surfaceCollider.offset, surfaceCollider.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
