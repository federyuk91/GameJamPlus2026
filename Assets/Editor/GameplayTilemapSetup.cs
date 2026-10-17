using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class GameplayTilemapSetup
{
    private const string Root = "Assets/Tilemaps";
    private const string GridPath = Root + "/GameplayGrid.prefab";
    private static readonly string[] Sheets = { "TilemapFloor", "TilemapTube", "TilePipes" };

    [MenuItem("Tools/Mouse Platformer/Tilemaps/Prepara tile e palette")]
    public static void CreateAssets()
    {
        EnsureFolder(Root);
        EnsureFolder(Root + "/Tiles");
        EnsureFolder(Root + "/Palettes");
        foreach (string sheet in Sheets) CreateSheetAssets(sheet);
        CreateGridPrefab();
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/CodexTilemapSetup.done", "Tile e palette creati; GameplayGrid pronto.");
        Debug.Log("Tilemaps: tile, tre palette e GameplayGrid pronti in Assets/Tilemaps.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void CreateSheetAssets(string sheet)
    {
        string texturePath = "Assets/Art/Tiles/" + sheet + ".png";
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Texture non trovata: " + texturePath);
        bool changed = importer.filterMode != FilterMode.Point || importer.spritePixelsPerUnit != 32f
            || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed;
        if (changed)
        {
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 32f;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        string folder = Root + "/Tiles/" + sheet;
        EnsureFolder(folder);
        List<Sprite> sprites = new List<Sprite>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
            if (asset is Sprite sprite) sprites.Add(sprite);
        sprites.Sort((Sprite a, Sprite b) =>
        {
            int row = b.rect.y.CompareTo(a.rect.y);
            return row != 0 ? row : a.rect.x.CompareTo(b.rect.x);
        });
        if (sprites.Count == 0) throw new InvalidOperationException("Nessuno sprite suddiviso in " + texturePath);
        Dictionary<Sprite, Tile> tiles = new Dictionary<Sprite, Tile>();
        foreach (Sprite sprite in sprites)
        {
            string path = folder + "/" + sprite.name + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = sprite.name;
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Sprite;
                AssetDatabase.CreateAsset(tile, path);
            }
            tiles.Add(sprite, tile);
        }
        string palettePath = Root + "/Palettes/" + sheet + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) != null) return;
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject root = new GameObject(sheet, typeof(Grid));
            SceneManager.MoveGameObjectToScene(root, preview);
            root.GetComponent<Grid>().cellSize = Vector3.one;
            Tilemap map = CreateMap(root.transform, "Palette", 0, false);
            foreach (Sprite sprite in sprites)
                map.SetTile(new Vector3Int(Mathf.RoundToInt(sprite.rect.x / 32f), Mathf.RoundToInt(sprite.rect.y / 32f), 0), tiles[sprite]);
            PrefabUtility.SaveAsPrefabAsset(root, palettePath);
            GridPalette settings = ScriptableObject.CreateInstance<GridPalette>();
            settings.name = "Palette Settings";
            settings.cellSizing = GridPalette.CellSizing.Manual;
            AssetDatabase.AddObjectToAsset(settings, palettePath);
            EditorUtility.SetDirty(settings);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static Tilemap CreateMap(Transform parent, string name, int sortingOrder, bool solid)
    {
        GameObject gameObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        SceneManager.MoveGameObjectToScene(gameObject, parent.gameObject.scene);
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = LayerMask.NameToLayer("Default");
        TilemapRenderer renderer = gameObject.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        if (solid)
        {
            Rigidbody2D rigidbody = gameObject.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Static;
            CompositeCollider2D composite = gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            composite.vertexDistance = 0.001f;
            composite.offsetDistance = 0.002f;
            TilemapCollider2D collider = gameObject.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            collider.extrusionFactor = 0.001f;
        }
        return gameObject.GetComponent<Tilemap>();
    }

    private static void CreateGridPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(GridPath) != null) return;
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject root = new GameObject("Gameplay Tilemaps", typeof(Grid));
            SceneManager.MoveGameObjectToScene(root, preview);
            root.GetComponent<Grid>().cellSize = Vector3.one;
            CreateMap(root.transform, "Background - Decorative", -20, false);
            CreateMap(root.transform, "Terrain - Solid", -1, true);
            CreateMap(root.transform, "Pipes - Solid", -1, true);
            CreateMap(root.transform, "Foreground - Decorative", 5, false);
            PrefabUtility.SaveAsPrefabAsset(root, GridPath);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    [MenuItem("Tools/Mouse Platformer/Tilemaps/Apri Tile Palette")]
    private static void OpenPalette() => EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
}
