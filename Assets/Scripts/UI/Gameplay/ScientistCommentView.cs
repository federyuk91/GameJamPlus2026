using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class ScientistCommentView : IDisposable
    {
        private const float ScreenInset = 10f;
        private float displayWidth = 160f;
        private float displayHeight = 46f;
        private const float CharactersPerSecond = 30f;
        private readonly VisualElement screen;
        private readonly VisualElement avatarScreen;
        private readonly VisualElement displayViewport;
        private readonly VisualElement display;
        private readonly VisualElement signal;
        private readonly VisualElement content;
        private readonly VisualElement powerGlow;
        private readonly Image avatar;
        private readonly Label message;
        private readonly Label speaker;
        private readonly VisualElement hints;
        private readonly ScrollView transcript;
        private readonly Button advanceButton;
        private readonly VisualElement advanceIcon;
        private readonly Label advanceAction;
        private Sprite advanceSprite;
        private bool canAdvance;
        private bool typing;
        private bool completeTypingRequested;
        private bool nextMessageRequested;
        private bool followTyping;
        private string advanceKey = "F";
        private bool hasFollowingMessage;
        private bool communicationVisible;
        private bool systemVisible;

        public void SetAdvanceKey(string key, Sprite sprite = null)
        {
            advanceKey = key;
            advanceSprite = sprite;
            advanceIcon.style.display = sprite != null ? DisplayStyle.Flex : DisplayStyle.None;
            advanceIcon.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
            advanceButton.tooltip = key;
            RefreshAdvanceLabel();
        }

        private void RefreshAdvanceLabel()
        {
            string action = typing ? "Reveal" : hasFollowingMessage ? "Next" : "Close";
            advanceAction.text = advanceSprite != null ? action : advanceKey + " " + action;
        }

        public bool IsOpen => canAdvance || systemVisible;
        public bool IsVisible => communicationVisible;

        public void SetBottomInset(float inset)
        {
            screen.style.bottom = inset;
            avatarScreen.style.bottom = inset;
        }

        public ScientistCommentView(VisualElement root, int lowerBorderPixels = 20)
        {
            screen = root.Q("scientist-screen");
            avatarScreen = root.Q("scientist-avatar-screen");
            displayViewport = root.Q("scientist-display-viewport");
            display = root.Q("scientist-display");
            signal = root.Q("scientist-signal");
            content = root.Q("scientist-content");
            powerGlow = root.Q("scientist-power-glow");
            avatar = root.Q<Image>("scientist-avatar");
            avatar.scaleMode = ScaleMode.ScaleToFit;
            message = root.Q<Label>("scientist-text");
            speaker = root.Q<Label>("communication-speaker");
            hints = root.Q("interaction-hints");
            transcript = root.Q<ScrollView>("scientist-transcript");
            transcript.pickingMode = PickingMode.Position;
            transcript.contentViewport.pickingMode = PickingMode.Position;
            transcript.tooltip = "Scorri: rotella, PagSu/PagGiù o stick destro";
            transcript.RegisterCallback<WheelEvent>(OnManualScroll, TrickleDown.TrickleDown);
            advanceButton = root.Q<Button>("scientist-advance");
            advanceButton.text = string.Empty;
            advanceIcon = new VisualElement { pickingMode = PickingMode.Ignore };
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
            SetBottomInset(ScreenInset - lowerBorderPixels);
            Hide();
        }

        private void OnDisplayGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            if (geometryEvent.newRect.width <= 0f || geometryEvent.newRect.height <= 0f) return;
            displayWidth = Mathf.Round(geometryEvent.newRect.width);
            displayHeight = Mathf.Round(geometryEvent.newRect.height);
            content.style.width = displayWidth;
            content.style.height = displayHeight;
            if (systemVisible || canAdvance) SetDisplaySize(displayWidth, displayHeight);
        }

        public void ShowSystemHints(bool visible)
        {
            if (communicationVisible) return;
            if (!visible)
            {
                if (systemVisible) Hide();
                return;
            }
            if (!systemVisible) transcript.scrollOffset = Vector2.zero;
            systemVisible = true;
            speaker.text = "SYSTEM";
            speaker.style.display = DisplayStyle.Flex;
            message.style.display = DisplayStyle.None;
            hints.style.display = DisplayStyle.Flex;
            content.RemoveFromClassList("scientist-dialogue");
            advanceButton.style.display = DisplayStyle.None;
            avatar.sprite = null;
            avatar.style.opacity = 0f;
            signal.AddToClassList("signal-active");
            display.style.display = DisplayStyle.Flex;
            content.style.opacity = 1f;
            powerGlow.style.opacity = 0f;
            SetDisplaySize(displayWidth, displayHeight);
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

            communicationVisible = true;
            systemVisible = false;
            hints.Clear();
            speaker.style.display = DisplayStyle.None;
            message.style.display = DisplayStyle.Flex;
            hints.style.display = DisplayStyle.None;
            content.AddToClassList("scientist-dialogue");
            advanceButton.style.display = DisplayStyle.Flex;
            signal.AddToClassList("signal-active");
            avatar.sprite = portrait;
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

                while ((waitForAdvance || elapsed < pageTimeout) && !nextMessageRequested)
                {
                    elapsed += Time.unscaledDeltaTime;
                    int visibleCharacters = completeTypingRequested ? characterStarts.Length
                        : Mathf.Min(characterStarts.Length, Mathf.FloorToInt(elapsed * CharactersPerSecond));
                    if (visibleCharacters != revealedCharacters)
                    {
                        revealedCharacters = visibleCharacters;
                        int endIndex = revealedCharacters < characterStarts.Length
                            ? characterStarts[revealedCharacters] : text.Length;
                        message.text = text.Substring(0, endIndex);
                    }
                    typing = revealedCharacters < characterStarts.Length;
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
            canAdvance = false;
            typing = false;
            communicationVisible = false;
            systemVisible = false;
            signal.RemoveFromClassList("signal-active");
            screen.style.display = DisplayStyle.Flex;
            display.style.display = DisplayStyle.None;
            message.text = string.Empty;
            avatar.sprite = null;
            hints.Clear();
            hints.style.display = DisplayStyle.None;
            avatar.style.opacity = 0f;
            advanceButton.SetEnabled(false);
            content.style.opacity = 0f;
            powerGlow.style.opacity = 0f;
        }

        public void Dispose()
        {
            advanceButton.clicked -= Advance;
            transcript.UnregisterCallback<WheelEvent>(OnManualScroll, TrickleDown.TrickleDown);
            displayViewport.UnregisterCallback<GeometryChangedEvent>(OnDisplayGeometryChanged);
            Hide();
        }

        private IEnumerator AnimateSize(float fromWidth, float toWidth, float fromHeight, float toHeight, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
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
            content.style.width = displayWidth;
            content.style.height = displayHeight;
            display.style.width = width;
            display.style.height = height;
            display.style.left = left;
            display.style.top = top;
            // Reveal the fixed-size content through a centred CRT aperture without reflowing the text.
            content.style.left = -left;
            content.style.top = -top;
        }

        private IEnumerator AnimatePowerGlow(float from, float to, float duration, bool revealContent)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
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
