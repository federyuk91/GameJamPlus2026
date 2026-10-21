using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent]
    public sealed class ScientistVoice : MonoBehaviour
    {
        [Serializable]
        private sealed class LetterSound
        {
            public string letters;
            public AudioClip clip;
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

        public string TestText => testText;
        public float TestCharactersPerSecond => Mathf.Max(1f, testCharactersPerSecond);

        private double nextSoundTime;
#if UNITY_EDITOR
        private double nextPreviewSoundTime;
#endif

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
        public void PreviewCharacter(char character, double time, AudioSource previewAudio)
        {
            if (previewAudio != null) PlayCharacter(character, time, previewAudio, ref nextPreviewSoundTime);
        }

        public void ResetPreview() => nextPreviewSoundTime = 0d;
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

            char letter = char.ToUpperInvariant(character);
            if (letter > 127)
            {
                string normalized = letter.ToString().Normalize(NormalizationForm.FormD);
                foreach (char candidate in normalized)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(candidate) == UnicodeCategory.NonSpacingMark) continue;
                    letter = candidate;
                    break;
                }
            }

            AudioClip clip = otherLetterClip;
            foreach (LetterSound sound in letterSounds)
            {
                if (sound == null || string.IsNullOrEmpty(sound.letters)
                    || sound.letters.IndexOf(letter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                clip = sound.clip;
                break;
            }
            if (clip == null) return;

            float letterPitch = ((letter * 17) % 11 - 5) / 5f;
            source.pitch = Mathf.Clamp(voicePitch + letterPitch * pitchVariation, 0.5f, 2f);
            source.volume = voiceVolume * GameAudioSettings.EffectsVolume;
            source.clip = clip;
            source.Play();
            nextTime = now + minimumInterval;
        }

        public void StopSpeaking()
        {
            if (voiceAudio != null) voiceAudio.Stop();
            nextSoundTime = 0d;
        }

        private void OnDisable() => StopSpeaking();
    }
}
