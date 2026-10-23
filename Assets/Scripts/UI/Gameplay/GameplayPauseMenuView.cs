using System;
using System.Collections.Generic;
using MousePlatformer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class GameplayPauseMenuView : IDisposable
    {
        private readonly VisualElement overlay;
        private readonly VisualElement mainPage;
        private readonly VisualElement controlsPage;
        private readonly VisualElement optionsPage;
        private readonly BodyControlsView controlsView;
        private readonly VisualElement card;
        private readonly Label hint;
        private readonly Label status;
        private readonly Slider master;
        private readonly Slider music;
        private readonly Slider effects;
        private readonly Toggle fullscreen;
        private readonly PlayerInputReader playerInput;
        private readonly Func<bool> canPause;
        private readonly InputAction pause;
        private readonly InputAction navigate;
        private readonly InputAction confirm;
        private readonly InputAction cancel;
        private readonly List<VisualElement> selection = new List<VisualElement>();
        private readonly Dictionary<Button, Action> buttonActions = new Dictionary<Button, Action>();
        private readonly List<VisualElement> interactiveElements = new List<VisualElement>();
        private int selectedIndex;
        private float nextNavigationTime;
        private float previousTimeScale;
        private bool playerInputWasEnabled;
        private bool usingGamepad;
        private string confirmKey = "Space";
        private string cancelKey = "Esc";
        private string pauseKey = "Esc";
        public bool IsOpen { get; private set; }
        public event Action<bool> Changed;

        public GameplayPauseMenuView(VisualElement root, PlayerInputReader input, Func<bool> canOpen, BodyControlsSettings controlsSettings)
        {
            overlay = root.Q("pause-overlay");
            mainPage = root.Q("pause-main");
            controlsPage = root.Q("pause-controls");
            optionsPage = root.Q("pause-options");
            card = overlay.Q(className: "pause-card");
            controlsView = new BodyControlsView(root.Q("pause-controls-content"), controlsSettings);
            controlsView.PageChanged += RefreshControlsSelection;
            hint = root.Q<Label>("pause-hint");
            status = root.Q<Label>("pause-status");
            master = root.Q<Slider>("pause-master");
            music = root.Q<Slider>("pause-music");
            effects = root.Q<Slider>("pause-effects");
            fullscreen = root.Q<Toggle>("pause-fullscreen");
            bool savedFullscreen = PlayerPrefs.GetInt("Settings.Fullscreen", 1) == 1;
            fullscreen.SetValueWithoutNotify(savedFullscreen);
            Screen.fullScreen = savedFullscreen;
            playerInput = input;
            canPause = canOpen;
            overlay.pickingMode = PickingMode.Position;
            overlay.Query<VisualElement>().ForEach(element =>
            {
                element.pickingMode = PickingMode.Position;
                element.focusable = false;
            });
            Bind(root.Q<Button>("pause-resume"), Resume);
            Bind(root.Q<Button>("pause-show-controls"), ShowControls);
            Bind(root.Q<Button>("pause-show-options"), ShowOptions);
            Bind(root.Q<Button>("pause-main-menu"), ReturnToMainMenu);
            Bind(root.Q<Button>("pause-quit"), QuitGame);
            Bind(root.Q<Button>("pause-controls-back"), ShowMain);
            Bind(root.Q<Button>("pause-options-back"), ShowMain);
            foreach (Button button in controlsView.AllButtons) RegisterHover(button);
            foreach (VisualElement element in new VisualElement[] { master, music, effects, fullscreen }) RegisterHover(element);
            master.RegisterValueChangedCallback(OnMasterChanged);
            music.RegisterValueChangedCallback(OnMusicChanged);
            effects.RegisterValueChangedCallback(OnEffectsChanged);
            fullscreen.RegisterValueChangedCallback(OnFullscreenChanged);

            pause = new InputAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");
            navigate = new InputAction("Pause Navigate", InputActionType.Value, expectedControlType: "Vector2");
            navigate.AddBinding("<Gamepad>/leftStick").WithProcessor("stickDeadzone");
            navigate.AddBinding("<Gamepad>/dpad");
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/downArrow").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/leftArrow").With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/rightArrow").With("Right", "<Keyboard>/d");
            confirm = new InputAction("Pause Confirm", InputActionType.Button);
            confirm.AddBinding("<Keyboard>/space");
            confirm.AddBinding("<Gamepad>/buttonSouth");
            cancel = new InputAction("Pause Back", InputActionType.Button);
            cancel.AddBinding("<Gamepad>/buttonEast");
            pause.Enable();
            overlay.style.display = DisplayStyle.None;
        }

        private void Bind(Button button, Action action)
        {
            buttonActions.Add(button, action);
            button.clicked += action;
            RegisterHover(button);
        }

        private void RegisterHover(VisualElement element)
        {
            interactiveElements.Add(element);
            element.RegisterCallback<PointerEnterEvent>(OnHover);
        }

        private void OnHover(PointerEnterEvent pointerEvent)
        {
            int index = selection.IndexOf(pointerEvent.currentTarget as VisualElement);
            if (IsOpen && index >= 0) Select(index);
        }

        public void SetDevice(InputDevicePrompts prompts)
        {
            usingGamepad = prompts.UsingGamepad;
            confirmKey = prompts.Confirm;
            cancelKey = prompts.Cancel;
            pauseKey = prompts.Pause;
            controlsView.SetDevice(prompts);
            RefreshHint();
        }

        public void Tick()
        {
            if (IsOpen && controlsPage.style.display.value != DisplayStyle.None
                && controlsView.HandleModalInput(navigate.ReadValue<Vector2>(), confirm.WasPressedThisFrame(),
                    cancel.WasPressedThisFrame() || pause.WasPressedThisFrame())) return;
            if (pause.WasPressedThisFrame())
            {
                if (!IsOpen) Open();
                else if (mainPage.style.display.value != DisplayStyle.None
                    || !(pause.activeControl?.device is Keyboard)) Resume();
                else ShowMain();
                return;
            }
            if (!IsOpen) return;
            if (cancel.WasPressedThisFrame())
            {
                if (mainPage.style.display.value == DisplayStyle.None) ShowMain();
                else Resume();
                return;
            }
            Vector2 direction = navigate.ReadValue<Vector2>();
            if (direction.sqrMagnitude < 0.2f) nextNavigationTime = 0f;
            else if (Time.unscaledTime >= nextNavigationTime)
            {
                nextNavigationTime = Time.unscaledTime + 0.18f;
                if (Mathf.Abs(direction.y) > 0.5f)
                    Select(controlsPage.style.display.value != DisplayStyle.None
                        ? controlsView.FindVerticalSelection(selection, selectedIndex, direction.y)
                        : (selectedIndex + (direction.y > 0f ? -1 : 1) + selection.Count) % selection.Count);
                else if (controlsPage.style.display.value != DisplayStyle.None)
                {
                    if (controlsView.IsPageNavigation(selection[selectedIndex])) controlsView.Step(direction.x > 0f ? 1 : -1);
                }
                else if (selection[selectedIndex] is Slider slider)
                    slider.value = Mathf.Clamp01(slider.value + Mathf.Sign(direction.x) * 0.05f);
            }
            if (!confirm.WasPressedThisFrame()) return;
            VisualElement selected = selection[selectedIndex];
            if (selected is Button button)
            {
                if (buttonActions.TryGetValue(button, out Action action)) action();
                else controlsView.Activate(button);
            }
            else if (selected is Toggle toggle) toggle.value = !toggle.value;
        }

        private void Open()
        {
            if (Time.timeScale <= 0f || !canPause()) return;
            previousTimeScale = Time.timeScale;
            playerInputWasEnabled = playerInput != null && playerInput.enabled;
            if (playerInputWasEnabled) playerInput.enabled = false;
            IsOpen = true;
            Time.timeScale = 0f;
            overlay.style.display = DisplayStyle.Flex;
            status.text = string.Empty;
            navigate.Enable(); confirm.Enable(); cancel.Enable();
            ShowMain();
            Changed?.Invoke(true);
        }

        public void Resume()
        {
            if (!IsOpen) return;
            controlsView.CancelCapture();
            IsOpen = false;
            overlay.style.display = DisplayStyle.None;
            navigate.Disable(); confirm.Disable(); cancel.Disable();
            if (playerInputWasEnabled && playerInput != null) playerInput.enabled = true;
            Time.timeScale = previousTimeScale;
            PlayerPrefs.Save();
            Changed?.Invoke(false);
        }

        private void ShowMain()
        {
            ShowPage(mainPage);
            List<VisualElement> buttons = new List<VisualElement>();
            foreach (VisualElement element in mainPage.Children())
                if (element is Button) buttons.Add(element);
            SetSelection(buttons.ToArray());
        }

        private void ShowControls()
        {
            ShowPage(controlsPage);
            controlsView.Show();
            RefreshControlsSelection();
        }

        private void RefreshControlsSelection()
        {
            List<VisualElement> buttons = controlsView.VisibleButtons();
            buttons.Add(controlsPage.Q<Button>("pause-controls-back"));
            float navigationTime = nextNavigationTime;
            SetSelection(buttons.ToArray());
            nextNavigationTime = navigationTime;
        }

        private void ShowOptions()
        {
            master.SetValueWithoutNotify(GameAudioSettings.MasterVolume);
            music.SetValueWithoutNotify(GameAudioSettings.MusicVolume);
            effects.SetValueWithoutNotify(GameAudioSettings.EffectsVolume);
            fullscreen.SetValueWithoutNotify(PlayerPrefs.GetInt("Settings.Fullscreen", 1) == 1);
            ShowPage(optionsPage);
            SetSelection(master, music, effects, fullscreen, optionsPage.Q<Button>("pause-options-back"));
        }

        private void ShowPage(VisualElement page)
        {
            if (page != controlsPage) controlsView.CancelCapture();
            card.EnableInClassList("controls-open", page == controlsPage);
            mainPage.style.display = page == mainPage ? DisplayStyle.Flex : DisplayStyle.None;
            controlsPage.style.display = page == controlsPage ? DisplayStyle.Flex : DisplayStyle.None;
            optionsPage.style.display = page == optionsPage ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshHint();
        }

        private void RefreshHint()
        {
            bool main = mainPage.style.display.value != DisplayStyle.None;
            hint.text = main
                ? (usingGamepad ? $"{pauseKey} / {cancelKey} - Resume   {confirmKey} - Confirm" : "Esc - Resume   Space - Confirm")
                : $"{cancelKey} - Back   {confirmKey} - Confirm";
        }

        private void SetSelection(params VisualElement[] elements)
        {
            foreach (VisualElement element in selection) element.RemoveFromClassList("pause-selected");
            selection.Clear(); selection.AddRange(elements);
            nextNavigationTime = 0f;
            Select(0);
        }

        private void Select(int index)
        {
            foreach (VisualElement element in selection) element.RemoveFromClassList("pause-selected");
            selectedIndex = index;
            selection[index].AddToClassList("pause-selected");
        }

        private void OnMasterChanged(ChangeEvent<float> changeEvent) => GameAudioSettings.MasterVolume = changeEvent.newValue;
        private void OnMusicChanged(ChangeEvent<float> changeEvent) => GameAudioSettings.MusicVolume = changeEvent.newValue;
        private void OnEffectsChanged(ChangeEvent<float> changeEvent) => GameAudioSettings.EffectsVolume = changeEvent.newValue;
        private void OnFullscreenChanged(ChangeEvent<bool> changeEvent)
        {
            Screen.fullScreen = changeEvent.newValue;
            PlayerPrefs.SetInt("Settings.Fullscreen", changeEvent.newValue ? 1 : 0);
        }

        private void ReturnToMainMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                status.text = "Main menu is unavailable.";
                return;
            }
            Resume();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void QuitGame()
        {
            Resume();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Dispose()
        {
            Resume();
            controlsView.PageChanged -= RefreshControlsSelection;
            controlsView.Dispose();
            pause.Dispose(); navigate.Dispose(); confirm.Dispose(); cancel.Dispose();
            foreach (KeyValuePair<Button, Action> entry in buttonActions) entry.Key.clicked -= entry.Value;
            foreach (VisualElement element in interactiveElements) element.UnregisterCallback<PointerEnterEvent>(OnHover);
            master.UnregisterValueChangedCallback(OnMasterChanged);
            music.UnregisterValueChangedCallback(OnMusicChanged);
            effects.UnregisterValueChangedCallback(OnEffectsChanged);
            fullscreen.UnregisterValueChangedCallback(OnFullscreenChanged);
        }
    }
}
