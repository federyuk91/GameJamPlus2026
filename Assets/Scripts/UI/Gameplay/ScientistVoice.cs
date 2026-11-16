using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent]
    public sealed class ScientistVoice : MonoBehaviour
    {
        public enum VoiceTone { Neutral = 0, Reassuring = 1, Warning = 2 }

        [Serializable]
        private sealed class LetterSound
        {
            public string letters;
            public AudioClip clip;
        }

        [Serializable]
        private sealed class CombinationSound
        {
            public string letters;
            public AudioClip clip;
            [Tooltip("Treat the clip as a vowel cue at full level, rather than a soft accent.")]
            public bool voiced;
        }

        [SerializeField] private AudioSource voiceAudio;
        [SerializeField] private LetterSound[] letterSounds = new LetterSound[0];
        [SerializeField] private AudioClip otherLetterClip;
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 0.35f;
        [SerializeField, Range(0.5f, 2f)] private float voicePitch = 1f;
        [SerializeField, Range(0f, 0.25f)] private float pitchVariation = 0.08f;
        [SerializeField, Min(0.02f)] private float minimumInterval = 0.085f;
        [SerializeField, Min(0f)] private float punctuationPause = 0.15f;
        [SerializeField, TextArea(2, 5)] private string testText = "Welcome to Universe-10. Relax and enjoy your stay.";
        [SerializeField, Min(1f)] private float testCharactersPerSecond = 30f;
        [SerializeField, Tooltip("Delivery used only by the Inspector voice preview.")]
        private VoiceTone testTone = VoiceTone.Neutral;
        [SerializeField, Tooltip("Letter combinations that add short accents in Test Voice.")]
        private CombinationSound[] testCombinations = Array.Empty<CombinationSound>();

        public string TestText => testText;
        public float TestCharactersPerSecond => Mathf.Max(1f, testCharactersPerSecond);
        public VoiceTone TestTone => testTone;

        private double nextSoundTime;

        private void Awake()
        {
            if (voiceAudio == null) return;
            voiceAudio.playOnAwake = false;
            voiceAudio.loop = false;
            voiceAudio.spatialBlend = 0f;
        }

        public void Speak(char character)
        {
            if (!isActiveAndEnabled || voiceAudio == null) return;
            PlayCharacter(character, Time.unscaledTimeAsDouble, voiceAudio, ref nextSoundTime);
        }

#if UNITY_EDITOR
        public AudioClip GetPreviewVowelClip(char vowel) => GetLetterClip(vowel);

        public AudioClip GetPreviewCombination(string normalizedText, int start, out int length, out bool voiced)
        {
            length = 0;
            voiced = false;
            AudioClip clip = null;
            foreach (CombinationSound sound in testCombinations)
            {
                if (sound == null || sound.clip == null || string.IsNullOrEmpty(sound.letters)) continue;
                int count = sound.letters.Length;
                if (count < 2 || count <= length || count > normalizedText.Length - start) continue;
                bool matches = true;
                for (int index = 0; index < count; index++)
                {
                    char letter = NormalizeLetter(sound.letters[index]);
                    if (char.IsLetter(letter) && normalizedText[start + index] == letter) continue;
                    matches = false;
                    break;
                }
                if (!matches) continue;
                length = count;
                clip = sound.clip;
                voiced = sound.voiced;
            }
            return clip;
        }

        public void PlayPreviewClip(AudioClip clip, float pitchContour, float gain, AudioSource previewAudio)
        {
            if (clip != null && previewAudio != null) PlayClip(previewAudio, clip, pitchContour, gain);
        }
#endif

        private void PlayCharacter(char character, double now, AudioSource source, ref double nextTime)
        {
            if (char.IsPunctuation(character))
            {
                // Apostrophes belong to words; other punctuation separates vocal phrases.
                if (character != '\'' && character != '\u2019')
                    nextTime = Math.Max(nextTime, now + punctuationPause);
                return;
            }
            if (!char.IsLetter(character) || now < nextTime || source.isPlaying) return;

            char letter = NormalizeLetter(character);
            AudioClip clip = GetLetterClip(letter);
            if (clip == null) return;

            float letterPitch = ((letter * 17) % 11 - 5) / 5f;
            PlayClip(source, clip, letterPitch);
            nextTime = now + minimumInterval;
        }

        private void PlayClip(AudioSource source, AudioClip clip, float pitchOffset, float gain = 1f)
        {
            source.pitch = Mathf.Clamp(voicePitch + pitchOffset * pitchVariation, 0.5f, 2f);
            source.volume = Mathf.Clamp01(voiceVolume * GameAudioSettings.EffectsVolume * gain);
            source.clip = clip;
            source.Play();
        }

        public static char NormalizeLetter(char character)
        {
            char letter = char.ToUpperInvariant(character);
            if (letter > 127 && !char.IsSurrogate(letter))
            {
                string normalized = letter.ToString().Normalize(NormalizationForm.FormD);
                foreach (char candidate in normalized)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(candidate) == UnicodeCategory.NonSpacingMark) continue;
                    letter = candidate;
                    break;
                }
            }

            return letter;
        }

        private AudioClip GetLetterClip(char letter)
        {
            foreach (LetterSound sound in letterSounds)
            {
                if (sound == null || string.IsNullOrEmpty(sound.letters)
                    || sound.letters.IndexOf(letter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                return sound.clip;
            }
            return otherLetterClip;
        }

        public void StopSpeaking()
        {
            if (voiceAudio != null) voiceAudio.Stop();
            nextSoundTime = 0d;
        }

        private void OnDisable() => StopSpeaking();
    }
}
