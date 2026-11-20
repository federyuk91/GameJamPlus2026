using System;
using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PlayerMovement))]
    public sealed class PlayerRopeInteraction : MonoBehaviour, IRopeGripConstraint
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
        private Vector2 previousGrip;
        private Vector2 carrierVelocity;
        private readonly List<RaycastHit2D> contactHits = new List<RaycastHit2D>(8);
        public bool Attached => rope != null;
        public float WeightKg => body != null && body.isActiveAndEnabled ? body.Weight : 0f;
        public float ClimbSpeed { get; private set; }
        public float GrabRadius => Mathf.Max(0f, LocalMovement.handGrabReach);
        public event Action JumpedFromRope;
        public event Action<Vector2> JumpLaunched;

        public Vector2 LimitGrip(Vector2 proposed)
        {
            Vector2 hand = (Vector2)transform.position + HandOffset;
            return hand + body.AllowedDisplacement(proposed - hand);
        }

        public Vector2 LimitSegmentPoint(Vector2 start, Vector2 proposed)
        {
            Vector2 displacement = proposed - start;
            float distance = displacement.magnitude;
            if (distance <= 0.00001f) return proposed;
            const float radius = 0.01f;
            Vector2 direction = displacement / distance;
            Physics2D.CircleCast(start, radius, direction, body.SolidFilter, contactHits, distance + radius);
            float allowed = distance;
            foreach (RaycastHit2D hit in contactHits)
            {
                Collider2D obstacle = hit.collider;
                if (hit.distance <= 0f || Vector2.Dot(hit.normal, direction) >= -0.01f
                    || obstacle.transform.IsChildOf(transform)
                    || (rope != null && rope.BalloonAnchor != null && obstacle.transform.IsChildOf(rope.BalloonAnchor.transform))
                    || (rope != null && rope.Load != null && obstacle.transform.IsChildOf(rope.Load.transform))) continue;
                if (obstacle is BoxCollider2D platform && platform.TryGetComponent(out OneWayPlatform2D oneWay)
                    && oneWay.isActiveAndEnabled && !oneWay.Blocks(platform, start, Vector2.one * radius,
                        Vector2.right, Vector2.up, direction, hit.normal, body.Skin)) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - radius));
            }
            return start + direction * allowed;
        }

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
                || distance < rope.ClimbableStart || distance > rope.ClimbableLength || !CanHoldRope(rope, distance)))
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
                    // A neutral release follows the current motion, without adding a launch impulse.
                    velocity *= Mathf.Clamp01(Settings.ropeReleaseMomentumMultiplier);
                }
                velocity = body.LimitVelocity(velocity);
                Detach();
                JumpLaunched?.Invoke(velocity);
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
            Vector2 desired = rope.Chain.Point(distance) - HandOffset;
            Vector2 alignment = desired - (Vector2)transform.position;
            bool obstructed = Vector2.Distance(body.AllowedDisplacement(alignment), alignment) > body.Skin * 2f;
            if (obstructed && distance != previousDistance)
            {
                distance = previousDistance;
                ClimbSpeed = 0f;
                desired = rope.Chain.Point(distance) - HandOffset;
                alignment = desired - (Vector2)transform.position;
            }
            Vector2 start = transform.position;
            // Il movimento rispetta i solidi, il contatto non interrompe la presa.
            body.Move(alignment);
            previousGrip = rope.Chain.Point(distance);
            velocity = body.LimitVelocity(((Vector2)transform.position - start) / dt);
            if (obstructed) carrierVelocity = velocity;
            bool stillAligned = Vector2.Distance(transform.position, desired) <= 0.06f;
            rope.SetDrive(this, distance, stillAligned ? horizontal * Settings.ropeSwingAcceleration : 0f);
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
                if (!CanHoldRope(candidate, along)) continue;
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
            if (target == null || !CanHoldRope(target, grip)
                || !target.Attach(this, grip, body.LimitVelocity(incomingVelocity))) return false;
            rope = target;
            distance = grip;
            cooldown = 0f;
            previousGrip = target.Chain.Point(grip);
            carrierVelocity = Vector2.zero;
            return true;
        }

        private bool CanHoldRope(RopeSimulation target, float grip)
        {
            if (target.Chain == null) return false;
            if (!target.IsBottomAnchored) return true;
            return !WaterVolume.ContainsPoint(target.Chain.Point(grip))
                && WaterVolume.SubmergedFraction(body.Shape.bounds) <= 0f;
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
