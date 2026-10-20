using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(110), DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PropsMotion), typeof(RopeAttachment))]
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
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PropsMotion motion;
        [SerializeField] private RopeAttachment attachment;
        [SerializeField, Tooltip("Optional specific animal that activates the plank. If unassigned, a standing passenger registered by PlayerPlatformRide starts the countdown.")]
        private SimulatedBody2D player;
        [SerializeField, Min(0f), Tooltip("Seconds from the mouse first stepping on the plank until its support rope breaks. Leaving the plank does not cancel the countdown.")]
        private float fallDelay = 0.4f;
        [SerializeField, Min(0f), Tooltip("Seconds of falling before disappearing.")]
        private float disappearDelay = 2f;
        private float elapsed;

        public PlatformState State { get; private set; }

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (motion == null) motion = GetComponent<PropsMotion>();
            if (attachment == null) attachment = GetComponent<RopeAttachment>();
            if (shape == null) shape = body.Shape;
            motion.enabled = true;
        }

        private void OnEnable()
        {
            if (attachment != null && attachment.Rope != null) attachment.Rope.Cut += HandleRopeCut;
        }

        private void OnDisable()
        {
            if (attachment != null && attachment.Rope != null) attachment.Rope.Cut -= HandleRopeCut;
        }

        private void HandleRopeCut(RopeSimulation rope)
        {
            if (State == PlatformState.Falling || State == PlatformState.Hidden) return;
            State = PlatformState.Falling;
            elapsed = 0f;
        }

        private void FixedUpdate()
        {
            if (attachment == null || attachment.Rope == null) return;
            if (!attachment.HasRopeConnection && (State == PlatformState.Ready || State == PlatformState.Waiting))
                HandleRopeCut(attachment.Rope);
            if (State == PlatformState.Ready)
            {
                bool hasStandingPlayer = player != null
                    ? player.isActiveAndEnabled && player.Grounded && player.GroundCollider == shape
                    : motion.HasStandingPassenger;
                if (!hasStandingPlayer) return;
                State = PlatformState.Waiting;
                elapsed = 0f;
            }

            float dt = Time.fixedDeltaTime;
            if (State == PlatformState.Waiting)
            {
                elapsed += dt;
                if (elapsed < fallDelay) return;
                attachment.Rope.TryCutSegment(attachment.Rope.PieceCount - 1);
            }

            if (State != PlatformState.Falling) return;
            elapsed += dt;
            if (elapsed < disappearDelay) return;
            State = PlatformState.Hidden;
            gameObject.SetActive(false);
        }

        private void Reset()
        {
            body = GetComponent<SimulatedBody2D>();
            motion = GetComponent<PropsMotion>();
            attachment = GetComponent<RopeAttachment>();
            shape = body != null ? body.Shape : GetComponent<BoxCollider2D>();
            if (motion != null) motion.enabled = true;
            if (shape != null) shape.isTrigger = false;
        }
    }
}
