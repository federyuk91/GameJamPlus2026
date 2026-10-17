using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(Animator), typeof(PlayerMovement), typeof(SimulatedBody2D))]
    public sealed class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField, Min(0f)] private float runThreshold = 0.05f;

        private static readonly int IsRunning = Animator.StringToHash("IsRunning");

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
        }

        private void Update()
        {
            animator.SetBool(IsRunning, body.Grounded && Mathf.Abs(movement.Velocity.x) > runThreshold);
        }

        private void OnDisable()
        {
            if (animator != null) animator.SetBool(IsRunning, false);
        }
    }
}
