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
        private InputAction bite;
        private bool jumpQueued;
        public float Horizontal => move == null ? 0f : move.ReadValue<Vector2>().x;
        public float Vertical => move == null ? 0f : move.ReadValue<Vector2>().y;
        public bool JumpHeld => jump != null && jump.IsPressed();
        public bool GrabHeld => grab != null && grab.IsPressed();
        public bool BiteHeld => bite != null && bite.IsPressed();

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
            bite = new InputAction("Bite", InputActionType.Button);
            bite.AddBinding("<Keyboard>/f");
            bite.AddBinding("<Gamepad>/buttonEast");
        }
        private void QueueJump(InputAction.CallbackContext context) => jumpQueued = true;
        public bool ConsumeJump()
        {
            bool result = jumpQueued;
            jumpQueued = false;
            return result;
        }
        private void OnEnable() { move.Enable(); jump.Enable(); grab.Enable(); bite.Enable(); }
        private void OnDisable() { move.Disable(); jump.Disable(); grab.Disable(); bite.Disable(); jumpQueued = false; }
        private void OnDestroy() { move.Dispose(); jump.Dispose(); grab.Dispose(); bite.Dispose(); }
    }
}
