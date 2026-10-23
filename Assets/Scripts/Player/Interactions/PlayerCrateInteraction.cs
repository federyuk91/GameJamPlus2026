using System;
using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PlayerMovement))]
    public sealed class PlayerCrateInteraction : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerMovement playerMovement;
        private PhysicsSettings WorldSettings => (body != null ? body : body = GetComponent<SimulatedBody2D>()).Settings;
        private PlayerInteractionSettings Settings => WorldSettings != null ? WorldSettings.interactions : PhysicsSettings.DefaultInteractions;
        private PlayerMovementSettings LocalMovement => (playerMovement != null
            ? playerMovement : playerMovement = GetComponent<PlayerMovement>()).MovementSettings;
        private float MaximumPushWeight => Mathf.Max(0f, LocalMovement.maximumPushWeight);
        private Pushable held;
        private bool interactionStarted;
        private readonly List<RaycastHit2D> triggerHits = new List<RaycastHit2D>(8);
        public event Action InteractionStarted;
        public Pushable HeldCrate => held;
        public float GrabReach => Mathf.Max(0f, LocalMovement.horizontalHandInteraction);
        public bool HasReachableCrate => isActiveAndEnabled && body != null
            && (NearbyCrate(1f, GrabReach, out _) != null
                || NearbyCrate(-1f, GrabReach, out _) != null);
        public void Release()
        {
            held = null;
            interactionStarted = false;
        }

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        }

        // Called by PlayerMovement, so input, interaction and movement share one ordered tick.
        public float ResolveHorizontal(float requested, bool grab, bool canInteract)
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (!grab || !canInteract)
            {
                Release();
                return requested;
            }

            Pushable previousHeld = held;
            if (held != null && (!held.isActiveAndEnabled || !CanReach(held))) held = null;
            if (held == null)
                held = NearbyCrate(Mathf.Sign(requested), GrabReach, out _)
                    ?? NearbyCrate(-Mathf.Sign(requested), GrabReach, out _);
            if (held != previousHeld) interactionStarted = false;

            if (held != null)
            {
                float weight = held.StackWeight;
                if (weight >= MaximumPushWeight)
                {
                    Release();
                    return 0f;
                }
                if (!interactionStarted && Mathf.Abs(requested) > 0.0001f)
                {
                    interactionStarted = true;
                    InteractionStarted?.Invoke();
                }
                float maxDisplacement = Settings.crateMoveSpeed / Mathf.Max(0.01f, weight) * Time.fixedDeltaTime;
                float limited = Mathf.Clamp(requested, -maxDisplacement, maxDisplacement);
                float playerAllowed = body.AllowedHorizontal(limited, held.Body.Shape);
                float crateAllowed = held.Body.AllowedHorizontal(limited, body.Shape);
                float shared = Mathf.Sign(limited) * Mathf.Min(Mathf.Abs(playerAllowed), Mathf.Abs(crateAllowed));
                return held.MoveHorizontal(shared, body.Shape);
            }

            return requested;
        }

        private Pushable NearbyCrate(float direction, float reach, out float distance)
        {
            Vector2 axis = Vector2.right * direction;
            Collider2D hit = body.Probe(axis, reach, out distance);
            Pushable nearest = GetReachablePushable(hit);
            BoxCollider2D shape = body.Shape;
            Vector2 scale = shape.transform.lossyScale;
            Vector2 size = Vector2.Scale(shape.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            ContactFilter2D filter = body.SolidFilter;
            filter.useTriggers = true;
            triggerHits.Clear();
            Physics2D.BoxCast(shape.transform.TransformPoint(shape.offset), size,
                shape.transform.eulerAngles.z, axis, filter, triggerHits, reach);
            Bounds playerBounds = shape.bounds;
            foreach (RaycastHit2D candidateHit in triggerHits)
            {
                Collider2D candidateShape = candidateHit.collider;
                if (candidateShape == null || !candidateShape.isTrigger) continue;
                Pushable candidate = GetReachablePushable(candidateShape);
                if (candidate == null || candidateShape != candidate.Body.Shape) continue;
                Bounds bounds = candidateShape.bounds;
                float gap = Mathf.Max(0f, direction > 0f
                    ? bounds.min.x - playerBounds.max.x : playerBounds.min.x - bounds.max.x);
                // Solid casts still bound reach, preventing grabs through walls or other crates.
                if (gap > distance || (nearest != null && gap >= distance)) continue;
                nearest = candidate;
                distance = gap;
            }
            return nearest;
        }

        private Pushable GetReachablePushable(Collider2D shape)
        {
            if (shape == null || !shape.TryGetComponent(out Pushable pushable)
                || !pushable.isActiveAndEnabled || !pushable.Body.Grounded
                || pushable.StackWeight >= MaximumPushWeight) return null;
            return pushable;
        }

        private bool CanReach(Pushable crate)
        {
            return crate.isActiveAndEnabled &&
                (NearbyCrate(1f, GrabReach, out _) == crate || NearbyCrate(-1f, GrabReach, out _) == crate);
        }

        public Vector2 ResolveSwimming(Vector2 displacement, float dt)
        {
            Release();
            return new Vector2(ResolveSwimmingAxis(Vector2.right, displacement.x, dt),
                ResolveSwimmingAxis(Vector2.up, displacement.y, dt));
        }

        private float ResolveSwimmingAxis(Vector2 axis, float amount, float dt)
        {
            if (Mathf.Approximately(amount, 0f)) return 0f;
            Vector2 direction = axis * Mathf.Sign(amount);
            Collider2D obstacle = body.Probe(direction, Mathf.Abs(amount) + body.Skin, out float distance);
            if (obstacle == null || !obstacle.TryGetComponent(out PropsMotion crate) || !crate.InWater) return amount;
            // The swimmer's sinking acceleration must not cancel the buoyancy of their supporting crate.
            if (axis.y > 0f && amount < 0f && crate.IsPassenger(body)) return amount;
            float gap = Mathf.Max(0f, distance - body.Skin);
            float weight = crate.PushWeight;
            if (weight >= MaximumPushWeight)
                return Mathf.Sign(amount) * Mathf.Min(Mathf.Abs(amount), gap);
            float push = Mathf.Min(Mathf.Max(0f, Mathf.Abs(amount) - gap), Settings.crateMoveSpeed / Mathf.Max(0.01f, weight) * dt);
            Vector2 moved = crate.PushBySwimming(direction * push, body, MaximumPushWeight, dt);
            return Mathf.Sign(amount) * Mathf.Min(Mathf.Abs(amount), gap + Mathf.Max(0f, Vector2.Dot(moved, direction)));
        }

        private void OnDisable() => Release();
    }
}
