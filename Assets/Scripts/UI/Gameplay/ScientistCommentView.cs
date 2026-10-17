using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class ScientistCommentView : IDisposable
    {
        private const float ScreenWidth = 300f;
        private const float ScreenHeight = 60f;
        private const float ScreenInset = 10f;
        private float bottomInset;
        private const float CharactersPerSecond = 30f;
        private readonly VisualElement screen;
        private readonly VisualElement content;
        private readonly VisualElement powerGlow;
        private readonly VisualElement portraitFrame;
        private readonly Image avatar;
        private readonly Label message;
        private readonly Label progress;
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
        private string advanceKey = "Enter";
        private bool hasFollowingMessage;

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

        public bool IsOpen => canAdvance;
        public bool IsVisible => screen.style.display.value != DisplayStyle.None;

        public void SetBottomInset(float inset)
        {
            bottomInset = inset;
            if (screen.style.display.value != DisplayStyle.None)
                SetScreenSize(screen.style.width.value.value, screen.style.height.value.value);
        }

        public ScientistCommentView(VisualElement root, int lowerBorderPixels = 20)
        {
            bottomInset = ScreenInset - lowerBorderPixels;
            screen = root.Q("scientist-screen");
            content = root.Q("scientist-content");
            powerGlow = root.Q("scientist-power-glow");
            portraitFrame = root.Q("scientist-portrait-frame");
            avatar = root.Q<Image>("scientist-avatar");
            avatar.scaleMode = ScaleMode.ScaleToFit;
            message = root.Q<Label>("scientist-text");
            progress = root.Q<Label>("scientist-progress");
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
            Hide();
        }

        public void Advance()
        {
            if (!canAdvance) return;
            if (typing) completeTypingRequested = true;
            else nextMessageRequested = true;
        }

        public void ScrollText(float offset)
        {
            if (!canAdvance || Mathf.Abs(offset) < 0.001f) return;
            followTyping = false;
            transcript.scrollOffset = new Vector2(0f, Mathf.Clamp(transcript.scrollOffset.y + offset,
                0f, Mathf.Max(0f, transcript.verticalScroller.highValue)));
        }

        private void OnManualScroll(WheelEvent wheelEvent)
        {
            if (canAdvance) followTyping = false;
        }

        public IEnumerator Show(string[] texts, Sprite portrait, float timeout, bool waitForAdvance = false)
        {
            avatar.sprite = portrait;
            // Keep the monitor frame visible even before an actual scientist portrait is supplied.
            avatar.style.display = portrait != null ? DisplayStyle.Flex : DisplayStyle.None;
            portraitFrame.EnableInClassList("portrait-missing", portrait == null);
            message.text = string.Empty;
            progress.text = string.Empty;
            content.style.opacity = 0f;
            SetScreenSize(2f, 2f);
            powerGlow.style.opacity = 1f;
            screen.style.display = DisplayStyle.Flex;
            advanceButton.SetEnabled(false);

            yield return AnimateSize(2f, ScreenWidth, 2f, 2f, 0.12f);
            yield return AnimateSize(ScreenWidth, ScreenWidth, 2f, ScreenHeight, 0.18f);
            yield return AnimatePowerGlow(1f, 0f, 0.14f, true);
            advanceButton.SetEnabled(true);

            float pageTimeout = Mathf.Clamp(timeout, 1f, 30f);
            for (int page = 0; page < texts.Length; page++)
            {
                string text = texts[page];
                int[] characterStarts = StringInfo.ParseCombiningCharacters(text);
                int revealedCharacters = 0;
                int displayedCountdown = -1;
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

                    int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(pageTimeout - elapsed));
                    if (remainingSeconds != displayedCountdown)
                    {
                        displayedCountdown = remainingSeconds;
                        progress.text = waitForAdvance ? "Confirm to continue" : $"{page + 1}/{texts.Length} · {remainingSeconds}s";
                    }
                    if (followTyping)
                        transcript.scrollOffset = new Vector2(0f, Mathf.Max(0f, transcript.verticalScroller.highValue));
                    yield return null;
                }
                canAdvance = false;
            }

            advanceButton.SetEnabled(false);
            content.style.opacity = 0f;
            powerGlow.style.opacity = 0.85f;
            yield return AnimateSize(ScreenWidth, ScreenWidth, ScreenHeight, 2f, 0.16f);
            powerGlow.style.opacity = 1f;
            yield return AnimateSize(ScreenWidth, 2f, 2f, 2f, 0.12f);
            yield return AnimatePowerGlow(1f, 0f, 0.1f, false);
            Hide();
        }

        public void Hide()
        {
            canAdvance = false;
            typing = false;
            screen.style.display = DisplayStyle.None;
            content.style.opacity = 0f;
            powerGlow.style.opacity = 0f;
        }

        public void Dispose()
        {
            advanceButton.clicked -= Advance;
            transcript.UnregisterCallback<WheelEvent>(OnManualScroll, TrickleDown.TrickleDown);
            Hide();
        }

        private IEnumerator AnimateSize(float fromWidth, float toWidth, float fromHeight, float toHeight, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progressValue = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetScreenSize(Mathf.Lerp(fromWidth, toWidth, progressValue),
                    Mathf.Lerp(fromHeight, toHeight, progressValue));
                yield return null;
            }
            SetScreenSize(toWidth, toHeight);
        }

        private void SetScreenSize(float width, float height)
        {
            width = Mathf.Round(width);
            height = Mathf.Round(height);
            screen.style.width = width;
            screen.style.height = height;
            // A CRT collapses around its centre, while the final panel remains anchored at the bottom right.
            screen.style.right = ScreenInset + Mathf.Floor((ScreenWidth - width) * 0.5f);
            screen.style.bottom = bottomInset + Mathf.Floor((ScreenHeight - height) * 0.5f);
        }

        private IEnumerator AnimatePowerGlow(float from, float to, float duration, bool revealContent)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progressValue = Mathf.Clamp01(elapsed / duration);
                powerGlow.style.opacity = Mathf.Lerp(from, to, progressValue);
                if (revealContent) content.style.opacity = progressValue;
                yield return null;
            }
            powerGlow.style.opacity = to;
            if (revealContent) content.style.opacity = 1f;
        }
    }
}
