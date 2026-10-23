using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class ScientistCommentView : IDisposable
    {
        public event Action<char> CharacterRevealed;

        private const float CharactersPerSecond = 30f;
        private float displayWidth = 160f;
        private float displayHeight = 46f;
        private readonly VisualElement screen;
        private readonly ScientistEndingLayout endingLayout;
        private readonly VisualElement displayViewport;
        private readonly VisualElement display;
        private readonly VisualElement displayAperture;
        private readonly VisualElement signal;
        private readonly VisualElement content;
        private readonly VisualElement powerGlow;
        private readonly Image avatar;
        private readonly ScientistSpeechSettings speechSettings;
        private readonly ScientistSpeechAnimator speechAnimator;
        private bool animatePortrait;
        private readonly Label message;
        private readonly ScrollView transcript;
        private readonly Button advanceButton;
        private readonly InputPromptElement advanceIcon;
        private readonly Label advanceAction;
        private bool canAdvance;
        private bool typing;
        private bool completeTypingRequested;
        private bool nextMessageRequested;
        private bool followTyping;
        private bool hasFollowingMessage;
        private bool communicationVisible;

        public void SetAdvanceKey(InputPromptBinding binding)
        {
            advanceIcon.SetPrompt(binding);
            advanceIcon.style.display = DisplayStyle.Flex;
            advanceButton.tooltip = binding.Label;
            RefreshAdvanceLabel();
        }

        private void RefreshAdvanceLabel()
        {
            string action = typing ? "Reveal" : hasFollowingMessage ? "Next" : "Close";
            advanceAction.text = action;
        }

        public bool IsOpen => canAdvance;
        public bool IsVisible => communicationVisible;
        public bool IsScientistSpeaking => communicationVisible && animatePortrait;
        private bool suspended;

        public void SetSuspended(bool value) => suspended = value;

        public void SetEndingPresentation(bool value) => endingLayout.SetActive(value);

        public ScientistCommentView(VisualElement root, ScientistSpeechSettings speechSettings = null)
        {
            this.speechSettings = speechSettings;
            speechAnimator = new ScientistSpeechAnimator(speechSettings);
            screen = root.Q("scientist-screen");
            endingLayout = new ScientistEndingLayout(root, screen);
            displayViewport = root.Q("scientist-display-viewport");
            display = root.Q("scientist-display");
            displayAperture = root.Q("scientist-display-aperture")
                ?? new VisualElement { name = "scientist-display-aperture", pickingMode = PickingMode.Ignore };
            displayAperture.style.position = Position.Absolute;
            displayAperture.style.overflow = Overflow.Hidden;
            if (display.parent != displayAperture)
            {
                VisualElement displayParent = display.parent;
                int displayIndex = displayParent.IndexOf(display);
                displayParent.Insert(displayIndex, displayAperture);
                displayAperture.Add(display);
            }
            signal = root.Q("scientist-signal");
            content = root.Q("scientist-content");
            powerGlow = root.Q("scientist-power-glow");
            avatar = root.Q<Image>("scientist-avatar");
            avatar.scaleMode = ScaleMode.ScaleToFit;
            message = root.Q<Label>("scientist-text");
            transcript = root.Q<ScrollView>("scientist-transcript");
            transcript.pickingMode = PickingMode.Position;
            transcript.contentViewport.pickingMode = PickingMode.Position;
            transcript.tooltip = "Scroll: mouse wheel, Page Up/Page Down or right stick";
            transcript.RegisterCallback<WheelEvent>(OnManualScroll, TrickleDown.TrickleDown);
            advanceButton = root.Q<Button>("scientist-advance");
            advanceButton.text = string.Empty;
            advanceIcon = new InputPromptElement();
            advanceIcon.AddToClassList("input-key-icon");
            advanceIcon.style.display = DisplayStyle.None;
            advanceAction = new Label { pickingMode = PickingMode.Ignore };
            advanceAction.AddToClassList("scientist-advance-label");
            advanceButton.Add(advanceIcon);
            advanceButton.Add(advanceAction);
            advanceButton.pickingMode = PickingMode.Position;
            advanceButton.focusable = false;
            advanceButton.clicked += Advance;
            displayViewport.RegisterCallback<GeometryChangedEvent>(OnDisplayGeometryChanged);
            Hide();
        }

        private void OnDisplayGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            if (geometryEvent.newRect.width <= 0f || geometryEvent.newRect.height <= 0f) return;
            displayWidth = Mathf.Round(geometryEvent.newRect.width);
            displayHeight = Mathf.Round(geometryEvent.newRect.height);
            display.style.width = displayWidth;
            display.style.height = displayHeight;
            if (canAdvance) SetDisplaySize(displayWidth, displayHeight);
        }

        public void Advance()
        {
            if (!canAdvance) return;
            if (typing) completeTypingRequested = true;
            else nextMessageRequested = true;
        }

        public void ScrollText(float offset)
        {
            if (!IsOpen || Mathf.Abs(offset) < 0.001f) return;
            followTyping = false;
            transcript.scrollOffset = new Vector2(0f, Mathf.Clamp(transcript.scrollOffset.y + offset,
                0f, Mathf.Max(0f, transcript.verticalScroller.highValue)));
        }

        private void OnManualScroll(WheelEvent wheelEvent)
        {
            if (IsOpen) followTyping = false;
        }

        public IEnumerator Show(string[] texts, Sprite portrait, float timeout, bool waitForAdvance = false)
        {
            if (texts == null || texts.Length == 0)
            {
                Hide();
                yield break;
            }

            // Let UI Toolkit measure the enlarged viewport before starting the CRT aperture animation.
            if (endingLayout.IsActive) yield return null;

            communicationVisible = true;
            message.style.display = DisplayStyle.Flex;
            advanceButton.style.display = DisplayStyle.Flex;
            signal.AddToClassList("signal-active");
            speechAnimator.Reset();
            animatePortrait = speechSettings != null && speechSettings.Recognizes(portrait);
            avatar.sprite = animatePortrait ? speechSettings.IdlePortrait : portrait;
            // Keep the monitor frame visible even before an actual scientist portrait is supplied.
            avatar.style.display = portrait != null ? DisplayStyle.Flex : DisplayStyle.None;
            avatar.style.opacity = 0f;
            message.text = string.Empty;
            content.style.opacity = 0f;
            display.style.display = DisplayStyle.Flex;
            SetDisplaySize(2f, 2f);
            powerGlow.style.opacity = 1f;
            screen.style.display = DisplayStyle.Flex;
            advanceButton.SetEnabled(false);

            yield return AnimateSize(2f, displayWidth, 2f, 2f, 0.12f);
            yield return AnimateSize(displayWidth, displayWidth, 2f, displayHeight, 0.18f);
            yield return AnimatePowerGlow(1f, 0f, 0.14f, true);
            advanceButton.SetEnabled(true);

            float pageTimeout = Mathf.Clamp(timeout, 1f, 30f);
            for (int page = 0; page < texts.Length; page++)
            {
                string text = texts[page];
                speechAnimator.Reset();
                float charactersPerSecond = speechSettings != null ? speechSettings.TextCharactersPerSecond : CharactersPerSecond;
                int[] characterStarts = StringInfo.ParseCombiningCharacters(text);
                int revealedCharacters = 0;
                float elapsed = 0f;
                typing = true;
                canAdvance = true;
                completeTypingRequested = false;
                nextMessageRequested = false;
                followTyping = true;
                message.text = string.Empty;
                transcript.scrollOffset = Vector2.zero;
                hasFollowingMessage = waitForAdvance || page < texts.Length - 1;
                RefreshAdvanceLabel();

                while (elapsed < pageTimeout && !nextMessageRequested)
                {
                    if (suspended) { yield return null; continue; }
                    elapsed += Time.unscaledDeltaTime;
                    int visibleCharacters = completeTypingRequested ? characterStarts.Length
                        : Mathf.Min(characterStarts.Length, Mathf.FloorToInt(elapsed * charactersPerSecond));
                    if (visibleCharacters != revealedCharacters)
                    {
                        int previousCharacters = revealedCharacters;
                        revealedCharacters = visibleCharacters;
                        int endIndex = revealedCharacters < characterStarts.Length
                            ? characterStarts[revealedCharacters] : text.Length;
                        message.text = text.Substring(0, endIndex);
                        if (!completeTypingRequested)
                            for (int character = previousCharacters; character < revealedCharacters; character++)
                                CharacterRevealed?.Invoke(text[characterStarts[character]]);
                        if (animatePortrait)
                        {
                            if (completeTypingRequested) speechAnimator.Reset();
                            else if (revealedCharacters > 0) speechAnimator.Speak(text[characterStarts[revealedCharacters - 1]]);
                        }
                    }
                    typing = revealedCharacters < characterStarts.Length;
                    if (animatePortrait)
                    {
                        if (!typing) speechAnimator.RequestRest();
                        Sprite nextFrame = speechAnimator.Tick(Time.unscaledDeltaTime);
                        if (avatar.sprite != nextFrame) avatar.sprite = nextFrame;
                    }
                    RefreshAdvanceLabel();

                    if (followTyping)
                        transcript.scrollOffset = new Vector2(0f, Mathf.Max(0f, transcript.verticalScroller.highValue));
                    yield return null;
                }
                canAdvance = false;
            }

            advanceButton.SetEnabled(false);
            content.style.opacity = 0f;
            powerGlow.style.opacity = 0.85f;
            avatar.style.opacity = 0f;
            yield return AnimateSize(displayWidth, displayWidth, displayHeight, 2f, 0.16f);
            powerGlow.style.opacity = 1f;
            yield return AnimateSize(displayWidth, 2f, 2f, 2f, 0.12f);
            yield return AnimatePowerGlow(1f, 0f, 0.1f, false);
            Hide();
        }

        public void Hide()
        {
            speechAnimator.Reset();
            animatePortrait = false;
            canAdvance = false;
            typing = false;
            communicationVisible = false;
            signal.RemoveFromClassList("signal-active");
            screen.style.display = DisplayStyle.None;
            display.style.display = DisplayStyle.None;
            message.text = string.Empty;
            avatar.sprite = null;
            avatar.style.opacity = 0f;
            advanceButton.SetEnabled(false);
            content.style.opacity = 0f;
            powerGlow.style.opacity = 0f;
            if (endingLayout.IsActive) endingLayout.SetActive(false);
        }

        public void Dispose()
        {
            advanceButton.clicked -= Advance;
            transcript.UnregisterCallback<WheelEvent>(OnManualScroll, TrickleDown.TrickleDown);
            displayViewport.UnregisterCallback<GeometryChangedEvent>(OnDisplayGeometryChanged);
            Hide();
            advanceIcon.RemoveFromHierarchy();
            advanceAction.RemoveFromHierarchy();
        }

        private IEnumerator AnimateSize(float fromWidth, float toWidth, float fromHeight, float toHeight, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (suspended) { yield return null; continue; }
                elapsed += Time.unscaledDeltaTime;
                float progressValue = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetDisplaySize(Mathf.Lerp(fromWidth, toWidth, progressValue),
                    Mathf.Lerp(fromHeight, toHeight, progressValue));
                yield return null;
            }
            SetDisplaySize(toWidth, toHeight);
        }

        private void SetDisplaySize(float width, float height)
        {
            width = Mathf.Round(width);
            height = Mathf.Round(height);
            float left = Mathf.Floor((displayWidth - width) * 0.5f);
            float top = Mathf.Floor((displayHeight - height) * 0.5f);
            displayAperture.style.width = width;
            displayAperture.style.height = height;
            displayAperture.style.left = left;
            displayAperture.style.top = top;
            // Animate only the clipping window; the authored text and portrait layout stays intact.
            display.style.width = displayWidth;
            display.style.height = displayHeight;
            display.style.left = -left;
            display.style.top = -top;
        }

        private IEnumerator AnimatePowerGlow(float from, float to, float duration, bool revealContent)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (suspended) { yield return null; continue; }
                elapsed += Time.unscaledDeltaTime;
                float progressValue = Mathf.Clamp01(elapsed / duration);
                powerGlow.style.opacity = Mathf.Lerp(from, to, progressValue);
                if (revealContent)
                {
                    content.style.opacity = progressValue;
                    avatar.style.opacity = progressValue;
                }
                yield return null;
            }
            powerGlow.style.opacity = to;
            if (revealContent)
            {
                content.style.opacity = 1f;
                avatar.style.opacity = 1f;
            }
        }
    }
}
