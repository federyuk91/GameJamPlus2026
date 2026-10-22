using UnityEngine;
using UnityEngine.InputSystem;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputAction move;
        private InputAction jump;
        private InputAction bite;
        private InputAction collect;
        private bool collectionConsumed;
        private bool mouthConsumed;
        private bool jumpQueued;
        private AvailableGameplayActions selectedInteractions = AvailableGameplayActions.All;
        private bool gameplayLocked;
        private bool waitingForInteractionRelease;
        private bool cameraControlsLocked;
        public bool ExternalGameplayActionsLocked => gameplayLocked || waitingForInteractionRelease;
        public bool GameplayActionsLocked => ExternalGameplayActionsLocked || cameraControlsLocked;
        public Vector2 CameraMove => move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        public bool CameraJumpPressed => jump != null && jump.WasPressedThisFrame();

        public void SetCameraControlsLocked(bool locked)
        {
            cameraControlsLocked = locked;
            jumpQueued = false;
            ConsumeAllInteractions();
        }
        public bool WheelInteractionPressed => collect != null && collect.WasPressedThisFrame() && !collectionConsumed
            && (selectedInteractions & AvailableGameplayActions.UseWheel) != 0;

        public void SetGameplayActionsLocked(bool locked)
        {
            gameplayLocked = locked;
            waitingForInteractionRelease = !locked;
            jumpQueued = false;
        }
        private const AvailableGameplayActions CollectActions = AvailableGameplayActions.CollectPhoto | AvailableGameplayActions.CollectItem;
        private const AvailableGameplayActions EatActions = AvailableGameplayActions.EatCheese | AvailableGameplayActions.EatGreenCheese;
        public bool EatPressed => !GameplayActionsLocked && bite != null && bite.WasPressedThisFrame() && !mouthConsumed
            && (selectedInteractions & EatActions) != 0;
        public bool LedgeInteractionPressed => !GameplayActionsLocked && collect != null && collect.WasPressedThisFrame() && !collectionConsumed
            && (selectedInteractions & AvailableGameplayActions.ClimbLedge) != 0;
        public float Horizontal => GameplayActionsLocked || move == null ? 0f : move.ReadValue<Vector2>().x;
        public float Vertical => GameplayActionsLocked || move == null ? 0f : move.ReadValue<Vector2>().y;
        public bool JumpHeld => !GameplayActionsLocked && jump != null && jump.IsPressed();
        public bool GrabHeld => Vertical > 0.1f;
        public bool InteractionPressed => !GameplayActionsLocked && collect != null && collect.WasPressedThisFrame() && !collectionConsumed
            && (selectedInteractions & CollectActions) != 0;
        public bool PressButtonPressed => !GameplayActionsLocked && collect != null && collect.WasPressedThisFrame() && !collectionConsumed
            && (selectedInteractions & AvailableGameplayActions.PressButton) != 0;
        public bool RawBiteHeld => !GameplayActionsLocked && bite != null && bite.IsPressed();
        public bool BiteHeld => RawBiteHeld && !mouthConsumed
            && (selectedInteractions & (AvailableGameplayActions.CutRope | AvailableGameplayActions.Dig)) != 0;
        public bool CrateGrabHeld => !GameplayActionsLocked && collect != null && collect.IsPressed() && !collectionConsumed
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
            collect = new InputAction("FrontPaws", InputActionType.Button);
            collect.AddBinding("<Keyboard>/e");
            collect.AddBinding("<Gamepad>/buttonWest");
            bite = new InputAction("Mouth", InputActionType.Button);
            bite.AddBinding("<Keyboard>/f");
            bite.AddBinding("<Gamepad>/buttonNorth");
        }
        private void QueueJump(InputAction.CallbackContext context) { if (!GameplayActionsLocked) jumpQueued = true; }
        public void ConsumeCollectibleInteraction(CollectibleType type)
        {
            if (type == CollectibleType.Cheese || type == CollectibleType.GreenCheese) mouthConsumed = true;
            else ConsumeInteraction();
        }

        public void ConsumeAllInteractions()
        {
            mouthConsumed = true;
            collectionConsumed = true;
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
            }
            if (bite == null || !bite.IsPressed()) mouthConsumed = false;
            if ((collect == null || !collect.IsPressed()) && (bite == null || !bite.IsPressed()))
                waitingForInteractionRelease = false;
        }
        public bool ConsumeJump()
        {
            bool result = jumpQueued;
            jumpQueued = false;
            return result;
        }
        private void OnEnable() { move.Enable(); jump.Enable(); bite.Enable(); collect.Enable(); }
        private void OnDisable() { move.Disable(); jump.Disable(); bite.Disable(); collect.Disable(); jumpQueued = false; collectionConsumed = false; mouthConsumed = false; }
        private void OnDestroy() { move.Dispose(); jump.Dispose(); bite.Dispose(); collect.Dispose(); }
    }
}
