using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerMovement), typeof(SimulatedBody2D))]
    public sealed class PlayerAnimation : MonoBehaviour
    {
        private enum Motion { Idle, Run, Hanging, ClimbUp, LadderClimb, Swimming, Jump, Dig, Push, RopeClimb }

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        [SerializeField] private PlayerLadderInteraction ladderInteraction;
        [SerializeField] private PlayerSwimming swimming;
        [SerializeField] private PlayerDigging digging;
        [SerializeField] private PlayerCrateInteraction crateInteraction;
        [SerializeField] private PlayerRopeInteraction ropeInteraction;
        [SerializeField, Min(0f)] private float swimmingAnimationSpeed = 1f;
        [SerializeField, Min(0f)] private float ladderAnimationSpeed = 1f;
        [SerializeField, Min(0f)] private float diggingAnimationSpeed = 1f;
        [SerializeField, Min(0f)] private float pushingAnimationSpeed = 1f;
        [SerializeField, Min(0f)] private float runThreshold = 0.05f;

        private static readonly int IsRunning = Animator.StringToHash("IsRunning");
        private static readonly int LedgeState = Animator.StringToHash("LedgeState");
        private static readonly int IsOnLadder = Animator.StringToHash("IsOnLadder");
        private static readonly int LadderSpeed = Animator.StringToHash("LadderSpeed");
        private static readonly int IsSwimming = Animator.StringToHash("IsSwimming");
        private static readonly int SwimmingSpeed = Animator.StringToHash("SwimmingSpeed");
        private static readonly int MotionState = Animator.StringToHash("MotionState");
        private static readonly int DiggingSpeed = Animator.StringToHash("DiggingSpeed");
        private static readonly int PushingSpeed = Animator.StringToHash("PushingSpeed");

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (ledgeClimb == null) ledgeClimb = GetComponent<PlayerLedgeClimb>();
            if (ladderInteraction == null) ladderInteraction = GetComponent<PlayerLadderInteraction>();
            if (swimming == null) swimming = GetComponent<PlayerSwimming>();
            if (digging == null) digging = GetComponent<PlayerDigging>();
            if (crateInteraction == null) crateInteraction = GetComponent<PlayerCrateInteraction>();
            if (ropeInteraction == null) ropeInteraction = GetComponent<PlayerRopeInteraction>();
        }

        private void Update()
        {
            if (animator == null || movement == null || body == null) return;
            bool ledgeActive = ledgeClimb != null && ledgeClimb.Active;
            bool onLadder = !ledgeActive && ladderInteraction != null && ladderInteraction.Attached;
            bool inWater = !ledgeActive && !onLadder && swimming != null && swimming.isActiveAndEnabled && swimming.InWater;
            animator.SetBool(IsSwimming, inWater);
            animator.SetFloat(SwimmingSpeed, swimmingAnimationSpeed);
            animator.SetBool(IsOnLadder, onLadder);
            animator.SetFloat(LadderSpeed, onLadder && Mathf.Abs(ladderInteraction.ClimbSpeed) > 0.01f ? ladderAnimationSpeed : 0f);
            animator.SetInteger(LedgeState, ledgeClimb != null ? (int)ledgeClimb.State : 0);
            animator.SetBool(IsRunning, !ledgeActive && !onLadder && !inWater && body.Grounded && Mathf.Abs(movement.Velocity.x) > runThreshold);
            bool climbingRope = ropeInteraction != null && ropeInteraction.Attached && Mathf.Abs(ropeInteraction.ClimbSpeed) > 0.01f;
            if (climbingRope) animator.SetFloat(LadderSpeed, ladderAnimationSpeed);
            animator.SetFloat(DiggingSpeed, diggingAnimationSpeed);
            animator.SetFloat(PushingSpeed, Mathf.Abs(movement.Velocity.x) > runThreshold ? pushingAnimationSpeed : 0f);
            animator.SetInteger(MotionState, (int)ChooseMotion(ledgeActive, onLadder, inWater, climbingRope));
        }

        private Motion ChooseMotion(bool ledgeActive, bool onLadder, bool inWater, bool climbingRope)
        {
            if (ledgeActive)
                return ledgeClimb.State == PlayerLedgeClimb.LedgeState.Hanging ? Motion.Hanging : Motion.ClimbUp;
            if (onLadder) return Motion.LadderClimb;
            if (ropeInteraction != null && ropeInteraction.Attached) return climbingRope ? Motion.RopeClimb : Motion.Hanging;
            if (digging != null && digging.isActiveAndEnabled && digging.IsDigging) return Motion.Dig;
            if (inWater) return Motion.Swimming;
            if (!body.Grounded) return Motion.Jump;
            if (movement.CanInteractWithCrates && crateInteraction != null && crateInteraction.HeldCrate != null) return Motion.Push;
            return Mathf.Abs(movement.Velocity.x) > runThreshold ? Motion.Run : Motion.Idle;
        }

        private void OnDisable()
        {
            if (animator != null)
            {
                animator.SetBool(IsRunning, false);
                animator.SetInteger(LedgeState, 0);
                animator.SetBool(IsOnLadder, false);
                animator.SetFloat(LadderSpeed, 0f);
                animator.SetBool(IsSwimming, false);
                animator.SetFloat(SwimmingSpeed, 0f);
                animator.SetInteger(MotionState, (int)Motion.Idle);
                animator.SetFloat(DiggingSpeed, 0f);
                animator.SetFloat(PushingSpeed, 0f);
            }
        }
    }
}
