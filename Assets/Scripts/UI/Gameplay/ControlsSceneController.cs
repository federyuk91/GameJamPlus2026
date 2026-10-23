using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class ControlsSceneController : MonoBehaviour
    {
        public const string SceneName = "Controls";
        private static string requestedGameplayScene;
        [SerializeField] private UIDocument document;
        [SerializeField] private string gameplaySceneName = "Universo_10";
        private BodyControlsPageView overview;
        private InputDevicePrompts prompts;
        private InputAction confirm;
        private Button continueButton;
        private VisualElement navigationHelp;
        private Label status;
        private int openedFrame;
        private bool loading;
        private bool waitingForConfirmRelease;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRequest() => requestedGameplayScene = null;

        public static void OpenBeforeGameplay(string destination)
        {
            requestedGameplayScene = destination;
            SceneManager.LoadScene(SceneName);
        }

        private void OnEnable()
        {
            if (document == null) document = GetComponent<UIDocument>();
            Time.timeScale = 1f;
            if (!string.IsNullOrEmpty(requestedGameplayScene))
            {
                gameplaySceneName = requestedGameplayScene;
                requestedGameplayScene = null;
            }
            VisualElement root = document.rootVisualElement;
            overview = new BodyControlsPageView(root.Q("controls-overview"));
            continueButton = root.Q<Button>("controls-continue");
            navigationHelp = root.Q("controls-navigation-help");
            status = root.Q<Label>("controls-status");
            root.Query<VisualElement>().ForEach(element => element.focusable = false);
            continueButton.clicked += Continue;
            prompts = new InputDevicePrompts();
            prompts.Changed += RefreshDevice;
            RefreshDevice();
            confirm = new InputAction("Controls Continue", InputActionType.Button);
            confirm.AddBinding("<Keyboard>/space");
            confirm.AddBinding("<Gamepad>/buttonSouth");
            confirm.Enable();
            openedFrame = Time.frameCount;
            waitingForConfirmRelease = true;
            loading = false;
        }

        private void RefreshDevice()
        {
            Dictionary<string, string> tokens = InputDevicePrompts.CreateControlsTokens(prompts);
            overview.Render(prompts, tokens);
            InputPromptElement.BindAll(navigationHelp, prompts);
        }

        private void Update()
        {
            if (loading || Time.frameCount <= openedFrame) return;
            if (waitingForConfirmRelease)
            {
                waitingForConfirmRelease = confirm.IsPressed();
                return;
            }
            if (confirm.WasPressedThisFrame()) Continue();
        }

        private void Continue()
        {
            if (loading || Time.frameCount <= openedFrame) return;
            if (string.IsNullOrWhiteSpace(gameplaySceneName) || !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                status.text = "Scene unavailable: " + gameplaySceneName;
                return;
            }
            loading = true;
            PlayerPrefs.Save();
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OnDisable()
        {
            continueButton.clicked -= Continue;
            prompts.Changed -= RefreshDevice;
            prompts.Dispose();
            confirm.Dispose();
        }
    }
}
