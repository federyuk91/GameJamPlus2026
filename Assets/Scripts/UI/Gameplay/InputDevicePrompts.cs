using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;

namespace GameJamPlus.UI
{
    public sealed class InputDevicePrompts : IDisposable
    {
        private Gamepad gamepad;
        public bool UsingGamepad { get; private set; }
        public event Action Changed;
        public string Interact => UsingGamepad ? gamepad.buttonWest.displayName : "E";
        public string Collect => UsingGamepad ? gamepad.buttonWest.displayName : "F";
        public string CutOrDig => UsingGamepad ? gamepad.buttonEast.displayName : "F";
        public string Advance => UsingGamepad ? gamepad.buttonNorth.displayName : "Enter";
        public InputPromptFamily Family
        {
            get
            {
                if (!UsingGamepad) return InputPromptFamily.Keyboard;
                if (gamepad is DualShockGamepad) return InputPromptFamily.PlayStation;
                if (gamepad.layout.IndexOf("Switch", StringComparison.OrdinalIgnoreCase) >= 0)
                    return InputPromptFamily.NintendoSwitch;
                return InputPromptFamily.Xbox;
            }
        }

        public InputDevicePrompts()
        {
            gamepad = Gamepad.current;
            UsingGamepad = gamepad != null;
            InputSystem.onEvent += OnInputEvent;
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (eventPtr.type != StateEvent.Type && eventPtr.type != DeltaStateEvent.Type) return;
            if (!(device is Gamepad) && !(device is Keyboard) && !(device is Mouse)) return;
            foreach (InputControl control in eventPtr.EnumerateChangedControls(device, 0.35f))
            {
                // Ignore mouse position and controller sensor noise so idle devices cannot steal the prompts.
                if (control.noisy) continue;
                if (device is Mouse mouse && !(control is ButtonControl)
                    && control != mouse.scroll && control.parent != mouse.scroll) continue;
                bool useGamepad = device is Gamepad;
                Gamepad activeGamepad = device as Gamepad;
                if (UsingGamepad == useGamepad && (!useGamepad || gamepad == activeGamepad)) return;
                gamepad = activeGamepad;
                UsingGamepad = useGamepad;
                Changed?.Invoke();
                return;
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device != gamepad || (change != InputDeviceChange.Disconnected
                && change != InputDeviceChange.Removed && change != InputDeviceChange.Disabled)) return;
            gamepad = null;
            UsingGamepad = false;
            Changed?.Invoke();
        }

        public void Dispose()
        {
            InputSystem.onEvent -= OnInputEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;
        }
    }
}
