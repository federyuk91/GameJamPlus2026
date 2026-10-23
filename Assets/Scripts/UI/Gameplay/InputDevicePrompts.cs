using System.Collections.Generic;
using MousePlatformer;
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
        public Gamepad ActiveGamepad => UsingGamepad ? gamepad : null;
        public event Action Changed;
        public string BodyBinding(PlayerBodyAction action) => PlayerControlBindings.DisplayName(action,
            UsingGamepad, UsingGamepad ? (InputDevice)gamepad : Keyboard.current);
        public string Interact => PlayerControlBindings.DisplayName(PlayerBodyAction.FrontPaws, UsingGamepad, UsingGamepad ? (InputDevice)gamepad : Keyboard.current);
        public string MoveCrate => Interact;
        public string Collect => Interact;
        public string Up => UsingGamepad ? "Stick / D-pad Up" : "W / Up";
        public string Mouth => PlayerControlBindings.DisplayName(PlayerBodyAction.Mouth, UsingGamepad, UsingGamepad ? (InputDevice)gamepad : Keyboard.current);
        public string CutOrDig => Mouth;
        public string Advance => UsingGamepad ? gamepad.buttonEast.displayName : "F";
        public string Jump => PlayerControlBindings.DisplayName(PlayerBodyAction.HindPaws, UsingGamepad, UsingGamepad ? (InputDevice)gamepad : Keyboard.current);
        public string Confirm => UsingGamepad ? gamepad.buttonSouth.displayName : "Space";
        public string Cancel => UsingGamepad ? gamepad.buttonEast.displayName : "Esc";
        public string Pause => UsingGamepad ? gamepad.startButton.displayName : "Esc";
        public string Move => UsingGamepad ? gamepad.leftStick.displayName + " / " + gamepad.dpad.displayName : "WASD / Arrows";
        public string ScrollMessages => UsingGamepad ? gamepad.rightStick.displayName : "PgUp / PgDn";
        public string CameraHeightLock => UsingGamepad ? gamepad.rightShoulder.displayName : "C";
        public string CameraPositionLock => UsingGamepad ? gamepad.rightTrigger.displayName : "V";
        public InputPromptFamily Family => UsingGamepad ? FamilyFor(gamepad) : InputPromptFamily.Keyboard;

        private static InputPromptFamily FamilyFor(Gamepad controller)
        {
            if (controller is DualShockGamepad) return InputPromptFamily.PlayStation;
            string layout = controller != null ? controller.layout : string.Empty;
            if (layout.IndexOf("Switch", StringComparison.OrdinalIgnoreCase) >= 0) return InputPromptFamily.NintendoSwitch;
            if (layout.IndexOf("XInput", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("Xbox", StringComparison.OrdinalIgnoreCase) >= 0) return InputPromptFamily.Xbox;
            return InputPromptFamily.Generic;
        }

        public static InputPromptBinding FromControl(InputControl control)
        {
            InputPromptFamily family = control.device is Gamepad controller ? FamilyFor(controller) : InputPromptFamily.Keyboard;
            string name = control.path.Substring(control.device.path.Length + 1);
            string prefix = control.device is Gamepad ? "<Gamepad>/" : "<Keyboard>/";
            return MakeBinding(prefix + name, control.displayName, family);
        }

        private static InputPromptBinding MakeBinding(string path, string label, InputPromptFamily family)
        {
            if (family == InputPromptFamily.Generic)
            {
                if (path.EndsWith("/buttonNorth", StringComparison.Ordinal)) label = "N";
                else if (path.EndsWith("/buttonWest", StringComparison.Ordinal)) label = "W";
                else if (path.EndsWith("/buttonSouth", StringComparison.Ordinal)) label = "S";
                else if (path.EndsWith("/buttonEast", StringComparison.Ordinal)) label = "E";
            }
            return new InputPromptBinding(path, label, family);
        }

        public static InputPromptBinding ResolvePrompt(InputDevicePrompts device, string binding)
        {
            bool controller = device != null && device.UsingGamepad;
            InputPromptFamily family = device != null ? device.Family : InputPromptFamily.Keyboard;
            PlayerBodyAction? action = binding == "mouth" ? PlayerBodyAction.Mouth
                : binding == "front" ? PlayerBodyAction.FrontPaws : binding == "hind" ? PlayerBodyAction.HindPaws
                : binding == "tail" ? PlayerBodyAction.Tail : (PlayerBodyAction?)null;
            if (action.HasValue)
                return MakeBinding(PlayerControlBindings.Path(action.Value, controller),
                    device != null ? device.BodyBinding(action.Value) : PlayerControlBindings.DisplayName(action.Value, false), family);
            string keyboard;
            string gamepadControl;
            switch (binding)
            {
                case "camera": keyboard = "c"; gamepadControl = "rightShoulder"; break;
                case "cameraLock": keyboard = "v"; gamepadControl = "rightTrigger"; break;
                case "zoomIn": keyboard = "scroll/up"; gamepadControl = "leftShoulder"; break;
                case "zoomOut": keyboard = "scroll/down"; gamepadControl = "leftTrigger"; break;
                case "confirm": keyboard = "space"; gamepadControl = "buttonSouth"; break;
                case "cancel": keyboard = "escape"; gamepadControl = "buttonEast"; break;
                case "listenCancel": keyboard = "escape"; gamepadControl = "select"; break;
                case "advance": keyboard = "f"; gamepadControl = "buttonEast"; break;
                case "pause": keyboard = "escape"; gamepadControl = "start"; break;
                case "up": keyboard = "upArrow"; gamepadControl = "dpad/up"; break;
                case "horizontal": keyboard = "horizontal"; gamepadControl = "horizontal"; break;
                case "vertical": keyboard = "vertical"; gamepadControl = "vertical"; break;
                case "directions": keyboard = "directions"; gamepadControl = "directions"; break;
                default: return new InputPromptBinding(string.Empty, "Unassigned", family);
            }
            string path = controller ? "<Gamepad>/" + gamepadControl : "<Keyboard>/" + keyboard;
            if (!controller && (binding == "zoomIn" || binding == "zoomOut")) path = "<Mouse>/" + keyboard;
            string label = controller ? gamepadControl : keyboard;
            InputDevice activeDevice = controller ? (InputDevice)device.gamepad : Keyboard.current;
            InputControl control = activeDevice != null ? activeDevice.TryGetChildControl<InputControl>(controller ? gamepadControl : keyboard) : null;
            if (control != null) label = control.displayName;
            if (binding == "zoomIn" && !controller) label = "Scroll up";
            if (binding == "zoomOut" && !controller) label = "Scroll down";
            return MakeBinding(path, label, family);
        }

        public InputPromptBinding PromptForAction(InputPromptAction action)
        {
            string binding = action == InputPromptAction.CutOrDig ? "mouth" : action == InputPromptAction.Advance ? "advance"
                : action == InputPromptAction.Up ? "up" : "front";
            return ResolvePrompt(this, binding);
        }

        public static Dictionary<string, string> CreateControlsTokens(InputDevicePrompts prompts)
        {
            return new Dictionary<string, string>
            {
                { "move", prompts != null ? prompts.Move : "WASD / Arrows" },
                { "camera", prompts != null ? prompts.CameraHeightLock : "C" },
                { "cameraLock", prompts != null ? prompts.CameraPositionLock : "V" },
                { "zoom", prompts != null && prompts.UsingGamepad && prompts.gamepad != null
                    ? prompts.gamepad.leftShoulder.displayName + " / " + prompts.gamepad.leftTrigger.displayName : "mouse wheel" },
                { "pause", prompts != null ? prompts.Pause : "Esc" },
                { "advance", prompts != null ? prompts.Advance : "F" },
                { "confirm", prompts != null ? prompts.Confirm : "Space" },
                { "cancel", prompts != null ? prompts.Cancel : "Esc" }
            };
        }

        public InputDevicePrompts()
        {
            gamepad = Gamepad.current;
            UsingGamepad = gamepad != null;
            PlayerControlBindings.Changed += OnBindingsChanged;
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

        private void OnBindingsChanged() => Changed?.Invoke();

        public void Dispose()
        {
            PlayerControlBindings.Changed -= OnBindingsChanged;
            InputSystem.onEvent -= OnInputEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;
        }
    }
}
