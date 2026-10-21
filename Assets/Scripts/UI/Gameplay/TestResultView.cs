using System;
using GameJamPlus.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class TestResultView : IDisposable
    {
        private readonly VisualElement cover;
        private readonly TestEndingTunnel tunnel;
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
        private readonly VisualElement[] observationRows;
        private readonly ScrollView notes;
        private static readonly TestObservation[] ObservationFlags =
        {
            TestObservation.EnjoysFood, TestObservation.Exercises, TestObservation.LovesPool,
            TestObservation.ExtendedStay, TestObservation.EnjoysGardening, TestObservation.VeryCurious
        };

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
            cover.style.backgroundColor = Color.clear;
            tunnel = new TestEndingTunnel();
            cover.Add(tunnel);
            resultScreen = root.Q("test-result");
            counters = root.Q("resource-counters");
            timer = root.Q(className: "timer-anchor");
            elapsed = root.Q<Label>("test-elapsed");
            result = root.Q<Label>("test-result-message");
            notes = root.Q<ScrollView>("test-notes");
            notes.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            notes.scrollDecelerationRate = 0f;
            notes.contentContainer.style.flexGrow = 0f;
            notes.contentContainer.style.flexShrink = 0f;
            notes.pickingMode = PickingMode.Position;
            notes.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Position);
            universe = root.Q<Label>("test-universe");
            observationRows = new VisualElement[ObservationFlags.Length];
            for (int index = 0; index < observationRows.Length; index++)
                observationRows[index] = root.Q("test-observation-" + index);
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
            cover.style.backgroundColor = Color.clear;
            tunnel.style.display = DisplayStyle.Flex;
            tunnel.SetProgress(0f);
            cover.style.display = DisplayStyle.Flex;
        }

        public void SetCoverOpacity(float opacity)
        {
            cover.style.opacity = 1f;
            float progress = Mathf.Clamp01(opacity);
            bool complete = progress >= 1f;
            cover.style.backgroundColor = complete ? Color.white : Color.clear;
            tunnel.style.display = complete ? DisplayStyle.None : DisplayStyle.Flex;
            tunnel.SetProgress(progress);
        }

        public void Show(int universeNumber, double elapsedSeconds, string resultMessage)
        {
            long seconds = Math.Max(0L, (long)elapsedSeconds);
            elapsed.text = $"Stay: {seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
            result.text = resultMessage;
            result.style.display = string.IsNullOrWhiteSpace(resultMessage) ? DisplayStyle.None : DisplayStyle.Flex;
            SetObservations(GameSession.GetTestObservations(elapsedSeconds));
            notes.scrollOffset = Vector2.zero;
            universe.text = "Universe-" + universeNumber;
            resultScreen.style.display = DisplayStyle.Flex;
        }

        public void SetObservations(TestObservation observations)
        {
            for (int index = 0; index < observationRows.Length; index++)
                observationRows[index].EnableInClassList("observation-checked", (observations & ObservationFlags[index]) != 0);
        }

        public void ScrollNotes(float amount)
        {
            if (!IsVisible || Mathf.Abs(amount) < 0.001f) return;
            float maximumOffset = Mathf.Max(0f, notes.verticalScroller.highValue);
            float offset = Mathf.Clamp(notes.scrollOffset.y + amount, 0f, maximumOffset);
            if (!Mathf.Approximately(notes.scrollOffset.y, offset))
                notes.scrollOffset = new Vector2(0f, offset);
        }

        public void Continue()
        {
            if (IsVisible) continueRequested?.Invoke();
        }

        public void Dispose()
        {
            next.clicked -= Continue;
            tunnel.RemoveFromHierarchy();
        }
    }
}
