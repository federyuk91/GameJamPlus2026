using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MousePlatformer.EditorTools
{
    public static class PlayerFeedbackImportRepair
    {
        private const string RequestPath = "Temp/CodexGameplayCheck/repair-player-feedback.request";
        private const string ReportPath = "Temp/CodexGameplayCheck/player-feedback-import-report.txt";
        private static readonly string[] AssetPaths =
        {
            "Assets/Scripts/Player/Vitals/PlayerStats.cs",
            "Assets/Scripts/Player/Presentation/PlayerOxygenBubbles.cs",
            "Assets/Scripts/UI/Gameplay/SatietyHud.cs",
            "Assets/Art/Pixel Art/Effects/Oxygen/bubble1.png",
            "Assets/Art/Pixel Art/Effects/Oxygen/bubble2.png",
            "Assets/Art/Pixel Art/Effects/Oxygen/bubble3.png",
            "Assets/Prefabs/Characters/Player.prefab",
            "Assets/Scenes/LoopScene.unity"
        };

        [InitializeOnLoadMethod]
        private static void QueueRequestedRepair()
        {
            if (File.Exists(RequestPath)) EditorApplication.delayCall += RunRequestedRepair;
        }

        private static void RunRequestedRepair()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            Reimport();
        }

        [MenuItem("Tools/Gameplay/Repair Player Feedback Imports")]
        public static void Reimport()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            StringBuilder report = new StringBuilder(DateTime.UtcNow.ToString("O") + "\n");
            foreach (string path in AssetPaths)
            {
                string before = AssetDatabase.AssetPathToGUID(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                string after = AssetDatabase.AssetPathToGUID(path);
                report.AppendLine(path + " | GUID: " + after + " | preserved: " + (before == after));
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null) report.AppendLine("  Script class: " + script.GetClass()?.FullName);
            }
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab");
            int missingScripts = 0;
            if (player != null)
                foreach (Transform child in player.GetComponentsInChildren<Transform>(true))
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            report.AppendLine("Player prefab loaded: " + (player != null));
            report.AppendLine("Player missing scripts: " + missingScripts);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("Player feedback assets reimported. Report: " + ReportPath);
        }
    }
}
