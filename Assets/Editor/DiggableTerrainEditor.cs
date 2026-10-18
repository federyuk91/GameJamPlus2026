using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DiggableTerrain)), CanEditMultipleObjects]
public sealed class DiggableTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (targets.Length != 1) return;
        SerializedObject terrain = serializedObject;
        BoxCollider2D area = terrain.FindProperty("area").objectReferenceValue as BoxCollider2D;
        SpriteRenderer visual = terrain.FindProperty("visual").objectReferenceValue as SpriteRenderer;
        if (area == null || visual == null) return;
        if (visual.sprite == null)
        {
            EditorGUILayout.HelpBox("Visual has no valid Sprite assigned.", MessageType.Warning);
            return;
        }

        Bounds bounds = GetVisualBounds(area, visual);
        if (Vector2.Distance(area.size, bounds.size) > 0.001f || Vector2.Distance(area.offset, bounds.center) > 0.001f)
            EditorGUILayout.HelpBox("Area does not match Visual. Excavation and its mask use Area, regardless of the sprite's size. Use Fit Area To Visual after resizing the sprite.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            if (!GUILayout.Button("Fit Area To Visual")) return;
            Undo.RecordObject(area, "Fit Diggable Area To Visual");
            area.offset = bounds.center;
            area.size = bounds.size;
            if (PrefabUtility.IsPartOfPrefabInstance(area)) PrefabUtility.RecordPrefabInstancePropertyModifications(area);
            EditorUtility.SetDirty(area);
        }
    }

    private static Bounds GetVisualBounds(BoxCollider2D area, SpriteRenderer visual)
    {
        Bounds local = visual.localBounds;
        Matrix4x4 matrix = area.transform.worldToLocalMatrix * visual.transform.localToWorldMatrix;
        Bounds result = new Bounds(matrix.MultiplyPoint3x4(local.min), Vector3.zero);
        result.Encapsulate(matrix.MultiplyPoint3x4(local.max));
        result.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(local.min.x, local.max.y, 0f)));
        result.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(local.max.x, local.min.y, 0f)));
        return result;
    }
}
