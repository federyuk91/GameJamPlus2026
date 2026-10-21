using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractableButton)), CanEditMultipleObjects]
public sealed class InteractableButtonEditor : Editor
{
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
