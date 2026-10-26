using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScientistDeskAnchor))]
public sealed class ScientistDeskAnchorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ScientistDeskAnchor anchor = (ScientistDeskAnchor)target;
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || anchor.Scientist == null))
        {
            if (!GUILayout.Button("Align Bust To Desk")) return;
            Undo.RecordObject(anchor.Scientist.transform, "Align scientist to desk");
            Undo.RecordObject(anchor.Scientist, "Align scientist to desk");
            anchor.AlignToDesk();
            EditorUtility.SetDirty(anchor.Scientist.transform);
            EditorUtility.SetDirty(anchor.Scientist);
            if (PrefabUtility.IsPartOfPrefabInstance(anchor.Scientist))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(anchor.Scientist);
                PrefabUtility.RecordPrefabInstancePropertyModifications(anchor.Scientist.transform);
            }
        }
    }
}
