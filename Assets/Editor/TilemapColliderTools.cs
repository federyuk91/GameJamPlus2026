using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class TilemapColliderTools
{
    private const string RequestPath = "Temp/TileLayerMove/RebuildColliders.request";
    private const string ResultPath = "Temp/TileLayerMove/RebuildColliders.result";

    static TilemapColliderTools()
    {
        EditorApplication.delayCall += RepairPendingTransfer;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => RepairPendingTransfer();

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RepairPendingTransfer;
    }

    private static void RepairPendingTransfer()
    {
        if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/LoopScene.unity");
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (map.name != "Terrain - Solid CORRETTO") continue;
                int tileCount = 0;
                foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(cell)) tileCount++;
                // Do not save an old in-memory copy over the transferred tiles on disk.
                if (tileCount != 449)
                {
                    Debug.LogWarning("Ricarica LoopScene per rigenerare i collider delle 449 tile trasferite.", map);
                    return;
                }
                Rebuild(map);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) return;
                CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
                string result = "Tile: " + tileCount + "; percorsi collider: " + composite.pathCount + "; punti: " + composite.pointCount;
                File.WriteAllText(ResultPath, result);
                File.Delete(RequestPath);
                Debug.Log("Collider Terrain rigenerati e scena salvata. " + result, map);
                return;
            }
        }
    }

    [MenuItem("Tools/Mouse Platformer/Tilemaps/Ricalcola collider selezionati")]
    private static void RebuildSelected()
    {
        foreach (GameObject selected in Selection.gameObjects)
        {
            foreach (Tilemap map in selected.GetComponentsInChildren<Tilemap>(true))
            {
                if (map.GetComponent<TilemapCollider2D>() == null) continue;
                Rebuild(map);
                EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            }
        }
    }

    private static void Rebuild(Tilemap map)
    {
        TilemapCollider2D collider = map.GetComponent<TilemapCollider2D>();
        CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
        if (collider == null) return;
        Undo.RecordObject(collider, "Rigenera collider tilemap");
        if (composite != null) Undo.RecordObject(composite, "Rigenera collider composito");
        bool wasEnabled = collider.enabled;
        CompositeCollider2D.GenerationType generation = composite != null
            ? composite.generationType : CompositeCollider2D.GenerationType.Synchronous;
        try
        {
            if (composite != null) composite.generationType = CompositeCollider2D.GenerationType.Manual;
            collider.enabled = false;
            map.RefreshAllTiles();
            collider.enabled = true;
            collider.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
            if (composite != null) composite.GenerateGeometry();
        }
        finally
        {
            collider.enabled = wasEnabled;
            if (composite != null) composite.generationType = generation;
        }
        RecordChanges(collider);
        if (composite != null) RecordChanges(composite);
    }

    private static void RecordChanges(Object component)
    {
        EditorUtility.SetDirty(component);
        if (PrefabUtility.IsPartOfPrefabInstance(component))
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }
}
