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
        EditorGUILayout.HelpBox("Length conta i pezzi da una unita. Il primo resta a Y=0, gli altri a -1, -2...; il collider segue la lunghezza.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            bool rebuildClicked = GUILayout.Button("Ricostruisci e allinea pezzi");
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
        Undo.SetCurrentGroupName("Modifica lunghezza scala");
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
            Debug.LogWarning("Assegna un Piece Template per costruire la scala.", ladder);
            return;
        }
        if (undo)
        {
            Undo.RecordObject(ladder, "Aggiorna pezzi scala");
            Undo.RecordObject(ladder.Shape, "Aggiorna collider scala");
        }
        if (pieces.Count == 0) pieces.Add(template);
        while (pieces.Count < ladder.Length)
        {
            GameObject piece = Object.Instantiate(template.gameObject, ladder.transform);
            piece.SetActive(true);
            if (undo) Undo.RegisterCreatedObjectUndo(piece, "Aggiungi pezzo scala");
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
                Undo.RecordObject(piece.transform, "Allinea pezzo scala");
                Undo.RecordObject(piece.gameObject, "Rinomina pezzo scala");
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
