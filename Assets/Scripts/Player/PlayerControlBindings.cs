using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MousePlatformer
{
    public enum PlayerBodyAction { Mouth, FrontPaws, HindPaws, Tail }

    public static class PlayerControlBindings
    {
        private static readonly string[] KeyboardDefaults = { "<Keyboard>/f", "<Keyboard>/e", "<Keyboard>/space", "" };
        private static readonly string[] GamepadDefaults = { "<Gamepad>/buttonNorth", "<Gamepad>/buttonWest", "<Gamepad>/buttonSouth", "<Gamepad>/buttonEast" };
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscriptions() => Changed = null;

        private static string Key(PlayerBodyAction action, bool gamepad) => "Controls." + action + (gamepad ? ".Gamepad" : ".Keyboard");
        public static string DefaultPath(PlayerBodyAction action, bool gamepad) => (gamepad ? GamepadDefaults : KeyboardDefaults)[(int)action];
        public static string Path(PlayerBodyAction action, bool gamepad) => PlayerPrefs.GetString(Key(action, gamepad), DefaultPath(action, gamepad));
        public static bool IsDefault(PlayerBodyAction action, bool gamepad) => Path(action, gamepad) == DefaultPath(action, gamepad);

        public static void Apply(InputAction input, PlayerBodyAction action)
        {
            bool enabled = input.enabled;
            if (enabled) input.Disable();
            input.ApplyBindingOverride(0, Path(action, false));
            input.ApplyBindingOverride(1, Path(action, true));
            if (enabled) input.Enable();
        }

        public static string DisplayName(PlayerBodyAction action, bool gamepad, InputDevice device = null)
        {
            string path = Path(action, gamepad);
            if (string.IsNullOrEmpty(path)) return "Unassigned";
            int slash = path.IndexOf('/');
            string controlName = slash >= 0 ? path.Substring(slash + 1) : path;
            InputControl control = device != null ? device.TryGetChildControl<InputControl>(controlName) : null;
            if (control != null) return control.displayName;
            if (gamepad)
            {
                switch (controlName)
                {
                    case "buttonNorth": return "Y";
                    case "buttonWest": return "X";
                    case "buttonSouth": return "A";
                    case "buttonEast": return "B";
                }
            }
            return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        public static bool TrySet(PlayerBodyAction action, bool gamepad, InputControl control, out string message)
        {
            message = string.Empty;
            if (control == null || (gamepad ? !(control.device is Gamepad) : !(control.device is Keyboard)))
            {
                message = "Choose a button on the selected device.";
                return false;
            }
            string name = control.path.Substring(control.device.path.Length + 1);
            string path = (gamepad ? "<Gamepad>/" : "<Keyboard>/") + name;
            if (IsReserved(name, gamepad))
            {
                message = "That control is reserved for movement, camera or menus.";
                return false;
            }
            string previous = Path(action, gamepad);
            foreach (PlayerBodyAction other in Enum.GetValues(typeof(PlayerBodyAction)))
            {
                if (other == action || Path(other, gamepad) != path) continue;
                if (string.IsNullOrEmpty(previous))
                {
                    message = "Already assigned to " + other + ". Choose a free button.";
                    return false;
                }
                PlayerPrefs.SetString(Key(other, gamepad), previous);
                message = "Bindings swapped with " + other + ".";
                break;
            }
            PlayerPrefs.SetString(Key(action, gamepad), path);
            PlayerPrefs.Save();
            Changed?.Invoke();
            if (string.IsNullOrEmpty(message)) message = "Binding saved.";
            return true;
        }

        private static bool IsReserved(string name, bool gamepad)
        {
            if (gamepad) return name == "start" || name == "select" || name == "leftShoulder"
                || name == "rightShoulder" || name == "leftTrigger" || name == "rightTrigger"
                || name.StartsWith("dpad/", StringComparison.Ordinal);
            switch (name)
            {
                case "w": case "a": case "s": case "d": case "upArrow": case "downArrow":
                case "leftArrow": case "rightArrow": case "escape": case "c": case "v": return true;
                default: return false;
            }
        }

        public static void RestoreDefaults()
        {
            foreach (PlayerBodyAction action in Enum.GetValues(typeof(PlayerBodyAction)))
            {
                PlayerPrefs.DeleteKey(Key(action, false));
                PlayerPrefs.DeleteKey(Key(action, true));
            }
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
