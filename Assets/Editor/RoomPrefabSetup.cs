using System.IO;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class RoomPrefabSetup
{
    private const string Folder = "Assets/Prefabs/Rooms";
    private const string GeneratorPath = Folder + "/Room Layout Generator.prefab";

    [MenuItem("Tools/Mouse Platformer/Rooms/Prepare Template and Example Prefabs")]
    public static void CreateDefaults()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Rooms");
        CreateRoom("Room Template", false);
        RoomDefinition corner = CreateRoom("Example Up Left", true, RoomDirection.Up, RoomDirection.Left);
        RoomDefinition horizontal = CreateRoom("Example Left Right", true, RoomDirection.Left, RoomDirection.Right);
        RoomDefinition vertical = CreateRoom("Example Up Down", true, RoomDirection.Up, RoomDirection.Down);
        RoomDefinition[] terminals = new RoomDefinition[4];
        for (int i = 0; i < terminals.Length; i++)
            terminals[i] = CreateRoom("Terminal " + (RoomDirection)i, true, (RoomDirection)i);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(GeneratorPath) == null)
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = new GameObject("Room Layout Generator");
                SceneManager.MoveGameObjectToScene(root, preview);
                RoomLayoutGenerator generator = root.AddComponent<RoomLayoutGenerator>();
                SerializedObject data = new SerializedObject(generator);
                data.FindProperty("startRoom").objectReferenceValue = corner;
                SerializedProperty rooms = data.FindProperty("rooms");
                rooms.arraySize = 2;
                rooms.GetArrayElementAtIndex(0).objectReferenceValue = horizontal;
                rooms.GetArrayElementAtIndex(1).objectReferenceValue = vertical;
                SerializedProperty caps = data.FindProperty("terminalRooms");
                caps.arraySize = 4;
                for (int i = 0; i < 4; i++) caps.GetArrayElementAtIndex(i).objectReferenceValue = terminals[i];
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GeneratorPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/CodexRoomSetup.done", "Room prefabs and generator created.");
        Debug.Log("Rooms: template, terminal examples and generator ready in " + Folder);
    }

    private static RoomDefinition CreateRoom(string name, bool paint, params RoomDirection[] exits)
    {
        string path = Folder + "/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing.GetComponent<RoomDefinition>();
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, preview);
            RoomDefinition room = root.AddComponent<RoomDefinition>();
            GameObject gridPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tilemaps/GameplayGrid.prefab");
            if (gridPrefab == null) throw new System.InvalidOperationException("GameplayGrid.prefab not found.");
            GameObject grid = (GameObject)PrefabUtility.InstantiatePrefab(gridPrefab, preview);
            grid.transform.SetParent(root.transform, false);
            SerializedObject data = new SerializedObject(room);
            data.FindProperty("layoutGrid").objectReferenceValue = grid.GetComponent<Grid>();
            data.FindProperty("footprint").rectValue = new Rect(-4f, -3f, 8f, 6f);
            SerializedProperty ports = data.FindProperty("ports");
            ports.arraySize = exits.Length;
            for (int i = 0; i < exits.Length; i++)
            {
                GameObject socket = new GameObject("Exit " + exits[i]);
                SceneManager.MoveGameObjectToScene(socket, preview);
                socket.transform.SetParent(root.transform, false);
                switch (exits[i])
                {
                    case RoomDirection.Up: socket.transform.localPosition = new Vector3(0.5f, 3f, 0f); break;
                    case RoomDirection.Down: socket.transform.localPosition = new Vector3(0.5f, -3f, 0f); break;
                    case RoomDirection.Left: socket.transform.localPosition = new Vector3(-4f, 0.5f, 0f); break;
                    case RoomDirection.Right: socket.transform.localPosition = new Vector3(4f, 0.5f, 0f); break;
                }
                RoomPort port = socket.AddComponent<RoomPort>();
                SerializedObject portData = new SerializedObject(port);
                portData.FindProperty("direction").enumValueIndex = (int)exits[i];
                portData.ApplyModifiedPropertiesWithoutUndo();
                ports.GetArrayElementAtIndex(i).objectReferenceValue = port;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            if (paint)
            {
                Tilemap map = grid.transform.Find("Terrain - Solid").GetComponent<Tilemap>();
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/Tiles/TilemapFloor/TilemapFloor_2.asset");
                if (tile == null) throw new System.InvalidOperationException("Example tile not found.");
                for (int y = -3; y < 3; y++)
                for (int x = -4; x < 4; x++)
                {
                    if (x != -4 && x != 3 && y != -3 && y != 2) continue;
                    bool opening = false;
                    foreach (RoomDirection direction in exits)
                    {
                        if (direction == RoomDirection.Up && x == 0 && y == 2) opening = true;
                        if (direction == RoomDirection.Down && x == 0 && y == -3) opening = true;
                        if (direction == RoomDirection.Left && x == -4 && y == 0) opening = true;
                        if (direction == RoomDirection.Right && x == 3 && y == 0) opening = true;
                    }
                    if (!opening) map.SetTile(new Vector3Int(x, y, 0), tile);
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(map);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<RoomDefinition>();
    }
}
