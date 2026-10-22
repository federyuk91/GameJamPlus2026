using System.Collections.Generic;
using System.IO;
using GameJamPlus.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(Checkpoint))]
public sealed class CheckpointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("activationArea"));
        DrawSceneDropdown();
        SerializedProperty targetScene = serializedObject.FindProperty("targetScenePath");
        if (string.IsNullOrEmpty(targetScene.stringValue))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("respawnOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showGizmos"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onActivated"));
        serializedObject.ApplyModifiedProperties();

        Checkpoint checkpoint = (Checkpoint)target;
        string path = checkpoint.gameObject.scene.path;
        if (checkpoint.UsesLocalRespawn && path.EndsWith(".unity") && !IsEnabledScene(path))
        {
            EditorGUILayout.HelpBox("This scene must be enabled in Build Profiles to reload it after Stage complete.", MessageType.Warning);
            if (GUILayout.Button("Add This Scene To Build")) AddSceneToBuild(path);
        }
        EditorGUILayout.HelpBox("Entering only stores the destination. This uses the green respawn handle after Stage complete. Other scenes use their Game Manager's random starting Spawn Points and clear the previous checkpoint.", MessageType.Info);
    }

    private void DrawSceneDropdown()
    {
        SerializedProperty property = serializedObject.FindProperty("targetScenePath");
        List<string> labels = new List<string> { "This" };
        List<string> paths = new List<string> { string.Empty };
        int buildIndex = 0;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            labels.Add(buildIndex + " - " + Path.GetFileNameWithoutExtension(scene.path));
            paths.Add(scene.path);
            buildIndex++;
        }
        int selected = paths.IndexOf(property.stringValue ?? string.Empty);
        if (selected < 0)
        {
            labels.Add("Missing / disabled: " + property.stringValue);
            paths.Add(property.stringValue);
            selected = paths.Count - 1;
            EditorGUILayout.HelpBox("The selected scene is no longer enabled in Build Profiles. Select another scene or re-enable it.", MessageType.Warning);
        }
        EditorGUI.BeginChangeCheck();
        int next = EditorGUILayout.Popup("Target Scene", selected, labels.ToArray());
        if (EditorGUI.EndChangeCheck()) property.stringValue = paths[next];
    }

    private static bool IsEnabledScene(string path)
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled && scene.path == path) return true;
        return false;
    }

    private static void AddSceneToBuild(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path != path) continue;
            scene.enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
            return;
        }
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private void OnSceneGUI()
    {
        Checkpoint checkpoint = (Checkpoint)target;
        if (!checkpoint.ShowGizmos || !checkpoint.UsesLocalRespawn) return;
        Vector3 position = checkpoint.RespawnPosition;
        Handles.color = Color.green;
        Handles.Label(position + Vector3.up * 0.25f, "Checkpoint Respawn (player pivot)");
        EditorGUI.BeginChangeCheck();
        Vector3 next = Handles.PositionHandle(position, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(checkpoint, "Move checkpoint respawn");
        checkpoint.SetRespawnPosition(next);
        EditorUtility.SetDirty(checkpoint);
        if (PrefabUtility.IsPartOfPrefabInstance(checkpoint)) PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint);
        if (checkpoint.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(checkpoint.gameObject.scene);
    }
}
