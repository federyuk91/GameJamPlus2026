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

            if (held != null && !CanReach(held)) held = null;
            if (held == null)
                held = NearbyCrate(Mathf.Sign(requested), Settings.crateGrabReach, out _)
                    ?? NearbyCrate(-Mathf.Sign(requested), Settings.crateGrabReach, out _);

            if (held != null)
            {
                float limited = Mathf.Clamp(requested, -Settings.crateMoveSpeed * Time.fixedDeltaTime, Settings.crateMoveSpeed * Time.fixedDeltaTime);
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

        private void OnDisable() => held = null;
    }
}
