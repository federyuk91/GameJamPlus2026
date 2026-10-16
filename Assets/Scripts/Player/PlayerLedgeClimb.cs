using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerLedgeClimb : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PhysicsSettings physicsSettings;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private Collider2D support;
        private Vector3 localDestination;
        private bool raised;
        private float cooldown;
        private float elapsed;
        public bool Active { get; private set; }
        private LedgeClimbSettings Settings => physicsSettings != null ? physicsSettings.ledgeClimb : PhysicsSettings.DefaultLedgeClimb;

        public bool TryBegin(float horizontal)
        {
            if (!isActiveAndEnabled || !Settings.enabled || Active || cooldown > 0f || Mathf.Abs(horizontal) < 0.1f) return false;
            if (body == null) body = GetComponent<SimulatedBody2D>();
            float direction = Mathf.Sign(horizontal);
            Collider2D wall = body.Probe(Vector2.right * direction, Settings.forwardReach + body.Skin, out _);
            if (wall == null) return false;
            Bounds bounds = body.Shape.bounds;
            float probeX = bounds.center.x + direction * (bounds.extents.x + Settings.forwardReach);
            Vector2 origin = new Vector2(probeX, bounds.min.y + Settings.maxHeight + body.Skin * 2f);
            Physics2D.Raycast(origin, Vector2.down, body.SolidFilter, hits, Settings.maxHeight + body.Skin * 4f);
            RaycastHit2D top = default;
            float nearest = float.PositiveInfinity;
            foreach (var hit in hits)
            {
                if (hit.collider == body.Shape || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance < nearest) { nearest = hit.distance; top = hit; }
            }
            if (top.collider != wall || top.normal.y < 0.9f) return false;
            float rise = top.point.y + body.Skin - bounds.min.y;
            if (rise <= body.Skin || rise > Settings.maxHeight) return false;

            Vector2 shift = new Vector2(probeX - bounds.center.x, rise);
            body.Probe(Vector2.up, rise + body.Skin, out float ceilingDistance);
            if (ceilingDistance - body.Skin < rise - 0.0001f) return false;
            // Verify the raised horizontal corridor and final full-body clearance.
            Physics2D.BoxCast((Vector2)bounds.center + Vector2.up * rise, bounds.size, 0f,
                Vector2.right * direction, body.SolidFilter, hits, Mathf.Abs(shift.x) + body.Skin);
            foreach (var hit in hits)
            {
                if (hit.collider == body.Shape || hit.collider.transform.IsChildOf(transform)) continue;
                if (Vector2.Dot(hit.normal, Vector2.right * direction) < -0.01f
                    && hit.distance < Mathf.Abs(shift.x) + body.Skin) return false;
            }
            Physics2D.OverlapBox((Vector2)bounds.center + shift, bounds.size, 0f, body.SolidFilter, overlaps);
            foreach (var collider in overlaps)
                if (collider != body.Shape && !collider.transform.IsChildOf(transform)) return false;

            support = top.collider;
            localDestination = support.transform.InverseTransformPoint(transform.position + (Vector3)shift);
            raised = false;
            elapsed = 0f;
            Active = true;
            return true;
        }

        // Follow a swept up-then-across route; never teleport through the corner.
        public bool Tick(float dt, out Vector2 velocity)
        {
            velocity = Vector2.zero;
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (!Active) return false;
            if (!isActiveAndEnabled || !Settings.enabled || support == null || !support.enabled
                || !support.gameObject.activeInHierarchy || (elapsed += dt) > 2f)
            { Stop(); return true; }
            Vector2 target = support.transform.TransformPoint(localDestination);
            Vector2 start = transform.position;
            Vector2 delta = target - start;
            float step = Mathf.Max(0.01f, Settings.climbSpeed) * dt;
            if (delta.y > 0.001f) raised = false;
            if (!raised)
            {
                body.Move(Vector2.up * Mathf.Clamp(delta.y, -step, step));
                if (body.HitCeiling) { Stop(); return true; }
                raised = Mathf.Abs(transform.position.y - target.y) <= 0.001f;
            }
            else
            {
                body.Move(Vector2.right * Mathf.Clamp(delta.x, -step, step));
                if (body.HitWall) { Stop(); return true; }
                if (Mathf.Abs(transform.position.x - target.x) <= 0.001f)
                {
                    body.Move(Vector2.up * Mathf.Clamp(target.y - transform.position.y, -step, step));
                    if (Mathf.Abs(transform.position.y - target.y) <= 0.001f)
                    {
                        body.Move(Vector2.down * (body.Skin * 2f));
                        Stop();
                        return true;
                    }
                }
            }
            velocity = ((Vector2)transform.position - start) / dt;
            return true;
        }

        private void Stop() { Active = false; support = null; cooldown = Settings.retryDelay; }
        private void OnDisable() => Stop();
    }
}
