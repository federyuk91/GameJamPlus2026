using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CollectibleIdTools
{
    private const string Request = "Temp/RepairCollectibleIds.request";
    static CollectibleIdTools()
    {
        EditorSceneManager.sceneSaving += OnSceneSaving;
        EditorApplication.delayCall += ProcessRequest;
    }

    private static void OnSceneSaving(Scene scene, string path)
    {
        if (!path.Contains("/Recovered/")) RepairScene(scene, new HashSet<string>(), null);
    }

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        RepairAllScenes();
    }

    [MenuItem("Tools/Gameplay/Repair Collectible IDs In Scenes")]
    private static void RepairAllScenes()
    {
        StringBuilder report = new StringBuilder();
        HashSet<string> ids = new HashSet<string>();
        Scene active = SceneManager.GetActiveScene();
        string backupDirectory = "Recovery/CollectibleIdRepair_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff");
        Directory.CreateDirectory(backupDirectory);
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Recovered/")) continue;
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                int changed = RepairScene(scene, ids, report);
                if (changed > 0)
                {
                    File.Copy(path, backupDirectory + "/" + guid + ".unity.snapshot");
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!wasLoaded) EditorSceneManager.SaveScene(scene);
                }
                report.AppendLine(path + ": repaired IDs=" + changed + (wasLoaded ? "; left open, not saved" : "; saved only if changed"));
            }
            finally
            {
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }
        File.WriteAllText("Temp/CollectibleIdRepair-report.txt", report.ToString());
    }

    private static int RepairScene(Scene scene, HashSet<string> ids, StringBuilder report)
    {
        int changes = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (CollectibleTestGoal goal in root.GetComponentsInChildren<CollectibleTestGoal>(true))
            {
                SerializedObject goalSettings = new SerializedObject(goal);
                SerializedProperty scope = goalSettings.FindProperty("useSessionTotal");
                if (!scope.boolValue)
                {
                    Undo.RecordObject(goal, "Use HUD collection quantity");
                    scope.boolValue = true;
                    goalSettings.ApplyModifiedProperties();
                    if (PrefabUtility.IsPartOfPrefabInstance(goal)) PrefabUtility.RecordPrefabInstancePropertyModifications(goal);
                    changes++;
                }
            }
            foreach (CollectibleRemains remains in root.GetComponentsInChildren<CollectibleRemains>(true))
                changes += RepairId(remains, scene, ids, report);
            foreach (CollectiblePhoto photo in root.GetComponentsInChildren<CollectiblePhoto>(true))
            {
                changes += RepairId(photo, scene, ids, report);
                changes += CollectibleGoalSetup.RepairPhoto(photo);
            }
        }
        return changes;
    }

    private static int RepairId(Component component, Scene scene, HashSet<string> ids, StringBuilder report)
    {
        SerializedObject settings = new SerializedObject(component);
        SerializedProperty property = settings.FindProperty("collectionId");
        string previous = property.stringValue;
        if (!string.IsNullOrWhiteSpace(previous) && ids.Add(previous)) return 0;
        Undo.RecordObject(component, "Assign unique collectible ID");
        string id = AssetDatabase.AssetPathToGUID(scene.path) + "." + Guid.NewGuid().ToString("N");
        property.stringValue = id;
        settings.ApplyModifiedProperties();
        ids.Add(id);
        if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        report?.AppendLine("  " + component.name + ": '" + previous + "' -> " + id);
        return 1;
    }
}
