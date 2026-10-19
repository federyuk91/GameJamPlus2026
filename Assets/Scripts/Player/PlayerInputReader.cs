using UnityEngine;
using UnityEngine.InputSystem;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputAction move;
        private InputAction jump;
        private InputAction crateGrab;
        private InputAction bite;
        private InputAction collect;
        private bool collectionConsumed;
        private bool jumpQueued;
        private AvailableGameplayActions selectedInteractions = AvailableGameplayActions.All;
        private bool gameplayLocked;
        private bool waitingForInteractionRelease;
        public bool GameplayActionsLocked => gameplayLocked || waitingForInteractionRelease;
        public bool WheelInteractionPressed => collect != null && collect.WasPressedThisFrame() && !collectionConsumed
            && (selectedInteractions & AvailableGameplayActions.UseWheel) != 0;

        public void SetGameplayActionsLocked(bool locked)
        {
            gameplayLocked = locked;
            waitingForInteractionRelease = !locked;
            jumpQueued = false;
        }
        private const AvailableGameplayActions CollectActions = AvailableGameplayActions.EatCheese
            | AvailableGameplayActions.EatGreenCheese | AvailableGameplayActions.CollectPhoto;
        public float Horizontal => GameplayActionsLocked || move == null ? 0f : move.ReadValue<Vector2>().x;
        public float Vertical => GameplayActionsLocked || move == null ? 0f : move.ReadValue<Vector2>().y;
        public bool JumpHeld => !GameplayActionsLocked && jump != null && jump.IsPressed();
        public bool GrabHeld => Vertical > 0.1f;
        public bool InteractionPressed => !GameplayActionsLocked && collect != null && collect.WasPressedThisFrame() && !collectionConsumed
            && (selectedInteractions & CollectActions) != 0;
        public bool PressButtonPressed => !GameplayActionsLocked && collect != null && collect.WasPressedThisFrame() && !collectionConsumed
            && (selectedInteractions & AvailableGameplayActions.PressButton) != 0;
        public bool RawBiteHeld => !GameplayActionsLocked && bite != null && bite.IsPressed();
        public bool BiteHeld => RawBiteHeld && !collectionConsumed
            && (selectedInteractions & (AvailableGameplayActions.CutRope | AvailableGameplayActions.Dig)) != 0;
        public bool CrateGrabHeld => !GameplayActionsLocked && crateGrab != null && crateGrab.IsPressed()
            && !(collectionConsumed && crateGrab.activeControl?.device is Gamepad)
            && (selectedInteractions & AvailableGameplayActions.MoveCrate) != 0;

        public bool IsInteractionSelected(AvailableGameplayActions action) => (selectedInteractions & action) != 0;

        public void SetSelectedInteractions(AvailableGameplayActions actions, bool usingGamepad = false)
        {
            selectedInteractions = actions;
        }

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddBinding("<Gamepad>/leftStick").WithProcessor("stickDeadzone");
            move.AddBinding("<Gamepad>/dpad");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            jump = new InputAction("Jump", InputActionType.Button);
            jump.AddBinding("<Gamepad>/buttonSouth");
            jump.AddBinding("<Keyboard>/space");
            jump.performed += QueueJump;
            crateGrab = new InputAction("CrateGrab", InputActionType.Button);
            crateGrab.AddBinding("<Keyboard>/e");
            crateGrab.AddBinding("<Gamepad>/buttonEast");
            collect = new InputAction("Collect", InputActionType.Button);
            collect.AddBinding("<Keyboard>/f");
            collect.AddBinding("<Gamepad>/buttonEast");
            bite = new InputAction("Bite", InputActionType.Button);
            bite.AddBinding("<Keyboard>/f");
            bite.AddBinding("<Gamepad>/buttonEast");
        }
        private void QueueJump(InputAction.CallbackContext context) { if (!GameplayActionsLocked) jumpQueued = true; }
        public void ConsumeCollectibleInteraction()
        {
            ConsumeInteraction();
        }

        public void ConsumeInteraction()
        {
            collectionConsumed = true;
        }
        private void Update()
        {
            if (collect == null || !collect.IsPressed())
            {
                collectionConsumed = false;
                waitingForInteractionRelease = false;
            }
        }
        public bool ConsumeJump()
        {
            bool result = jumpQueued;
            jumpQueued = false;
            return result;
        }
        private void OnEnable() { move.Enable(); jump.Enable(); crateGrab.Enable(); bite.Enable(); collect.Enable(); }
        private void OnDisable() { move.Disable(); jump.Disable(); crateGrab.Disable(); bite.Disable(); collect.Disable(); jumpQueued = false; collectionConsumed = false; }
        private void OnDestroy() { move.Dispose(); jump.Dispose(); crateGrab.Dispose(); bite.Dispose(); collect.Dispose(); }
    }
}
