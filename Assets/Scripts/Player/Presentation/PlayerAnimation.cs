using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerMovement), typeof(SimulatedBody2D))]
    public sealed class PlayerAnimation : MonoBehaviour
    {
        private enum Motion { Idle, Run, Hanging, ClimbUp, LadderClimb, Swimming, Jump, Dig, Push, RopeClimb, PressButton, WallHanging, Shocked }

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerLedgeClimb ledgeClimb;
        [SerializeField] private PlayerLadderInteraction ladderInteraction;
        [SerializeField] private PlayerSwimming swimming;
        [SerializeField] private PlayerDigging digging;
        [SerializeField] private PlayerCrateInteraction crateInteraction;
        [SerializeField] private PlayerRopeInteraction ropeInteraction;
        [SerializeField] private PlayerButtonInteraction buttonInteraction;
        [SerializeField] private PlayerStats stats;
        [SerializeField, Min(0.01f)] private float shockedAnimationSpeed = 1.5f;
        [SerializeField] private AnimationClip pressButtonClip;
        [SerializeField, Min(0.01f)] private float pressButtonAnimationSpeed = 1f;
        private float buttonPressRemaining;
        [SerializeField, Min(0f)] private float swimmingAnimationSpeed = 1f;
        [SerializeField, Min(0f)] private float ladderAnimationSpeed = 1f;
        [SerializeField, Min(0f)] private float ropeAnimationSpeed = 1f;
        [SerializeField, Min(0f)] private float climbUpAnimationSpeed = 1f;
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
        private static readonly int ClimbUpSpeed = Animator.StringToHash("ClimbUpSpeed");
        private static readonly int PressButtonSpeed = Animator.StringToHash("PressButtonSpeed");
        private static readonly int PressButtonState = Animator.StringToHash("Base Layer.PressButton");
        private static readonly int ShockedSpeed = Animator.StringToHash("ShockedSpeed");

        private void OnEnable()
        {
            if (buttonInteraction != null) buttonInteraction.ButtonPressed += OnButtonPressed;
        }

        private void OnButtonPressed()
        {
            if (animator == null || pressButtonClip == null) return;
            if (stats != null && stats.IsParalyzed) return;
            buttonPressRemaining = pressButtonClip.length / Mathf.Max(0.01f, pressButtonAnimationSpeed);
            animator.SetFloat(PressButtonSpeed, pressButtonAnimationSpeed);
            animator.SetInteger(MotionState, (int)Motion.PressButton);
            animator.Play(PressButtonState, 0, 0f);
        }

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
            if (stats == null) stats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (animator == null || movement == null || body == null) return;
            buttonPressRemaining = Mathf.Max(0f, buttonPressRemaining - Time.deltaTime);
            animator.SetFloat(PressButtonSpeed, pressButtonAnimationSpeed);
            animator.SetFloat(ShockedSpeed, shockedAnimationSpeed);
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
            if (climbingRope) animator.SetFloat(LadderSpeed, ropeAnimationSpeed);
            animator.SetFloat(ClimbUpSpeed, climbUpAnimationSpeed);
            animator.SetFloat(DiggingSpeed, diggingAnimationSpeed);
            animator.SetFloat(PushingSpeed, Mathf.Abs(movement.Velocity.x) > runThreshold ? pushingAnimationSpeed : 0f);
            animator.SetInteger(MotionState, (int)ChooseMotion(ledgeActive, onLadder, inWater, climbingRope));
        }

        private Motion ChooseMotion(bool ledgeActive, bool onLadder, bool inWater, bool climbingRope)
        {
            if (stats != null && stats.IsParalyzed) return Motion.Shocked;
            if (ledgeActive)
                return ledgeClimb.State == PlayerLedgeClimb.LedgeState.Hanging ? Motion.WallHanging : Motion.ClimbUp;
            if (onLadder) return Motion.LadderClimb;
            if (ropeInteraction != null && ropeInteraction.Attached) return climbingRope ? Motion.RopeClimb : Motion.Hanging;
            if (buttonPressRemaining > 0f) return Motion.PressButton;
            if (digging != null && digging.isActiveAndEnabled && digging.IsDigging) return Motion.Dig;
            if (inWater) return Motion.Swimming;
            if (!body.Grounded) return Motion.Jump;
            if (movement.CanInteractWithCrates && crateInteraction != null && crateInteraction.HeldCrate != null) return Motion.Push;
            return Mathf.Abs(movement.Velocity.x) > runThreshold ? Motion.Run : Motion.Idle;
        }

        private void OnDisable()
        {
            if (buttonInteraction != null) buttonInteraction.ButtonPressed -= OnButtonPressed;
            buttonPressRemaining = 0f;
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
