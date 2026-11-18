using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Elevator))]
public sealed class ElevatorEditor : Editor
{
    private void OnSceneGUI()
    {
        if (Application.isPlaying) return;

        Elevator elevator = (Elevator)target;
        serializedObject.Update();
        DrawEndpoint(elevator.BottomPosition, "Bottom", serializedObject.FindProperty("bottomOffset"),
            float.NegativeInfinity, elevator.TopPosition.y, elevator.transform.position.y);
        DrawEndpoint(elevator.TopPosition, "Top", serializedObject.FindProperty("topOffset"),
            elevator.BottomPosition.y, float.PositiveInfinity, elevator.transform.position.y);
        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawEndpoint(Vector3 position, string label, SerializedProperty offset,
        float minimumY, float maximumY, float originY)
    {
        float handleSize = HandleUtility.GetHandleSize(position) * 0.1f;
        Handles.Label(position + Vector3.right * handleSize, label);
        EditorGUI.BeginChangeCheck();
        Vector3 nextPosition = Handles.Slider(position, Vector3.up, handleSize, Handles.DotHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
            offset.floatValue = Mathf.Clamp(nextPosition.y, minimumY, maximumY) - originY;
    }
}
