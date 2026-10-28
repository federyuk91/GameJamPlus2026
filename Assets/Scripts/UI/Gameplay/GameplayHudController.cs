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
        [SerializeField] private SatietyHud resourceHud;
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private CollectibleCollector collectibleCollector;
        [SerializeField] private GameplayInteractionAvailability interactionAvailability;
        [SerializeField, Tooltip("Testi condivisi dei suggerimenti di interazione mostrati nella HUD.")]
        private GameplayTooltipSettings tooltipSettings;
        private static readonly GameplayTooltipText DefaultTooltipTexts = new GameplayTooltipText();
        private GameplayTooltipText TooltipTexts => tooltipSettings != null && tooltipSettings.Texts != null
            ? tooltipSettings.Texts : DefaultTooltipTexts;
        [SerializeField] private BodyControlsSettings controlsSettings;
        [SerializeField] private PixelPerfectViewport viewport;
        [SerializeField] private PlatformCameraController cameraController;
        [SerializeField] private Sprite scientistPortrait;
        [SerializeField] private ExitTunnelSettings exitTunnel = new ExitTunnelSettings();
        [SerializeField] private ScientistSpeechSettings scientistSpeech;
        [SerializeField] private ScientistVoice scientistVoice;
        [SerializeField] private AudioSource typingAudio;
        [SerializeField] private AudioClip typingClip;
        [SerializeField, Range(0f, 1f)] private float typingVolume = 0.25f;
        [SerializeField] private Sprite defaultCounterIcon;
        [SerializeField] private Sprite[] counterIcons = new Sprite[4];
        [SerializeField, Tooltip("Cheese counter frames in collected-count order, from 0 to 9. Victory thresholds are configured separately on the collectible goal.")]
        private Sprite[] cheeseCountFrames = new Sprite[10];
        [SerializeField] private bool showWelcomeOnStart = true;
        [SerializeField] private bool pauseWhilePhotoOpen = true;
        [SerializeField, Range(0f, 1f)] private float shockLowFrequencyMotor = 0.45f;
        [SerializeField, Range(0f, 1f)] private float shockHighFrequencyMotor = 0.75f;
        private readonly ShockGamepadFeedback shockGamepadFeedback = new ShockGamepadFeedback();
        private bool applicationPaused;

        private readonly int[] counterValues = new int[CounterNames.Length];
        private readonly Label[] counterLabels = new Label[CounterNames.Length];
        private readonly Image[] counterImages = new Image[CounterNames.Length];
        private readonly IVisualElementScheduledItem[] counterHighlights = new IVisualElementScheduledItem[CounterNames.Length];
        private bool counterFeedbackEnabled;
        private PanelSettings originalPanel;
        private PanelSettings runtimePanel;
        private VisualElement hudRoot;
        private VisualElement frame;
        private VisualElement safeArea;
        private VisualElement bottomBlackBand;
        private VisualElement timerAnchor;
        private Label timeLabel;
        private Label campaignProgress;
        private Label subjectIdentity;
        private Label cameraHeightLock;
        private Label cameraLockReminder;
        private float cameraReminderUntil;
        private ScientistCommentView scientistView;
        private Coroutine scientistRoutine;
        private InputAction advanceScientistComment;
        private InputAction scrollScientistComment;
        private double elapsedSeconds;
        private long displayedSecond = -1;
        private bool timerRunning = true;
        private bool spawnPresentationActive;
        private int zoomBeforeSpawn;
        private bool ready;
        private TestResultView testResultView;
        private bool testEnding;
        private int universe = 10;
        private InputDevicePrompts devicePrompts;
        private VisualElement interactionHints;
        private bool hintsPaused;
        private bool communicationInProgress;
        private bool refreshingHints;
        private PhotoView photoView;
        private Action photoContinue;
        private float photoPreviousTimeScale;
        private bool photoOwnsPause;
        private GameplayPauseMenuView pauseMenu;

        public event Action TestCommentCompleted;
        public event Action TestContinueRequested;
        public event Action PhotoShown;

        public double ElapsedSeconds => elapsedSeconds;
        public float ExitTransitionDuration => exitTunnel != null ? exitTunnel.Duration : 4f;

        private void Awake()
        {
            if (typingAudio != null)
            {
                typingAudio.playOnAwake = false;
                typingAudio.loop = false;
                typingAudio.spatialBlend = 0f;
            }
            advanceScientistComment = new InputAction("Advance Scientist Comment", InputActionType.Button);
            advanceScientistComment.AddBinding("<Keyboard>/f");
            advanceScientistComment.AddBinding("<Gamepad>/buttonEast");
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
                Debug.LogError("Assign UIDocument, PanelSettings and PixelPerfectViewport to GameplayHudController.", this);
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
            bottomBlackBand = root.Q(className: "black-band-bottom");
            timerAnchor = root.Q(className: "timer-anchor");
            timeLabel = root.Q<Label>("elapsed-time");
            campaignProgress = root.Q<Label>("campaign-progress");
            subjectIdentity = root.Q<Label>("subject-identity");
            cameraHeightLock = root.Q<Label>("camera-height-lock");
            cameraLockReminder = root.Q<Label>("camera-lock-reminder");
            if (cameraController != null)
            {
                cameraController.ModeChanged += OnCameraModeChanged;
                cameraController.LockedMovementAttempted += OnLockedCameraMovementAttempted;
            }
            for (int index = 0; index < counterValues.Length; index++)
            {
                counterLabels[index] = root.Q<Label>("counter-" + CounterNames[index] + "-value");
                counterImages[index] = root.Q<Image>("counter-" + CounterNames[index] + "-icon");
                if (counterLabels[index] == null || counterImages[index] == null) continue;
                Sprite icon = counterIcons != null && index < counterIcons.Length ? counterIcons[index] : null;
                if (icon == null && index != 2) icon = counterImages[index].sprite;
                SetCounterSprite(index, icon != null ? icon : index == 2 ? null : defaultCounterIcon);
                counterImages[index].scaleMode = ScaleMode.ScaleToFit;
                counterImages[index].style.backgroundImage = StyleKeyword.None;
                if (index == 2)
                {
                    KnowledgeCounterIcon knowledgeIcon = counterImages[index].Q<KnowledgeCounterIcon>();
                    if (icon != null) knowledgeIcon?.RemoveFromHierarchy();
                    else if (knowledgeIcon == null) counterImages[index].Add(new KnowledgeCounterIcon());
                }
                RefreshCounter(index);
            }

            scientistView = new ScientistCommentView(root, scientistSpeech);
            scientistView.CharacterRevealed += OnScientistCharacterRevealed;
            testResultView = new TestResultView(root, OnTestContinueRequested, exitTunnel);
            photoView = new PhotoView(root, ContinuePhoto);
            pauseMenu = new GameplayPauseMenuView(root, playerInput,
                () => ready && !testEnding && !photoView.IsVisible && !testResultView.IsVisible, controlsSettings);
            pauseMenu.Changed += OnPauseChanged;
            interactionHints = root.Q("interaction-hints");
            devicePrompts = new InputDevicePrompts();
            devicePrompts.Changed += RefreshDevicePrompts;
            if (interactionAvailability != null) interactionAvailability.Changed += OnAvailableActionsChanged;
            hintsPaused = Time.timeScale <= 0f;
            RefreshDevicePrompts();
            advanceScientistComment.Enable();
            scrollScientistComment.Enable();
            ready = true;
            GameUISettings.Changed += RefreshInteractionHints;
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
            counterFeedbackEnabled = true;
        }

        private void Start()
        {
            if (spawnPresentationActive) return;
            ShowWelcomeComment();
        }

        private void ShowWelcomeComment()
        {
            if (showWelcomeOnStart)
                GameManager.Instance?.Dialogues?.Play(DialogueCase.Welcome);
        }

        public void BeginSpawnPresentation()
        {
            if (spawnPresentationActive) return;
            spawnPresentationActive = true;
            timerRunning = false;
            zoomBeforeSpawn = viewport.ZoomLevel;
            viewport.SetZoomLevel(1);
            HideScientistComment();
            RefreshInteractionHints();
        }

        public void CompleteSpawnPresentation()
        {
            if (!spawnPresentationActive) return;
            spawnPresentationActive = false;
            viewport.SetZoomLevel(zoomBeforeSpawn);
            HideScientistComment();
            timerRunning = true;
            ShowWelcomeComment();
            RefreshInteractionHints();
        }

        private void Update()
        {
            if (!ready)
                return;
            if (GameManager.Instance != null && GameManager.Instance.State == GameManager.LoopState.Spawning)
            {
                shockGamepadFeedback.Stop();
                return;
            }

            pauseMenu.Tick();
            RefreshShockGamepadFeedback();
            if (!ready || pauseMenu == null || pauseMenu.IsOpen) return;
            RefreshCameraLockReminder();

            bool paused = Time.timeScale <= 0f;
            if (hintsPaused != paused)
            {
                hintsPaused = paused;
                RefreshInteractionHints();
            }
            if (scientistView.IsOpen)
                scientistView.ScrollText(-scrollScientistComment.ReadValue<float>() * 60f * Time.unscaledDeltaTime);
            if (testResultView.IsVisible)
                testResultView.ScrollNotes(-scrollScientistComment.ReadValue<float>() * 60f * Time.unscaledDeltaTime);
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

        private void OnPauseChanged(bool paused)
        {
            scientistView.SetSuspended(paused);
            if (paused) scientistVoice?.StopSpeaking();
            if (paused)
            {
                advanceScientistComment.Disable();
                scrollScientistComment.Disable();
            }
            else
            {
                advanceScientistComment.Enable();
                scrollScientistComment.Enable();
            }
            hintsPaused = Time.timeScale <= 0f;
            RefreshInteractionHints();
        }

        public void SetTimerRunning(bool running)
        {
            timerRunning = running;
        }

        public void SetUniverse(int value) => universe = value;

        public void SetSubjectIdentity(string subjectId, int attempt, int maximumAttempts)
        {
            if (campaignProgress != null) campaignProgress.text = $"UNIVERSE {universe} / ATTEMPT {attempt}/{maximumAttempts}";
            if (subjectIdentity != null) subjectIdentity.text = "SUBJECT " + subjectId;
        }

        public void ShowPhoto(Texture2D photo, Action onContinue = null)
        {
            if (!ready || testEnding) return;
            pauseMenu?.Resume();
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

        public void BeginTestEnding(bool useExitImage = false)
        {
            testEnding = true;
            timerRunning = false;
            HideScientistComment();
            testResultView.Begin(useExitImage);
            interactionHints.style.display = DisplayStyle.None;
        }

        public void SetTestCoverOpacity(float opacity) => testResultView.SetCoverOpacity(opacity);

        public void PlayDamageFlash() => resourceHud?.PlayDamageFlash();

        public void ShowTestComment(string text, Sprite portrait, float duration = 7f)
        {
            if (portrait == null) portrait = scientistPortrait;
            HideScientistComment();
            scientistView.SetEndingPresentation(true);
            communicationInProgress = true;
            scientistRoutine = StartCoroutine(RunScientistComment(new[] { text }, portrait, duration, true));
        }

        public void ShowTestResult(int universeNumber, double elapsed, string result)
        {
            testResultView.Show(universeNumber, elapsed, result);
        }

        private void OnTestContinueRequested() => TestContinueRequested?.Invoke();

        public void SetCounter(int index, int value)
        {
            if (index < 0 || index >= counterValues.Length)
                throw new ArgumentOutOfRangeException(nameof(index), "Counter indices are 0, 1, 2 and 3.");

            int nextValue = Mathf.Max(0, value);
            bool increased = nextValue > counterValues[index];
            counterValues[index] = nextValue;
            if (ready && counterLabels[index] != null)
            {
                RefreshCounter(index);
                if (increased && counterFeedbackEnabled)
                {
                    VisualElement counter = counterLabels[index].parent;
                    counterHighlights[index]?.Pause();
                    counter.AddToClassList("counter-collected");
                    counterHighlights[index] = counter.schedule.Execute(() => counter.RemoveFromClassList("counter-collected")).StartingIn(350);
                }
            }
        }

        private void OnCollectibleCountChanged(CollectibleType type, int count)
        {
            switch (type)
            {
                case CollectibleType.Cheese:
                case CollectibleType.GreenCheese:
                    SetCounter(0, count);
                    break;
                case CollectibleType.Photo:
                    SetCounter(3, count);
                    break;
                case CollectibleType.Knowledge:
                    SetCounter(2, count);
                    break;
            }
        }

        public void ShowScientistComment(string text, Sprite portrait, float duration = 7f)
        {
            if (!ready || string.IsNullOrWhiteSpace(text))
                return;

            ShowScientistComment(new[] { text }, portrait, duration);
        }

        public void ShowScientistComment(string[] texts, Sprite portrait, float duration = 7f)
        {
            if (!ready || testEnding || texts == null) return;
            if (portrait == null) portrait = scientistPortrait;
            List<string> messages = new List<string>();
            foreach (string text in texts)
            {
                if (!string.IsNullOrWhiteSpace(text)) messages.Add(text);
            }
            if (messages.Count == 0) return;

            HideScientistComment();
            communicationInProgress = true;
            scientistRoutine = StartCoroutine(RunScientistComment(messages.ToArray(), portrait, duration, false));
            RefreshInteractionHints();
        }

        private void OnScientistCharacterRevealed(char character)
        {
            if (scientistView != null && scientistView.IsScientistSpeaking)
                scientistVoice?.Speak(character);
            if (char.IsWhiteSpace(character) || typingAudio == null || typingClip == null) return;
            typingAudio.volume = typingVolume * GameAudioSettings.EffectsVolume;
            typingAudio.PlayOneShot(typingClip);
        }

        private IEnumerator RunScientistComment(string[] texts, Sprite portrait, float duration, bool waitForAdvance)
        {
            yield return scientistView.Show(texts, portrait, duration, waitForAdvance);
            scientistRoutine = null;
            communicationInProgress = false;
            if (waitForAdvance) TestCommentCompleted?.Invoke();
            RefreshInteractionHints();
        }

        private void OnAdvanceScientistComment(InputAction.CallbackContext context)
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameManager.LoopState.Spawning) return;
            if (pauseMenu != null && pauseMenu.IsOpen) return;
            if (playerInput != null && playerInput.GameplayActionsLocked && !testEnding) return;
            bool modalOpen = (photoView != null && photoView.IsVisible) || (testResultView != null && testResultView.IsVisible);
            if (!testEnding && !modalOpen && playerInput != null && interactionAvailability != null)
            {
                AvailableGameplayActions available = interactionAvailability.RefreshCurrent();
                if (playerInput.HasAvailableInteractionOnControl(context.control, available)) return;
            }
            if ((photoView != null && photoView.IsVisible) || (testResultView != null && testResultView.IsVisible)
                || communicationInProgress || (scientistView != null && scientistView.IsVisible))
                collectibleCollector?.ConsumeInteractionInput();
            if (photoView != null && photoView.IsVisible) ContinuePhoto();
            else if (testResultView != null && testResultView.IsVisible) testResultView.Continue();
            else
            {
                scientistVoice?.StopSpeaking();
                scientistView?.Advance();
            }
        }

        public void HideScientistComment()
        {
            scientistVoice?.StopSpeaking();
            if (typingAudio != null) typingAudio.Stop();
            communicationInProgress = false;
            interactionHints?.Clear();
            if (interactionHints != null) interactionHints.style.display = DisplayStyle.None;
            if (scientistRoutine != null)
            {
                StopCoroutine(scientistRoutine);
                scientistRoutine = null;
            }
            scientistView?.Hide();
        }

        private void RefreshCounter(int index)
        {
            counterLabels[index].text = "\u00D7 " + counterValues[index];
            if (index == 0 && cheeseCountFrames != null && cheeseCountFrames.Length > 0)
            {
                int frameIndex = Mathf.Clamp(counterValues[index], 0, Mathf.Min(9, cheeseCountFrames.Length - 1));
                Sprite frameSprite = cheeseCountFrames[frameIndex];
                if (frameSprite != null) SetCounterSprite(index, frameSprite);
            }
        }

        private void SetCounterSprite(int index, Sprite sprite)
        {
            Image image = counterImages[index];
            image.sprite = sprite;
        }

        private void RefreshDevicePrompts()
        {
            RefreshShockGamepadFeedback();
            pauseMenu?.SetDevice(devicePrompts);
            interactionAvailability?.SetUsingGamepad(devicePrompts.UsingGamepad);
            InputPromptBinding advancePrompt = devicePrompts.PromptForAction(InputPromptAction.Advance);
            scientistView.SetAdvanceKey(advancePrompt);
            testResultView.SetAdvanceKey(advancePrompt);
            photoView.SetCloseKey(devicePrompts.Advance);
            RefreshCameraHeightLock();
            RefreshInteractionHints();
        }

        private void RefreshShockGamepadFeedback()
        {
            GameManager manager = GameManager.Instance;
            bool active = ready && !applicationPaused && Application.isFocused && Time.timeScale > 0f
                && playerInput != null && playerInput.IsParalyzed
                && (manager == null || manager.State == GameManager.LoopState.Playing);
            shockGamepadFeedback.Refresh(devicePrompts?.ActiveGamepad, active, shockLowFrequencyMotor, shockHighFrequencyMotor);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) shockGamepadFeedback.Stop();
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (paused) shockGamepadFeedback.Stop();
        }

        private void OnCameraModeChanged(PlatformCameraController.CameraMode mode)
        {
            cameraReminderUntil = 0f;
            RefreshCameraHeightLock();
            RefreshCameraLockReminder();
            RefreshInteractionHints();
        }

        private void OnLockedCameraMovementAttempted() => cameraReminderUntil = Time.unscaledTime + 2.5f;

        private void RefreshCameraHeightLock()
        {
            if (cameraHeightLock == null || devicePrompts == null) return;
            PlatformCameraController.CameraMode mode = cameraController != null
                ? cameraController.Mode : PlatformCameraController.CameraMode.Follow;
            cameraHeightLock.style.display = mode != PlatformCameraController.CameraMode.Follow ? DisplayStyle.Flex : DisplayStyle.None;
            switch (mode)
            {
                case PlatformCameraController.CameraMode.Inspect:
                    cameraHeightLock.text = cameraController.IsInspectionFrozen
                        ? "CAMERA LOCKED / RELEASE " + devicePrompts.CameraHeightLock + " TO CONTROL MOUSE"
                        : "HOLD " + devicePrompts.CameraHeightLock + " CAMERA / "
                            + devicePrompts.CameraPositionLock + (cameraController.IsPositionLocked ? " RECENTER" : " LOCK");
                    break;
                case PlatformCameraController.CameraMode.Fixed:
                    cameraHeightLock.text = "CAMERA LOCKED / HOLD " + devicePrompts.CameraHeightLock
                        + " ADJUST / " + devicePrompts.CameraPositionLock + " RECENTER";
                    break;
                case PlatformCameraController.CameraMode.FollowOffset:
                    cameraHeightLock.text = "CAMERA OFFSET ACTIVE \u2014 " + devicePrompts.CameraHeightLock + " TO ADJUST";
                    break;
            }
        }

        private void RefreshCameraLockReminder()
        {
            if (cameraLockReminder == null || devicePrompts == null) return;
            bool visible = cameraController != null && cameraController.IsHeightLocked
                && Time.unscaledTime < cameraReminderUntil && Time.timeScale > 0f;
            cameraLockReminder.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            cameraLockReminder.text = "STAND STILL AND HOLD " + devicePrompts.CameraHeightLock
                + " TO ADJUST\n" + devicePrompts.CameraPositionLock + " RECENTERS THE CAMERA";
            cameraLockReminder.style.opacity = Mathf.Lerp(0.35f, 1f,
                Mathf.PingPong(Time.unscaledTime * 3f, 1f));
        }

        private void OnAvailableActionsChanged(AvailableGameplayActions actions) => RefreshInteractionHints();

        private void RefreshInteractionHints()
        {
            if (refreshingHints) return;
            refreshingHints = true;
            try
            {
                RefreshCurrentInteractionHints();
            }
            finally
            {
                refreshingHints = false;
            }
        }

        private void RefreshCurrentInteractionHints()
        {
            interactionHints.Clear();
            if (spawnPresentationActive)
            {
                Label prompt = new Label(TooltipTexts.start) { pickingMode = PickingMode.Ignore };
                prompt.AddToClassList("interaction-hint-description");
                interactionHints.Add(prompt);
                interactionHints.style.display = DisplayStyle.Flex;
                return;
            }
            bool blocked = GameUISettings.HideTooltip || testEnding || Time.timeScale <= 0f || photoView.IsVisible || testResultView.IsVisible;
            if (blocked)
            {
                interactionHints.style.display = DisplayStyle.None;
                return;
            }

            AvailableGameplayActions available = interactionAvailability != null
                ? interactionAvailability.RefreshCurrent() : AvailableGameplayActions.None;
            // Availability callbacks can open a blocking overlay or end the stage.
            if (testEnding || Time.timeScale <= 0f || photoView.IsVisible || testResultView.IsVisible)
            {
                interactionHints.style.display = DisplayStyle.None;
                return;
            }
            if ((available & AvailableGameplayActions.MoveCrate) != 0)
                AddInteractionHint(InputPromptAction.MoveCrate, TooltipTexts.moveCrate, true);
            if ((available & AvailableGameplayActions.ClimbLedge) != 0)
                AddInteractionHint(InputPromptAction.Interact, TooltipTexts.climbUp);
            if ((available & AvailableGameplayActions.EatCheese) != 0)
                AddInteractionHint(InputPromptAction.CutOrDig, TooltipTexts.eatCheese);
            if ((available & AvailableGameplayActions.EatGreenCheese) != 0)
                AddInteractionHint(InputPromptAction.CutOrDig, TooltipTexts.eatGreenCheese);
            if ((available & AvailableGameplayActions.CollectPhoto) != 0)
                AddInteractionHint(InputPromptAction.Collect, TooltipTexts.collectPhoto);
            if ((available & AvailableGameplayActions.CollectItem) != 0)
                AddInteractionHint(InputPromptAction.Collect, TooltipTexts.collectItem);
            if ((available & AvailableGameplayActions.PressButton) != 0)
                AddInteractionHint(InputPromptAction.Collect, TooltipTexts.activateButton);
            if ((available & AvailableGameplayActions.UseWheel) != 0)
                AddInteractionHint(InputPromptAction.Collect,
                    interactionAvailability.IsUsingWheel ? TooltipTexts.leaveWheel : TooltipTexts.useWheel);
            if ((available & AvailableGameplayActions.CutRope) != 0)
                AddInteractionHint(InputPromptAction.CutOrDig, TooltipTexts.gnaw);
            if ((available & AvailableGameplayActions.Dig) != 0)
                AddInteractionHint(InputPromptAction.CutOrDig, TooltipTexts.dig, true);
            bool showHints = interactionHints.childCount > 0;
            interactionHints.style.display = showHints ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void AddInteractionHint(InputPromptAction action, string description, bool withDirection = false)
        {
            VisualElement row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("interaction-hint-row");
            VisualElement inputs = new VisualElement { pickingMode = PickingMode.Ignore };
            inputs.AddToClassList("interaction-hint-inputs");
            row.Add(inputs);
            InputPromptBinding binding = devicePrompts.PromptForAction(action);
            InputPromptElement prompt = new InputPromptElement();
            prompt.SetPrompt(binding);
            prompt.AddToClassList("input-prompt-hint-icon");
            inputs.Add(prompt);
            if (withDirection)
            {
                Label plus = new Label("+") { pickingMode = PickingMode.Ignore };
                plus.AddToClassList("interaction-hint-plus");
                inputs.Add(plus);
                InputPromptElement direction = new InputPromptElement();
                direction.SetPrompt(InputDevicePrompts.ResolvePrompt(devicePrompts, "directions"));
                direction.AddToClassList("input-prompt-hint-icon");
                inputs.Add(direction);
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
            safeArea.style.bottom = viewport.BottomBorderPixels;
            if (bottomBlackBand != null) bottomBlackBand.style.height = viewport.BottomBorderPixels;
            if (timerAnchor != null)
                timerAnchor.style.top = 1f - viewport.VerticalBorderPixels - viewport.FrameRect.y / scale;
        }

        private void OnDisable()
        {
            GameUISettings.Changed -= RefreshInteractionHints;
            shockGamepadFeedback.Stop();
            counterFeedbackEnabled = false;
            for (int index = 0; index < counterHighlights.Length; index++)
            {
                counterHighlights[index]?.Pause();
                counterHighlights[index] = null;
                counterLabels[index]?.parent?.RemoveFromClassList("counter-collected");
            }
            if (pauseMenu != null)
            {
                pauseMenu.Dispose();
                pauseMenu.Changed -= OnPauseChanged;
                pauseMenu = null;
            }
            if (cameraController != null)
            {
                cameraController.ModeChanged -= OnCameraModeChanged;
                cameraController.LockedMovementAttempted -= OnLockedCameraMovementAttempted;
            }
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
            if (scientistView != null) scientistView.CharacterRevealed -= OnScientistCharacterRevealed;
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
            shockGamepadFeedback.Dispose();
            if (advanceScientistComment != null)
            {
                advanceScientistComment.performed -= OnAdvanceScientistComment;
                advanceScientistComment.Dispose();
            }
            scrollScientistComment?.Dispose();
        }
    }
}
