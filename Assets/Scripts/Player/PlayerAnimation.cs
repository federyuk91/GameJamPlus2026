using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerMovement), typeof(SimulatedBody2D))]
    public sealed class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        [SerializeField, Min(0f)] private float runThreshold = 0.05f;

        private static readonly int IsRunning = Animator.StringToHash("IsRunning");
        private static readonly int LedgeState = Animator.StringToHash("LedgeState");

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (ledgeClimb == null) ledgeClimb = GetComponent<PlayerLedgeClimb>();
        }

        private void Update()
        {
            bool ledgeActive = ledgeClimb != null && ledgeClimb.Active;
            animator.SetInteger(LedgeState, ledgeClimb != null ? (int)ledgeClimb.State : 0);
            animator.SetBool(IsRunning, !ledgeActive && body.Grounded && Mathf.Abs(movement.Velocity.x) > runThreshold);
        }

        private void OnDisable()
        {
            if (animator != null)
            {
                animator.SetBool(IsRunning, false);
                animator.SetInteger(LedgeState, 0);
            }
        }
    }
}
