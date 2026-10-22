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
}
