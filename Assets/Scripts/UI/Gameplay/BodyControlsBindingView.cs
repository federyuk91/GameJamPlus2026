using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    internal sealed class BodyControlsBindingView
    {
        private readonly VisualElement root;
        private readonly Label listeningTitle;
        private readonly Label confirmTitle;
        private readonly VisualElement keyboardHint;
        private readonly VisualElement gamepadHint;
        private readonly Label confirmHint;
        private readonly Label error;
        private readonly InputPromptElement confirmGlyph;
        private readonly VisualElement choices;
        private readonly string listeningTemplate;
        private readonly string confirmTemplate;
        public Button Yes { get; }
        public Button No { get; }
        public Button Cancel { get; }

        public BodyControlsBindingView(VisualElement element)
        {
            root = element;
            listeningTitle = root.Q<Label>("body-binding-title");
            confirmTitle = root.Q<Label>("body-confirm-title");
            listeningTemplate = listeningTitle.text;
            confirmTemplate = confirmTitle.text;
            keyboardHint = root.Q("body-keyboard-hint");
            gamepadHint = root.Q("body-gamepad-hint");
            confirmHint = root.Q<Label>("body-confirm-hint");
            error = root.Q<Label>("body-binding-error");
            confirmGlyph = root.Q<InputPromptElement>("body-confirm-glyph");
            choices = root.Q("body-binding-choices");
            Yes = root.Q<Button>("body-binding-yes");
            No = root.Q<Button>("body-binding-no");
            Cancel = root.Q<Button>("body-binding-cancel");
            Close();
        }

        public void ShowListening(string part, bool gamepad)
        {
            root.style.display = DisplayStyle.Flex;
            confirmGlyph.style.display = DisplayStyle.None;
            listeningTitle.text = listeningTemplate.Replace("{part}", part);
            listeningTitle.style.display = DisplayStyle.Flex;
            confirmTitle.style.display = DisplayStyle.None;
            keyboardHint.style.display = gamepad ? DisplayStyle.None : DisplayStyle.Flex;
            gamepadHint.style.display = gamepad ? DisplayStyle.Flex : DisplayStyle.None;
            confirmHint.style.display = choices.style.display = DisplayStyle.None;
            error.text = string.Empty;
        }

        public void ShowConfirmation(InputPromptBinding binding, string part)
        {
            confirmTitle.text = confirmTemplate.Replace("{button}", binding.Label).Replace("{part}", part);
            confirmGlyph.SetPrompt(binding);
            confirmGlyph.style.display = DisplayStyle.Flex;
            confirmTitle.style.display = confirmHint.style.display = choices.style.display = DisplayStyle.Flex;
            listeningTitle.style.display = keyboardHint.style.display = gamepadHint.style.display = DisplayStyle.None;
            error.text = string.Empty;
        }

        public void ShowError(string message) => error.text = message;
        public void SetDevice(InputDevicePrompts prompts) => InputPromptElement.BindAll(root, prompts);
        public void Close() => root.style.display = DisplayStyle.None;
    }
}
