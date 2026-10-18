using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string FullscreenKey = "Settings.Fullscreen";

        [SerializeField] private UIDocument document;
        [SerializeField] private InputSystemUIInputModule uiInputModule;
        [SerializeField] private string gameplaySceneName = "LoopScene";
        [Header("Channel transition")]
        [SerializeField, Range(0.05f, 0.4f)] private float channelSwitchDelay = 0.12f;
        [Header("Credits")]
        [SerializeField] private string designerProgrammerName = "Federico Bachis";
        [SerializeField] private string artistName = "Roberta Piredda";
        [SerializeField] private string pixelArtistName = "Andrea Serra";
        [SerializeField] private string soundMusicName = "Giuliano Cau";

        private VisualElement root;
        private VisualElement mainPanel;
        private InputAction menuSubmit;
        private InputActionAsset menuInputAsset;
        private InputActionReference menuSubmitReference;
        private InputActionReference originalSubmitReference;
        private VisualElement optionsPanel;
        private VisualElement[] previewPanels;
        private Button[] menuButtons;
        private Button returnFocus;
        private Slider masterVolume;
        private Slider musicVolume;
        private Slider effectsVolume;
        private Toggle fullscreen;
        private Label status;
        private bool editingRightPanel;
        private VisualElement previewColumn;
        private VisualElement channelOverlay;
        private VisualElement channelViewport;
        private VisualElement channelAperture;
        private VisualElement channelContent;
        private Label channelTitle;
        private Label channelStatus;
        private float channelWidth = 338f;
        private float channelHeight = 186f;
        private Coroutine channelTransition;
        private VisualElement pendingPanelFocus;
        private int displayedPreview = -1;
        private int requestedPreview = -1;
        private IVisualElementScheduledItem initialFocus;
        private readonly List<VisualElement> navigationElements = new List<VisualElement>();

        private void OnEnable()
        {
            if (document == null)
            {
                Debug.LogError("Assign the UIDocument to MainMenuController.", this);
                return;
            }

            root = document.rootVisualElement;
            mainPanel = root.Q("main-panel");
            editingRightPanel = false;
            mainPanel.SetEnabled(true);
            mainPanel.RemoveFromClassList("menu-locked");
            ConfigureSubmit();
            optionsPanel = root.Q("options-panel");
            previewColumn = root.Q(className: "art-column");
            previewColumn.focusable = true;
            channelViewport = root.Q("channel-viewport");
            channelAperture = root.Q("channel-aperture");
            channelContent = root.Q("channel-content");
            channelTitle = root.Q<Label>("channel-title");
            channelStatus = root.Q<Label>("channel-status");
            channelViewport.RegisterCallback<GeometryChangedEvent>(OnChannelGeometryChanged);
            displayedPreview = requestedPreview = -1;
            CreateChannelOverlay();
            previewPanels = new VisualElement[] { root.Q("welcome-panel"), optionsPanel, root.Q("credits-panel"), root.Q("quit-panel") };
            menuButtons = new Button[] { root.Q<Button>("new-game"), root.Q<Button>("options"), root.Q<Button>("credits"), root.Q<Button>("quit") };
            status = root.Q<Label>("status");
            masterVolume = root.Q<Slider>("volume");
            musicVolume = root.Q<Slider>("music-volume");
            effectsVolume = root.Q<Slider>("effects-volume");
            fullscreen = root.Q<Toggle>("fullscreen");
            root.Q<Label>("credit-designer-programmer").text = designerProgrammerName;
            root.Q<Label>("credit-artist").text = artistName;
            root.Q<Label>("credit-pixel-artist").text = pixelArtistName;
            root.Q<Label>("credit-sound-music").text = soundMusicName;

            menuButtons[0].clicked += StartNewGame;
            menuButtons[1].clicked += OpenOptions;
            menuButtons[3].clicked += OpenQuit;
            foreach (Button button in menuButtons)
            {
                button.RegisterCallback<PointerEnterEvent>(OnMenuPointerEnter);
                button.RegisterCallback<FocusInEvent>(OnMenuFocusIn);
            }
            optionsPanel.RegisterCallback<PointerDownEvent>(OnOptionsPointerDown, TrickleDown.TrickleDown);
            root.Q<Button>("options-back").clicked += CloseRightPanel;
            root.Q<Button>("quit-back").clicked += CloseRightPanel;
            root.Q<Button>("quit-confirm").clicked += QuitGame;
            root.RegisterCallback<NavigationCancelEvent>(OnCancel);
            root.RegisterCallback<NavigationMoveEvent>(OnNavigate);

            GameAudioSettings.ApplySavedVolume();
            masterVolume.SetValueWithoutNotify(GameAudioSettings.MasterVolume);
            musicVolume.SetValueWithoutNotify(GameAudioSettings.MusicVolume);
            effectsVolume.SetValueWithoutNotify(GameAudioSettings.EffectsVolume);
            bool savedFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            fullscreen.SetValueWithoutNotify(savedFullscreen);
            Screen.fullScreen = savedFullscreen;
            masterVolume.RegisterValueChangedCallback(OnMasterVolumeChanged);
            musicVolume.RegisterValueChangedCallback(OnMusicVolumeChanged);
            effectsVolume.RegisterValueChangedCallback(OnEffectsVolumeChanged);
            fullscreen.RegisterValueChangedCallback(OnFullscreenChanged);

            SelectMenuButton(menuButtons[0]);
            initialFocus = root.schedule.Execute(() => menuButtons[0].Focus());
        }

        private void OnDisable()
        {
            initialFocus?.Pause();
            if (root == null) return;
            CancelChannelTransition();
            channelViewport.UnregisterCallback<GeometryChangedEvent>(OnChannelGeometryChanged);
            channelOverlay.RemoveFromHierarchy();

            menuButtons[0].clicked -= StartNewGame;
            menuButtons[1].clicked -= OpenOptions;
            menuButtons[3].clicked -= OpenQuit;
            foreach (Button button in menuButtons)
            {
                button.UnregisterCallback<PointerEnterEvent>(OnMenuPointerEnter);
                button.UnregisterCallback<FocusInEvent>(OnMenuFocusIn);
            }
            optionsPanel.UnregisterCallback<PointerDownEvent>(OnOptionsPointerDown, TrickleDown.TrickleDown);
            root.Q<Button>("options-back").clicked -= CloseRightPanel;
            root.Q<Button>("quit-back").clicked -= CloseRightPanel;
            root.Q<Button>("quit-confirm").clicked -= QuitGame;
            root.UnregisterCallback<NavigationCancelEvent>(OnCancel);
            root.UnregisterCallback<NavigationMoveEvent>(OnNavigate);
            masterVolume.UnregisterValueChangedCallback(OnMasterVolumeChanged);
            musicVolume.UnregisterValueChangedCallback(OnMusicVolumeChanged);
            effectsVolume.UnregisterValueChangedCallback(OnEffectsVolumeChanged);
            fullscreen.UnregisterValueChangedCallback(OnFullscreenChanged);
            PlayerPrefs.Save();
            RestoreSubmit();
            root = null;
        }

        private void ConfigureSubmit()
        {
            if (uiInputModule == null) return;
            originalSubmitReference = uiInputModule.submit;
            menuInputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            menuInputAsset.name = "Main Menu Input";
            InputActionMap menuMap = menuInputAsset.AddActionMap("MainMenu");
            menuSubmit = menuMap.AddAction("Confirm", InputActionType.Button);
            menuSubmit.AddBinding("<Keyboard>/space");
            menuSubmit.AddBinding("<Gamepad>/buttonSouth");
            menuSubmit.AddBinding("<Joystick>/{Submit}");
            menuSubmit.AddBinding("<XRController>/{Submit}");
            menuSubmitReference = InputActionReference.Create(menuSubmit);
            uiInputModule.submit = menuSubmitReference;
            menuSubmit.Enable();
        }

        private void RestoreSubmit()
        {
            if (uiInputModule != null && menuSubmitReference != null)
                uiInputModule.submit = originalSubmitReference;
            menuSubmit?.Disable();
            menuSubmit?.Dispose();
            if (menuSubmitReference != null) Destroy(menuSubmitReference);
            if (menuInputAsset != null) Destroy(menuInputAsset);
            menuSubmit = null;
            menuSubmitReference = null;
            menuInputAsset = null;
            originalSubmitReference = null;
        }

        private void OnMenuPointerEnter(PointerEnterEvent pointerEvent)
        {
            if (editingRightPanel) return;
            Button button = pointerEvent.currentTarget as Button;
            if (button == null) return;
            SelectMenuButton(button);
            button.Focus();
        }

        private void OnMenuFocusIn(FocusInEvent focusEvent)
        {
            if (editingRightPanel) return;
            Button button = focusEvent.currentTarget as Button;
            if (button != null) SelectMenuButton(button);
        }

        private void SelectMenuButton(Button button)
        {
            if (editingRightPanel) return;
            SetRightControlsFocusable(false);
            SetMainNavigation();
            returnFocus = button;
            int previewIndex = System.Array.IndexOf(menuButtons, button);
            ShowPreview(previewIndex);
        }

        private void ShowPreview(int index, VisualElement focusAfterSwitch = null)
        {
            for (int buttonIndex = 0; buttonIndex < menuButtons.Length; buttonIndex++)
                menuButtons[buttonIndex].EnableInClassList("menu-selected", buttonIndex == index);
            if (requestedPreview == index && channelTransition != null)
            {
                if (focusAfterSwitch != null) pendingPanelFocus = focusAfterSwitch;
                return;
            }
            CancelChannelTransition();
            requestedPreview = index;
            pendingPanelFocus = focusAfterSwitch;
            if (displayedPreview < 0 || displayedPreview == index)
            {
                ApplyPreview(index);
                CompletePanelFocus();
                return;
            }
            channelTransition = StartCoroutine(ChangeChannel(index));
        }

        private void CreateChannelOverlay()
        {
            channelOverlay = new VisualElement { pickingMode = PickingMode.Ignore };
            channelOverlay.AddToClassList("channel-overlay");
            channelAperture.Add(channelOverlay);
        }

        private void OnChannelGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            if (geometryEvent.newRect.width <= 0f || geometryEvent.newRect.height <= 0f) return;
            channelWidth = Mathf.Round(geometryEvent.newRect.width);
            channelHeight = Mathf.Round(geometryEvent.newRect.height);
            channelContent.style.width = channelWidth;
            channelContent.style.height = channelHeight;
            if (channelTransition == null) SetChannelSize(channelWidth, channelHeight);
        }

        private void SetChannelSize(float width, float height)
        {
            float left = Mathf.Floor((channelWidth - width) * 0.5f);
            float top = Mathf.Floor((channelHeight - height) * 0.5f);
            channelAperture.style.left = left;
            channelAperture.style.top = top;
            channelAperture.style.width = Mathf.Round(width);
            channelAperture.style.height = Mathf.Round(height);
            channelContent.style.left = -left;
            channelContent.style.top = -top;
            channelContent.style.width = channelWidth;
            channelContent.style.height = channelHeight;
        }

        private IEnumerator AnimateChannelSize(float fromWidth, float toWidth, float fromHeight, float toHeight, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetChannelSize(Mathf.Lerp(fromWidth, toWidth, progress), Mathf.Lerp(fromHeight, toHeight, progress));
                yield return null;
            }
            SetChannelSize(toWidth, toHeight);
        }

        private void ApplyPreview(int index)
        {
            for (int panelIndex = 0; panelIndex < previewPanels.Length; panelIndex++)
                previewPanels[panelIndex].EnableInClassList("hidden", panelIndex != index);
            displayedPreview = index;
            string[] titles = { "WELCOME ROOM", "OPTIONS", "CREDITS", "QUIT" };
            string[] statuses = { "ENVIRONMENT READY", "PERSONAL SETTINGS", "TEAM CREDITS", "SESSION CONTROL" };
            channelTitle.text = titles[index];
            channelStatus.text = statuses[index];
        }

        private IEnumerator ChangeChannel(int index)
        {
            foreach (VisualElement panel in previewPanels) panel.SetEnabled(false);
            channelOverlay.style.display = DisplayStyle.Flex;
            channelOverlay.style.opacity = 0.85f;
            channelContent.style.opacity = 0f;
            yield return AnimateChannelSize(channelWidth, channelWidth, channelHeight, 2f, 0.16f);
            yield return AnimateChannelSize(channelWidth, 2f, 2f, 2f, 0.12f);
            channelOverlay.style.display = DisplayStyle.None;
            float elapsed = 0f;
            while (elapsed < channelSwitchDelay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            ApplyPreview(index);
            channelOverlay.style.display = DisplayStyle.Flex;
            channelOverlay.style.opacity = 1f;
            yield return AnimateChannelSize(2f, channelWidth, 2f, 2f, 0.12f);
            yield return AnimateChannelSize(channelWidth, channelWidth, 2f, channelHeight, 0.18f);
            const float revealDuration = 0.14f;
            elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / revealDuration);
                channelContent.style.opacity = progress;
                channelOverlay.style.opacity = 1f - progress;
                yield return null;
            }
            ResetChannelVisuals();
            channelTransition = null;
            CompletePanelFocus();
        }

        private void CancelChannelTransition()
        {
            if (channelTransition != null) StopCoroutine(channelTransition);
            channelTransition = null;
            pendingPanelFocus = null;
            if (previewPanels != null) ResetChannelVisuals();
        }

        private void ResetChannelVisuals()
        {
            channelOverlay.style.display = DisplayStyle.None;
            channelContent.style.opacity = 1f;
            SetChannelSize(channelWidth, channelHeight);
            foreach (VisualElement panel in previewPanels)
            {
                panel.style.opacity = 1f;
                panel.SetEnabled(true);
            }
        }

        private void CompletePanelFocus()
        {
            VisualElement target = pendingPanelFocus;
            pendingPanelFocus = null;
            if (editingRightPanel && target != null) target.Focus();
        }

        private void StartNewGame()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName) || !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                status.text = "The next session is currently unavailable.";
                return;
            }
            GameJamPlus.Gameplay.GameSession.Reset();
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OpenOptions()
        {
            EnterRightPanel(1, masterVolume, musicVolume, effectsVolume, fullscreen, root.Q<Button>("options-back"));
        }

        private void OnOptionsPointerDown(PointerDownEvent pointerEvent)
        {
            if (!editingRightPanel && pointerEvent.button == 0) OpenOptions();
        }

        private void OpenQuit()
        {
            EnterRightPanel(3, root.Q<Button>("quit-back"), root.Q<Button>("quit-confirm"));
        }

        private void EnterRightPanel(int previewIndex, params VisualElement[] controls)
        {
            returnFocus = menuButtons[previewIndex];
            editingRightPanel = true;
            mainPanel.AddToClassList("menu-locked");
            mainPanel.SetEnabled(false);
            SetRightControlsFocusable(true);
            navigationElements.Clear();
            navigationElements.AddRange(controls);
            ShowPreview(previewIndex, controls[0]);
            if (channelTransition != null) previewColumn.Focus();
        }

        private void CloseRightPanel()
        {
            PlayerPrefs.Save();
            pendingPanelFocus = null;
            editingRightPanel = false;
            mainPanel.SetEnabled(true);
            mainPanel.RemoveFromClassList("menu-locked");
            SetRightControlsFocusable(false);
            SetMainNavigation();
            returnFocus.Focus();
        }

        private void SetRightControlsFocusable(bool value)
        {
            masterVolume.focusable = value;
            musicVolume.focusable = value;
            effectsVolume.focusable = value;
            fullscreen.focusable = value;
            root.Q<Button>("options-back").focusable = value;
            root.Q<Button>("quit-back").focusable = value;
            root.Q<Button>("quit-confirm").focusable = value;
        }

        private void SetMainNavigation()
        {
            navigationElements.Clear();
            navigationElements.AddRange(menuButtons);
        }

        private void OnNavigate(NavigationMoveEvent navigationEvent)
        {
            if (editingRightPanel && channelTransition != null)
            {
                root.focusController.IgnoreEvent(navigationEvent);
                navigationEvent.StopPropagation();
                return;
            }
            VisualElement focused = root.focusController.focusedElement as VisualElement;
            if (!editingRightPanel && navigationEvent.direction == NavigationMoveEvent.Direction.Right && focused == menuButtons[1])
            {
                OpenOptions();
                root.focusController.IgnoreEvent(navigationEvent);
                navigationEvent.StopPropagation();
                return;
            }

            int step;
            switch (navigationEvent.direction)
            {
                case NavigationMoveEvent.Direction.Up: step = -1; break;
                case NavigationMoveEvent.Direction.Down: step = 1; break;
                default: return;
            }
            int index = navigationElements.FindIndex(element => element == focused || element.Contains(focused));
            int nextIndex = index < 0 ? 0 : (index + step + navigationElements.Count) % navigationElements.Count;
            navigationElements[nextIndex].Focus();
            root.focusController.IgnoreEvent(navigationEvent);
            navigationEvent.StopPropagation();
        }

        private void OnCancel(NavigationCancelEvent cancelEvent)
        {
            if (editingRightPanel) CloseRightPanel();
            else OpenQuit();
            cancelEvent.StopPropagation();
        }

        private void OnMasterVolumeChanged(ChangeEvent<float> changeEvent) => GameAudioSettings.MasterVolume = changeEvent.newValue;
        private void OnMusicVolumeChanged(ChangeEvent<float> changeEvent) => GameAudioSettings.MusicVolume = changeEvent.newValue;
        private void OnEffectsVolumeChanged(ChangeEvent<float> changeEvent) => GameAudioSettings.EffectsVolume = changeEvent.newValue;

        private void OnFullscreenChanged(ChangeEvent<bool> changeEvent)
        {
            Screen.fullScreen = changeEvent.newValue;
            PlayerPrefs.SetInt(FullscreenKey, changeEvent.newValue ? 1 : 0);
        }

        private void QuitGame()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
