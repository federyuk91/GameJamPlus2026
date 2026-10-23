using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractableButton)), CanEditMultipleObjects]
public sealed class InteractableButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Input and weight always work together. On Pressed fires once at the bottom; On Release fires when fully raised. The button stays down while a simulated body rests on the surface. Single Use stays locked down until ActivateButton.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        InteractableButton button = (InteractableButton)target;
        if (button == null || !button.ShowGizmos) return;
        if (!button.UsesFixedSpawnPosition)
        {
            DrawSpawnPoints(button);
            return;
        }
        Vector3 position = button.SpawnPosition;
        Handles.Label(position + Vector3.up * 0.15f, "Spawn Point");
        EditorGUI.BeginChangeCheck();
        Vector3 nextPosition = Handles.PositionHandle(position, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck()) return;

        Undo.RecordObject(button, "Move button spawn point");
        button.SetSpawnPosition(nextPosition);
        if (PrefabUtility.IsPartOfPrefabInstance(button))
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
        EditorUtility.SetDirty(button);
    }

    private static void DrawSpawnPoints(InteractableButton button)
    {
        for (int index = 0; index < button.SpawnPoints.Count; index++)
        {
            Transform point = button.SpawnPoints[index];
            if (point == null) continue;
            Handles.Label(point.position + Vector3.up * 0.15f, "Spawn " + (index + 1));
            EditorGUI.BeginChangeCheck();
            Vector3 position = Handles.PositionHandle(point.position, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck()) continue;
            Undo.RecordObject(point, "Move button spawn point");
            point.position = position;
            if (PrefabUtility.IsPartOfPrefabInstance(point)) PrefabUtility.RecordPrefabInstancePropertyModifications(point);
            EditorUtility.SetDirty(point);
        }
    }
}
