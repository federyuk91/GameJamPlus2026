using System;
using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PlayerMovement))]
    public sealed class PlayerLedgeClimb : MonoBehaviour
    {
        public enum LedgeState { None, Hanging, Climbing }
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerMovement playerMovement;
        private PhysicsSettings WorldSettings => (body != null ? body : body = GetComponent<SimulatedBody2D>()).Settings;
        [SerializeField, Tooltip("Diagnostics for the latest attachment or climb attempt, updated in Play Mode.")]
        private string lastAttempt;
        [SerializeField] private Collider2D blockingCollider;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private Collider2D support;
        private Vector3 localDestination;
        private Vector3 localHangPosition;
        private Vector2 lastHangTarget;
        private Vector2 lastClimbTarget;
        private bool raised;
        private float cooldown;
        private float elapsed;
        public LedgeState State { get; private set; }
        public bool Active => State != LedgeState.None;
        public float Direction { get; private set; }
        public event Action ClimbStarted;
        private LedgeClimbSettings Settings => WorldSettings != null ? WorldSettings.ledgeClimb : PhysicsSettings.DefaultLedgeClimb;
        private SharedPlayerMovementSettings Movement => WorldSettings != null ? WorldSettings.player : PhysicsSettings.DefaultPlayer;
        private PlayerMovementSettings LocalMovement => (playerMovement != null
            ? playerMovement : playerMovement = GetComponent<PlayerMovement>()).MovementSettings;
        private float HorizontalReach => Mathf.Max(0f, LocalMovement.horizontalHandInteraction);
        private float VerticalReach => Mathf.Max(0f, LocalMovement.verticalHandInteraction);

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        }

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
                wall = body.Probe(Vector2.right * direction, HorizontalReach + body.Skin,
                    out _, out Vector2 wallNormal);
                if (Mathf.Abs(wallNormal.x) < 0.1f) return Reject("No reachable side", wall);
            }
            if (wall == null) return Reject("No reachable side");
            float probeX = bounds.center.x + direction * (bounds.extents.x + HorizontalReach);
            Vector2 origin = new Vector2(probeX, bounds.min.y + Settings.maxHeight + body.Skin * 2f);
            Physics2D.Raycast(origin, Vector2.down, body.SolidFilter, hits, Settings.maxHeight + body.Skin * 4f);
            RaycastHit2D top = default;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == body.Shape || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider != wall || hit.normal.y < 0.9f) continue;
                if (hit.distance <= 0f) continue;
                if (hit.distance < nearest) { nearest = hit.distance; top = hit; }
            }
            if (top.collider != wall || top.normal.y < 0.9f
                || Vector2.Angle(top.normal, Vector2.up) > Mathf.Clamp(body.MaxSlopeAngle, 0f, 89f) + 0.01f)
                return Reject("No valid supporting surface above the side", top.collider);
            float surfaceRise = top.point.y - bounds.min.y;
            if (!fromGround)
            {
                if (!TryFindLocalGrip(bounds, direction, top, out RaycastHit2D localTop)) return false;
                top = localTop;
                surfaceRise = top.point.y - bounds.min.y;
            }
            // Keep the landing box outside the contact tolerance, then settle onto the support.
            float landingGap = Mathf.Max(body.Skin, Physics2D.defaultContactOffset * 2f);
            float rise = surfaceRise + landingGap;
            if (surfaceRise <= 0f || surfaceRise > Settings.maxHeight) return Reject("Height exceeds the limit", wall);
            if (fromGround && surfaceRise + body.Skin <= Movement.maxStepHeight) return Reject("Height handled by standard stepping", wall);
            float hangShift = rise - bounds.size.y * Settings.hangHeightFraction;
            if (!fromGround && Mathf.Abs(hangShift) > VerticalReach) return Reject("Ledge is beyond the paws' vertical reach", wall);

            Vector2 shift = new Vector2(probeX - bounds.center.x, rise);
            Vector2 originalPosition = transform.position;
            if (fromGround && !CanClimbTo(originalPosition + shift)) return false;
            if (!fromGround)
            {
                Vector2 adjustment = Vector2.up * hangShift;
                if ((body.AllowedDisplacement(adjustment) - adjustment).sqrMagnitude > 0.000001f)
                    return Reject("Ledge alignment obstructed", wall);
                body.Move(adjustment);
            }
            support = top.collider;
            localDestination = support.transform.InverseTransformPoint(originalPosition + shift);
            lastClimbTarget = support.transform.TransformPoint(localDestination);
            localHangPosition = support.transform.InverseTransformPoint(transform.position);
            lastHangTarget = transform.position;
            Direction = direction;
            raised = false;
            elapsed = 0f;
            State = fromGround ? LedgeState.Climbing : LedgeState.Hanging;
            if (fromGround) ClimbStarted?.Invoke();
            lastAttempt = fromGround ? "Climb started" : "Attachment successful";
            return true;
        }

        private bool TryFindLocalGrip(Bounds bounds, float direction, RaycastHit2D candidate,
            out RaycastHit2D localTop)
        {
            localTop = default;
            float clearance = Mathf.Max(0.001f, Settings.gripProbeClearance);
            float depth = Mathf.Max(0.001f, Settings.minimumGripDepth);
            float reach = bounds.extents.x + HorizontalReach + body.Skin;
            Vector2 lowerOrigin = new Vector2(bounds.center.x, candidate.point.y - clearance);
            if (!TryFindGripSurface(lowerOrigin, Vector2.right * direction, reach, out RaycastHit2D side)
                || side.collider != candidate.collider || side.distance <= 0f || side.normal.x * direction > -0.5f)
                return Reject("No local side beneath the grip", side.collider);

            // Compare nearby geometry, including other crates, rather than collider identity alone.
            Vector2 upperOrigin = new Vector2(bounds.center.x, candidate.point.y + clearance);
            float gripReach = side.distance + depth;
            if (TryFindGripSurface(upperOrigin, Vector2.right * direction, gripReach, out RaycastHit2D upper))
                return Reject("Wall above the grip leaves no exposed shelf", upper.collider);

            Vector2 topOrigin = new Vector2(side.point.x + direction * depth, upperOrigin.y);
            if (!TryFindGripSurface(topOrigin, Vector2.down, clearance * 2f, out localTop)
                || localTop.distance <= 0f || localTop.collider != candidate.collider
                || localTop.normal.y < 0.9f
                || Mathf.Abs(localTop.point.y - candidate.point.y) > clearance * 0.5f)
                return Reject("No supporting shelf beside the paws", localTop.collider);
            return true;
        }

        private bool TryFindGripSurface(Vector2 origin, Vector2 direction, float distance, out RaycastHit2D nearest)
        {
            Physics2D.Raycast(origin, direction, body.SolidFilter, hits, distance);
            nearest = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == body.Shape || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance >= nearestDistance) continue;
                nearest = hit;
                nearestDistance = hit.distance;
            }
            return nearest.collider != null;
        }

        private bool Reject(string reason, Collider2D blocker = null)
        {
            lastAttempt = reason;
            blockingCollider = blocker;
            return false;
        }

        private bool CanClimbTo(Vector2 destination)
        {
            Physics2D.SyncTransforms();
            blockingCollider = null;
            Bounds bounds = body.Shape.bounds;
            Vector2 shift = destination - (Vector2)transform.position;
            // Inset clearance queries avoid counting the adjacent floor or wall as initial overlaps.
            Vector2 clearanceSize = (Vector2)bounds.size - Vector2.one * (body.Skin * 2f);
            clearanceSize.x = Mathf.Max(0.001f, clearanceSize.x);
            clearanceSize.y = Mathf.Max(0.001f, clearanceSize.y);
            if (!HasClearPath(bounds.center, clearanceSize, Vector2.up * shift.y)) return false;
            Vector2 raisedCenter = (Vector2)bounds.center + Vector2.up * shift.y;
            if (!HasClearPath(raisedCenter, clearanceSize, Vector2.right * shift.x)) return false;
            overlaps.Clear();
            Physics2D.OverlapBox((Vector2)bounds.center + shift, clearanceSize, 0f, body.SolidFilter, overlaps);
            foreach (Collider2D collider in overlaps)
                if (collider != body.Shape && !collider.transform.IsChildOf(transform))
                    return Reject("Landing space occupied", collider);
            return true;
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
                if (hit.distance < distance) return Reject("Climb path obstructed", hit.collider);
            }
            return true;
        }

        private Collider2D FindRiser(Bounds bounds, float direction)
        {
            // Probe inside the riser height: a full-body cast can report a corner normal on low steps.
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + body.Skin * 2f);
            Physics2D.Raycast(origin, Vector2.right * direction, body.SolidFilter, hits,
                bounds.extents.x + HorizontalReach + body.Skin);
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
            return Tick(dt, horizontal, vertical, jumpPressed, false, out velocity, out released);
        }

        public bool Tick(float dt, float horizontal, float vertical, bool jumpPressed, bool interactionPressed,
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
                bool towardLedge = horizontal * Direction >= 0.1f;
                bool jumpToClimb = jumpPressed && towardLedge;
                bool awayFromLedge = horizontal * Direction <= -0.1f;
                if (jumpPressed && !towardLedge)
                {
                    velocity = carryVelocity + new Vector2(awayFromLedge ? -Direction * LocalMovement.moveSpeed : 0f,
                        JumpPhysics.LaunchSpeed(LocalMovement.jumpHeight, body.GravityAcceleration));
                    Stop();
                    released = true;
                    return true;
                }
                if (vertical < -0.1f && !jumpToClimb)
                {
                    velocity = new Vector2(0f, Mathf.Min(0f, carryVelocity.y));
                    Stop();
                    released = true;
                    return true;
                }
                velocity = carryVelocity;
                if (vertical <= 0.1f && !interactionPressed && !jumpToClimb) return true;
                Vector2 destination = support.transform.TransformPoint(localDestination);
                if (!CanClimbTo(destination))
                    return true;
                State = LedgeState.Climbing;
                lastClimbTarget = destination;
                elapsed = 0f;
                ClimbStarted?.Invoke();
            }
            if ((elapsed += dt) > 2f)
            { Reject("Climb interrupted: traversal timeout"); Stop(); released = true; return true; }
            Vector2 target = support.transform.TransformPoint(localDestination);
            Vector2 start = transform.position;
            // Follow the carrier first; climb speed limits only movement relative to that carrier.
            Vector2 carryDisplacement = target - lastClimbTarget;
            lastClimbTarget = target;
            Vector2 carrierVelocity = dt > 0f ? carryDisplacement / dt : Vector2.zero;
            body.Move(carryDisplacement, ignored: support);
            if (Vector2.Distance(transform.position, start + carryDisplacement) > 0.01f)
            {
                Reject("Climb interrupted: carrier motion obstructed");
                Stop();
                velocity = carrierVelocity;
                released = true;
                return true;
            }
            Vector2 delta = target - (Vector2)transform.position;
            float step = LocalMovement.ClimbSpeed * dt;
            if (delta.y > 0.001f) raised = false;
            if (!raised)
            {
                body.MovePlayer(Vector2.up * Mathf.Clamp(delta.y, -step, step), 0f, followGround: false);
                if (body.HitCeiling) { Reject("Climb interrupted: overhead collision"); Stop(); released = true; return true; }
                raised = Mathf.Abs(transform.position.y - target.y) <= 0.001f;
            }
            if (raised)
            {
                // Ground snapping would lower the body before the climb reaches its landing position.
                body.MovePlayer(Vector2.right * Mathf.Clamp(delta.x, -step, step), 0f, followGround: false);
                if (body.HitWall) { Reject("Climb interrupted: side collision"); Stop(); released = true; return true; }
                if (Mathf.Abs(transform.position.x - target.x) <= 0.001f)
                {
                    body.MovePlayer(Vector2.up * Mathf.Clamp(target.y - transform.position.y, -step, step), 0f, followGround: false);
                    if (Mathf.Abs(transform.position.y - target.y) <= 0.001f)
                    {
                        body.Move(Vector2.down * (Mathf.Max(body.Skin, Physics2D.defaultContactOffset * 2f) + body.Skin));
                        velocity = new Vector2((transform.position.x - start.x) / dt, carrierVelocity.y);
                        lastAttempt = "Climb completed";
                        Stop();
                        released = true;
                        return true;
                    }
                }
            }
            velocity = ((Vector2)transform.position - start) / dt;
            return true;
        }

        public void Cancel() => Stop();
        private void Stop() { State = LedgeState.None; support = null; cooldown = Settings.retryDelay; }
        private void OnDisable() => Stop();
    }
}
