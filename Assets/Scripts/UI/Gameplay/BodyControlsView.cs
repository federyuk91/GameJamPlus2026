using System;
using System.Collections.Generic;
using MousePlatformer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class BodyControlsView : IDisposable
    {
        private readonly BodyControlsPageView[] pages = new BodyControlsPageView[5];
        private readonly Label feedback;
        private readonly BodyControlsBindingView bindingView;
        private readonly Button[] tabs = new Button[5];
        private readonly VisualElement help;
        private InputDevicePrompts prompts;
        private bool usingGamepad;
        private readonly Button previous;
        private readonly Button next;
        private readonly Button reset;
        private readonly Dictionary<Button, Action> actions = new Dictionary<Button, Action>();
        private readonly Button[] choiceButtons = new Button[3];
        private InputControl pendingControl;
        private PlayerBodyAction captureBodyAction;
        private bool captureGamepad;
        private bool awaitingConfirmation;
        private int choiceIndex;
        private float nextChoiceTime;
        private InputAction captureAction;
        private InputActionRebindingExtensions.RebindingOperation capture;
        private int blockedThroughFrame = -1;
        private int page;
        public bool BlocksNavigation => capture != null || awaitingConfirmation || Time.frameCount <= blockedThroughFrame;
        public event Action PageChanged;
        public IEnumerable<Button> AllButtons => actions.Keys;

        public BodyControlsView(VisualElement host, BodyControlsSettings configuration)
        {
            if (configuration == null || configuration.Layout == null)
                throw new ArgumentException("Assign the shared BodyControls layout in BodyControlsSettings.", nameof(configuration));
            if (host.Q("body-title") == null) configuration.Layout.CloneTree(host);
            for (int index = 0; index < pages.Length; index++)
                pages[index] = new BodyControlsPageView(host.Q("body-page-" + index));
            feedback = host.Q<Label>("body-feedback");
            previous = Bind(host.Q<Button>("body-previous"), () => Step(-1));
            next = Bind(host.Q<Button>("body-next"), () => Step(1));
            for (int index = 0; index < tabs.Length; index++)
            {
                int tabIndex = index;
                tabs[index] = Bind(host.Q<Button>("body-tab-" + index), () => ActivateTab(tabIndex));
                tabs[index].RegisterCallback<PointerEnterEvent>(OnTabHover);
            }
            reset = Bind(host.Q<Button>("body-reset"), RestoreDefaults);
            help = host.Q("body-help");
            bindingView = new BodyControlsBindingView(host.Q("body-binding-modal"));
            choiceButtons[0] = Bind(bindingView.Yes, AcceptBinding);
            choiceButtons[1] = Bind(bindingView.No, RetryBinding);
            choiceButtons[2] = Bind(bindingView.Cancel, CancelCapture);
            PlayerControlBindings.Changed += RefreshBindings;
            Show();
        }

        private Button Bind(Button button, Action action)
        {
            button.focusable = false;
            button.clicked += action;
            actions.Add(button, action);
            return button;
        }

        public void Activate(Button button)
        {
            if (!BlocksNavigation && actions.TryGetValue(button, out Action action)) action();
        }

        public List<VisualElement> VisibleButtons()
        {
            List<VisualElement> buttons = new List<VisualElement>();
            buttons.Add(tabs[page]);
            buttons.Add(previous);
            for (int index = 0; index < tabs.Length; index++)
                if (index != page) buttons.Add(tabs[index]);
            buttons.Add(next);
            buttons.Add(reset);
            return buttons;
        }

        public int FindVerticalSelection(IReadOnlyList<VisualElement> elements, int currentIndex, float direction)
        {
            Vector2 origin = elements[currentIndex].worldBound.center;
            int bestIndex = currentIndex;
            float bestDistance = float.PositiveInfinity;
            for (int index = 0; index < elements.Count; index++)
            {
                VisualElement candidate = elements[index];
                if (candidate == previous || candidate == next) continue;
                bool inactiveTab = false;
                for (int tab = 0; tab < tabs.Length; tab++)
                    if (candidate == tabs[tab] && tab != page) inactiveTab = true;
                if (inactiveTab) continue;
                Vector2 offset = candidate.worldBound.center - origin;
                float verticalDistance = direction > 0f ? -offset.y : offset.y;
                // Ignore controls on the same row; Left/Right already handles the tabs.
                if (verticalDistance <= Mathf.Max(4f, elements[currentIndex].worldBound.height * 0.5f)) continue;
                float distance = verticalDistance * 1000f + Mathf.Abs(offset.x);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestIndex = index;
            }
            return bestIndex;
        }

        public void Show() { CancelCapture(); page = 0; RefreshPage(); }
        public bool IsPageNavigation(VisualElement element)
        {
            if (element == previous || element == next) return true;
            foreach (Button tab in tabs)
                if (element == tab) return true;
            return false;
        }

        public void Step(int direction)
        {
            if (BlocksNavigation) return;
            SelectPage((page + direction + pages.Length) % pages.Length);
        }

        private void SelectPage(int index)
        {
            if (BlocksNavigation) return;
            page = index;
            RefreshPage(); PageChanged?.Invoke();
        }

        private void ActivateTab(int index)
        {
            if (BlocksNavigation) return;
            SelectPage(index);
            if (index > 0) BeginCapture(usingGamepad);
        }

        private void OnTabHover(PointerEnterEvent pointerEvent)
        {
            if (BlocksNavigation) return;
            for (int index = 0; index < tabs.Length; index++)
            {
                if (pointerEvent.currentTarget != tabs[index]) continue;
                if (page != index) SelectPage(index);
                return;
            }
        }

        public void SetDevice(InputDevicePrompts devicePrompts)
        {
            prompts = devicePrompts;
            // Keep the capture tied to the device selected when it started.
            if (capture != null || awaitingConfirmation) return;
            usingGamepad = prompts.UsingGamepad;
            RefreshBindings();
        }

        private void RefreshPage()
        {
            for (int index = 0; index < pages.Length; index++) pages[index].SetVisible(index == page);
            for (int index = 0; index < tabs.Length; index++)
                tabs[index].EnableInClassList("body-tab-active", index == page);
            feedback.text = string.Empty;
            RefreshBindings();
        }

        public void RefreshBindings()
        {
            Dictionary<string, string> tokens = InputDevicePrompts.CreateControlsTokens(prompts);
            foreach (BodyControlsPageView pageView in pages) pageView.Render(prompts, tokens);
            InputPromptElement.BindAll(help, prompts);
            bindingView.SetDevice(prompts);
        }

        private string PageTitle => tabs[page].Q<Label>().text;

        private string Binding(PlayerBodyAction action)
        {
            return prompts != null ? prompts.BodyBinding(action)
                : PlayerControlBindings.DisplayName(action, usingGamepad, Keyboard.current);
        }

        private void BeginCapture(bool controller)
        {
            if (page == 0 || BlocksNavigation) return;
            captureBodyAction = (PlayerBodyAction)(page - 1);
            captureGamepad = controller;
            pendingControl = null;
            bindingView.ShowListening(PageTitle, controller);
            captureAction = new InputAction("Rebind body control", InputActionType.Button);
            captureAction.AddBinding(controller ? "<Gamepad>/buttonNorth" : "<Keyboard>/f");
            capture = captureAction.PerformInteractiveRebinding(0)
                .WithExpectedControlType("Button")
                .WithControlsHavingToMatchPath(controller ? "<Gamepad>" : "<Keyboard>")
                .WithCancelingThrough(controller ? "<Gamepad>/select" : "<Keyboard>/escape")
                .WithMatchingEventsBeingSuppressed()
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation =>
                {
                    pendingControl = operation.selectedControl;
                    awaitingConfirmation = true;
                    FinishCapture();
                    bindingView.ShowConfirmation(InputDevicePrompts.FromControl(pendingControl), PageTitle);
                    SelectChoice(0);
                })
                .OnCancel(operation => { FinishCapture(); CloseBindingModal("Binding unchanged."); });
            capture.Start();
        }

        private void FinishCapture()
        {
            capture?.Dispose(); capture = null;
            captureAction?.Dispose(); captureAction = null;
            blockedThroughFrame = Time.frameCount + 1;
            if (!awaitingConfirmation && prompts != null) usingGamepad = prompts.UsingGamepad;
            RefreshBindings();
        }

        public bool HandleModalInput(Vector2 direction, bool confirmPressed, bool cancelPressed)
        {
            if (!BlocksNavigation) return false;
            if (Time.frameCount <= blockedThroughFrame) return true;
            if (cancelPressed) { CancelCapture(); return true; }
            if (!awaitingConfirmation) return true;
            if (direction.sqrMagnitude < 0.2f) nextChoiceTime = 0f;
            else if (Time.unscaledTime >= nextChoiceTime)
            {
                nextChoiceTime = Time.unscaledTime + 0.2f;
                int step = Mathf.Abs(direction.x) > 0.5f ? (direction.x > 0f ? 1 : -1) : (direction.y < 0f ? 1 : -1);
                SelectChoice((choiceIndex + step + choiceButtons.Length) % choiceButtons.Length);
            }
            if (confirmPressed) actions[choiceButtons[choiceIndex]]();
            return true;
        }

        private void SelectChoice(int index)
        {
            choiceIndex = index;
            for (int choice = 0; choice < choiceButtons.Length; choice++)
                choiceButtons[choice].EnableInClassList("pause-selected", choice == index);
        }

        private void AcceptBinding()
        {
            if (!awaitingConfirmation || Time.frameCount <= blockedThroughFrame) return;
            if (PlayerControlBindings.TrySet(captureBodyAction, captureGamepad, pendingControl, out string message))
                CloseBindingModal(message);
            else bindingView.ShowError(message);
        }

        private void RetryBinding()
        {
            if (!awaitingConfirmation || Time.frameCount <= blockedThroughFrame) return;
            awaitingConfirmation = false;
            BeginCapture(captureGamepad);
        }

        private void CloseBindingModal(string message)
        {
            awaitingConfirmation = false;
            pendingControl = null;
            bindingView.Close();
            blockedThroughFrame = Time.frameCount + 1;
            feedback.text = message;
            if (prompts != null) usingGamepad = prompts.UsingGamepad;
            RefreshBindings();
        }

        public void CancelCapture()
        {
            if (capture != null) capture.Cancel();
            else if (awaitingConfirmation) CloseBindingModal("Binding unchanged.");
        }
        private void RestoreDefaults()
        {
            if (BlocksNavigation) return;
            PlayerControlBindings.RestoreDefaults(); feedback.text = "Default bindings restored.";
        }

        public void Dispose()
        {
            CancelCapture();
            PlayerControlBindings.Changed -= RefreshBindings;
            foreach (Button tab in tabs) tab.UnregisterCallback<PointerEnterEvent>(OnTabHover);
            foreach (KeyValuePair<Button, Action> entry in actions) entry.Key.clicked -= entry.Value;
        }
    }
}
