using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [UxmlElement]
    public sealed partial class InputPromptElement : VisualElement
    {
        private readonly VisualElement icon;
        private readonly Label fallback;
        private bool hasBinding;
        private string previewPath = "<Keyboard>/f";
        private string previewLabel = "F";
        private InputPromptFamily previewFamily = InputPromptFamily.Keyboard;
        [UxmlAttribute] public string Binding { get; set; }
        [UxmlAttribute] public string PreviewPath { get => previewPath; set { previewPath = value; RefreshPreview(); } }
        [UxmlAttribute] public string PreviewLabel { get => previewLabel; set { previewLabel = value; RefreshPreview(); } }
        [UxmlAttribute] public InputPromptFamily PreviewFamily { get => previewFamily; set { previewFamily = value; RefreshPreview(); } }

        public InputPromptElement()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("input-prompt");
            icon = new VisualElement { name = "prompt-glyph", pickingMode = PickingMode.Ignore };
            icon.AddToClassList("input-prompt-glyph");
            fallback = new Label { name = "prompt-fallback", pickingMode = PickingMode.Ignore };
            fallback.AddToClassList("input-prompt-overlay");
            Add(icon); Add(fallback);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent attachEvent)
        {
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (!hasBinding && icon != null) Render(new InputPromptBinding(PreviewPath, PreviewLabel, PreviewFamily));
        }

        public void SetPrompt(InputPromptBinding binding)
        {
            hasBinding = true;
            Render(binding);
        }

        private void Render(InputPromptBinding binding)
        {
            InputPromptCatalog catalog = InputPromptCatalog.Default;
            bool overlay = true;
            Texture2D texture = catalog != null ? catalog.Resolve(binding, out overlay) : null;
            icon.style.backgroundImage = texture != null ? new StyleBackground(texture) : new StyleBackground(StyleKeyword.None);
            icon.style.display = texture != null ? DisplayStyle.Flex : DisplayStyle.None;
            fallback.text = string.IsNullOrEmpty(binding.Path) ? "—" : binding.Label;
            fallback.style.display = overlay ? DisplayStyle.Flex : DisplayStyle.None;
            tooltip = binding.Label;
            EnableInClassList("input-prompt-fallback", overlay);
            EnableInClassList("input-prompt-long", overlay && !string.IsNullOrEmpty(binding.Path)
                && binding.Label != null && binding.Label.Length > 3);
        }

        public static void BindAll(VisualElement root, InputDevicePrompts device)
        {
            root.Query<InputPromptElement>().ForEach(element =>
            {
                if (!string.IsNullOrEmpty(element.Binding)) element.SetPrompt(InputDevicePrompts.ResolvePrompt(device, element.Binding));
            });
        }
    }
}
