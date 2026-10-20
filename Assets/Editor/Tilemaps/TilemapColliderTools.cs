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
        RebuildRequestedTileShapes();
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
                    Debug.LogWarning("Reload LoopScene to rebuild colliders for the 449 transferred tiles.", map);
                    return;
                }
                Rebuild(map);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) return;
                CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
                string result = "Tile: " + tileCount + "; collider paths: " + composite.pathCount + "; points: " + composite.pointCount;
                File.WriteAllText(ResultPath, result);
                File.Delete(RequestPath);
                Debug.Log("Terrain colliders rebuilt and scene saved. " + result, map);
                return;
            }
        }
    }

    private static void RebuildRequestedTileShapes()
    {
        const string request = "Temp/TileColliderInvestigation/Rebuild.request";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/LoopScene.unity");
        if (!scene.IsValid() || !scene.isLoaded) return;
        File.Delete(request);
        AssetDatabase.ImportAsset("Assets/Art/Tiles/TilemapTube.png",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset("Assets/Art/Tiles/TilemapTube_complete.png",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        int rebuilt = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (map.GetComponent<TilemapCollider2D>() == null) continue;
                Rebuild(map);
                rebuilt++;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        string result = "TilemapTube and TilemapTube_complete shapes reimported; rebuilt tilemaps: " + rebuilt + ". Scene not saved automatically.";
        File.WriteAllText("Temp/TileColliderInvestigation/Rebuild.result", result);
        Debug.Log(result);
    }

    [MenuItem("Tools/Mouse Platformer/Tilemaps/Rebuild Selected Colliders")]
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
        Undo.RecordObject(collider, "Rebuild Tilemap collider");
        if (composite != null) Undo.RecordObject(composite, "Rebuild composite collider");
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
