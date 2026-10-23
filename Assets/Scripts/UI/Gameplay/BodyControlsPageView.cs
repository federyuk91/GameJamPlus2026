using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    // Binds authored elements without constructing or replacing their layout.
    internal sealed class BodyControlsPageView
    {
        private readonly VisualElement root;
        private readonly Label summary;
        private readonly string summaryTemplate;
        private readonly List<Label> cameraCallouts = new List<Label>();
        private readonly List<string> cameraTemplates = new List<string>();

        public BodyControlsPageView(VisualElement element)
        {
            root = element;
            summary = root.Q<Label>("body-device-summary");
            summaryTemplate = summary != null ? summary.text : string.Empty;
            root.Query<Label>(className: "body-camera-callout").ForEach(label =>
            {
                cameraCallouts.Add(label);
                cameraTemplates.Add(label.text);
            });
        }

        public void SetVisible(bool visible) => root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public void Render(InputDevicePrompts prompts, IReadOnlyDictionary<string, string> tokens)
        {
            InputPromptElement.BindAll(root, prompts);
            if (summary != null) summary.text = Format(summaryTemplate, tokens);
            for (int index = 0; index < cameraCallouts.Count; index++)
                cameraCallouts[index].text = Format(cameraTemplates[index], tokens);
        }

        public static string Format(string template, IReadOnlyDictionary<string, string> tokens)
        {
            string result = template;
            foreach (KeyValuePair<string, string> token in tokens) result = result.Replace("{" + token.Key + "}", token.Value);
            return result;
        }

    }
}
