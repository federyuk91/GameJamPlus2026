using System;
using System.Collections;
using System.Collections.Generic;
using GameJamPlus.Gameplay;
using MousePlatformer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class GameplayHudController : MonoBehaviour
    {
        private static readonly string[] CounterNames = { "primary", "secondary", "tertiary", "quaternary" };

        [SerializeField] private UIDocument document;
        [SerializeField] private CollectibleCollector collectibleCollector;
        [SerializeField] private GameplayInteractionAvailability interactionAvailability;
        [SerializeField] private InputPromptIcons inputPromptIcons;
        [SerializeField] private PixelPerfectViewport viewport;
        [SerializeField] private Sprite defaultCounterIcon;
        [SerializeField] private Sprite[] counterIcons = new Sprite[4];
        [SerializeField] private bool showWelcomeOnStart = true;
        [SerializeField, TextArea] private string welcomeText =
            "Benvenuto nell'universo-10, rilassati e goditi il soggiorno";
        [SerializeField] private Sprite welcomePortrait;
        [SerializeField, Range(1f, 30f)] private float welcomeDuration = 30f;
        [SerializeField] private bool pauseWhilePhotoOpen = true;

        private readonly int[] counterValues = new int[CounterNames.Length];
        private readonly Label[] counterLabels = new Label[CounterNames.Length];
        private readonly Image[] counterImages = new Image[CounterNames.Length];
        private PanelSettings originalPanel;
        private PanelSettings runtimePanel;
        private VisualElement hudRoot;
        private VisualElement frame;
        private VisualElement safeArea;
        private Label timeLabel;
        private ScientistCommentView scientistView;
        private Coroutine scientistRoutine;
        private InputAction advanceScientistComment;
        private InputAction scrollScientistComment;
        private double elapsedSeconds;
        private long displayedSecond = -1;
        private bool timerRunning = true;
        private bool ready;
        private TestResultView testResultView;
        private bool testEnding;
        private int universe = 10;
        private InputDevicePrompts devicePrompts;
        private VisualElement interactionHints;
        private bool hintsPaused;
        private bool hintsDialogueVisible;
        private PhotoView photoView;
        private Action photoContinue;
        private float photoPreviousTimeScale;
        private bool photoOwnsPause;

        public event Action TestCommentCompleted;
        public event Action TestContinueRequested;
        public event Action PhotoShown;

        public double ElapsedSeconds => elapsedSeconds;

        private void Awake()
        {
            advanceScientistComment = new InputAction("Advance Scientist Comment", InputActionType.Button);
            advanceScientistComment.AddBinding("<Keyboard>/f");
            advanceScientistComment.AddBinding("<Gamepad>/buttonWest");
            advanceScientistComment.performed += OnAdvanceScientistComment;
            scrollScientistComment = new InputAction("Scroll Scientist Comment", InputActionType.Value,
                expectedControlType: "Axis");
            scrollScientistComment.AddBinding("<Gamepad>/rightStick/y").WithProcessor("axisDeadzone");
            scrollScientistComment.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/pageUp")
                .With("Negative", "<Keyboard>/pageDown");
        }

        private void OnEnable()
        {
            if (document == null || document.panelSettings == null || viewport == null)
            {
                Debug.LogError("Assegna UIDocument, PanelSettings e PixelPerfectViewport al GameplayHudController.", this);
                return;
            }

            originalPanel = document.panelSettings;
            runtimePanel = Instantiate(originalPanel);
            runtimePanel.name = "Gameplay HUD Runtime Panel";
            runtimePanel.scaleMode = PanelScaleMode.ConstantPixelSize;
            document.panelSettings = runtimePanel;

            VisualElement root = document.rootVisualElement;
            hudRoot = root;
            root.pickingMode = PickingMode.Ignore;
            root.focusable = false;
            root.Query<VisualElement>().ForEach(element =>
            {
                element.pickingMode = PickingMode.Ignore;
                element.focusable = false;
            });
            frame = root.Q("hud-frame");
            safeArea = root.Q("hud-safe-area");
            timeLabel = root.Q<Label>("elapsed-time");
            for (int index = 0; index < counterValues.Length; index++)
            {
                counterLabels[index] = root.Q<Label>("counter-" + CounterNames[index] + "-value");
                counterImages[index] = root.Q<Image>("counter-" + CounterNames[index] + "-icon");
                Sprite icon = counterIcons != null && index < counterIcons.Length ? counterIcons[index] : null;
                counterImages[index].sprite = icon != null ? icon : index == 2 ? null : defaultCounterIcon;
                counterImages[index].scaleMode = ScaleMode.ScaleToFit;
                counterImages[index].style.backgroundImage = StyleKeyword.None;
                if (index == 2 && icon == null)
                    counterImages[index].Add(new KnowledgeCounterIcon());
                RefreshCounter(index);
            }

            scientistView = new ScientistCommentView(root, viewport.VerticalBorderPixels);
            testResultView = new TestResultView(root, OnTestContinueRequested);
            photoView = new PhotoView(root, ContinuePhoto);
            interactionHints = root.Q("interaction-hints");
            devicePrompts = new InputDevicePrompts();
            devicePrompts.Changed += RefreshDevicePrompts;
            if (interactionAvailability != null) interactionAvailability.Changed += OnAvailableActionsChanged;
            hintsPaused = Time.timeScale <= 0f;
            RefreshDevicePrompts();
            advanceScientistComment.Enable();
            scrollScientistComment.Enable();
            ready = true;
            displayedSecond = -1;
            RefreshTimer();
            viewport.ViewportChanged += RefreshViewport;
            root.RegisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);
            RefreshViewport();
            if (collectibleCollector != null)
            {
                collectibleCollector.CountChanged += OnCollectibleCountChanged;
            }
            foreach (CollectibleType type in Enum.GetValues(typeof(CollectibleType)))
                OnCollectibleCountChanged(type, GameSession.GetCollectibleCount(type));
        }

        private void Start()
        {
            if (showWelcomeOnStart)
                ShowScientistComment(welcomeText.Replace("{universe}", universe.ToString()), welcomePortrait, welcomeDuration);
        }

        private void Update()
        {
            if (!ready)
                return;

            bool paused = Time.timeScale <= 0f;
            bool dialogueVisible = scientistView.IsVisible;
            if (hintsPaused != paused || hintsDialogueVisible != dialogueVisible)
            {
                hintsPaused = paused;
                hintsDialogueVisible = dialogueVisible;
                RefreshInteractionHints();
            }
            if (scientistView.IsOpen)
                scientistView.ScrollText(-scrollScientistComment.ReadValue<float>() * 60f * Time.unscaledDeltaTime);
            if (!timerRunning) return;

            elapsedSeconds += Time.deltaTime;
            RefreshTimer();
        }

        public void ResetTimer()
        {
            elapsedSeconds = 0d;
            displayedSecond = -1;
            if (ready) RefreshTimer();
        }

        public void SetTimerRunning(bool running)
        {
            timerRunning = running;
        }

        public void SetUniverse(int value) => universe = value;

        public void ShowPhoto(Texture2D photo, Action onContinue = null)
        {
            if (!ready || testEnding) return;
            bool opening = !photoView.IsVisible;
            if (!photoView.IsVisible && pauseWhilePhotoOpen)
            {
                photoPreviousTimeScale = Time.timeScale;
                photoOwnsPause = true;
                Time.timeScale = 0f;
            }
            photoContinue = onContinue;
            photoView.Show(photo);
            RefreshInteractionHints();
            if (opening) PhotoShown?.Invoke();
        }

        public void HidePhoto()
        {
            if (photoView == null || !photoView.IsVisible) return;
            photoView.Hide();
            photoContinue = null;
            if (photoOwnsPause)
            {
                Time.timeScale = photoPreviousTimeScale;
                photoOwnsPause = false;
            }
            hintsPaused = Time.timeScale <= 0f;
            if (ready && devicePrompts != null) RefreshInteractionHints();
        }

        private void ContinuePhoto()
        {
            if (photoView == null || !photoView.IsVisible) return;
            Action continued = photoContinue;
            HidePhoto();
            continued?.Invoke();
        }

        public void BeginTestEnding()
        {
            testEnding = true;
            timerRunning = false;
            HideScientistComment();
            testResultView.Begin();
            interactionHints.style.display = DisplayStyle.None;
        }

        public void SetTestCoverOpacity(float opacity) => testResultView.SetCoverOpacity(opacity);

        public void ShowTestComment(string text, Sprite portrait)
        {
            HideScientistComment();
            scientistRoutine = StartCoroutine(RunScientistComment(new[] { text }, portrait, 30f, true));
        }

        public void ShowTestResult(int universeNumber, double elapsed, string result)
        {
            testResultView.Show(universeNumber, elapsed, result);
        }

        private void OnTestContinueRequested() => TestContinueRequested?.Invoke();

        public void SetCounter(int index, int value)
        {
            if (index < 0 || index >= counterValues.Length)
                throw new ArgumentOutOfRangeException(nameof(index), "Gli indici dei contatori sono 0, 1, 2 e 3.");

            counterValues[index] = Mathf.Max(0, value);
            if (ready) RefreshCounter(index);
        }

        private void OnCollectibleCountChanged(CollectibleType type, int count)
        {
            switch (type)
            {
                case CollectibleType.Cheese:
                    SetCounter(0, count);
                    break;
                case CollectibleType.GreenCheese:
                    SetCounter(1, count);
                    break;
                case CollectibleType.Photo:
                    SetCounter(3, count);
                    break;
                case CollectibleType.Knowledge:
                    SetCounter(2, count);
                    break;
            }
        }

        public void ShowScientistComment(string text, Sprite portrait, float duration = 30f)
        {
            if (!ready || string.IsNullOrWhiteSpace(text))
                return;

            ShowScientistComment(new[] { text }, portrait, duration);
        }

        public void ShowScientistComment(string[] texts, Sprite portrait, float duration = 30f)
        {
            if (!ready || testEnding || texts == null) return;
            List<string> messages = new List<string>();
            foreach (string text in texts)
            {
                if (!string.IsNullOrWhiteSpace(text)) messages.Add(text);
            }
            if (messages.Count == 0) return;

            HideScientistComment();
            scientistRoutine = StartCoroutine(RunScientistComment(messages.ToArray(), portrait, duration, false));
            RefreshInteractionHints();
        }

        private IEnumerator RunScientistComment(string[] texts, Sprite portrait, float duration, bool waitForAdvance)
        {
            yield return scientistView.Show(texts, portrait, duration, waitForAdvance);
            scientistRoutine = null;
            if (waitForAdvance) TestCommentCompleted?.Invoke();
        }

        private void OnAdvanceScientistComment(InputAction.CallbackContext context)
        {
            if ((photoView != null && photoView.IsVisible) || (testResultView != null && testResultView.IsVisible)
                || (scientistView != null && scientistView.IsVisible))
                collectibleCollector?.ConsumeInteractionInput();
            if (photoView != null && photoView.IsVisible) ContinuePhoto();
            else if (testResultView != null && testResultView.IsVisible) testResultView.Continue();
            else scientistView?.Advance();
        }

        public void HideScientistComment()
        {
            if (scientistRoutine != null)
            {
                StopCoroutine(scientistRoutine);
                scientistRoutine = null;
            }
            scientistView?.Hide();
        }

        private void RefreshCounter(int index)
        {
            counterLabels[index].text = counterValues[index] + "×";
        }

        private void RefreshDevicePrompts()
        {
            Sprite advanceIcon = GetPromptIcon(InputPromptAction.Advance);
            scientistView.SetAdvanceKey(devicePrompts.Advance, advanceIcon);
            testResultView.SetAdvanceKey(devicePrompts.Advance, advanceIcon);
            photoView.SetCloseKey(devicePrompts.Advance);
            RefreshInteractionHints();
        }

        private void OnAvailableActionsChanged(AvailableGameplayActions actions) => RefreshInteractionHints();

        private void RefreshInteractionHints()
        {
            AvailableGameplayActions available = interactionAvailability != null
                ? interactionAvailability.Current : AvailableGameplayActions.None;
            interactionHints.Clear();
            if ((available & AvailableGameplayActions.MoveCrate) != 0)
                AddInteractionHint(InputPromptAction.Interact, devicePrompts.Interact, "Hold + direction to push / pull");
            if ((available & AvailableGameplayActions.EatCheese) != 0)
                AddInteractionHint(InputPromptAction.Collect, devicePrompts.Collect, "Eat cheese");
            if ((available & AvailableGameplayActions.EatGreenCheese) != 0)
                AddInteractionHint(InputPromptAction.Collect, devicePrompts.Collect, "Eat green cheese");
            if ((available & AvailableGameplayActions.CollectPhoto) != 0)
                AddInteractionHint(InputPromptAction.Collect, devicePrompts.Collect, "Collect photo");
            if ((available & AvailableGameplayActions.CutRope) != 0)
                AddInteractionHint(InputPromptAction.CutOrDig, devicePrompts.CutOrDig, "Hold to cut rope");
            if ((available & AvailableGameplayActions.Dig) != 0)
                AddInteractionHint(InputPromptAction.CutOrDig, devicePrompts.CutOrDig, "Hold + direction to dig");
            interactionHints.style.display = !testEnding && !hintsPaused && !photoView.IsVisible
                && !scientistView.IsVisible && interactionHints.childCount > 0
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private Sprite GetPromptIcon(InputPromptAction action)
        {
            return inputPromptIcons != null ? inputPromptIcons.Get(devicePrompts.Family, action) : null;
        }

        private void AddInteractionHint(InputPromptAction action, string key, string description)
        {
            VisualElement row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("interaction-hint-row");
            Sprite sprite = GetPromptIcon(action);
            if (sprite != null)
            {
                VisualElement icon = new VisualElement { pickingMode = PickingMode.Ignore, tooltip = key };
                icon.AddToClassList("input-key-icon");
                icon.style.backgroundImage = new StyleBackground(sprite);
                row.Add(icon);
            }
            else
            {
                Label fallback = new Label(key) { pickingMode = PickingMode.Ignore };
                fallback.AddToClassList("input-key-fallback");
                row.Add(fallback);
            }
            Label label = new Label(description) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("interaction-hint-description");
            row.Add(label);
            interactionHints.Add(row);
        }

        private void RefreshTimer()
        {
            long totalSeconds = (long)elapsedSeconds;
            if (totalSeconds == displayedSecond) return;
            displayedSecond = totalSeconds;
            timeLabel.text = $"{totalSeconds / 3600:00}:{totalSeconds / 60 % 60:00}:{totalSeconds % 60:00}";
        }

        private void OnPanelGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            RefreshViewport();
        }

        private void RefreshViewport()
        {
            if (!ready || viewport.DisplayScale <= 0f)
                return;

            float scale = viewport.DisplayScale;
            runtimePanel.scale = scale;
            frame.style.left = viewport.FrameRect.x / scale;
            frame.style.top = viewport.FrameRect.y / scale;
            frame.style.width = viewport.ReferenceResolution.x;
            frame.style.height = viewport.ReferenceResolution.y;
            safeArea.style.top = viewport.VerticalBorderPixels;
            safeArea.style.bottom = viewport.VerticalBorderPixels;
            // Include any external letterboxing so the margins are measured from the screen edges.
            float bottomLetterbox = (Screen.height - viewport.FrameRect.yMax) / scale;
            float bottomAnchor = 10f - viewport.VerticalBorderPixels - bottomLetterbox;
            hudRoot.Q("resource-counters").style.bottom = bottomAnchor;
            hudRoot.Q(className: "timer-anchor").style.top =
                10f - viewport.VerticalBorderPixels - viewport.FrameRect.y / scale;
            scientistView.SetBottomInset(bottomAnchor);
            interactionHints.style.bottom = bottomAnchor;
        }

        private void OnDisable()
        {
            HidePhoto();
            if (interactionAvailability != null) interactionAvailability.Changed -= OnAvailableActionsChanged;
            if (devicePrompts != null)
            {
                devicePrompts.Changed -= RefreshDevicePrompts;
                devicePrompts.Dispose();
                devicePrompts = null;
            }
            if (collectibleCollector != null)
                collectibleCollector.CountChanged -= OnCollectibleCountChanged;
            HideScientistComment();
            advanceScientistComment?.Disable();
            scrollScientistComment?.Disable();
            scientistView?.Dispose();
            scientistView = null;
            testResultView?.Dispose();
            testResultView = null;
            photoView?.Dispose();
            photoView = null;
            if (viewport != null) viewport.ViewportChanged -= RefreshViewport;
            if (hudRoot != null)
                hudRoot.UnregisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);

            ready = false;
            if (document != null && runtimePanel != null)
                document.panelSettings = originalPanel;
            if (runtimePanel != null) Destroy(runtimePanel);
            runtimePanel = null;
            hudRoot = null;
        }

        private void OnDestroy()
        {
            if (advanceScientistComment != null)
            {
                advanceScientistComment.performed -= OnAdvanceScientistComment;
                advanceScientistComment.Dispose();
            }
            scrollScientistComment?.Dispose();
        }
    }
}
