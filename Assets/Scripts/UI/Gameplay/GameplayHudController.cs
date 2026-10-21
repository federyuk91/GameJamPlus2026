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
        [SerializeField] private InputPromptIcons inputPromptIcons;
        [SerializeField] private PixelPerfectViewport viewport;
        [SerializeField] private PlatformCameraController cameraController;
        [SerializeField] private Sprite scientistPortrait;
        [SerializeField] private ScientistSpeechSettings scientistSpeech;
        [SerializeField] private ScientistVoice scientistVoice;
        [SerializeField] private AudioSource typingAudio;
        [SerializeField] private AudioClip typingClip;
        [SerializeField, Range(0f, 1f)] private float typingVolume = 0.25f;
        [SerializeField] private Sprite defaultCounterIcon;
        [SerializeField] private Sprite[] counterIcons = new Sprite[4];
        [SerializeField, Tooltip("Cheese counter frames ordered from full plate to empty plate. Zero collected uses the last frame; each collected cheese advances one frame toward the first. The numeric total remains uncapped.")]
        private Sprite[] cheeseCounterFrames = new Sprite[0];
        [SerializeField] private bool showWelcomeOnStart = true;
        [SerializeField, TextArea] private string welcomeText =
            "Welcome to Universe-10. Relax and enjoy your stay.";
        [SerializeField] private Sprite welcomePortrait;
        [SerializeField, Range(1f, 30f)] private float welcomeDuration = 7f;
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
        private bool ready;
        private TestResultView testResultView;
        private bool testEnding;
        private int universe = 10;
        private InputDevicePrompts devicePrompts;
        private VisualElement interactionHints;
        private bool hintsPaused;
        private bool hintsDialogueVisible;
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
            timeLabel = root.Q<Label>("elapsed-time");
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
                Sprite icon = counterIcons != null && index < counterIcons.Length ? counterIcons[index] : null;
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

            scientistView = new ScientistCommentView(root, viewport.VerticalBorderPixels, scientistSpeech);
            scientistView.CharacterRevealed += OnScientistCharacterRevealed;
            testResultView = new TestResultView(root, OnTestContinueRequested);
            photoView = new PhotoView(root, ContinuePhoto);
            pauseMenu = new GameplayPauseMenuView(root, playerInput,
                () => ready && !testEnding && !photoView.IsVisible && !testResultView.IsVisible);
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

            pauseMenu.Tick();
            if (!ready || pauseMenu == null || pauseMenu.IsOpen) return;
            RefreshCameraLockReminder();

            bool paused = Time.timeScale <= 0f;
            bool dialogueVisible = communicationInProgress || scientistView.IsVisible;
            if (hintsPaused != paused || hintsDialogueVisible != dialogueVisible)
            {
                hintsPaused = paused;
                hintsDialogueVisible = dialogueVisible;
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

        public void BeginTestEnding()
        {
            testEnding = true;
            timerRunning = false;
            HideScientistComment();
            testResultView.Begin();
            interactionHints.style.display = DisplayStyle.None;
        }

        public void SetTestCoverOpacity(float opacity) => testResultView.SetCoverOpacity(opacity);

        public void PlayDamageFlash() => resourceHud?.PlayDamageFlash();

        public void ShowTestComment(string text, Sprite portrait)
        {
            if (portrait == null) portrait = scientistPortrait;
            HideScientistComment();
            communicationInProgress = true;
            scientistRoutine = StartCoroutine(RunScientistComment(new[] { text }, portrait, 7f, true));
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
            if (pauseMenu != null && pauseMenu.IsOpen) return;
            if (playerInput != null && playerInput.GameplayActionsLocked && !testEnding) return;
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
            counterLabels[index].text = counterValues[index] + "\u00D7";
            if (index == 0 && cheeseCounterFrames != null && cheeseCounterFrames.Length > 0)
            {
                int frameIndex = Mathf.Clamp(cheeseCounterFrames.Length - 1 - counterValues[index],
                    0, cheeseCounterFrames.Length - 1);
                Sprite sprite = cheeseCounterFrames[frameIndex];
                if (sprite != null) SetCounterSprite(index, sprite);
            }
        }

        private void SetCounterSprite(int index, Sprite sprite)
        {
            Image image = counterImages[index];
            image.sprite = sprite;
            image.style.width = sprite != null ? Mathf.RoundToInt(sprite.rect.width) : 16;
            image.style.height = sprite != null ? Mathf.RoundToInt(sprite.rect.height) : 16;
        }

        private void RefreshDevicePrompts()
        {
            pauseMenu?.SetDevice(devicePrompts);
            interactionAvailability?.SetUsingGamepad(devicePrompts.UsingGamepad);
            Sprite advanceIcon = GetPromptIcon(InputPromptAction.Advance);
            scientistView.SetAdvanceKey(devicePrompts.Advance, advanceIcon);
            testResultView.SetAdvanceKey(devicePrompts.Advance, advanceIcon);
            photoView.SetCloseKey(devicePrompts.Advance);
            RefreshCameraHeightLock();
            RefreshInteractionHints();
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
                    cameraHeightLock.text = "CAMERA CONTROL \u2014 " + devicePrompts.Jump + " TO LOCK / "
                        + devicePrompts.CameraHeightLock + " TO RETURN";
                    break;
                case PlatformCameraController.CameraMode.Fixed:
                    cameraHeightLock.text = "CAMERA LOCKED \u2014 " + devicePrompts.Jump + " TO ADJUST / "
                        + devicePrompts.CameraHeightLock + " TO CONTROL MOUSE";
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
            cameraLockReminder.text = "CAMERA LOCKED\nPress " + devicePrompts.Jump
                + " to move the camera\nor " + devicePrompts.CameraHeightLock + " to control the mouse";
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
            scientistView.ClearSystemCommands();
            bool blocked = communicationInProgress || scientistView.IsVisible || testEnding
                || Time.timeScale <= 0f || photoView.IsVisible || testResultView.IsVisible;
            if (blocked)
            {
                interactionHints.style.display = DisplayStyle.None;
                scientistView.ShowSystemHints(false);
                return;
            }

            AvailableGameplayActions available = interactionAvailability != null
                ? interactionAvailability.RefreshCurrent() : AvailableGameplayActions.None;
            // Refreshing availability can invoke gameplay callbacks that start a communication.
            if (communicationInProgress || scientistView.IsVisible)
            {
                interactionHints.style.display = DisplayStyle.None;
                scientistView.ShowSystemHints(false);
                return;
            }
            if ((available & AvailableGameplayActions.MoveCrate) != 0)
                AddInteractionHint(InputPromptAction.MoveCrate, devicePrompts.MoveCrate, "Hold {key} and move to push or pull.");
            if ((available & AvailableGameplayActions.ClimbLedge) != 0)
                AddInteractionHint(InputPromptAction.Up, devicePrompts.Up,
                    "Press {key} to climb up, or " + devicePrompts.Jump + " toward the ledge. Down to drop; "
                    + devicePrompts.Jump + " away to jump off. If blocked, " + devicePrompts.Jump + " + Up to jump upward.");
            if ((available & AvailableGameplayActions.EatCheese) != 0)
                AddInteractionHint(InputPromptAction.Collect, devicePrompts.Collect, "Press {key} to eat the cheese.");
            if ((available & AvailableGameplayActions.EatGreenCheese) != 0)
                AddInteractionHint(InputPromptAction.Collect, devicePrompts.Collect, "Press {key} to eat the green cheese.");
            if ((available & AvailableGameplayActions.CollectPhoto) != 0)
                AddInteractionHint(InputPromptAction.Collect, devicePrompts.Collect, "Press {key} to collect the photo.");
            if ((available & AvailableGameplayActions.PressButton) != 0)
                AddInteractionHint(InputPromptAction.Collect, devicePrompts.Collect, "Press {key} to activate the button.");
            if ((available & AvailableGameplayActions.UseWheel) != 0)
                AddInteractionHint(InputPromptAction.Collect, devicePrompts.Collect,
                    interactionAvailability.IsUsingWheel ? "Press {key} to step off the wheel." : "Press {key} to use the wheel.");
            if ((available & AvailableGameplayActions.CutRope) != 0)
                AddInteractionHint(InputPromptAction.CutOrDig, devicePrompts.CutOrDig, "Hold {key} to cut the rope.");
            if ((available & AvailableGameplayActions.Dig) != 0)
                AddInteractionHint(InputPromptAction.CutOrDig, devicePrompts.CutOrDig, "Hold {key} and choose a direction to dig.");
            bool showSystem = interactionHints.childCount > 0;
            interactionHints.style.display = showSystem ? DisplayStyle.Flex : DisplayStyle.None;
            scientistView.ShowSystemHints(showSystem);
        }

        private Sprite GetPromptIcon(InputPromptAction action)
        {
            return inputPromptIcons != null ? inputPromptIcons.Get(devicePrompts.Family, action) : null;
        }

        private void AddInteractionHint(InputPromptAction action, string key, string description)
        {
            VisualElement row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("interaction-hint-row");
            scientistView.AddSystemCommand(key, GetPromptIcon(action));
            Label label = new Label(description.Replace("{key}", key)) { pickingMode = PickingMode.Ignore };
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
            hudRoot.Q(className: "black-band-top").style.height = viewport.VerticalBorderPixels;
            hudRoot.Q(className: "black-band-bottom").style.height = viewport.BottomBorderPixels;
            // Include any external letterboxing so the margins are measured from the screen edges.
            float bottomLetterbox = (Screen.height - viewport.FrameRect.yMax) / scale;
            float bottomAnchor = 10f - viewport.BottomBorderPixels - bottomLetterbox;
            hudRoot.Q("resource-dashboard").style.bottom = bottomAnchor;
            hudRoot.Q(className: "timer-anchor").style.top =
                1f - viewport.VerticalBorderPixels - viewport.FrameRect.y / scale;
            scientistView.SetBottomInset(bottomAnchor);
        }

        private void OnDisable()
        {
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
            if (advanceScientistComment != null)
            {
                advanceScientistComment.performed -= OnAdvanceScientistComment;
                advanceScientistComment.Dispose();
            }
            scrollScientistComment?.Dispose();
        }
    }
}
