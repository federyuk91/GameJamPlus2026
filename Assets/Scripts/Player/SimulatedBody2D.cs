using UnityEngine;

namespace MousePlatformer
{
    // Queries the world, then moves the Transform. No Rigidbody2D or solver forces.
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class SimulatedBody2D : MonoBehaviour
    {
        [SerializeField] private LayerMask solidLayers = 1;
        [SerializeField, Min(0.001f)] private float skin = 0.005f;
        private BoxCollider2D shape;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        public BoxCollider2D Shape => shape != null ? shape : shape = GetComponent<BoxCollider2D>();
        public float Skin => skin;
        public ContactFilter2D SolidFilter
        {
            get
            {
                ContactFilter2D filter = new ContactFilter2D();
                filter.SetLayerMask(solidLayers);
                filter.useTriggers = false;
                return filter;
            }
        }
        public bool Grounded { get; private set; }
        public bool HitCeiling { get; private set; }
        public bool HitWall { get; private set; }
        private void Awake() => shape = GetComponent<BoxCollider2D>();

        public void Move(Vector2 displacement, Collider2D horizontalIgnored = null, float maxStepHeight = 0f)
        {
            Physics2D.SyncTransforms();
            bool canStep = Grounded && displacement.y <= 0f && maxStepHeight > 0f && horizontalIgnored == null;
            float startX = transform.position.x;
            HitWall = MoveAxis(Vector2.right, displacement.x, horizontalIgnored);
            if (HitWall && canStep)
            {
                float remaining = displacement.x - (transform.position.x - startX);
                if (TryStep(remaining, maxStepHeight)) HitWall = false;
            }
            HitCeiling = false;
            bool verticalHit = MoveAxis(Vector2.up, displacement.y);
            HitCeiling = verticalHit && displacement.y > 0f;
            Grounded = displacement.y <= 0f && (verticalHit || Cast(Vector2.down, skin * 2f) < skin * 2f);
            Physics2D.SyncTransforms();
        }

        private bool TryStep(float horizontal, float maxHeight)
        {
            if (Mathf.Approximately(horizontal, 0f)) return false;
            Probe(Vector2.up, maxHeight + skin, out float ceilingDistance);
            float lift = Mathf.Min(maxHeight, Mathf.Max(0f, ceilingDistance - skin));
            if (lift <= 0f) return false;

            // Query an up-across-down route before committing any vertical movement.
            Vector2 raisedOffset = Vector2.up * lift;
            Vector2 direction = Vector2.right * Mathf.Sign(horizontal);
            float requested = Mathf.Abs(horizontal);
            Collider2D wall = ProbeFromOffset(raisedOffset, direction, requested + skin, out float wallDistance);
            if (wall != null && wallDistance - skin < requested) return false;

            Vector2 landingOffset = raisedOffset + Vector2.right * horizontal;
            Collider2D support = ProbeFromOffset(landingOffset, Vector2.down, lift + skin, out float groundDistance);
            if (support == null) return false;
            float rise = lift - Mathf.Max(0f, groundDistance - skin);
            if (rise <= 0f || rise > maxHeight) return false;

            transform.position += new Vector3(horizontal, rise, 0f);
            return true;
        }

        public float AllowedHorizontal(float amount, Collider2D ignored = null)
        {
            if (Mathf.Approximately(amount, 0f)) return 0f;
            Probe(Vector2.right * Mathf.Sign(amount), Mathf.Abs(amount) + skin, out float distance, ignored);
            return Mathf.Sign(amount) * Mathf.Min(Mathf.Abs(amount), Mathf.Max(0f, distance - skin));
        }

        public float MoveHorizontal(float amount, Collider2D ignored = null)
        {
            float allowed = AllowedHorizontal(amount, ignored);
            transform.position += Vector3.right * allowed;
            Physics2D.SyncTransforms();
            return allowed;
        }

        private bool MoveAxis(Vector2 axis, float amount, Collider2D ignored = null)
        {
            if (Mathf.Approximately(amount, 0f)) return false;
            Vector2 direction = axis * Mathf.Sign(amount);
            float requested = Mathf.Abs(amount);
            Probe(direction, requested + skin, out float distance, ignored);
            float allowed = Mathf.Min(requested, Mathf.Max(0f, distance - skin));
            transform.position += (Vector3)(direction * allowed);
            return allowed < requested;
        }

        private float Cast(Vector2 direction, float distance)
        {
            Probe(direction, distance, out float nearest);
            return nearest;
        }

        public Collider2D Probe(Vector2 direction, float distance, out float nearest, Collider2D ignored = null)
        {
            return ProbeFromOffset(Vector2.zero, direction, distance, out nearest, ignored);
        }

        private Collider2D ProbeFromOffset(Vector2 offset, Vector2 direction, float distance, out float nearest, Collider2D ignored = null)
        {
            Physics2D.SyncTransforms();
            shape = Shape;
            // Read geometry from the Transform so the second axis sees the first axis's move.
            Vector2 center = (Vector2)transform.TransformPoint(shape.offset) + offset;
            Vector2 scale = transform.lossyScale;
            Vector2 size = Vector2.Scale(shape.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            ContactFilter2D filter = SolidFilter;
            int count = Physics2D.BoxCast(center, size, transform.eulerAngles.z, direction, filter, hits, distance);
            nearest = distance;
            Collider2D closest = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == shape || hit.collider == ignored || hit.collider.transform.IsChildOf(transform)) continue;
                // Ignore surfaces parallel to this axis (floor while moving horizontally).
                if (Vector2.Dot(hit.normal, direction) >= -0.01f) continue;
                if (hit.distance <= nearest)
                {
                    nearest = hit.distance;
                    closest = hit.collider;
                }
            }
            return closest;
        }
    }
}
