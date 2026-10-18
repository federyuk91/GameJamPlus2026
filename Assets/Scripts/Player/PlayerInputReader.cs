using UnityEngine;
using UnityEngine.InputSystem;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputAction move;
        private InputAction jump;
        private InputAction grab;
        private InputAction crateGrab;
        private InputAction bite;
        private InputAction collect;
        private bool collectionConsumed;
        private bool jumpQueued;
        private AvailableGameplayActions selectedInteractions = AvailableGameplayActions.All;
        private bool gamepadInteractionScheme;
        private const AvailableGameplayActions CollectActions = AvailableGameplayActions.EatCheese
            | AvailableGameplayActions.EatGreenCheese | AvailableGameplayActions.CollectPhoto;
        private const AvailableGameplayActions SharedInteractionActions = CollectActions | AvailableGameplayActions.PressButton;
        public float Horizontal => move == null ? 0f : move.ReadValue<Vector2>().x;
        public float Vertical => move == null ? 0f : move.ReadValue<Vector2>().y;
        public bool JumpHeld => jump != null && jump.IsPressed();
        public bool GrabHeld => grab != null && grab.IsPressed()
            && (!gamepadInteractionScheme || (selectedInteractions & SharedInteractionActions) == 0);
        public bool InteractionPressed => collect != null && collect.WasPressedThisFrame() && !collectionConsumed
            && (selectedInteractions & CollectActions) != 0;
        public bool PressButtonPressed => collect != null && collect.WasPressedThisFrame() && !collectionConsumed
            && (selectedInteractions & AvailableGameplayActions.PressButton) != 0;
        public bool RawBiteHeld => bite != null && bite.IsPressed();
        public bool BiteHeld => RawBiteHeld && !collectionConsumed
            && (selectedInteractions & (AvailableGameplayActions.CutRope | AvailableGameplayActions.Dig)) != 0;
        public bool CrateGrabHeld => crateGrab != null && crateGrab.IsPressed()
            && (selectedInteractions & AvailableGameplayActions.MoveCrate) != 0;

        public bool IsInteractionSelected(AvailableGameplayActions action) => (selectedInteractions & action) != 0;

        public void SetSelectedInteractions(AvailableGameplayActions actions, bool usingGamepad = false)
        {
            selectedInteractions = actions;
            gamepadInteractionScheme = usingGamepad;
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
            grab = new InputAction("Grab", InputActionType.Button);
            grab.AddBinding("<Keyboard>/e");
            grab.AddBinding("<Gamepad>/buttonWest");
            crateGrab = new InputAction("CrateGrab", InputActionType.Button);
            crateGrab.AddBinding("<Keyboard>/e");
            crateGrab.AddBinding("<Gamepad>/buttonEast");
            collect = new InputAction("Collect", InputActionType.Button);
            collect.AddBinding("<Keyboard>/f");
            collect.AddBinding("<Gamepad>/buttonWest");
            bite = new InputAction("Bite", InputActionType.Button);
            bite.AddBinding("<Keyboard>/f");
            bite.AddBinding("<Gamepad>/buttonEast");
        }
        private void QueueJump(InputAction.CallbackContext context) => jumpQueued = true;
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
            if (collect == null || !collect.IsPressed()) collectionConsumed = false;
        }
        public bool ConsumeJump()
        {
            bool result = jumpQueued;
            jumpQueued = false;
            return result;
        }
        private void OnEnable() { move.Enable(); jump.Enable(); grab.Enable(); crateGrab.Enable(); bite.Enable(); collect.Enable(); }
        private void OnDisable() { move.Disable(); jump.Disable(); grab.Disable(); crateGrab.Disable(); bite.Disable(); collect.Disable(); jumpQueued = false; collectionConsumed = false; }
        private void OnDestroy() { move.Dispose(); jump.Dispose(); grab.Dispose(); crateGrab.Dispose(); bite.Dispose(); collect.Dispose(); }
    }
}
