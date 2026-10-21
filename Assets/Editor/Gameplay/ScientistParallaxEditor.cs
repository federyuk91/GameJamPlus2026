using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScientistParallax))]
public sealed class ScientistParallaxEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ScientistParallax parallax = (ScientistParallax)target;
        if (!EditorApplication.isPlaying && !serializedObject.FindProperty("customLensLimits").boolValue)
        {
            Undo.RecordObject(parallax, "Initialize lens limits");
            parallax.InitializeLensLimits();
            serializedObject.Update();
        }
        DrawDefaultInspector();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            if (GUILayout.Button("Create Background Groups")) ScientistParallaxSetup.ConfigureLayers(parallax);
        EditorGUILayout.HelpBox("Drag the two lens handles in Scene view. Put background objects under groups 01 to 04, far to near. Lens Vertical Offset adjusts their shared height relative to the bust.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        ScientistParallax parallax = (ScientistParallax)target;
        if (!parallax.ShowGizmos) return;
        DrawLimit(parallax, true, Color.cyan, "Lens Left Limit");
        DrawLimit(parallax, false, Color.yellow, "Lens Right Limit");
    }

    private static void DrawLimit(ScientistParallax parallax, bool left, Color color, string label)
    {
        Vector3 position = left ? parallax.LeftLimitWorld : parallax.RightLimitWorld;
        Handles.color = color;
        Handles.Label(position + Vector3.up * HandleUtility.GetHandleSize(position) * 0.1f, label);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            EditorGUI.BeginChangeCheck();
            Vector3 next = Handles.PositionHandle(position, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(parallax, "Move lens limit");
            parallax.SetLensLimitWorld(left, next);
            EditorUtility.SetDirty(parallax);
            if (PrefabUtility.IsPartOfPrefabInstance(parallax)) PrefabUtility.RecordPrefabInstancePropertyModifications(parallax);
        }
    }
}
