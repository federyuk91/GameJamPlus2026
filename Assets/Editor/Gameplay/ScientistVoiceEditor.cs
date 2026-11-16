using System.Globalization;
using GameJamPlus.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScientistVoice))]
public sealed class ScientistVoiceEditor : Editor
{
    private AudioSource previewAudio;
    private AudioSource overlapAudio;
    private string previewText;
    private int[] characterStarts;
    private double[] revealTimes;
    private AudioClip[] clips;
    private bool[] accentCues;
    private float[] pitchContours;
    private float[] gains;
    private int nextCharacter;
    private double startTime;
    private bool playing;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ScientistVoice voice = (ScientistVoice)target;
        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(voice.TestText)))
        {
            if (GUILayout.Button(playing ? "Restart Test Voice" : "Test Voice")) StartPreview(voice);
        }
        using (new EditorGUI.DisabledScope(!playing))
        {
            if (GUILayout.Button("Stop Test Voice")) StopPreview();
        }
        if (playing)
        {
            int end = nextCharacter < characterStarts.Length ? characterStarts[nextCharacter] : previewText.Length;
            EditorGUILayout.LabelField(previewText.Substring(0, end), EditorStyles.wordWrappedLabel);
        }
    }

    private void StartPreview(ScientistVoice voice)
    {
        StopPreview();
        GameObject previewObject = new GameObject("Scientist Voice Preview")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        previewAudio = previewObject.AddComponent<AudioSource>();
        previewAudio.playOnAwake = false;
        previewAudio.spatialBlend = 0f;
        previewText = voice.TestText;
        characterStarts = StringInfo.ParseCombiningCharacters(previewText);
        (float speed, float pause, float startPitch, float endPitch, float attack, float tail) = voice.TestTone switch
        {
            ScientistVoice.VoiceTone.Reassuring => (0.9f, 1.25f, 0.3f, -0.6f, 0.95f, 0.72f),
            ScientistVoice.VoiceTone.Warning => (1.1f, 0.75f, 1.05f, -0.35f, 1.18f, 0.96f),
            _ => (1f, 1f, 0.55f, -0.5f, 1f, 1f)
        };
        BuildRevealTimes(voice.TestCharactersPerSecond * speed, pause);
        overlapAudio = previewObject.AddComponent<AudioSource>();
        overlapAudio.playOnAwake = false;
        overlapAudio.spatialBlend = 0f;
        BuildSpeechCues(voice, startPitch, endPitch, attack, tail);
        nextCharacter = 0;
        startTime = EditorApplication.timeSinceStartup;
        playing = true;
        EditorApplication.update += TickPreview;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void TickPreview()
    {
        ScientistVoice voice = target as ScientistVoice;
        if (voice == null || previewAudio == null)
        {
            StopPreview();
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        double elapsed = now - startTime;
        int firstUnrevealed = nextCharacter;
        int latestCue = -1;
        while (nextCharacter < revealTimes.Length && revealTimes[nextCharacter] <= elapsed)
        {
            if (clips[nextCharacter] != null
                && (latestCue < 0 || accentCues[latestCue] || !accentCues[nextCharacter]))
                latestCue = nextCharacter;
            nextCharacter++;
        }

        // After an editor stall, play one cue and give vowels priority over accents.
        if (latestCue >= 0)
        {
            AudioSource source = !previewAudio.isPlaying ? previewAudio : overlapAudio;
            if (source != null && !source.isPlaying)
                voice.PlayPreviewClip(clips[latestCue], pitchContours[latestCue], gains[latestCue], source);
        }

        if (nextCharacter != firstUnrevealed) Repaint();
        if (nextCharacter == characterStarts.Length && !previewAudio.isPlaying
            && (overlapAudio == null || !overlapAudio.isPlaying))
            StopPreview();
    }

    private char CharacterAt(int index) => previewText[characterStarts[index]];

    private void BuildRevealTimes(float charactersPerSecond, float pauseScale)
    {
        revealTimes = new double[characterStarts.Length];
        double time = 0d;
        for (int index = 0; index < revealTimes.Length; index++)
        {
            revealTimes[index] = time;
            float units = RevealUnits(CharacterAt(index));
            // Treat consecutive marks such as "..." or "?!" as one pause.
            if (units > 1f && index + 1 < revealTimes.Length && RevealUnits(CharacterAt(index + 1)) > 1f)
                units = 1f;
            time += (1d + (units - 1f) * pauseScale) / charactersPerSecond;
        }
    }

    private void BuildSpeechCues(ScientistVoice voice, float startPitch, float endPitch, float attack, float tail)
    {
        clips = new AudioClip[characterStarts.Length];
        accentCues = new bool[characterStarts.Length];
        pitchContours = new float[characterStarts.Length];
        gains = new float[characterStarts.Length];
        char[] normalized = new char[characterStarts.Length];
        for (int index = 0; index < normalized.Length; index++)
            normalized[index] = ScientistVoice.NormalizeLetter(CharacterAt(index));
        string normalizedText = new string(normalized);

        for (int index = 0; index < normalized.Length; index++)
        {
            AudioClip combination = voice.GetPreviewCombination(normalizedText, index, out int length, out bool voiced);
            if (combination != null)
            {
                clips[index] = combination;
                accentCues[index] = !voiced;
                index += length - 1;
                continue;
            }
            if (IsVowel(normalized[index]) && (index == 0 || !IsVowel(normalized[index - 1])))
                clips[index] = voice.GetPreviewVowelClip(normalized[index]);
        }

        for (int phraseStart = 0; phraseStart < clips.Length;)
        {
            int phraseEnd = phraseStart;
            while (phraseEnd < clips.Length && RevealUnits(CharacterAt(phraseEnd)) == 1f)
                phraseEnd++;

            int nextPhraseStart = phraseEnd;
            bool question = false;
            int dots = 0;
            while (nextPhraseStart < clips.Length && RevealUnits(CharacterAt(nextPhraseStart)) > 1f)
            {
                char mark = CharacterAt(nextPhraseStart);
                question |= mark == '?';
                if (mark == '.') dots++;
                if (mark == '…') dots += 3;
                nextPhraseStart++;
            }
            ApplyDelivery(phraseStart, phraseEnd, question, dots >= 3, startPitch, endPitch, attack, tail);
            phraseStart = nextPhraseStart;
        }
    }

    private static bool IsVowel(char letter) => "AEIOUY".IndexOf(letter) >= 0;

    private void ApplyDelivery(int start, int end, bool question, bool trailingOff,
        float startPitch, float endPitch, float attack, float tail)
    {
        int vowelCount = 0;
        for (int index = start; index < end; index++)
            if (clips[index] != null && !accentCues[index]) vowelCount++;

        int vowelIndex = 0;
        for (int index = start; index < end; index++)
        {
            if (clips[index] == null) continue;
            bool isAccent = accentCues[index];
            float progress = vowelCount > 1 ? (float)vowelIndex / (vowelCount - 1) : 0.5f;
            progress = Mathf.Clamp01(progress);
            pitchContours[index] = Mathf.Lerp(startPitch, endPitch, progress);
            if (question && !isAccent && vowelIndex == vowelCount - 1)
                pitchContours[index] = Mathf.Max(0.8f, startPitch + 0.15f);

            float gain = Mathf.Lerp(attack, tail, progress);
            if (trailingOff && vowelCount > 0 && vowelIndex >= vowelCount - 2)
                gain *= vowelIndex >= vowelCount - 1 ? 0.65f : 0.85f;
            gains[index] = isAccent ? gain * 0.55f : gain;
            if (!isAccent) vowelIndex++;
        }
    }

    private static float RevealUnits(char character)
    {
        switch (character)
        {
            case ',':
            case ':':
            case ';': return 3f;
            case '.':
            case '?':
            case '!':
            case '…':
            case '\n':
            case '\r': return 6f;
            default: return 1f;
        }
    }

    private void StopPreview()
    {
        EditorApplication.update -= TickPreview;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        if (previewAudio != null)
        {
            previewAudio.Stop();
            if (overlapAudio != null) overlapAudio.Stop();
            DestroyImmediate(previewAudio.gameObject);
        }
        previewAudio = null;
        overlapAudio = null;
        playing = false;
        Repaint();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state) => StopPreview();
    private void OnDisable() => StopPreview();
}
