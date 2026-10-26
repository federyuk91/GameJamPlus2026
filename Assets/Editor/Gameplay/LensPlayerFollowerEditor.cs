using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LensPlayerFollower))]
public sealed class LensPlayerFollowerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        LensPlayerFollower follower = (LensPlayerFollower)target;
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || follower.LensTransform == null))
        {
            if (!GUILayout.Button("Center Lens In Bounds")) return;
            Undo.RecordObject(follower.LensTransform, "Center magnifying lens");
            if (follower.LensRenderer != null) Undo.RecordObject(follower.LensRenderer, "Center magnifying lens");
            follower.CenterInBounds();
            EditorUtility.SetDirty(follower.LensTransform);
            if (follower.LensRenderer != null) EditorUtility.SetDirty(follower.LensRenderer);
            if (PrefabUtility.IsPartOfPrefabInstance(follower.LensTransform))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(follower.LensTransform);
                if (follower.LensRenderer != null) PrefabUtility.RecordPrefabInstancePropertyModifications(follower.LensRenderer);
            }
        }
    }

    private void OnSceneGUI()
    {
        LensPlayerFollower follower = (LensPlayerFollower)target;
        if (!follower.ShowGizmos || follower.BoundsSpace == null || EditorApplication.isPlaying) return;
        DrawLimit(follower, true, follower.MinimumLocalPosition, "Lens minimum");
        DrawLimit(follower, false, follower.MaximumLocalPosition, "Lens maximum");
    }

    private static void DrawLimit(LensPlayerFollower follower, bool minimum, Vector2 local, string label)
    {
        Vector3 position = follower.BoundsSpace.TransformPoint(local);
        Handles.Label(position, label);
        EditorGUI.BeginChangeCheck();
        Vector3 next = Handles.PositionHandle(position, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(follower, "Move lens limit");
        follower.SetLocalLimit(minimum, follower.BoundsSpace.InverseTransformPoint(next));
        EditorUtility.SetDirty(follower);
        if (PrefabUtility.IsPartOfPrefabInstance(follower)) PrefabUtility.RecordPrefabInstancePropertyModifications(follower);
    }
}
