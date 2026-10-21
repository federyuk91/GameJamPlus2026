using System.Globalization;
using GameJamPlus.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScientistVoice))]
public sealed class ScientistVoiceEditor : Editor
{
    private AudioSource previewAudio;
    private string previewText;
    private int[] characterStarts;
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
    }

    private void StartPreview(ScientistVoice voice)
    {
        StopPreview();
        voice.ResetPreview();
        GameObject previewObject = new GameObject("Scientist Voice Preview")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        previewAudio = previewObject.AddComponent<AudioSource>();
        previewAudio.playOnAwake = false;
        previewAudio.spatialBlend = 0f;
        previewText = voice.TestText;
        characterStarts = StringInfo.ParseCombiningCharacters(previewText);
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
        int revealed = Mathf.Min(characterStarts.Length,
            (int)((now - startTime) * voice.TestCharactersPerSecond) + 1);
        while (nextCharacter < revealed)
        {
            voice.PreviewCharacter(previewText[characterStarts[nextCharacter]], now, previewAudio);
            nextCharacter++;
        }
        if (nextCharacter == characterStarts.Length && !previewAudio.isPlaying) StopPreview();
    }

    private void StopPreview()
    {
        EditorApplication.update -= TickPreview;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        if (previewAudio != null)
        {
            previewAudio.Stop();
            DestroyImmediate(previewAudio.gameObject);
        }
        previewAudio = null;
        playing = false;
        Repaint();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state) => StopPreview();
    private void OnDisable() => StopPreview();
}
