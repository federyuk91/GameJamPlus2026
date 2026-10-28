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
        private readonly ExitTunnelImage exitImage;
        private readonly VisualElement root;
        private bool useExitImage;
        private readonly VisualElement resultScreen;
        private readonly VisualElement counters;
        private readonly VisualElement campaignReadout;
        private readonly Label elapsed;
        private readonly Label result;
        private readonly Label universe;
        private readonly Label subject;
        private readonly Button next;
        private readonly Action continueRequested;
        private readonly InputPromptElement advanceIcon;
        private readonly Label advanceLabel;
        private readonly VisualElement[] observationRows;
        private readonly ScrollView notes;
        private static readonly TestObservation[] ObservationFlags =
        {
            TestObservation.EnjoysFood, TestObservation.Exercises, TestObservation.LovesPool,
            TestObservation.ExtendedStay, TestObservation.LittleMechanic, TestObservation.VeryCurious,
            TestObservation.Electrician, TestObservation.SenseOfDirection
        };

        public bool IsVisible => resultScreen.style.display.value == DisplayStyle.Flex;
        public void SetAdvanceKey(InputPromptBinding binding)
        {
            advanceIcon.SetPrompt(binding);
            advanceIcon.style.display = DisplayStyle.Flex;
            advanceLabel.text = "Next";
            next.tooltip = binding.Label;
        }

        public TestResultView(VisualElement root, Action continueRequested, ExitTunnelSettings exitSettings)
        {
            this.root = root;
            this.continueRequested = continueRequested;
            cover = root.Q("test-white-cover");
            cover.style.backgroundColor = Color.clear;
            tunnel = new TestEndingTunnel();
            cover.Add(tunnel);
            exitImage = new ExitTunnelImage(exitSettings);
            cover.Add(exitImage);
            root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            cover.parent.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            resultScreen = root.Q("test-result");
            counters = root.Q("resource-counters");
            campaignReadout = root.Q("campaign-readout") ?? root.Q(className: "timer-anchor");
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
            subject = root.Q<Label>("test-subject");
            observationRows = new VisualElement[ObservationFlags.Length];
            for (int index = 0; index < observationRows.Length; index++)
                observationRows[index] = root.Q("test-observation-" + index);
            next = root.Q<Button>("test-next");
            next.text = string.Empty;
            advanceIcon = new InputPromptElement();
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

        public void Begin(bool showExitImage)
        {
            useExitImage = showExitImage && exitImage.IsAvailable;
            if (showExitImage && !useExitImage)
                Debug.LogWarning("Assign the Exit Tunnel image and shader on Gameplay HUD to show the goal transition.");
            cover.BringToFront();
            if (useExitImage) FitCoverToScreen();
            if (counters != null) counters.style.display = DisplayStyle.None;
            if (campaignReadout != null) campaignReadout.style.display = DisplayStyle.None;
            cover.style.opacity = useExitImage ? 1f : 0f;
            cover.style.backgroundColor = Color.clear;
            tunnel.style.display = useExitImage ? DisplayStyle.None : DisplayStyle.Flex;
            tunnel.SetProgress(0f);
            exitImage.style.display = useExitImage ? DisplayStyle.Flex : DisplayStyle.None;
            if (useExitImage) exitImage.SetProgress(0f);
            cover.style.display = DisplayStyle.Flex;
        }

        private void OnRootGeometryChanged(GeometryChangedEvent change)
        {
            if (useExitImage) FitCoverToScreen();
        }

        private void FitCoverToScreen()
        {
            // Extend beyond the pixel-perfect frame to include letterbox/pillarbox borders.
            Rect screen = root.ChangeCoordinatesTo(cover.parent, root.contentRect);
            cover.style.left = screen.x;
            cover.style.top = screen.y;
            cover.style.right = cover.style.bottom = StyleKeyword.Auto;
            cover.style.width = screen.width;
            cover.style.height = screen.height;
        }

        public void SetCoverOpacity(float opacity)
        {
            cover.style.opacity = 1f;
            float progress = Mathf.Clamp01(opacity);
            bool complete = progress >= 1f;
            cover.style.backgroundColor = complete ? Color.white : Color.clear;
            tunnel.style.display = complete || useExitImage ? DisplayStyle.None : DisplayStyle.Flex;
            exitImage.style.display = !complete && useExitImage ? DisplayStyle.Flex : DisplayStyle.None;
            if (useExitImage) exitImage.SetProgress(progress);
            else tunnel.SetProgress(progress);
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
            if (subject != null) subject.text = $"Subject: {GameSession.SubjectId} / Attempt {GameSession.Attempt}/{GameSession.AttemptsPerUniverse}";
            resultScreen.style.display = DisplayStyle.Flex;
            resultScreen.BringToFront();
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
            root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            cover.parent.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            exitImage.Dispose();
            tunnel.RemoveFromHierarchy();
            advanceIcon.RemoveFromHierarchy();
            advanceLabel.RemoveFromHierarchy();
        }
    }
}
