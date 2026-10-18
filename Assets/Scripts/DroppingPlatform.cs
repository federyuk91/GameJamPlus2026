using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(110), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class DroppingPlatform : MonoBehaviour
    {
        public enum PlatformState
        {
            Ready,
            Waiting,
            Falling,
            Hidden
        }

        [SerializeField] private BoxCollider2D shape;
        [SerializeField] private SimulatedBody2D player;
        [SerializeField, Min(0f), Tooltip("Seconds from the mouse first stepping on the platform until it falls.")]
        private float fallDelay = 0.4f;
        [SerializeField, Min(0f), Tooltip("Seconds of falling before disappearing.")]
        private float disappearDelay = 2f;
        [SerializeField, Min(0f)] private float fallAcceleration = 20f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 15f;
        private float elapsed;
        private float fallSpeed;

        public PlatformState State { get; private set; }

        private void Awake()
        {
            if (shape == null) shape = GetComponent<BoxCollider2D>();
        }

        private void FixedUpdate()
        {
            if (State == PlatformState.Ready)
            {
                if (player == null || !player.isActiveAndEnabled || !player.Grounded || player.GroundCollider != shape) return;
                State = PlatformState.Waiting;
                elapsed = 0f;
            }

            float dt = Time.fixedDeltaTime;
            if (State == PlatformState.Waiting)
            {
                elapsed += dt;
                if (elapsed < fallDelay) return;
                State = PlatformState.Falling;
                elapsed = 0f;
                // Once released, the visual falls freely and cannot catch or obstruct the player.
                shape.enabled = false;
            }

            if (State != PlatformState.Falling) return;
            float nextSpeed = Mathf.Min(maxFallSpeed, fallSpeed + fallAcceleration * dt);
            transform.position += Vector3.down * ((fallSpeed + nextSpeed) * 0.5f * dt);
            fallSpeed = nextSpeed;
            elapsed += dt;
            if (elapsed < disappearDelay) return;
            State = PlatformState.Hidden;
            gameObject.SetActive(false);
        }

        private void Reset()
        {
            shape = GetComponent<BoxCollider2D>();
            if (shape != null) shape.isTrigger = false;
        }
    }
}
