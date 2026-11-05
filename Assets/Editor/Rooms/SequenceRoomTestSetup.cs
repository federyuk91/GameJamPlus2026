using System;
using System.Collections.Generic;
using System.IO;
using GameJamPlus.Gameplay;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class SequenceRoomTestSetup
{
    private const string SourcePath = "Assets/Scenes/Universes/Universo_1.unity";
    private const string ScenePath = "Assets/Scenes/Sequence Room Test.unity";
    private const string SceneName = "Sequence Room Test";

    [MenuItem("Tools/Mouse Platformer/Rooms/Create Sequence Room Test", false, 0)]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before preparing the room scene.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("Sequence Room Test already exists; open it to keep editing its rooms.");

        Scene source = SceneManager.GetSceneByPath(SourcePath);
        bool openedSource = !source.IsValid() || !source.isLoaded;
        if (openedSource) source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Additive);
        try
        {
            // Save a copy of the loaded source, preserving unsaved source edits without saving over it.
            if (!EditorSceneManager.SaveScene(source, ScenePath, true))
                throw new IOException("Could not create the room test scene.");
        }
        finally
        {
            if (openedSource) EditorSceneManager.CloseScene(source, true);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(root))
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        }

        GameManager manager = FindComponent<GameManager>(scene);
        Grid sourceGrid = FindTerrainGrid(scene);
        List<Tilemap> maps = FindRoomMaps(scene);
        GameObject roomObject = CreateObject(scene, "Room1", null);
        GameObject gridObject = CreateObject(scene, "Terrain Grid", roomObject.transform);
        Grid grid = gridObject.AddComponent<Grid>();
        EditorUtility.CopySerialized(sourceGrid, grid);
        Tilemap terrain = null;
        foreach (Tilemap map in maps)
        {
            map.transform.SetParent(gridObject.transform, true);
            map.gameObject.SetActive(true);
            TilemapRenderer renderer = map.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerID = 0;
                renderer.sortingOrder = map.name == "Background - Decorative" ? 99
                    : map.name == "Foreground - Decorative" ? 600 : 500;
            }
            if (map.name == "Terrain - Solid") terrain = map;
        }

        foreach (string name in new[] { "Props", "Interactables", "Hazards", "Collectibles", "Decorations" })
            CreateObject(scene, name, roomObject.transform);

        RoomDefinition room = roomObject.AddComponent<RoomDefinition>();
        Rect footprint = new Rect(-8f, -2f, 16f, 12f);
        RoomPort left = CreatePort(scene, roomObject.transform, RoomDirection.Left, new Vector2(-8f, 0.5f));
        RoomPort right = CreatePort(scene, roomObject.transform, RoomDirection.Right, new Vector2(8f, 0.5f));
        SerializedObject roomData = new SerializedObject(room);
        roomData.FindProperty("layoutGrid").objectReferenceValue = grid;
        roomData.FindProperty("footprint").rectValue = footprint;
        SerializedProperty ports = roomData.FindProperty("ports");
        ports.arraySize = 2;
        ports.GetArrayElementAtIndex(0).objectReferenceValue = left;
        ports.GetArrayElementAtIndex(1).objectReferenceValue = right;
        roomData.ApplyModifiedPropertiesWithoutUndo();

        if (terrain != null && terrain.GetUsedTilesCount() == 0)
        {
            TileBase floor = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/Tiles/TilemapFloor/TilemapFloor_2.asset");
            if (floor == null) throw new InvalidOperationException("The starter floor tile could not be found.");
            for (int x = -8; x < 8; x++) terrain.SetTile(new Vector3Int(x, -1, 0), floor);
        }

        SerializedObject managerData = new SerializedObject(manager);
        managerData.FindProperty("campaign").objectReferenceValue = null;
        managerData.FindProperty("startingUniverse").intValue = 1;
        managerData.FindProperty("gameplaySceneName").stringValue = SceneName;
        managerData.FindProperty("endGameSceneName").stringValue = SceneName;
        managerData.ApplyModifiedPropertiesWithoutUndo();

        foreach (GameObject root in scene.GetRootGameObjects())
            if (root != roomObject && root.GetComponentInChildren<GameManager>(true) != null)
                root.name = "Room Test Environment";

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!buildScenes.Exists(entry => entry.path == ScenePath))
            buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the configured room scene.");
        Selection.activeGameObject = roomObject;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(new Bounds(Vector3.zero, new Vector3(20f, 16f, 1f)), false);
        File.WriteAllText("Temp/SequenceRoomTestSetup.done", ScenePath);
        Debug.Log("Created " + ScenePath + ". Select Room1 to edit its footprint and exits; paint its Terrain Grid and save Room1 as a separate prefab.");
    }

    private static GameObject CreateObject(Scene scene, string name, Transform parent)
    {
        GameObject result = new GameObject(name);
        SceneManager.MoveGameObjectToScene(result, scene);
        if (parent != null) result.transform.SetParent(parent, false);
        return result;
    }

    private static RoomPort CreatePort(Scene scene, Transform parent, RoomDirection direction, Vector2 position)
    {
        GameObject socket = CreateObject(scene, "Exit " + direction, parent);
        socket.transform.localPosition = position;
        RoomPort port = socket.AddComponent<RoomPort>();
        SerializedObject data = new SerializedObject(port);
        data.FindProperty("direction").enumValueIndex = (int)direction;
        data.ApplyModifiedPropertiesWithoutUndo();
        return port;
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        throw new InvalidOperationException("The source scene is missing " + typeof(T).Name + ".");
    }

    private static Grid FindTerrainGrid(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Grid grid in root.GetComponentsInChildren<Grid>(true))
                if (grid.name == "02 Terrain") return grid;
        throw new InvalidOperationException("The source scene is missing its 02 Terrain Grid.");
    }

    private static List<Tilemap> FindRoomMaps(Scene scene)
    {
        List<Tilemap> maps = new List<Tilemap>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
                if (map.name == "Terrain - Solid" || map.name == "Pipes - Solid"
                    || map.name == "Foreground - Decorative" || map.name == "Background - Decorative") maps.Add(map);
        if (maps.Count != 4) throw new InvalidOperationException("Expected exactly four room Tilemaps in the source scene.");
        return maps;
    }
}
