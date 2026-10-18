using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Ladder))]
public sealed class LadderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();
        Ladder ladder = (Ladder)target;
        EditorGUILayout.HelpBox("Length counts one-unit pieces. The first stays at Y=0, the others at -1, -2...; the collider follows the length.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            bool rebuildClicked = GUILayout.Button("Rebuild and align pieces");
            if (!EditorApplication.isPlaying && (changed || rebuildClicked)) Rebuild(ladder, changed);
        }
    }

    private static void Rebuild(Ladder ladder, bool propertyChanged)
    {
        if (EditorUtility.IsPersistent(ladder))
        {
            string path = AssetDatabase.GetAssetPath(ladder);
            GameObject assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string relative = AnimationUtility.CalculateTransformPath(ladder.transform, assetRoot.transform);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform owner = string.IsNullOrEmpty(relative) ? root.transform : root.transform.Find(relative);
                Ladder editable = owner.GetComponent<Ladder>();
                editable.SetLength(ladder.Length);
                Synchronize(editable, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return;
        }
        if (!propertyChanged) Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Change ladder length");
        Synchronize(ladder, true);
        Undo.CollapseUndoOperations(group);
    }

    private static void Synchronize(Ladder ladder, bool undo)
    {
        List<SpriteRenderer> pieces = new List<SpriteRenderer>();
        for (int i = 0; i < ladder.PieceCount; i++)
            if (ladder.Piece(i) != null) pieces.Add(ladder.Piece(i));
        SpriteRenderer template = ladder.PieceTemplate;
        if (pieces.Count == 0)
        {
            // One-time migration for existing ladders: only direct visual children are candidates.
            foreach (SpriteRenderer renderer in ladder.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.transform.parent == ladder.transform
                    && (template == null || renderer.sprite == template.sprite)) pieces.Add(renderer);
            pieces.Sort((SpriteRenderer a, SpriteRenderer b) => b.transform.localPosition.y.CompareTo(a.transform.localPosition.y));
        }
        if (template == null && pieces.Count > 0) template = pieces[0];
        if (template == null)
        {
            Debug.LogWarning("Assign a Piece Template to build the ladder.", ladder);
            return;
        }
        if (undo)
        {
            Undo.RecordObject(ladder, "Update ladder pieces");
            Undo.RecordObject(ladder.Shape, "Update ladder collider");
        }
        if (pieces.Count == 0) pieces.Add(template);
        while (pieces.Count < ladder.Length)
        {
            GameObject piece = Object.Instantiate(template.gameObject, ladder.transform);
            piece.SetActive(true);
            if (undo) Undo.RegisterCreatedObjectUndo(piece, "Add ladder piece");
            pieces.Add(piece.GetComponent<SpriteRenderer>());
        }
        while (pieces.Count > ladder.Length)
        {
            int index = pieces.Count - 1;
            GameObject piece = pieces[index].gameObject;
            pieces.RemoveAt(index);
            if (undo) Undo.DestroyObjectImmediate(piece);
            else Object.DestroyImmediate(piece);
        }
        if (undo)
        {
            foreach (SpriteRenderer piece in pieces)
            {
                Undo.RecordObject(piece.transform, "Align ladder piece");
                Undo.RecordObject(piece.gameObject, "Rename ladder piece");
            }
        }
        ladder.SetPieces(pieces.ToArray());
        EditorUtility.SetDirty(ladder);
        EditorUtility.SetDirty(ladder.Shape);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ladder);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ladder.Shape);
        foreach (SpriteRenderer piece in pieces) PrefabUtility.RecordPrefabInstancePropertyModifications(piece.transform);
    }
}
