using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class TestResultView : IDisposable
    {
        private readonly VisualElement cover;
        private readonly VisualElement resultScreen;
        private readonly VisualElement counters;
        private readonly VisualElement timer;
        private readonly Label elapsed;
        private readonly Label result;
        private readonly Label universe;
        private readonly Button next;
        private readonly Action continueRequested;
        private readonly VisualElement advanceIcon;
        private readonly Label advanceLabel;

        public bool IsVisible => resultScreen.style.display.value == DisplayStyle.Flex;
        public void SetAdvanceKey(string key, Sprite sprite = null)
        {
            advanceIcon.style.display = sprite != null ? DisplayStyle.Flex : DisplayStyle.None;
            advanceIcon.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
            advanceLabel.text = sprite != null ? "Next" : key + " · Next";
            next.tooltip = key;
        }

        public TestResultView(VisualElement root, Action continueRequested)
        {
            this.continueRequested = continueRequested;
            cover = root.Q("test-white-cover");
            resultScreen = root.Q("test-result");
            counters = root.Q("resource-counters");
            timer = root.Q(className: "timer-anchor");
            elapsed = root.Q<Label>("test-elapsed");
            result = root.Q<Label>("test-result-message");
            universe = root.Q<Label>("test-universe");
            next = root.Q<Button>("test-next");
            next.text = string.Empty;
            advanceIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            advanceIcon.AddToClassList("input-key-icon");
            advanceIcon.style.display = DisplayStyle.None;
            advanceLabel = new Label { pickingMode = PickingMode.Ignore };
            advanceLabel.AddToClassList("test-next-label");
            next.Add(advanceIcon);
            next.Add(advanceLabel);
            next.pickingMode = PickingMode.Position;
            next.clicked += Continue;
            cover.style.display = DisplayStyle.None;
            resultScreen.style.display = DisplayStyle.None;
        }

        public void Begin()
        {
            counters.style.display = DisplayStyle.None;
            timer.style.display = DisplayStyle.None;
            cover.style.opacity = 0f;
            cover.style.display = DisplayStyle.Flex;
        }

        public void SetCoverOpacity(float opacity) => cover.style.opacity = Mathf.Clamp01(opacity);

        public void Show(int universeNumber, double elapsedSeconds, string resultMessage)
        {
            long seconds = Math.Max(0L, (long)elapsedSeconds);
            elapsed.text = $"Success! {seconds / 3600} hours {seconds / 60 % 60} minutes and {seconds % 60} seconds.";
            result.text = resultMessage;
            universe.text = "Universe-" + universeNumber;
            resultScreen.style.display = DisplayStyle.Flex;
        }

        public void Continue()
        {
            if (IsVisible) continueRequested?.Invoke();
        }

        public void Dispose() => next.clicked -= Continue;
    }
}
