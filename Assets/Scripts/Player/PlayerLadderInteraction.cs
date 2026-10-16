using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerLadderInteraction : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private Vector2 handOffset = new Vector2(0f, 0.2f);
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private Ladder ladder;
        private float height;
        private Vector2 previousGrip;
        private float cooldown;
        private float facing = 1f;
        public bool Attached => ladder != null;
        private LadderSettings Settings => physicsSettings != null ? physicsSettings.ladder : PhysicsSettings.DefaultLadder;

        public bool Tick(float horizontal, float vertical, bool jump, Vector2 incomingVelocity, float dt,
            out Vector2 velocity, out bool released)
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            velocity = incomingVelocity;
            released = false;
            if (!isActiveAndEnabled) return false;
            if (Mathf.Abs(horizontal) > 0.1f) facing = Mathf.Sign(horizontal);
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (ladder != null && !ladder.Available)
            {
                Detach();
                released = true;
                return true;
            }
            if (ladder == null && cooldown <= 0f && !jump && Mathf.Abs(vertical) > 0.1f) TryAttach();
            if (ladder == null) return false;

            Vector2 grip = ladder.Point(height);
            Vector2 carrierVelocity = (grip - previousGrip) / dt;
            if (jump)
            {
                // Carry the player up to this tick's ladder position before releasing.
                body.Move(grip - handOffset - (Vector2)transform.position);
                velocity = Settings.JumpVelocity(carrierVelocity, horizontal, facing);
                Detach();
                released = true;
                return true;
            }

            float bottom = ladder.Bottom;
            // Allow the feet to clear the top before returning to normal movement.
            float top = ladder.Top + (body.Shape.bounds.extents.y + handOffset.y + body.Skin) / ladder.AxisScale;
            height = Settings.Climb(height, vertical, ladder.AxisScale, dt, bottom, top);
            Vector2 desired = ladder.Point(height) - handOffset;
            Vector2 start = transform.position;
            body.Move(desired - start);
            velocity = ((Vector2)transform.position - start) / dt;
            previousGrip = ladder.Point(height);
            if (Vector2.Distance(transform.position, desired) > 0.06f
                || (vertical > 0.1f && height >= top) || (vertical < -0.1f && height <= bottom))
            {
                Detach();
                released = true;
            }
            return true;
        }

        private void TryAttach()
        {
            Physics2D.SyncTransforms();
            var filter = new ContactFilter2D { useTriggers = true };
            Physics2D.OverlapBox(body.Shape.bounds.center, body.Shape.bounds.size, 0f, filter, overlaps);
            float nearest = Settings.grabReach;
            Ladder selected = null;
            Vector2 hand = (Vector2)transform.position + handOffset;
            foreach (var hit in overlaps)
            {
                var candidate = hit.GetComponent<Ladder>();
                if (candidate == null || !candidate.Available || candidate.AxisScale < 0.0001f) continue;
                float distance = Vector2.Distance(candidate.Point(candidate.ClosestHeight(hand)), hand);
                if (distance > nearest) continue;
                nearest = distance;
                selected = candidate;
            }
            if (selected == null) return;
            ladder = selected;
            float top = ladder.Top + (body.Shape.bounds.extents.y + handOffset.y + body.Skin) / ladder.AxisScale;
            height = Mathf.Clamp(ladder.transform.InverseTransformPoint(hand).y, ladder.Bottom, top);
            previousGrip = ladder.Point(height);
        }

        private void Detach() { ladder = null; cooldown = Settings.regrabDelay; }
        private void OnDisable() => Detach();
    }
}
