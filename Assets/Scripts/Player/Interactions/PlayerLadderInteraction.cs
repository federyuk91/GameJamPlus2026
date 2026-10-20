using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PlayerMovement))]
    public sealed class PlayerLadderInteraction : MonoBehaviour
    {
        private const float TopStopOffset = 0.6f;
        [SerializeField] private SimulatedBody2D body;
        private PhysicsSettings WorldSettings => (body != null ? body : body = GetComponent<SimulatedBody2D>()).Settings;
        [SerializeField] private PlayerMovement playerMovement;
        private PlayerMovementSettings LocalMovement => (playerMovement != null
            ? playerMovement : playerMovement = GetComponent<PlayerMovement>()).MovementSettings;
        private Vector2 HandOffset => LocalMovement.handOffset;
        public float GrabReach => Mathf.Max(0f, LocalMovement.handGrabReach);
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private Ladder ladder;
        private float height;
        private Vector2 previousGrip;
        private float cooldown;
        private float facing = 1f;
        public bool Attached => ladder != null;
        public float ClimbSpeed { get; private set; }
        private LadderSettings Settings => WorldSettings != null ? WorldSettings.ladder : PhysicsSettings.DefaultLadder;

        public bool Tick(float horizontal, float vertical, bool jump, Vector2 incomingVelocity, float dt,
            out Vector2 velocity, out bool released)
        {
            ClimbSpeed = 0f;
            if (body == null) body = GetComponent<SimulatedBody2D>();
            velocity = incomingVelocity;
            released = false;
            if (!isActiveAndEnabled || dt <= 0f) return false;
            if (Mathf.Abs(horizontal) > 0.1f) facing = Mathf.Sign(horizontal);
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (ladder != null && !ladder.Available)
            {
                Detach();
                released = true;
                return true;
            }
            bool wasAttached = ladder != null;
            if (ladder == null && cooldown <= 0f && Mathf.Abs(vertical) > 0.1f) TryAttach();
            if (ladder == null) return false;
            jump &= wasAttached;

            Vector2 grip = ladder.Point(height);
            Vector2 carrierVelocity = body.LimitVelocity((grip - previousGrip) / dt);
            carrierVelocity = body.AllowedDisplacement(carrierVelocity * dt) / dt;
            if (jump)
            {
                // Carry the player up to this tick's ladder position before releasing.
                body.Move(grip - HandOffset - (Vector2)transform.position);
                velocity = body.LimitVelocity(Settings.JumpVelocity(carrierVelocity, horizontal, facing));
                Detach();
                released = true;
                return true;
            }

            float bottom = ladder.Bottom;
            float top = GetTopHeight();
            float previousHeight = height;
            if (Vector2.Distance(transform.position, grip - HandOffset) <= 0.06f)
                height = Settings.Climb(height, vertical, ladder.AxisScale, dt, bottom, top, LocalMovement.ClimbSpeed);
            ClimbSpeed = (height - previousHeight) * ladder.AxisScale / dt;
            Vector2 desired = ladder.Point(height) - HandOffset;
            Vector2 alignment = desired - (Vector2)transform.position;
            bool obstructed = Vector2.Distance(body.AllowedDisplacement(alignment), alignment) > body.Skin * 2f;
            body.Move(alignment);
            // Snapping to the ladder axis must not become inherited sideways speed.
            velocity = body.LimitVelocity(carrierVelocity + (ladder.Point(height) - grip) / dt);
            previousGrip = ladder.Point(height);
            if ((obstructed && Vector2.Distance(transform.position, desired) > 0.06f)
                || (vertical < -0.1f && height <= bottom))
            {
                velocity = carrierVelocity;
                Detach();
                released = true;
            }
            return true;
        }

        private void TryAttach()
        {
            Physics2D.SyncTransforms();
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            Physics2D.OverlapBox(body.Shape.bounds.center, body.Shape.bounds.size, 0f, filter, overlaps);
            float nearest = GrabReach;
            Ladder selected = null;
            Vector2 hand = (Vector2)transform.position + HandOffset;
            foreach (Collider2D hit in overlaps)
            {
                Ladder candidate = hit.GetComponent<Ladder>();
                if (candidate == null || !candidate.Available || candidate.AxisScale < 0.0001f) continue;
                float distance = Vector2.Distance(candidate.Point(candidate.ClosestHeight(hand)), hand);
                if (distance > nearest) continue;
                float candidateHeight = Mathf.Clamp(candidate.transform.InverseTransformPoint(hand).y,
                    candidate.Bottom, GetTopHeight(candidate));
                Vector2 alignment = candidate.Point(candidateHeight) - hand;
                if (alignment.magnitude > GrabReach
                    || Vector2.Distance(body.AllowedDisplacement(alignment), alignment) > body.Skin * 2f) continue;
                nearest = distance;
                selected = candidate;
            }
            if (selected == null) return;
            ladder = selected;
            float top = GetTopHeight();
            height = Mathf.Clamp(ladder.transform.InverseTransformPoint(hand).y, ladder.Bottom, top);
            previousGrip = ladder.Point(height);
        }

        private float GetTopHeight() => GetTopHeight(ladder);

        private float GetTopHeight(Ladder candidate)
        {
            // Convert the world-space stop offset to ladder-local height, including scaled ladders.
            float offset = (body.Shape.bounds.extents.y + HandOffset.y + body.Skin - TopStopOffset) / candidate.AxisScale;
            return Mathf.Max(candidate.Bottom, candidate.Top + offset);
        }

        public void Detach() { ladder = null; ClimbSpeed = 0f; cooldown = Settings.regrabDelay; }
        private void OnDisable() => Detach();
    }
}
