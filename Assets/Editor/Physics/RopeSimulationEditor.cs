using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RopeSimulation))]
public sealed class RopeSimulationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty length = serializedObject.FindProperty("length");
        SerializedProperty segmentLength = serializedObject.FindProperty("segmentLength");
        EditorGUI.BeginChangeCheck();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            EditorGUILayout.PropertyField(length);
            EditorGUILayout.PropertyField(segmentLength);
        }
        bool geometryChanged = EditorGUI.EndChangeCheck();
        DrawPropertiesExcluding(serializedObject, "m_Script", "length", "segmentLength");
        serializedObject.ApplyModifiedProperties();

        RopeSimulation rope = (RopeSimulation)target;
        EditorGUILayout.HelpBox("Length counts pieces; total length depends on Segment Length. The anchor stays fixed. Edit geometry outside Play Mode.", MessageType.Info);
        EditorGUILayout.LabelField("Total length", (rope.ConfiguredPieceCount * segmentLength.floatValue).ToString("0.##") + " units");
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            bool rebuild = GUILayout.Button("Rebuild and align pieces");
            if (!EditorApplication.isPlaying && (geometryChanged || rebuild)) Rebuild(rope, geometryChanged);
        }
    }

    private static void Rebuild(RopeSimulation rope, bool propertyChanged)
    {
        if (EditorUtility.IsPersistent(rope))
        {
            string path = AssetDatabase.GetAssetPath(rope);
            GameObject assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string relative = AnimationUtility.CalculateTransformPath(rope.transform, assetRoot.transform);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform owner = string.IsNullOrEmpty(relative) ? root.transform : root.transform.Find(relative);
                RopeSimulation editable = owner.GetComponent<RopeSimulation>();
                SerializedObject source = new SerializedObject(rope);
                SerializedObject destination = new SerializedObject(editable);
                destination.FindProperty("length").intValue = rope.ConfiguredPieceCount;
                destination.FindProperty("segmentLength").floatValue = source.FindProperty("segmentLength").floatValue;
                destination.ApplyModifiedPropertiesWithoutUndo();
                Synchronize(editable, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return;
        }

        if (!propertyChanged) Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Change rope length");
        Synchronize(rope, true);
        Undo.CollapseUndoOperations(group);
    }

    private static void Synchronize(RopeSimulation rope, bool undo)
    {
        List<Transform> pieces = new List<Transform>();
        for (int i = 0; i < rope.PieceCount; i++)
            if (rope.Piece(i) != null) pieces.Add(rope.Piece(i));
        if (pieces.Count == 0)
        {
            Debug.LogWarning("The rope must have at least one reference piece.", rope);
            return;
        }

        if (undo) Undo.RecordObject(rope, "Update rope pieces");
        Transform template = pieces[0];
        while (pieces.Count < rope.ConfiguredPieceCount)
        {
            GameObject piece = Object.Instantiate(template.gameObject, rope.transform);
            piece.SetActive(true);
            if (undo) Undo.RegisterCreatedObjectUndo(piece, "Add rope piece");
            pieces.Add(piece.transform);
        }
        while (pieces.Count > rope.ConfiguredPieceCount)
        {
            int index = pieces.Count - 1;
            GameObject piece = pieces[index].gameObject;
            pieces.RemoveAt(index);
            if (undo) Undo.DestroyObjectImmediate(piece);
            else Object.DestroyImmediate(piece);
        }
        if (undo)
        {
            foreach (Transform piece in pieces)
            {
                Undo.RecordObject(piece, "Align rope piece");
                Undo.RecordObject(piece.gameObject, "Rename rope piece");
            }
        }
        rope.SetPieces(pieces.ToArray());
        RecordChanges(rope);
        foreach (Transform piece in pieces)
        {
            RecordChanges(piece);
            RecordChanges(piece.gameObject);
        }
    }

    private static void RecordChanges(Object value)
    {
        EditorUtility.SetDirty(value);
        if (PrefabUtility.IsPartOfPrefabInstance(value))
            PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }
}
