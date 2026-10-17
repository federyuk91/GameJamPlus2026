using System;
using UnityEngine;

namespace GameJamPlus.UI
{
    public enum InputPromptFamily { Keyboard, Xbox, PlayStation, NintendoSwitch }
    public enum InputPromptAction { Interact, CutOrDig, Advance, Collect }

    [Serializable]
    public sealed class InputPromptIconSet
    {
        [SerializeField] private Sprite interact;
        [SerializeField] private Sprite cutOrDig;
        [SerializeField] private Sprite advance;

        public Sprite Get(InputPromptAction action)
        {
            switch (action)
            {
                case InputPromptAction.Interact: return interact;
                case InputPromptAction.CutOrDig: return cutOrDig;
                default: return advance;
            }
        }
    }

    [CreateAssetMenu(menuName = "Game/UI/Input Prompt Icons")]
    public sealed class InputPromptIcons : ScriptableObject
    {
        [SerializeField] private InputPromptIconSet keyboard;
        [SerializeField] private InputPromptIconSet xbox;
        [SerializeField] private InputPromptIconSet playStation;
        [SerializeField] private InputPromptIconSet nintendoSwitch;

        public Sprite Get(InputPromptFamily family, InputPromptAction action)
        {
            if (action == InputPromptAction.Collect)
                action = family == InputPromptFamily.Keyboard ? InputPromptAction.CutOrDig : InputPromptAction.Interact;
            InputPromptIconSet icons;
            switch (family)
            {
                case InputPromptFamily.Xbox: icons = xbox; break;
                case InputPromptFamily.PlayStation: icons = playStation; break;
                case InputPromptFamily.NintendoSwitch: icons = nintendoSwitch; break;
                default: icons = keyboard; break;
            }
            return icons?.Get(action);
        }
    }
}
