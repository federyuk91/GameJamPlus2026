using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string VolumeKey = "Settings.MasterVolume";
        private const string FullscreenKey = "Settings.Fullscreen";

        [SerializeField] private UIDocument document;
        [SerializeField] private string gameplaySceneName = "LoopScene";
        [SerializeField, TextArea] private string creditsText =
            "PLEASE PRESS CONTINUE\n\nSviluppo • Il tuo team\nGrafica • Il tuo team\nMusica e suoni • Il tuo team\n\nGrazie per la visita.";

        private VisualElement root;
        private VisualElement mainPanel;
        private VisualElement modal;
        private VisualElement activePanel;
        private Button returnFocus;
        private Slider volume;
        private Toggle fullscreen;
        private Label status;
        private IVisualElementScheduledItem initialFocus;
        private readonly List<VisualElement> navigationElements = new List<VisualElement>();

        private void OnEnable()
        {
            if (document == null)
            {
                Debug.LogError("Assegna il UIDocument al MainMenuController.", this);
                return;
            }

            root = document.rootVisualElement;
            mainPanel = root.Q("main-panel");
            modal = root.Q("modal");
            status = root.Q<Label>("status");
            volume = root.Q<Slider>("volume");
            fullscreen = root.Q<Toggle>("fullscreen");
            root.Q<Label>(className: "credits-text").text = creditsText;

            root.Q<Button>("new-game").clicked += StartNewGame;
            root.Q<Button>("options").clicked += OpenOptions;
            root.Q<Button>("credits").clicked += OpenCredits;
            root.Q<Button>("quit").clicked += OpenQuit;
            root.Q<Button>("options-back").clicked += CloseModal;
            root.Q<Button>("credits-back").clicked += CloseModal;
            root.Q<Button>("quit-back").clicked += CloseModal;
            root.Q<Button>("quit-confirm").clicked += QuitGame;
            root.RegisterCallback<NavigationCancelEvent>(OnCancel);
            root.RegisterCallback<NavigationMoveEvent>(OnNavigate);

            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            volume.SetValueWithoutNotify(AudioListener.volume);
            bool savedFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            fullscreen.SetValueWithoutNotify(savedFullscreen);
            Screen.fullScreen = savedFullscreen;
            volume.RegisterValueChangedCallback(OnVolumeChanged);
            fullscreen.RegisterValueChangedCallback(OnFullscreenChanged);

            SetMainNavigation();
            initialFocus = root.schedule.Execute(() => root.Q<Button>("new-game").Focus());
        }

        private void OnDisable()
        {
            initialFocus?.Pause();
            if (root == null)
                return;

            root.Q<Button>("new-game").clicked -= StartNewGame;
            root.Q<Button>("options").clicked -= OpenOptions;
            root.Q<Button>("credits").clicked -= OpenCredits;
            root.Q<Button>("quit").clicked -= OpenQuit;
            root.Q<Button>("options-back").clicked -= CloseModal;
            root.Q<Button>("credits-back").clicked -= CloseModal;
            root.Q<Button>("quit-back").clicked -= CloseModal;
            root.Q<Button>("quit-confirm").clicked -= QuitGame;
            root.UnregisterCallback<NavigationCancelEvent>(OnCancel);
            root.UnregisterCallback<NavigationMoveEvent>(OnNavigate);
            volume.UnregisterValueChangedCallback(OnVolumeChanged);
            fullscreen.UnregisterValueChangedCallback(OnFullscreenChanged);
            PlayerPrefs.Save();
        }

        private void StartNewGame()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName) || !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                status.text = "Assegna una scena di gioco inclusa nei Build Profiles.";
                return;
            }

            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OpenOptions()
        {
            ShowModal("options-panel", "options", volume, fullscreen, root.Q<Button>("options-back"));
        }

        private void OpenCredits()
        {
            ShowModal("credits-panel", "credits", root.Q<Button>("credits-back"));
        }

        private void OpenQuit()
        {
            ShowModal("quit-panel", "quit", root.Q<Button>("quit-back"), root.Q<Button>("quit-confirm"));
        }

        private void ShowModal(string panelName, string sourceButtonName, params VisualElement[] controls)
        {
            returnFocus = root.Q<Button>(sourceButtonName);
            activePanel = root.Q(panelName);
            mainPanel.SetEnabled(false);
            modal.RemoveFromClassList("hidden");
            activePanel.RemoveFromClassList("hidden");
            navigationElements.Clear();
            navigationElements.AddRange(controls);
            controls[0].Focus();
        }

        private void CloseModal()
        {
            if (activePanel == null)
                return;

            PlayerPrefs.Save();
            activePanel.AddToClassList("hidden");
            activePanel = null;
            modal.AddToClassList("hidden");
            mainPanel.SetEnabled(true);
            SetMainNavigation();
            returnFocus.Focus();
        }

        private void SetMainNavigation()
        {
            navigationElements.Clear();
            navigationElements.Add(root.Q<Button>("new-game"));
            navigationElements.Add(root.Q<Button>("options"));
            navigationElements.Add(root.Q<Button>("credits"));
            navigationElements.Add(root.Q<Button>("quit"));
        }

        private void OnNavigate(NavigationMoveEvent navigationEvent)
        {
            int step;
            switch (navigationEvent.direction)
            {
                case NavigationMoveEvent.Direction.Up:
                    step = -1;
                    break;
                case NavigationMoveEvent.Direction.Down:
                    step = 1;
                    break;
                default:
                    return;
            }

            VisualElement focused = root.focusController.focusedElement as VisualElement;
            int index = navigationElements.FindIndex(element => element == focused || element.Contains(focused));
            int nextIndex = index < 0 ? 0 : (index + step + navigationElements.Count) % navigationElements.Count;
            navigationElements[nextIndex].Focus();
            root.focusController.IgnoreEvent(navigationEvent);
            navigationEvent.StopPropagation();
        }

        private void OnCancel(NavigationCancelEvent cancelEvent)
        {
            if (activePanel == null)
                OpenQuit();
            else
                CloseModal();

            cancelEvent.StopPropagation();
        }

        private void OnVolumeChanged(ChangeEvent<float> changeEvent)
        {
            AudioListener.volume = changeEvent.newValue;
            PlayerPrefs.SetFloat(VolumeKey, changeEvent.newValue);
        }

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
