using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PiranhaPatrol))]
public sealed class PiranhaPatrolEditor : Editor
{
    private void OnSceneGUI()
    {
        PiranhaPatrol patrol = (PiranhaPatrol)target;
        Handles.color = Color.cyan;
        for (int i = 0; i < patrol.WaypointCount; i++)
        {
            Vector2 point = patrol.WaypointPosition(i);
            float size = HandleUtility.GetHandleSize(point) * 0.06f;
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.FreeMoveHandle(point, size, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(patrol, "Sposta punto ronda piranha");
                patrol.SetWaypointPosition(i, moved);
                EditorUtility.SetDirty(patrol);
            }
            Handles.Label(point + Vector2.up * size * 2f, "Ronda " + (i + 1));
        }
    }
}

[CustomEditor(typeof(PiranhaChase))]
public sealed class PiranhaChaseEditor : Editor
{
    private void OnSceneGUI()
    {
        PiranhaChase chase = (PiranhaChase)target;
        Vector2 eye = chase.EyePosition;
        Vector2 forward = chase.ViewDirection;
        Vector2 rangePoint = eye + forward * chase.ViewDistance;
        float size = HandleUtility.GetHandleSize(rangePoint) * 0.06f;
        Handles.color = Color.yellow;
        EditorGUI.BeginChangeCheck();
        Vector3 movedRange = Handles.FreeMoveHandle(rangePoint, size, Vector3.zero, Handles.RectangleHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(chase, "Modifica distanza visiva piranha");
            chase.SetVision(Vector2.Distance(eye, movedRange), chase.ViewAngle);
            EditorUtility.SetDirty(chase);
        }
        Handles.Label(rangePoint, "Distanza visiva");
        float angle = (Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg + chase.ViewAngle * 0.5f) * Mathf.Deg2Rad;
        Vector2 conePoint = eye + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * chase.ViewDistance;
        EditorGUI.BeginChangeCheck();
        Vector3 movedCone = Handles.FreeMoveHandle(conePoint, size, Vector3.zero, Handles.DotHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(chase, "Modifica cono visivo piranha");
            float opening = Mathf.Abs(Vector2.SignedAngle(forward, (Vector2)movedCone - eye)) * 2f;
            chase.SetVision(chase.ViewDistance, opening);
            EditorUtility.SetDirty(chase);
        }
        Handles.Label(conePoint, "Apertura cono");
    }
}
