using System.IO;
using System.Text;
using GameJamPlus.UI;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ScientistVoiceAudit
{
    static ScientistVoiceAudit() => EditorApplication.delayCall += Run;

    [MenuItem("Tools/Gameplay/Audit Scientist Voice")]
    public static void Run()
    {
        StringBuilder report = new StringBuilder();
        foreach (GameplayHudController hud in Resources.FindObjectsOfTypeAll<GameplayHudController>())
        {
            SerializedObject serialized = new SerializedObject(hud);
            ScientistSpeechSettings speech = serialized.FindProperty("scientistSpeech").objectReferenceValue as ScientistSpeechSettings;
            Sprite portrait = serialized.FindProperty("scientistPortrait").objectReferenceValue as Sprite;
            Sprite welcome = serialized.FindProperty("welcomePortrait").objectReferenceValue as Sprite;
            ScientistVoice voice = serialized.FindProperty("scientistVoice").objectReferenceValue as ScientistVoice;
            report.AppendLine($"HUD {hud.name}, scene {hud.gameObject.scene.path}, active {hud.isActiveAndEnabled}");
            report.AppendLine($"Speech {speech}, portrait {portrait}, welcome {welcome}");
            report.AppendLine($"Recognizes default {speech != null && speech.Recognizes(portrait)}, welcome {speech != null && speech.Recognizes(welcome)}");
            report.AppendLine($"Voice {voice}");
            if (voice == null) continue;
            SerializedObject voiceObject = new SerializedObject(voice);
            AudioSource source = voiceObject.FindProperty("voiceAudio").objectReferenceValue as AudioSource;
            report.AppendLine($"Source {source}, volume {voiceObject.FindProperty("voiceVolume").floatValue}, clips {voiceObject.FindProperty("letterSounds").arraySize}");
            if (source != null) report.AppendLine($"Enabled {source.enabled}, mute {source.mute}, spatialBlend {source.spatialBlend}, playing {source.isPlaying}, clip {source.clip}");
        }
        report.AppendLine($"Master {GameAudioSettings.MasterVolume}, SFX {GameAudioSettings.EffectsVolume}, listener pause {AudioListener.pause}");
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/ScientistVoiceAudit.txt", report.ToString());
    }
}
