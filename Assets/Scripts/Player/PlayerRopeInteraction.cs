using System;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerRopeInteraction : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private Vector2 handOffset = new Vector2(0f, 0.2f);
        private PlayerInteractionSettings Settings => physicsSettings != null ? physicsSettings.interactions : PhysicsSettings.DefaultInteractions;
        private RopeSimulation rope;
        private float distance;
        private float cooldown;
        public bool Attached => rope != null;
        public float ClimbSpeed { get; private set; }
        public float GrabRadius => Settings.ropeGrabRadius;
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
            ClimbSpeed = 0f;
            Vector2 desired = rope.Chain.Point(distance) - handOffset;
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

        // True means the rope handled this tick, including a possible release velocity.
        public bool Tick(float horizontal, float vertical, bool grab, bool jump,
            Vector2 incomingVelocity, float dt, out Vector2 velocity, out bool released)
        {
            velocity = incomingVelocity;
            ClimbSpeed = 0f;
            released = false;
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (rope != null && (!rope.isActiveAndEnabled || rope.ClimbableEndSegment <= rope.ClimbableFirstSegment
                || distance < rope.ClimbableStart || distance > rope.ClimbableLength))
            {
                if (rope.Chain != null) velocity = rope.Chain.Velocity(distance);
                Detach();
                released = true;
                return true;
            }
            if (rope == null && cooldown <= 0f && !jump && (grab || vertical > 0.1f))
                TryAttach(incomingVelocity);
            if (rope == null) return false;

            velocity = rope.Chain.Velocity(distance);
            if (jump)
            {
                Vector2 tangent = rope.Chain.Tangent(distance, rope.ClimbableStart, rope.ClimbableLength);
                Vector2 swing = new Vector2(-tangent.y, tangent.x);
                float direction = Vector2.Dot(velocity, swing);
                if (Mathf.Abs(direction) < 0.1f)
                    direction = Mathf.Abs(horizontal) > 0.1f ? horizontal * Mathf.Sign(swing.x) : 1f;
                GravitySettings gravity = physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity;
                velocity += swing * (Mathf.Sign(direction) * gravity.ropeJumpSwingBoost) + Vector2.up * gravity.ropeJumpUpSpeed;
                Detach();
                JumpedFromRope?.Invoke();
                released = true;
                return true;
            }

            // Floor-anchored ropes run upward, so their climbing coordinate has the opposite sign.
            float climbDirection = Mathf.Sign(rope.Chain.Tangent(distance, rope.ClimbableStart, rope.ClimbableLength).y);
            float previousDistance = distance;
            distance = Mathf.Clamp(distance + vertical * climbDirection * Settings.ropeClimbSpeed * dt,
                rope.MinimumGripDistance, rope.ClimbableLength);
            ClimbSpeed = dt > 0f ? (distance - previousDistance) / dt : 0f;
            rope.SetDrive(this, distance, horizontal * Settings.ropeSwingAcceleration);
            Vector2 desired = rope.Chain.Point(distance) - handOffset;
            Vector2 start = transform.position;
            body.Move(desired - start);
            velocity = ((Vector2)transform.position - start) / dt;
            if (Vector2.Distance(transform.position, desired) > 0.06f)
            {
                // The player must never be dragged through a solid wall by the rope.
                Detach();
                released = true;
            }
            return true;
        }

        private void TryAttach(Vector2 incomingVelocity)
        {
            RopeSimulation nearest = null;
            float best = Settings.ropeGrabRadius;
            float grip = 0f;
            for (int ropeIndex = 0; ropeIndex < RopeSimulation.ActiveRopes.Count; ropeIndex++)
            {
                RopeSimulation candidate = RopeSimulation.ActiveRopes[ropeIndex];
                if (candidate == null || !candidate.Available) continue;
                float along = candidate.Chain.ClosestDistance((Vector2)transform.position + handOffset, out float separation,
                    candidate.ClimbableFirstSegment, candidate.ClimbableEndSegment);
                if (separation > best) continue;
                best = separation;
                nearest = candidate;
                grip = Mathf.Clamp(along, candidate.MinimumGripDistance, candidate.ClimbableLength);
            }
            if (nearest != null && nearest.Attach(this, grip, incomingVelocity))
            {
                rope = nearest;
                distance = grip;
            }
        }

        public void Detach()
        {
            ClimbSpeed = 0f;
            if (rope != null) rope.Release(this);
            rope = null;
            cooldown = Settings.ropeRegrabDelay;
        }
        private void OnDisable() => Detach();
    }
}
