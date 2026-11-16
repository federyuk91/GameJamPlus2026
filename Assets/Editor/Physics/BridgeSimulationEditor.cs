using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BridgeSimulation))]
public sealed class BridgeSimulationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        BridgeSimulation bridge = (BridgeSimulation)target;
        bool locked = EditorApplication.isPlaying || EditorUtility.IsPersistent(bridge);
        EditorGUI.BeginChangeCheck();
        using (new EditorGUI.DisabledScope(locked))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bridgeStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bridgeEnd"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sliceTemplate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sliceCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deckSurfaceHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deckThickness"));
        }
        bool geometryChanged = EditorGUI.EndChangeCheck();
        DrawPropertiesExcluding(serializedObject, "m_Script", "bridgeStart", "bridgeEnd", "sliceTemplate",
            "sliceCount", "deckSurfaceHeight", "deckThickness");
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.HelpBox("Slice Count adds/removes middle pieces. Start stays in place; End follows the new length. "
            + "Geometry is editable outside Play Mode; open prefab assets in Prefab Mode. Distances are units (32 pixels/unit).", MessageType.Info);
        using (new EditorGUI.DisabledScope(locked))
        {
            bool rebuild = GUILayout.Button("Rebuild and align bridge");
            if (!locked && (geometryChanged || rebuild)) Rebuild(bridge, geometryChanged);
        }
    }

    private static void Rebuild(BridgeSimulation bridge, bool propertyChanged)
    {
        if (bridge.StartPiece == null || bridge.EndPiece == null || bridge.SliceTemplate == null)
        {
            Debug.LogWarning("Assign Bridge Start, Bridge End and Slice Template before rebuilding.", bridge);
            return;
        }
        Transform root = bridge.transform;
        if (bridge.StartPiece.transform.parent != root || bridge.EndPiece.transform.parent != root
            || bridge.SliceTemplate.transform.parent != root || bridge.StartPiece == bridge.EndPiece
            || bridge.SliceTemplate == bridge.StartPiece || bridge.SliceTemplate == bridge.EndPiece)
        {
            Debug.LogWarning("Start, End and Slice Template must be distinct direct children of Bridge.", bridge);
            return;
        }
        List<SpriteRenderer> slices = new List<SpriteRenderer>();
        for (int index = 0; index < bridge.ExistingSliceCount; index++)
            if (bridge.Slice(index) != null && !slices.Contains(bridge.Slice(index))) slices.Add(bridge.Slice(index));
        if (slices.Count == 0) slices.Add(bridge.SliceTemplate);

        if (!propertyChanged) Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rebuild bridge");
        Undo.RecordObject(bridge, "Update bridge composition");
        while (slices.Count < bridge.SliceCount)
        {
            GameObject piece = Object.Instantiate(bridge.SliceTemplate.gameObject, root);
            piece.SetActive(true);
            Undo.RegisterCreatedObjectUndo(piece, "Add bridge slice");
            slices.Add(piece.GetComponent<SpriteRenderer>());
        }
        while (slices.Count > bridge.SliceCount)
        {
            int last = slices.Count - 1;
            GameObject piece = slices[last].gameObject;
            slices.RemoveAt(last);
            Undo.DestroyObjectImmediate(piece);
        }
        List<SpriteRenderer> parts = new List<SpriteRenderer> { bridge.StartPiece };
        parts.AddRange(slices);
        parts.Add(bridge.EndPiece);
        List<BoxCollider2D> surfaces = new List<BoxCollider2D>();
        foreach (SpriteRenderer part in parts)
        {
            BoxCollider2D surface = part.GetComponent<BoxCollider2D>();
            if (surface == null) surface = Undo.AddComponent<BoxCollider2D>(part.gameObject);
            if (part.GetComponent<OneWayPlatform2D>() == null) Undo.AddComponent<OneWayPlatform2D>(part.gameObject);
            Undo.RecordObject(part.transform, "Align bridge piece");
            Undo.RecordObject(part.gameObject, "Rename bridge piece");
            Undo.RecordObject(surface, "Update bridge surface");
            surfaces.Add(surface);
        }
        bridge.StartPiece.name = "Bridge Start";
        bridge.EndPiece.name = "Bridge End";
        for (int index = 0; index < slices.Count; index++) slices[index].name = "Bridge Slice " + (index + 1);
        bridge.SetPieces(slices.ToArray(), surfaces.ToArray());
        RecordChanges(bridge);
        foreach (SpriteRenderer part in parts)
        {
            RecordChanges(part.transform);
            RecordChanges(part.gameObject);
        }
        foreach (BoxCollider2D surface in surfaces) RecordChanges(surface);
        Undo.CollapseUndoOperations(group);
        Physics2D.SyncTransforms();
    }

    private static void RecordChanges(Object value)
    {
        EditorUtility.SetDirty(value);
        if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }
}
