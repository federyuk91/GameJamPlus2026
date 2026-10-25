using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MousePlatformer
{
    // Queries the world, then moves the Transform. No Rigidbody2D or solver forces.
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D), typeof(GroundContact2D))]
    public sealed class SimulatedBody2D : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared world settings used by every physics behaviour on this body.")]
        private PhysicsSettings physicsSettings;
        [Header("Prop resistance")]
        [SerializeField, Min(0f), Tooltip("Air velocity damping in inverse seconds, shared by PropsMotion and BalloonLift. Character locomotion keeps its own acceleration and braking.")]
        private float airDrag = 0.35f;
        [SerializeField, Min(0f), Tooltip("Individual response to shared Physics Settings > Water > Water Drag. 1 uses the shared drag; props apply it progressively with immersion. Swimmers damp both axes.")]
        private float waterDragMultiplier = 5f / SwimMotion.DefaultWaterDrag;
        [SerializeField, Min(0f), FormerlySerializedAs("groundDrag"), Tooltip("Ground friction coefficient. Maximum resisting force is this value times the force pressing the body against the ground. A resting body holds while tan(slope angle) is no greater than this coefficient under gravity alone. Replaces the former Ground Drag damping formula.")]
        private float groundFriction = 1f;
        [SerializeField, Range(0f, 89f), Tooltip("Maximum walkable surface angle in degrees for this body. Does not set the sliding threshold: sliding is calculated from forces and Ground Friction.")]
        private float maxSlopeAngle = 45f;
        [SerializeField, Tooltip("Shared ground detector, refreshed by the movement driver and after body moves.")]
        private GroundContact2D groundContact;
        [Header("Speed limit")]
        [SerializeField, Tooltip("Enable a local speed limit instead of Physics Settings > Max Speed. Disabled by default.")]
        private bool overrideMaxSpeed;
        [SerializeField, Min(0.01f), Tooltip("Local maximum total movement speed, used only while Override Max Speed is enabled. Caps vector magnitude and the movement budget in every medium.")]
        private float maxSpeed = 26f;
        [SerializeField, Min(0f), Tooltip("Individual multiplier of world gravity, shared by props and animal movement. 1 applies world gravity unchanged. Swimming and airborne descent can apply additional gameplay multipliers.")]
        private float gravityScale = 1f;
        [Header("Collision and mass")]
        [SerializeField, Tooltip("Collider geometry used for custom casts. May be a trigger to keep the body non-blocking; keep pickup triggers separate.")]
        private BoxCollider2D shape;
        [SerializeField, Tooltip("Ignore player colliders in this body's movement queries, so non-blocking props cannot land on or be stopped by the player.")]
        private bool ignorePlayerColliders;
        [SerializeField, Tooltip("The trigger shape supports other simulated props while remaining pass-through for the player. Pickup triggers are not affected.")]
        private bool blocksOtherProps;
        [SerializeField, Tooltip("Whether this body counts as weight on pressure buttons. Does not change physical mass or collisions.")]
        private bool canPressButtons = true;
        [SerializeField] private LayerMask solidLayers = 1;
        [SerializeField, Min(0.001f)] private float skin = 0.005f;
        [SerializeField, Min(0.001f), Tooltip("Dry body mass in kilograms. Shared by pushing, flotation, rope loads and balloon balance.")]
        private float weight = 1f;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(32);
        private PropsMotion propMotion;
        private double movementStepTime = double.NegativeInfinity;
        private float movementRemaining;
        private float RemainingMovementDistance => movementStepTime == Time.fixedTimeAsDouble
            ? movementRemaining : float.PositiveInfinity;
        public BoxCollider2D Shape => shape != null ? shape : shape = GetComponent<BoxCollider2D>();
        public float Skin => skin;
        public float Weight => Mathf.Max(0.001f, weight);
        public bool CanPressButtons => canPressButtons;
        public void SetCanPressButtons(bool value) => canPressButtons = value;
        public PhysicsSettings Settings => physicsSettings;
        public float AirDrag => Mathf.Max(0f, airDrag);
        public float WaterDrag => Mathf.Max(0f, waterDragMultiplier) * Mathf.Max(0f,
            physicsSettings != null ? physicsSettings.water.waterDrag : SwimMotion.DefaultWaterDrag);
        public float MaxSpeed => Mathf.Max(0.01f, overrideMaxSpeed ? maxSpeed
            : physicsSettings != null ? physicsSettings.maxSpeed : PhysicsSettings.DefaultMaxSpeed);
        public float GravityScale => Mathf.Max(0f, gravityScale);
        public float GravityAcceleration => (physicsSettings != null
            ? physicsSettings.gravityAcceleration : PhysicsSettings.DefaultGravityAcceleration) * GravityScale;
        public Vector2 LimitVelocity(Vector2 velocity) => Vector2.ClampMagnitude(velocity, MaxSpeed);
        public float GroundFriction => Mathf.Max(0f, groundFriction);
        public float MaxSlopeAngle => Mathf.Clamp(maxSlopeAngle, 0f, 89f);
        public GroundContact2D GroundContact => groundContact != null
            ? groundContact : groundContact = GetComponent<GroundContact2D>();
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
        public bool Grounded => GroundContact.IsWalkable;
        public ContactFilter2D MotionFilter
        {
            get
            {
                ContactFilter2D filter = SolidFilter;
                filter.useTriggers = propMotion != null;
                return filter;
            }
        }

        public bool IsMotionObstacle(Collider2D candidate)
        {
            if (candidate == null) return false;
            if (!candidate.isTrigger) return true;
            if (propMotion == null) return false;
            if (candidate.TryGetComponent(out SimulatedBody2D other) && other.isActiveAndEnabled
                && other.blocksOtherProps && candidate == other.Shape) return true;
            // Empty plates block other props, while remaining pass-through for the animal.
            return candidate.TryGetComponent(out Pushable pushable)
                && pushable.isActiveAndEnabled && candidate == pushable.Body.Shape;
        }
        public bool HitCeiling { get; private set; }
        public bool HitWall { get; private set; }
        public bool OnSteepSlope => GroundContact.IsSteep;
        public bool HasGroundContact => GroundContact.HasContact;
        public Vector2 GroundNormal => GroundContact.Normal;
        public Vector2 GroundPoint => GroundContact.Point;
        public Collider2D GroundCollider => GroundContact.Collider;
        public float GroundAngle => GroundContact.Angle;
        private void Awake()
        {
            if (shape == null) shape = GetComponent<BoxCollider2D>();
            propMotion = GetComponent<PropsMotion>();
        }

        public void RefreshGround(bool canGround = true, Collider2D ignored = null)
            => GroundContact.Refresh(canGround, ignored);

        public void BeginMovementStep(float dt)
        {
            movementStepTime = Time.fixedTimeAsDouble;
            movementRemaining = MaxSpeed * Mathf.Max(0f, dt);
        }

        private Vector2 LimitDisplacement(Vector2 displacement)
            => Vector2.ClampMagnitude(displacement, Mathf.Min(MaxSpeed * Time.fixedDeltaTime, RemainingMovementDistance));

        private Vector2 Translate(Vector2 displacement)
        {
            Vector2 applied = Vector2.ClampMagnitude(displacement, RemainingMovementDistance);
            transform.position += (Vector3)applied;
            if (movementStepTime == Time.fixedTimeAsDouble)
                movementRemaining = Mathf.Max(0f, movementRemaining - applied.magnitude);
            return applied;
        }

        public void MovePlayer(Vector2 displacement, float maxStepHeight,
            Collider2D horizontalIgnored = null, bool followGround = false)
        {
            displacement = LimitDisplacement(displacement);
            if (horizontalIgnored != null)
            {
                if (followGround && Grounded)
                {
                    // Keep the shared push displacement while retaining the player's own step handling.
                    SweepMove(displacement, maxStepHeight, horizontalIgnored, true, maxStepHeight);
                    return;
                }
                Move(displacement, horizontalIgnored);
                RefreshGround(followGround || displacement.y <= 0f);
                return;
            }

            SweepMove(displacement, maxStepHeight, null, followGround);
        }

        private void SweepMove(Vector2 displacement, float maxStepHeight, Collider2D ignored, bool followGround, float maxStepDownHeight = 0f)
        {
            float maxSlopeAngle = MaxSlopeAngle;
            bool wasGrounded = HasGroundContact && followGround;
            float descent = Mathf.Abs(displacement.x) * Mathf.Abs(GroundNormal.x) / Mathf.Max(0.01f, GroundNormal.y);
            descent = Mathf.Min(descent, displacement.magnitude + maxStepHeight);
            if (wasGrounded) descent = Mathf.Max(descent, Mathf.Max(0f, maxStepDownHeight));
            HitWall = HitCeiling = false;
            Vector2 remaining = displacement;
            for (int i = 0; i < 4 && remaining.sqrMagnitude > 0.0000000001f; i++)
            {
                remaining = Vector2.ClampMagnitude(remaining, RemainingMovementDistance);
                float requested = remaining.magnitude;
                if (requested <= 0.00001f) break;
                Vector2 direction = remaining / requested;
                Collider2D obstacle = Probe(direction, requested + skin, out float distance, out Vector2 normal, ignored);
                float separation = skin / Mathf.Max(0.01f, -Vector2.Dot(direction, normal));
                float allowed = obstacle != null ? Mathf.Min(requested, Mathf.Max(0f, distance - separation)) : requested;
                remaining -= Translate(direction * allowed);
                if (obstacle == null || allowed >= requested) break;

                bool walkable = IsWalkable(normal, maxSlopeAngle);
                if (wasGrounded && !walkable && Mathf.Abs(normal.x) > 0.01f && maxStepHeight > 0f
                    && TryStep(remaining.x, maxStepHeight, maxSlopeAngle, ignored))
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
                Collider2D support = Probe(Vector2.down, descent + skin * 2f, out float distance, out Vector2 normal, ignored);
                if (support != null && normal.y > 0.01f && (maxStepDownHeight <= 0f
                    || (IsWalkable(normal, maxSlopeAngle) && distance - skin <= descent + 0.00001f)))
                    Translate(Vector2.down * Mathf.Max(0f, distance - skin));
            }
            RefreshGround(followGround || displacement.y <= 0f, ignored);
            Physics2D.SyncTransforms();
        }

        private static bool IsWalkable(Vector2 normal, float maxSlopeAngle)
        {
            return normal.y > 0.01f && Vector2.Angle(normal, Vector2.up) <= Mathf.Clamp(maxSlopeAngle, 0f, 89f) + 0.01f;
        }

        public void Move(Vector2 displacement, Collider2D horizontalIgnored = null, float maxStepHeight = 0f,
            Collider2D ignored = null)
        {
            displacement = LimitDisplacement(displacement);
            if (horizontalIgnored == null)
            {
                bool followGround = HasGroundContact && Vector2.Dot(displacement, GroundNormal) <= 0.00001f;
                SweepMove(displacement, maxStepHeight, ignored, followGround);
                return;
            }
            HitWall = MoveAxis(Vector2.right, displacement.x, horizontalIgnored);
            bool verticalHit = MoveAxis(Vector2.up, displacement.y, ignored);
            HitCeiling = verticalHit && displacement.y > 0f;
            RefreshGround(displacement.y <= 0f, ignored);
            Physics2D.SyncTransforms();
        }

        private bool TryStep(float horizontal, float maxHeight, float maxSlopeAngle, Collider2D ignored = null)
        {
            if (!TryGetStep(horizontal, maxHeight, maxSlopeAngle, ignored, out Vector2 step)) return false;
            Translate(step);
            return true;
        }

        private bool TryGetStep(float horizontal, float maxHeight, float maxSlopeAngle, Collider2D ignored, out Vector2 step)
        {
            step = Vector2.zero;
            if (Mathf.Approximately(horizontal, 0f)) return false;
            Probe(Vector2.up, maxHeight + skin, out float ceilingDistance, ignored);
            float lift = Mathf.Min(maxHeight, Mathf.Max(0f, ceilingDistance - skin));
            if (lift <= 0f) return false;

            // Query an up-across-down route before committing any vertical movement.
            Vector2 raisedOffset = Vector2.up * lift;
            Vector2 direction = Vector2.right * Mathf.Sign(horizontal);
            float requested = Mathf.Abs(horizontal);
            Collider2D wall = ProbeFromOffset(raisedOffset, direction, requested + skin, out float wallDistance, ignored);
            if (wall != null && wallDistance - skin < requested) return false;

            Vector2 landingOffset = raisedOffset + Vector2.right * horizontal;
            Collider2D support = ProbeFromOffset(landingOffset, Vector2.down, lift + skin, out float groundDistance, out Vector2 normal, ignored);
            if (support == null || !IsWalkable(normal, maxSlopeAngle)) return false;
            float rise = lift - Mathf.Max(0f, groundDistance - skin);
            if (rise <= 0f || rise > maxHeight) return false;

            step = new Vector2(horizontal, rise);
            if (step.magnitude > RemainingMovementDistance) return false;
            return true;
        }

        public float AllowedHorizontal(float amount, Collider2D ignored = null, float maxStepHeight = 0f)
        {
            Vector2 desired = HorizontalDisplacement(amount);
            float allowed = AllowedDisplacement(desired, ignored).x;
            if (Grounded && maxStepHeight > 0f && Mathf.Abs(allowed) + 0.00001f < Mathf.Abs(desired.x)
                && TryGetStep(amount, maxStepHeight, MaxSlopeAngle, ignored, out Vector2 step)
                && step.magnitude <= MaxSpeed * Time.fixedDeltaTime) return amount;
            return allowed;
        }

        private Vector2 HorizontalDisplacement(float amount)
        {
            if (!HasGroundContact) return Vector2.right * amount;
            Vector2 tangent = new Vector2(GroundNormal.y, -GroundNormal.x);
            return tangent * (amount / Mathf.Max(0.01f, tangent.x));
        }

        public Vector2 AllowedDisplacement(Vector2 displacement, Collider2D ignored = null)
        {
            float requested = displacement.magnitude;
            if (requested <= 0.00001f) return Vector2.zero;
            Vector2 direction = displacement / requested;
            Collider2D obstacle = Probe(direction, requested + skin, out float distance, ignored);
            float allowed = obstacle != null ? Mathf.Min(requested, Mathf.Max(0f, distance - skin)) : requested;
            return direction * allowed;
        }

        public float MoveHorizontal(float amount, Collider2D ignored = null, float maxStepHeight = 0f)
        {
            float startX = transform.position.x;
            Vector2 displacement = HorizontalDisplacement(amount);
            if (Grounded && maxStepHeight > 0f)
                SweepMove(LimitDisplacement(displacement), maxStepHeight, ignored, true, maxStepHeight);
            else Move(displacement, ignored: ignored);
            return transform.position.x - startX;
        }

        private bool MoveAxis(Vector2 axis, float amount, Collider2D ignored = null)
        {
            if (Mathf.Approximately(amount, 0f)) return false;
            Vector2 direction = axis * Mathf.Sign(amount);
            float requested = Mathf.Abs(amount);
            Probe(direction, requested + skin, out float distance, ignored);
            float allowed = Mathf.Min(requested, Mathf.Max(0f, distance - skin));
            Vector2 applied = Translate(direction * allowed);
            return applied.magnitude < requested;
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

        public Collider2D Probe(Vector2 direction, float distance, out float nearest,
            out Vector2 normal, out Vector2 point, Collider2D ignored = null)
        {
            return ProbeFromOffset(Vector2.zero, direction, distance, out nearest, out normal, out point, ignored);
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
            // Starting inside the floor can hide a wall on the same tilemap collider.
            float inset = Mathf.Min(skin, Mathf.Min(size.x, size.y) * 0.25f);
            size -= Vector2.one * (inset * 2f);
            Vector2 right = transform.right;
            Vector2 up = transform.up;
            ContactFilter2D filter = MotionFilter;
            const float minimumApproach = 0.01f;
            float queryDistance = distance + inset * Mathf.Sqrt(2f) / minimumApproach;
            int count = Physics2D.BoxCast(center, size, transform.eulerAngles.z, direction, filter, hits, queryDistance);
            nearest = distance;
            normal = Vector2.zero;
            point = Vector2.zero;
            Collider2D closest = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = hits[i];
                if (!IsMotionObstacle(hit.collider)) continue;
                if (hit.collider == shape || hit.collider == ignored || hit.collider.transform.IsChildOf(transform)) continue;
                if (ignorePlayerColliders && hit.collider.GetComponentInParent<PlayerMovement>() != null) continue;
                // Ignore surfaces parallel to this axis (floor while moving horizontally).
                float approach = -Vector2.Dot(hit.normal, direction);
                if (approach <= minimumApproach) continue;
                // Report the full body's travel distance, not the smaller query box's distance.
                float insetAlongNormal = inset * (Mathf.Abs(Vector2.Dot(hit.normal, right))
                    + Mathf.Abs(Vector2.Dot(hit.normal, up)));
                float bodyDistance = Mathf.Max(0f, hit.distance - insetAlongNormal / approach);
                if (bodyDistance < nearest || (Mathf.Approximately(bodyDistance, nearest) && hit.normal.y > normal.y))
                {
                    nearest = bodyDistance;
                    closest = hit.collider;
                    normal = hit.normal;
                    point = hit.point;
                }
            }
            return closest;
        }
    }
}
