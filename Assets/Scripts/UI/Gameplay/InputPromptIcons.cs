using System;
using UnityEngine;

namespace GameJamPlus.UI
{
    public enum InputPromptFamily { Keyboard, Xbox, PlayStation, NintendoSwitch }
    public enum InputPromptAction { Interact, CutOrDig, Advance, Collect, MoveCrate, Up }

    [Serializable]
    public sealed class InputPromptIconSet
    {
        [SerializeField] private Sprite interact;
        [SerializeField] private Sprite collect;
        [SerializeField] private Sprite moveCrate;
        [SerializeField] private Sprite cutOrDig;
        [SerializeField] private Sprite advance;
        [SerializeField] private Sprite up;

        public Sprite Get(InputPromptAction action)
        {
            switch (action)
            {
                case InputPromptAction.Interact: return interact;
                case InputPromptAction.Collect: return collect;
                case InputPromptAction.MoveCrate: return moveCrate;
                case InputPromptAction.CutOrDig: return cutOrDig;
                case InputPromptAction.Up: return up;
                case InputPromptAction.Advance: return advance;
                default: return null;
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
