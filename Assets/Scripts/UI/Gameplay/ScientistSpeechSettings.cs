using System;
using System.Globalization;
using UnityEngine;

namespace GameJamPlus.UI
{
    [CreateAssetMenu(menuName = "Game/Scientist Speech Settings")]
    public sealed class ScientistSpeechSettings : ScriptableObject
    {
        [Serializable]
        private sealed class LetterAnimation
        {
            public string letters;
            public Sprite[] frames = new Sprite[0];
        }

        [SerializeField] private Sprite idlePortrait;
        [SerializeField] private Sprite[] idleFrames = new Sprite[0];
        [SerializeField, Min(1f)] private float idleFramesPerSecond = 8f;
        [SerializeField, Tooltip("Other scientist portraits that may be replaced by talking frames. Other avatars remain static.")]
        private Sprite[] recognizedPortraits = new Sprite[0];
        [SerializeField] private LetterAnimation[] letterAnimations = new LetterAnimation[0];
        [SerializeField, Tooltip("Sequence used for letters absent from Letter Animations.")]
        private Sprite[] otherLetterFrames = new Sprite[0];
        [SerializeField, Min(1f)] private float mouthFramesPerSecond = 20f;
        [SerializeField, Min(1f)] private float textCharactersPerSecond = 30f;

        public Sprite IdlePortrait => idleFrames != null && idleFrames.Length > 0 && idleFrames[0] != null ? idleFrames[0] : idlePortrait;
        public int IdleFrameCount => idleFrames != null ? idleFrames.Length : 0;
        public float IdleFrameDuration => 1f / Mathf.Max(1f, idleFramesPerSecond);
        public Sprite IdleFrame(int index) => IdleFrameCount > 0 ? idleFrames[index % IdleFrameCount] ?? IdlePortrait : IdlePortrait;
        public float FrameDuration => 1f / Mathf.Max(1f, mouthFramesPerSecond);
        public float TextCharactersPerSecond => Mathf.Max(1f, textCharactersPerSecond);

        public bool Recognizes(Sprite portrait)
        {
            if (portrait == null || IdlePortrait == null) return false;
            if (portrait == idlePortrait || portrait == IdlePortrait || Array.IndexOf(idleFrames, portrait) >= 0
                || Array.IndexOf(recognizedPortraits, portrait) >= 0) return true;
            if (Array.IndexOf(otherLetterFrames, portrait) >= 0) return true;
            foreach (LetterAnimation animation in letterAnimations)
                if (animation != null && Array.IndexOf(animation.frames, portrait) >= 0) return true;
            return false;
        }

        public Sprite[] FramesFor(char character)
        {
            if (!char.IsLetter(character)) return null;
            char letter = char.ToUpperInvariant(character);
            if (letter > 127)
            {
                string normalized = letter.ToString().Normalize(System.Text.NormalizationForm.FormD);
                foreach (char candidate in normalized)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(candidate) == UnicodeCategory.NonSpacingMark) continue;
                    letter = candidate;
                    break;
                }
            }
            foreach (LetterAnimation animation in letterAnimations)
                if (animation != null && !string.IsNullOrEmpty(animation.letters)
                    && animation.letters.IndexOf(letter, StringComparison.OrdinalIgnoreCase) >= 0
                    && animation.frames != null && animation.frames.Length > 0)
                    return animation.frames;
            return otherLetterFrames;
        }
    }
}
