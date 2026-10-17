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
        public bool OnSteepSlope { get; private set; }
        public bool HasGroundContact { get; private set; }
        public Vector2 GroundNormal { get; private set; } = Vector2.up;
        public Vector2 GroundPoint { get; private set; }
        public Collider2D GroundCollider { get; private set; }
        public float GroundAngle => Vector2.Angle(GroundNormal, Vector2.up);
        private void Awake() => shape = GetComponent<BoxCollider2D>();

        public void RefreshGround(float maxSlopeAngle, bool canGround = true)
        {
            // On steep ramps, vertical distance exaggerates the actual separation from the surface.
            Collider2D support = ProbeFromOffset(Vector2.zero, Vector2.down, skin * 2f / 0.01f,
                out float distance, out Vector2 normal, out Vector2 point);
            bool touching = canGround && support != null && normal.y > 0.01f
                && distance * normal.y <= skin * 2f;
            GroundNormal = touching ? normal : Vector2.up;
            GroundPoint = touching ? point : (Vector2)transform.position;
            HasGroundContact = touching;
            GroundCollider = touching ? support : null;
            Grounded = touching && IsWalkable(normal, maxSlopeAngle);
            OnSteepSlope = touching && !Grounded;
        }

        public void MovePlayer(Vector2 displacement, float maxStepHeight, float maxSlopeAngle,
            Collider2D horizontalIgnored = null, bool followGround = false)
        {
            if (horizontalIgnored != null)
            {
                // A held crate and player must retain their shared horizontal displacement.
                Move(displacement, horizontalIgnored);
                RefreshGround(maxSlopeAngle, followGround || displacement.y <= 0f);
                return;
            }

            bool wasGrounded = Grounded && followGround;
            float descent = Mathf.Abs(displacement.x) * Mathf.Abs(GroundNormal.x) / Mathf.Max(0.01f, GroundNormal.y);
            descent = Mathf.Min(descent, displacement.magnitude + maxStepHeight);
            HitWall = HitCeiling = false;
            Vector2 remaining = displacement;
            for (int i = 0; i < 4 && remaining.sqrMagnitude > 0.0000000001f; i++)
            {
                float requested = remaining.magnitude;
                Vector2 direction = remaining / requested;
                Collider2D obstacle = Probe(direction, requested + skin, out float distance, out Vector2 normal);
                float separation = skin / Mathf.Max(0.01f, -Vector2.Dot(direction, normal));
                float allowed = obstacle != null ? Mathf.Min(requested, Mathf.Max(0f, distance - separation)) : requested;
                transform.position += (Vector3)(direction * allowed);
                remaining -= direction * allowed;
                if (obstacle == null || allowed >= requested) break;

                bool walkable = IsWalkable(normal, maxSlopeAngle);
                if (wasGrounded && !walkable && Mathf.Abs(normal.x) > 0.01f && maxStepHeight > 0f
                    && TryStep(remaining.x, maxStepHeight, maxSlopeAngle))
                {
                    remaining.x = 0f;
                    continue;
                }

                if (normal.y < -0.01f) HitCeiling = true;
                if (!walkable && Mathf.Abs(normal.x) > 0.01f) HitWall = true;
                if (walkable && !followGround && remaining.y < 0f)
                {
                    // Landing absorbs the fall instead of converting it into downhill motion.
                    Vector2 tangent = new Vector2(normal.y, -normal.x);
                    remaining = tangent * (remaining.x / normal.y);
                }
                else
                {
                    remaining -= normal * Mathf.Min(0f, Vector2.Dot(remaining, normal));
                }
                // Project onto ramps, but never turn horizontal input into uphill motion on steep ones.
                if (normal.y > 0.01f && !walkable && remaining.y > 0f) remaining = Vector2.zero;
            }

            if (wasGrounded)
            {
                Collider2D support = Probe(Vector2.down, descent + skin * 2f, out float distance, out Vector2 normal);
                if (support != null && IsWalkable(normal, maxSlopeAngle))
                    transform.position += Vector3.down * Mathf.Max(0f, distance - skin);
            }
            RefreshGround(maxSlopeAngle, followGround || displacement.y <= 0f);
            Physics2D.SyncTransforms();
        }

        private static bool IsWalkable(Vector2 normal, float maxSlopeAngle)
        {
            return normal.y > 0.01f && Vector2.Angle(normal, Vector2.up) <= Mathf.Clamp(maxSlopeAngle, 0f, 89f) + 0.01f;
        }

        public void Move(Vector2 displacement, Collider2D horizontalIgnored = null, float maxStepHeight = 0f,
            Collider2D ignored = null)
        {
            Physics2D.SyncTransforms();
            bool canStep = Grounded && displacement.y <= 0f && maxStepHeight > 0f && horizontalIgnored == null;
            float startX = transform.position.x;
            HitWall = MoveAxis(Vector2.right, displacement.x, horizontalIgnored != null ? horizontalIgnored : ignored);
            if (HitWall && canStep)
            {
                float remaining = displacement.x - (transform.position.x - startX);
                if (TryStep(remaining, maxStepHeight)) HitWall = false;
            }
            HitCeiling = false;
            bool verticalHit = MoveAxis(Vector2.up, displacement.y, ignored);
            HitCeiling = verticalHit && displacement.y > 0f;
            Collider2D support = Probe(Vector2.down, skin * 2f, out float groundDistance, ignored);
            Grounded = displacement.y <= 0f && (verticalHit || (support != null && groundDistance < skin * 2f));
            GroundCollider = Grounded ? support : null;
            OnSteepSlope = false;
            HasGroundContact = Grounded;
            GroundNormal = Vector2.up;
            GroundPoint = (Vector2)Shape.bounds.center + Vector2.down * (Shape.bounds.extents.y + skin);
            Physics2D.SyncTransforms();
        }

        private bool TryStep(float horizontal, float maxHeight, float maxSlopeAngle = 89f)
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
            Collider2D support = ProbeFromOffset(landingOffset, Vector2.down, lift + skin, out float groundDistance, out Vector2 normal);
            if (support == null || !IsWalkable(normal, maxSlopeAngle)) return false;
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

        public Collider2D Probe(Vector2 direction, float distance, out float nearest, out Vector2 normal, Collider2D ignored = null)
        {
            return ProbeFromOffset(Vector2.zero, direction, distance, out nearest, out normal, ignored);
        }

        private Collider2D ProbeFromOffset(Vector2 offset, Vector2 direction, float distance, out float nearest, Collider2D ignored = null)
        {
            return ProbeFromOffset(offset, direction, distance, out nearest, out _, ignored);
        }

        private Collider2D ProbeFromOffset(Vector2 offset, Vector2 direction, float distance, out float nearest, out Vector2 normal, Collider2D ignored = null)
        {
            return ProbeFromOffset(offset, direction, distance, out nearest, out normal, out _, ignored);
        }

        private Collider2D ProbeFromOffset(Vector2 offset, Vector2 direction, float distance, out float nearest,
            out Vector2 normal, out Vector2 point, Collider2D ignored = null)
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
            normal = Vector2.zero;
            point = Vector2.zero;
            Collider2D closest = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == shape || hit.collider == ignored || hit.collider.transform.IsChildOf(transform)) continue;
                // Ignore surfaces parallel to this axis (floor while moving horizontally).
                if (Vector2.Dot(hit.normal, direction) >= -0.01f) continue;
                if (hit.distance < nearest || (Mathf.Approximately(hit.distance, nearest) && hit.normal.y > normal.y))
                {
                    nearest = hit.distance;
                    closest = hit.collider;
                    normal = hit.normal;
                    point = hit.point;
                }
            }
            return closest;
        }
    }
}
