using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerCrateInteraction : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PhysicsSettings physicsSettings;
        private PlayerInteractionSettings Settings => physicsSettings != null ? physicsSettings.interactions : PhysicsSettings.DefaultInteractions;
        private PushableCrate held;
        public PushableCrate HeldCrate => held;
        public bool HasReachableCrate => isActiveAndEnabled && body != null
            && (NearbyCrate(1f, Settings.crateGrabReach, out _) != null
                || NearbyCrate(-1f, Settings.crateGrabReach, out _) != null);
        public void Release() => held = null;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
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

            if (held != null && (!held.isActiveAndEnabled || !CanReach(held))) held = null;
            if (held == null)
                held = NearbyCrate(Mathf.Sign(requested), Settings.crateGrabReach, out _)
                    ?? NearbyCrate(-Mathf.Sign(requested), Settings.crateGrabReach, out _);

            if (held != null)
            {
                float maxDisplacement = Settings.crateMoveSpeed / held.Weight * Time.fixedDeltaTime;
                float limited = Mathf.Clamp(requested, -maxDisplacement, maxDisplacement);
                float playerAllowed = body.AllowedHorizontal(limited, held.Body.Shape);
                float crateAllowed = held.Body.AllowedHorizontal(limited, body.Shape);
                float shared = Mathf.Sign(limited) * Mathf.Min(Mathf.Abs(playerAllowed), Mathf.Abs(crateAllowed));
                held.Body.MoveHorizontal(shared, body.Shape);
                return shared;
            }

            return requested;
        }

        private PushableCrate NearbyCrate(float direction, float reach, out float distance)
        {
            Collider2D hit = body.Probe(Vector2.right * direction, reach, out distance);
            PushableCrate crate = hit != null ? hit.GetComponent<PushableCrate>() : null;
            return crate != null && crate.isActiveAndEnabled && crate.Body.Grounded ? crate : null;
        }

        private bool CanReach(PushableCrate crate)
        {
            return crate.isActiveAndEnabled &&
                (NearbyCrate(1f, Settings.crateGrabReach, out _) == crate || NearbyCrate(-1f, Settings.crateGrabReach, out _) == crate);
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
            if (obstacle == null || !obstacle.TryGetComponent(out CrateMotion crate) || !crate.InWater) return amount;
            // The swimmer's sinking acceleration must not cancel the buoyancy of their supporting crate.
            if (axis.y > 0f && amount < 0f && crate.IsPassenger(body)) return amount;
            float gap = Mathf.Max(0f, distance - body.Skin);
            float push = Mathf.Min(Mathf.Max(0f, Mathf.Abs(amount) - gap), Settings.crateMoveSpeed / crate.Weight * dt);
            Vector2 moved = crate.PushBySwimming(direction * push, body, dt);
            return Mathf.Sign(amount) * Mathf.Min(Mathf.Abs(amount), gap + Mathf.Max(0f, Vector2.Dot(moved, direction)));
        }

        private void OnDisable() => held = null;
    }
}
