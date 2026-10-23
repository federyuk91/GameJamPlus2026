using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    internal sealed class ScientistEndingLayout
    {
        private struct SavedLayout
        {
            private VisualElement element;
            private StyleLength left, right, top, bottom, width, height;
            private StyleLength minWidth, maxWidth, minHeight, maxHeight, fontSize, marginBottom;
            private StyleEnum<Justify> justifyContent;

            public SavedLayout(VisualElement element)
            {
                this.element = element;
                IStyle style = element.style;
                left = style.left;
                right = style.right;
                top = style.top;
                bottom = style.bottom;
                width = style.width;
                height = style.height;
                minWidth = style.minWidth;
                maxWidth = style.maxWidth;
                minHeight = style.minHeight;
                maxHeight = style.maxHeight;
                fontSize = style.fontSize;
                marginBottom = style.marginBottom;
                justifyContent = style.justifyContent;
            }

            public void Restore()
            {
                IStyle style = element.style;
                style.left = left;
                style.right = right;
                style.top = top;
                style.bottom = bottom;
                style.width = width;
                style.height = height;
                style.minWidth = minWidth;
                style.maxWidth = maxWidth;
                style.minHeight = minHeight;
                style.maxHeight = maxHeight;
                style.fontSize = fontSize;
                style.marginBottom = marginBottom;
                style.justifyContent = justifyContent;
            }

            public void UseStylesheet()
            {
                IStyle style = element.style;
                style.left = style.right = style.top = style.bottom = StyleKeyword.Null;
                style.width = style.height = StyleKeyword.Null;
                style.minWidth = style.maxWidth = style.minHeight = style.maxHeight = StyleKeyword.Null;
                style.fontSize = style.marginBottom = StyleKeyword.Null;
                style.justifyContent = StyleKeyword.Null;
            }
        }

        private readonly VisualElement frame;
        private readonly VisualElement screen;
        private readonly ScrollView transcript;
        private readonly Label message;
        private readonly Button advance;
        private readonly VisualElement content;
        private readonly Image avatar;
        private readonly VisualElement footer;
        private VisualElement originalAdvanceParent;
        private int originalAdvanceIndex;
        private VisualElement originalParent;
        private int originalIndex;
        private SavedLayout screenLayout, transcriptLayout, messageLayout, advanceLayout;
        private SavedLayout contentLayout, avatarLayout;

        public bool IsActive { get; private set; }

        public ScientistEndingLayout(VisualElement root, VisualElement screen)
        {
            frame = root.Q("hud-frame");
            this.screen = screen;
            transcript = screen.Q<ScrollView>("scientist-transcript");
            message = screen.Q<Label>("scientist-text");
            advance = screen.Q<Button>("scientist-advance");
            content = screen.Q("scientist-content");
            avatar = screen.Q<Image>("scientist-avatar");
            footer = root.Q("scientist-ending-footer");
        }

        public void SetActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;
            if (!active)
            {
                screen.RemoveFromClassList("scientist-ending");
                advance.RemoveFromClassList("scientist-ending-advance");
                originalAdvanceParent.Insert(Mathf.Min(originalAdvanceIndex, originalAdvanceParent.childCount), advance);
                footer.style.display = DisplayStyle.None;
                originalParent.Insert(Mathf.Min(originalIndex, originalParent.childCount), screen);
                screenLayout.Restore();
                transcriptLayout.Restore();
                messageLayout.Restore();
                advanceLayout.Restore();
                contentLayout.Restore();
                avatarLayout.Restore();
                return;
            }

            originalParent = screen.parent;
            originalIndex = originalParent.IndexOf(screen);
            originalAdvanceParent = advance.parent;
            originalAdvanceIndex = originalAdvanceParent.IndexOf(advance);
            screenLayout = new SavedLayout(screen);
            transcriptLayout = new SavedLayout(transcript);
            messageLayout = new SavedLayout(message);
            advanceLayout = new SavedLayout(advance);
            contentLayout = new SavedLayout(content);
            avatarLayout = new SavedLayout(avatar);
            // Center against the full frame, independent of the gameplay safe area's black bands.
            frame.Add(screen);
            frame.Add(footer);
            screen.AddToClassList("scientist-ending");
            screen.style.left = new Length(12.5f, LengthUnit.Percent);
            screen.style.top = new Length(100f / 6f, LengthUnit.Percent);
            screen.style.right = StyleKeyword.Auto;
            screen.style.bottom = StyleKeyword.Auto;
            screen.style.width = new Length(75f, LengthUnit.Percent);
            screen.style.height = new Length(200f / 3f, LengthUnit.Percent);
            contentLayout.UseStylesheet();
            avatarLayout.UseStylesheet();
            transcriptLayout.UseStylesheet();
            messageLayout.UseStylesheet();
            advanceLayout.UseStylesheet();
            footer.Add(advance);
            advance.AddToClassList("scientist-ending-advance");
            footer.style.display = DisplayStyle.Flex;
        }
    }
}
