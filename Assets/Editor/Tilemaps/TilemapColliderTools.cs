using System.IO;
using System.Collections.Generic;
using System.Text;
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
        EditorApplication.update += RepairFullTileset155;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void RepairFullTileset155()
    {
        const string request = "Library/RebuildFullTileset155.final.request";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode
            || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(request);
        try { RebuildFullTileset155(); }
        catch (System.Exception exception)
        {
            File.WriteAllText("Library/FullTileset155-result.txt", exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Mouse Platformer/Tilemaps/Rebuild Full TileSet 155")]
    public static void RebuildFullTileset155()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string spritePath = "Assets/Art/Tiles/Full TileSet.png";
        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset("Assets/Tilemaps/Tiles/All/Full TileSet_155.asset",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        Sprite sprite = null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(spritePath))
            if (asset is Sprite candidate && candidate.name == "Full TileSet_155") sprite = candidate;
        if (sprite == null) throw new System.InvalidOperationException("Full TileSet_155 sprite is missing.");
        StringBuilder report = new StringBuilder();
        report.AppendLine("Imported sprite paths: " + sprite.GetPhysicsShapeCount());
        List<Vector2> points = new List<Vector2>();
        List<Vector2[]> physicsPaths = new List<Vector2[]>();
        for (int path = 0; path < sprite.GetPhysicsShapeCount(); path++)
        {
            sprite.GetPhysicsShape(path, points);
            report.AppendLine("Path " + path + ": " + string.Join(", ", points));
            physicsPaths.Add(points.ToArray());
        }
        sprite.OverridePhysicsShape(physicsPaths);
        int maps = 0;
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            if (!scene.isLoaded) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
                {
                    List<Vector3Int> cells = new List<Vector3Int>();
                    foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                        if (map.GetSprite(cell) == sprite) cells.Add(cell);
                    if (cells.Count == 0) continue;
                    TilemapCollider2D collider = map.GetComponent<TilemapCollider2D>();
                    report.AppendLine(scene.name + " / " + map.name + ": " + cells.Count
                        + " cells, TilemapCollider present: " + (collider != null));
                    if (collider == null) continue;
                    string colliderSettings = EditorJsonUtility.ToJson(collider);
                    Undo.DestroyObjectImmediate(collider);
                    collider = Undo.AddComponent<TilemapCollider2D>(map.gameObject);
                    EditorJsonUtility.FromJsonOverwrite(colliderSettings, collider);
                    EditorUtility.SetDirty(collider);
                    report.AppendLine("Recreated TilemapCollider2D with original settings (Undo available).");
                    Undo.RecordObject(map, "Refresh Full TileSet 155 collider");
                    RecreateCells(map, collider, cells);
                    foreach (Vector3Int cell in cells)
                    {
                        report.AppendLine("Cell " + cell + " collider: " + map.GetColliderType(cell)
                            + ", tile asset: " + AssetDatabase.GetAssetPath(map.GetTile(cell)));
                        map.RefreshTile(cell);
                        if (map.GetColliderType(cell) != Tile.ColliderType.Sprite)
                            map.SetColliderType(cell, Tile.ColliderType.Sprite);
                    }
                    Rebuild(map);
                    CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
                    Collider2D generated = composite != null ? composite : collider;
                    report.AppendLine("Map active: " + map.gameObject.activeInHierarchy + ", collider enabled: "
                        + collider.enabled + ", generated enabled: " + generated.enabled + ", bounds: " + generated.bounds
                        + ", map position: " + map.transform.position + ", scale: " + map.transform.lossyScale
                        + ", anchor: " + map.tileAnchor + ", orientation: " + map.orientationMatrix);
                    if (composite != null) report.AppendLine("Composite paths: " + composite.pathCount
                        + ", points: " + composite.pointCount + ", rigidbody: " + composite.attachedRigidbody);
                    if (composite != null)
                    {
                        Vector2 target = map.GetCellCenterWorld(cells[0]);
                        for (int path = 0; path < composite.pathCount; path++)
                        {
                            Vector2[] vertices = new Vector2[composite.GetPathPointCount(path)];
                            composite.GetPath(path, vertices);
                            foreach (Vector2 vertex in vertices)
                            {
                                Vector2 world = composite.transform.TransformPoint(vertex + composite.offset);
                                if (Vector2.Distance(world, target) < 1.6f)
                                    report.AppendLine("Nearby composite vertex: " + world.ToString("F4"));
                            }
                        }
                    }
                    foreach (Vector3Int cell in cells)
                    {
                        Matrix4x4 matrix = map.GetTransformMatrix(cell);
                        Vector3 center = map.CellToLocalInterpolated((Vector3)cell + map.tileAnchor);
                        Vector3 left = center + matrix.MultiplyPoint3x4(new Vector3(-0.375f, -0.25f));
                        Vector3 middle = center + matrix.MultiplyPoint3x4(new Vector3(0f, -0.25f));
                        Vector3 right = center + matrix.MultiplyPoint3x4(new Vector3(0.375f, -0.25f));
                        report.AppendLine("Generated collision " + cell + ": left="
                            + generated.OverlapPoint(map.transform.TransformPoint(left)) + ", center="
                            + generated.OverlapPoint(map.transform.TransformPoint(middle)) + ", right="
                            + generated.OverlapPoint(map.transform.TransformPoint(right)));
                        report.AppendLine("Cell matrix: " + matrix + "; local center: " + center
                            + "; sample left world: " + map.transform.TransformPoint(left));
                        if (composite != null)
                            report.AppendLine("Composite polygon containment: left="
                                + CompositeContains(composite, map.transform.TransformPoint(left)) + ", center="
                                + CompositeContains(composite, map.transform.TransformPoint(middle)) + ", right="
                                + CompositeContains(composite, map.transform.TransformPoint(right)));
                    }
                    EditorUtility.SetDirty(map);
                    if (PrefabUtility.IsPartOfPrefabInstance(map)) PrefabUtility.RecordPrefabInstancePropertyModifications(map);
                    EditorSceneManager.MarkSceneDirty(scene);
                    maps++;
                }
        }
        report.AppendLine("Rebuilt tilemaps: " + maps + ". Open scenes were not saved or reloaded.");
        File.WriteAllText("Library/FullTileset155-result.txt", report.ToString());
        EditorApplication.QueuePlayerLoopUpdate();
        EditorApplication.delayCall += () => AuditFinal155(sprite, report);
        Debug.Log(report.ToString());
    }

    private static void AuditFinal155(Sprite sprite, StringBuilder report)
    {
        report.AppendLine("Deferred geometry check:");
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
                {
                    CompositeCollider2D collider = map.GetComponent<CompositeCollider2D>();
                    if (collider == null) continue;
                    foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                    {
                        if (map.GetSprite(cell) != sprite) continue;
                        Matrix4x4 matrix = map.GetTransformMatrix(cell);
                        Vector3 center = map.GetCellCenterLocal(cell);
                        bool left = CompositeContains(collider, map.transform.TransformPoint(center
                            + matrix.MultiplyPoint3x4(new Vector3(-0.375f, -0.25f))));
                        bool middle = CompositeContains(collider, map.transform.TransformPoint(center
                            + matrix.MultiplyPoint3x4(new Vector3(0f, -0.25f))));
                        bool right = CompositeContains(collider, map.transform.TransformPoint(center
                            + matrix.MultiplyPoint3x4(new Vector3(0.375f, -0.25f))));
                        report.AppendLine(cell + ": left=" + left + ", center=" + middle + ", right=" + right);
                    }
                }
        }
        File.WriteAllText("Library/FullTileset155-result.txt", report.ToString());
    }

    private static void RecreateCells(Tilemap map, TilemapCollider2D collider, List<Vector3Int> cells)
    {
        TileBase[] tiles = new TileBase[cells.Count];
        Color[] colors = new Color[cells.Count];
        Matrix4x4[] matrices = new Matrix4x4[cells.Count];
        TileFlags[] flags = new TileFlags[cells.Count];
        for (int index = 0; index < cells.Count; index++)
        {
            Vector3Int cell = cells[index];
            tiles[index] = map.GetTile(cell);
            colors[index] = map.GetColor(cell);
            matrices[index] = map.GetTransformMatrix(cell);
            flags[index] = map.GetTileFlags(cell);
        }
        // Process the empty state separately so Unity cannot reuse the old per-cell physics cache.
        try
        {
            foreach (Vector3Int cell in cells) map.SetTile(cell, null);
            collider.ProcessTilemapChanges();
        }
        finally
        {
            for (int index = 0; index < cells.Count; index++)
            {
                Vector3Int cell = cells[index];
                map.SetTile(cell, tiles[index]);
                map.SetTileFlags(cell, TileFlags.None);
                map.SetColor(cell, colors[index]);
                map.SetTransformMatrix(cell, matrices[index]);
                map.SetColliderType(cell, Tile.ColliderType.Sprite);
                map.SetTileFlags(cell, flags[index]);
            }
        }
        collider.ProcessTilemapChanges();
    }

    private static bool CompositeContains(CompositeCollider2D collider, Vector2 worldPoint)
    {
        Vector2 point = (Vector2)collider.transform.InverseTransformPoint(worldPoint) - collider.offset;
        bool inside = false;
        for (int path = 0; path < collider.pathCount; path++)
        {
            Vector2[] vertices = new Vector2[collider.GetPathPointCount(path)];
            collider.GetPath(path, vertices);
            for (int index = 0, previous = vertices.Length - 1; index < vertices.Length; previous = index++)
            {
                Vector2 a = vertices[index];
                Vector2 b = vertices[previous];
                if ((a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
        }
        return inside;
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
        Collider2D.CompositeOperation operation = collider.compositeOperation;
        CompositeCollider2D.GenerationType generation = composite != null
            ? composite.generationType : CompositeCollider2D.GenerationType.Synchronous;
        try
        {
            if (composite != null) composite.generationType = CompositeCollider2D.GenerationType.Manual;
            collider.compositeOperation = Collider2D.CompositeOperation.None;
            collider.enabled = false;
            map.RefreshAllTiles();
            collider.enabled = true;
            collider.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
            collider.compositeOperation = operation;
            if (composite != null) composite.GenerateGeometry();
        }
        finally
        {
            collider.enabled = wasEnabled;
            collider.compositeOperation = operation;
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
