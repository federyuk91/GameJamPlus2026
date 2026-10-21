using System;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PlayerMovement))]
    public sealed class PlayerRopeInteraction : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField, Tooltip("Optional rope to grab automatically at scene start. Leave empty for normal manual grabbing.")]
        private RopeSimulation initialRope;
        [SerializeField, Range(0f, 1f), Tooltip("Initial grip along the assigned rope: 0 near its anchor, 1 at its end.")]
        private float initialGripFraction = 1f;
        private PhysicsSettings WorldSettings => (body != null ? body : body = GetComponent<SimulatedBody2D>()).Settings;
        [SerializeField] private PlayerMovement playerMovement;
        private PlayerMovementSettings LocalMovement => (playerMovement != null
            ? playerMovement : playerMovement = GetComponent<PlayerMovement>()).MovementSettings;
        private Vector2 HandOffset => LocalMovement.handOffset;
        private PlayerInteractionSettings Settings => WorldSettings != null ? WorldSettings.interactions : PhysicsSettings.DefaultInteractions;
        private RopeSimulation rope;
        private float distance;
        private float cooldown;
        private float lastSwingDirection;
        private Vector2 previousGrip;
        private Vector2 carrierVelocity;
        public bool Attached => rope != null;
        public float WeightKg => body != null && body.isActiveAndEnabled ? body.Weight : 0f;
        public float ClimbSpeed { get; private set; }
        public float GrabRadius => Mathf.Max(0f, LocalMovement.handGrabReach);
        public event Action JumpedFromRope;

        public bool TryGetGrip(out RopeSimulation heldRope, out int segment, out Vector2 point)
        {
            heldRope = null;
            segment = -1;
            point = Vector2.zero;
            if (rope == null || !rope.isActiveAndEnabled || rope.Chain == null
                || rope.ClimbableEndSegment <= rope.ClimbableFirstSegment
                || distance < rope.ClimbableStart || distance > rope.ClimbableLength) return false;
            heldRope = rope;
            // At a node, select the segment on the anchor side of the grip.
            segment = Mathf.Clamp(Mathf.CeilToInt(distance / rope.Chain.SegmentLength) - 1,
                rope.ClimbableFirstSegment, rope.ClimbableEndSegment - 1);
            point = rope.Chain.Point(distance);
            return true;
        }

        internal void RetainGripAfterCut(RopeSimulation cutRope)
        {
            if (rope != cutRope) return;
            if (rope.ClimbableEndSegment <= rope.ClimbableFirstSegment)
            {
                Detach();
                return;
            }
            float retainedDistance = Mathf.Clamp(distance, rope.ClimbableStart, rope.ClimbableLength);
            if (Mathf.Approximately(distance, retainedDistance)) return;

            distance = retainedDistance;
            previousGrip = rope.Chain.Point(distance);
            carrierVelocity = Vector2.zero;
            ClimbSpeed = 0f;
            Vector2 desired = rope.Chain.Point(distance) - HandOffset;
            body.Move(desired - (Vector2)transform.position);
            // Regripping must still respect solid walls and ceilings.
            if (Vector2.Distance(transform.position, desired) > 0.06f)
            {
                Detach();
                return;
            }
            rope.SetDrive(this, distance, 0f);
        }

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
        }

        private void Start()
        {
            // Rope chains are initialized in Awake, before any initial player attachment.
            if (initialRope == null) return;
            float grip = Mathf.Clamp(Mathf.Lerp(initialRope.ClimbableStart, initialRope.ClimbableLength, initialGripFraction),
                initialRope.MinimumGripDistance, initialRope.ClimbableLength);
            if (!AttachTo(initialRope, grip, Vector2.zero))
                Debug.LogWarning("The assigned Initial Rope is unavailable for the initial grab.", this);
        }

        // True means the rope handled this tick, including a possible release velocity.
        public bool Tick(float horizontal, float vertical, bool grab, bool jump,
            Vector2 incomingVelocity, float dt, out Vector2 velocity, out bool released)
        {
            velocity = incomingVelocity;
            ClimbSpeed = 0f;
            released = false;
            if (!isActiveAndEnabled || dt <= 0f) return false;
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (rope != null && (!rope.isActiveAndEnabled || rope.ClimbableEndSegment <= rope.ClimbableFirstSegment
                || distance < rope.ClimbableStart || distance > rope.ClimbableLength))
            {
                velocity = body.LimitVelocity(carrierVelocity);
                Detach();
                released = true;
                return true;
            }
            bool wasAttached = rope != null;
            if (rope == null && cooldown <= 0f && (grab || vertical > 0.1f))
                TryAttach(incomingVelocity);
            if (rope == null) return false;
            // A Jump press used while grabbing must not also release the new grip.
            jump &= wasAttached;

            // Compare the same point before climbing; grip corrections are not launch momentum.
            carrierVelocity = body.LimitVelocity((rope.Chain.Point(distance) - previousGrip) / dt);
            carrierVelocity = body.AllowedDisplacement(carrierVelocity * dt) / dt;
            velocity = carrierVelocity;
            Vector2 tangent = rope.Chain.Tangent(distance, rope.ClimbableStart, rope.ClimbableLength);
            Vector2 swing = new Vector2(-tangent.y, tangent.x);
            float swingSpeed = Vector2.Dot(velocity, swing);
            if (Mathf.Abs(swingSpeed) > 0.01f) lastSwingDirection = Mathf.Sign(swingSpeed);
            if (jump)
            {
                if (Mathf.Abs(horizontal) > 0.1f)
                {
                    // Redirect inherited momentum too, so an opposite swing cannot override the chosen side.
                    velocity.x = Mathf.Sign(horizontal) * (Mathf.Abs(velocity.x) + Settings.ropeJumpSwingBoost);
                    velocity.y += Settings.ropeJumpUpSpeed;
                }
                else
                {
                    velocity += swing * (lastSwingDirection * Settings.ropeJumpSwingBoost) + Vector2.up * Settings.ropeJumpUpSpeed;
                }
                velocity = body.LimitVelocity(velocity);
                Detach();
                JumpedFromRope?.Invoke();
                released = true;
                return true;
            }

            // Floor-anchored ropes run upward, so their climbing coordinate has the opposite sign.
            float climbDirection = Mathf.Sign(rope.Chain.Tangent(distance, rope.ClimbableStart, rope.ClimbableLength).y);
            float previousDistance = distance;
            bool aligned = Vector2.Distance(transform.position, rope.Chain.Point(distance) - HandOffset) <= 0.06f;
            if (aligned)
                distance = Mathf.Clamp(distance + vertical * climbDirection * Settings.ropeClimbSpeed * dt,
                    rope.MinimumGripDistance, rope.ClimbableLength);
            ClimbSpeed = dt > 0f ? (distance - previousDistance) / dt : 0f;
            rope.SetDrive(this, distance, aligned ? horizontal * Settings.ropeSwingAcceleration : 0f);
            Vector2 desired = rope.Chain.Point(distance) - HandOffset;
            Vector2 alignment = desired - (Vector2)transform.position;
            bool obstructed = Vector2.Distance(body.AllowedDisplacement(alignment), alignment) > body.Skin * 2f;
            body.Move(alignment);
            previousGrip = rope.Chain.Point(distance);
            velocity = body.LimitVelocity(carrierVelocity + tangent * ClimbSpeed);
            if (obstructed && Vector2.Distance(transform.position, desired) > 0.06f)
            {
                // The player must never be dragged through a solid wall by the rope.
                velocity = carrierVelocity;
                Detach();
                released = true;
            }
            return true;
        }

        private void TryAttach(Vector2 incomingVelocity)
        {
            RopeSimulation nearest = null;
            float best = GrabRadius;
            float grip = 0f;
            for (int ropeIndex = 0; ropeIndex < RopeSimulation.ActiveRopes.Count; ropeIndex++)
            {
                RopeSimulation candidate = RopeSimulation.ActiveRopes[ropeIndex];
                if (candidate == null || !candidate.Available) continue;
                Vector2 hand = (Vector2)transform.position + HandOffset;
                float along = candidate.Chain.ClosestDistance(hand, out _,
                    candidate.ClimbableFirstSegment, candidate.ClimbableEndSegment);
                along = Mathf.Clamp(along, candidate.MinimumGripDistance, candidate.ClimbableLength);
                float separation = Vector2.Distance(hand, candidate.Chain.Point(along));
                if (separation > best) continue;
                Vector2 alignment = candidate.Chain.Point(along) - hand;
                if (Vector2.Distance(body.AllowedDisplacement(alignment), alignment) > body.Skin * 2f) continue;
                best = separation;
                nearest = candidate;
                grip = along;
            }
            if (nearest != null) AttachTo(nearest, grip, incomingVelocity);
        }

        private bool AttachTo(RopeSimulation target, float grip, Vector2 incomingVelocity)
        {
            if (target == null || !target.Attach(this, grip, body.LimitVelocity(incomingVelocity))) return false;
            rope = target;
            distance = grip;
            cooldown = 0f;
            lastSwingDirection = 0f;
            previousGrip = target.Chain.Point(grip);
            carrierVelocity = Vector2.zero;
            return true;
        }

        public void Detach()
        {
            ClimbSpeed = 0f;
            lastSwingDirection = 0f;
            if (rope != null) rope.Release(this);
            rope = null;
            cooldown = Settings.ropeRegrabDelay;
        }
        private void OnDisable() => Detach();
    }
}
