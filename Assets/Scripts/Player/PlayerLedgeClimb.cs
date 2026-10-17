using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerLedgeClimb : MonoBehaviour
    {
        public enum LedgeState { None, Hanging, Climbing }
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField, Tooltip("Diagnostica dell'ultimo tentativo di aggancio o salita, aggiornata in Play Mode.")]
        private string lastAttempt;
        [SerializeField] private Collider2D blockingCollider;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private Collider2D support;
        private Vector3 localDestination;
        private Vector3 localHangPosition;
        private Vector2 lastHangTarget;
        private bool raised;
        private float cooldown;
        private float elapsed;
        public LedgeState State { get; private set; }
        public bool Active => State != LedgeState.None;
        public float Direction { get; private set; }
        private LedgeClimbSettings Settings => physicsSettings != null ? physicsSettings.ledgeClimb : PhysicsSettings.DefaultLedgeClimb;
        private PlayerMovementSettings Movement => physicsSettings != null ? physicsSettings.player : PhysicsSettings.DefaultPlayer;
        private GravitySettings Gravity => physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity;

        public bool TryBegin(float horizontal, bool fromGround = true)
        {
            if (!isActiveAndEnabled || !Settings.enabled || Active || cooldown > 0f || Mathf.Abs(horizontal) < 0.1f) return false;
            if (body == null) body = GetComponent<SimulatedBody2D>();
            Physics2D.SyncTransforms();
            blockingCollider = null;
            float direction = Mathf.Sign(horizontal);
            Bounds bounds = body.Shape.bounds;
            Collider2D wall;
            if (fromGround) wall = FindRiser(bounds, direction);
            else
            {
                wall = body.Probe(Vector2.right * direction, Settings.forwardReach + body.Skin,
                    out _, out Vector2 wallNormal);
                if (Mathf.Abs(wallNormal.x) < 0.1f) return Reject("Nessun fianco raggiungibile", wall);
            }
            if (wall == null) return Reject("Nessun fianco raggiungibile");
            float probeX = bounds.center.x + direction * (bounds.extents.x + Settings.forwardReach);
            Vector2 origin = new Vector2(probeX, bounds.min.y + Settings.maxHeight + body.Skin * 2f);
            Physics2D.Raycast(origin, Vector2.down, body.SolidFilter, hits, Settings.maxHeight + body.Skin * 4f);
            RaycastHit2D top = default;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == body.Shape || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance <= 0f) continue;
                if (hit.distance < nearest) { nearest = hit.distance; top = hit; }
            }
            if (top.collider != wall || top.normal.y < 0.9f
                || Vector2.Angle(top.normal, Vector2.up) > Mathf.Clamp(Movement.maxSlopeAngle, 0f, 89f) + 0.01f)
                return Reject("Nessun piano di appoggio valido sopra il fianco", top.collider);
            float surfaceRise = top.point.y - bounds.min.y;
            // Keep the landing box outside the contact tolerance, then settle onto the support.
            float landingGap = Mathf.Max(body.Skin, Physics2D.defaultContactOffset * 2f);
            float rise = surfaceRise + landingGap;
            if (surfaceRise <= 0f || surfaceRise > Settings.maxHeight) return Reject("Altezza fuori dal limite", wall);
            if (fromGround && surfaceRise + body.Skin <= Movement.maxStepHeight) return Reject("Altezza gestita dal gradino standard", wall);
            float hangShift = rise - bounds.size.y * Settings.hangHeightFraction;
            if (!fromGround && Mathf.Abs(hangShift) > Settings.grabVerticalReach) return Reject("Bordo fuori dalla portata verticale delle zampe", wall);

            Vector2 shift = new Vector2(probeX - bounds.center.x, rise);
            // Inset clearance queries avoid counting the adjacent floor or wall as initial overlaps.
            Vector2 clearanceSize = (Vector2)bounds.size - Vector2.one * (body.Skin * 2f);
            clearanceSize.x = Mathf.Max(0.001f, clearanceSize.x);
            clearanceSize.y = Mathf.Max(0.001f, clearanceSize.y);
            if (!HasClearPath(bounds.center, clearanceSize, Vector2.up * rise)) return false;
            Vector2 raisedCenter = (Vector2)bounds.center + Vector2.up * rise;
            if (!HasClearPath(raisedCenter, clearanceSize, Vector2.right * shift.x)) return false;
            overlaps.Clear();
            Physics2D.OverlapBox((Vector2)bounds.center + shift, clearanceSize, 0f, body.SolidFilter, overlaps);
            foreach (Collider2D collider in overlaps)
                if (collider != body.Shape && !collider.transform.IsChildOf(transform))
                    return Reject($"Spazio di arrivo occupato: piedi {bounds.min.y + rise:F4}, "
                        + $"sommità collider {collider.bounds.max.y:F4}", collider);

            Vector2 originalPosition = transform.position;
            if (!fromGround)
            {
                Vector2 adjustment = Vector2.up * hangShift;
                if ((body.AllowedDisplacement(adjustment) - adjustment).sqrMagnitude > 0.000001f)
                    return Reject("Allineamento al bordo ostruito", wall);
                body.Move(adjustment);
            }
            support = top.collider;
            localDestination = support.transform.InverseTransformPoint(originalPosition + shift);
            localHangPosition = support.transform.InverseTransformPoint(transform.position);
            lastHangTarget = transform.position;
            Direction = direction;
            raised = false;
            elapsed = 0f;
            State = fromGround ? LedgeState.Climbing : LedgeState.Hanging;
            lastAttempt = fromGround ? "Salita avviata" : "Aggancio riuscito";
            return true;
        }

        private bool Reject(string reason, Collider2D blocker = null)
        {
            lastAttempt = reason;
            blockingCollider = blocker;
            return false;
        }

        private bool HasClearPath(Vector2 center, Vector2 size, Vector2 displacement)
        {
            float distance = displacement.magnitude;
            if (distance <= 0.00001f) return true;
            Vector2 direction = displacement / distance;
            Physics2D.BoxCast(center, size, 0f, direction, body.SolidFilter, hits, distance);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == body.Shape || hit.collider.transform.IsChildOf(transform)) continue;
                if (Vector2.Dot(hit.normal, direction) >= -0.01f) continue;
                if (hit.distance < distance) return Reject("Percorso di salita ostruito", hit.collider);
            }
            return true;
        }

        private Collider2D FindRiser(Bounds bounds, float direction)
        {
            // Probe inside the riser height: a full-body cast can report a corner normal on low steps.
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + body.Skin * 2f);
            Physics2D.Raycast(origin, Vector2.right * direction, body.SolidFilter, hits,
                bounds.extents.x + Settings.forwardReach + body.Skin);
            Collider2D nearest = null;
            float distance = float.PositiveInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == body.Shape || hit.collider.transform.IsChildOf(transform) || hit.distance <= 0f) continue;
                if (Mathf.Abs(hit.normal.y) > 0.01f || hit.normal.x * direction > -0.9f) continue;
                if (hit.distance >= distance) continue;
                nearest = hit.collider;
                distance = hit.distance;
            }
            return nearest;
        }

        // Follow a swept up-then-across route; never teleport through the corner.
        public bool Tick(float dt, out Vector2 velocity)
        {
            return Tick(dt, 0f, 0f, false, out velocity, out _);
        }

        public bool Tick(float dt, float horizontal, float vertical, bool jumpPressed,
            out Vector2 velocity, out bool released)
        {
            velocity = Vector2.zero;
            released = false;
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (!Active) return false;
            if (!isActiveAndEnabled || !Settings.enabled || support == null || !support.enabled
                || !support.gameObject.activeInHierarchy)
            { Stop(); released = true; return true; }
            if (State == LedgeState.Hanging)
            {
                Vector2 hangTarget = support.transform.TransformPoint(localHangPosition);
                Vector2 carry = hangTarget - (Vector2)transform.position;
                Vector2 carryVelocity = dt > 0f ? (hangTarget - lastHangTarget) / dt : Vector2.zero;
                lastHangTarget = hangTarget;
                body.Move(carry, ignored: support);
                if (Vector2.Distance(transform.position, hangTarget) > 0.01f)
                { Stop(); velocity = carryVelocity; released = true; return true; }
                if (jumpPressed)
                {
                    velocity = carryVelocity + new Vector2(-Direction * Movement.speed,
                        JumpPhysics.LaunchSpeed(Gravity.jumpHeight, Gravity.playerGravity));
                    Stop();
                    released = true;
                    return true;
                }
                if (vertical < -0.1f)
                {
                    velocity = new Vector2(0f, Mathf.Min(0f, carryVelocity.y));
                    Stop();
                    released = true;
                    return true;
                }
                velocity = carryVelocity;
                if (horizontal * Direction < 0.1f) return true;
                State = LedgeState.Climbing;
                elapsed = 0f;
            }
            if ((elapsed += dt) > 2f)
            { Stop(); released = true; return true; }
            Vector2 target = support.transform.TransformPoint(localDestination);
            Vector2 start = transform.position;
            Vector2 delta = target - start;
            float step = Mathf.Max(0.01f, Settings.climbSpeed) * dt;
            if (delta.y > 0.001f) raised = false;
            if (!raised)
            {
                body.Move(Vector2.up * Mathf.Clamp(delta.y, -step, step));
                if (body.HitCeiling) { Reject("Salita interrotta: collisione verso l’alto"); Stop(); released = true; return true; }
                raised = Mathf.Abs(transform.position.y - target.y) <= 0.001f;
            }
            else
            {
                body.Move(Vector2.right * Mathf.Clamp(delta.x, -step, step));
                if (body.HitWall) { Reject("Salita interrotta: collisione laterale"); Stop(); released = true; return true; }
                if (Mathf.Abs(transform.position.x - target.x) <= 0.001f)
                {
                    body.Move(Vector2.up * Mathf.Clamp(target.y - transform.position.y, -step, step));
                    if (Mathf.Abs(transform.position.y - target.y) <= 0.001f)
                    {
                        body.Move(Vector2.down * (Mathf.Max(body.Skin, Physics2D.defaultContactOffset * 2f) + body.Skin));
                        Stop();
                        return true;
                    }
                }
            }
            velocity = ((Vector2)transform.position - start) / dt;
            return true;
        }

        private void Stop() { State = LedgeState.None; support = null; cooldown = Settings.retryDelay; }
        private void OnDisable() => Stop();
    }
}
