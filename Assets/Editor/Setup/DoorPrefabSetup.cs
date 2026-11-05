using System;
using System.IO;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DoorPrefabSetup
{
    private const string Request = "Library/CreateUniverseDoor.request";
    private const string Report = "Library/UniverseDoor-result.txt";
    private const string ScenePath = "Assets/Scenes/Universes/Universo_10.unity";
    private const string PrefabPath = "Assets/Prefabs/Props/Door.prefab";

    static DoorPrefabSetup() => EditorApplication.update += ProcessRequest;

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Configure(); }
        catch (Exception exception)
        {
            File.WriteAllText(Report, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Gameplay/Create Door Prefab from Universe 10")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Sprite openSprite = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Tiles/Full TileSet.png"))
            if (asset is Sprite sprite && sprite.name == "Full TileSet_155") openSprite = sprite;
        if (openSprite == null) throw new InvalidOperationException("Full TileSet_155 is missing.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        Scene previous = SceneManager.GetActiveScene();
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        bool wasDirty = scene.isDirty;
        try
        {
            GameObject owner = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name != "Door") continue;
                    if (owner != null) throw new InvalidOperationException("More than one Door found in Universe 10.");
                    owner = candidate.gameObject;
                }
            if (owner == null) throw new InvalidOperationException("Door was not found in Universe 10.");
            SpriteRenderer visual = owner.GetComponent<SpriteRenderer>();
            Collider2D blockingCollider = owner.GetComponent<Collider2D>();
            if (visual == null || blockingCollider == null)
                throw new InvalidOperationException("Door needs its existing SpriteRenderer and collider.");
            Door door = owner.GetComponent<Door>();
            if (door == null) door = Undo.AddComponent<Door>(owner);
            SerializedObject settings = new SerializedObject(door);
            settings.FindProperty("visual").objectReferenceValue = visual;
            settings.FindProperty("blockingCollider").objectReferenceValue = blockingCollider;
            settings.FindProperty("closedSprite").objectReferenceValue = visual.sprite;
            settings.FindProperty("openSprite").objectReferenceValue = openSprite;
            settings.FindProperty("startsOpen").boolValue = false;
            settings.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAssetAndConnect(owner, PrefabPath, InteractionMode.UserAction, out bool success);
            if (!success) throw new IOException("Could not create the Door prefab.");
            EditorSceneManager.MarkSceneDirty(scene);
            bool save = opened || !wasDirty;
            if (save && !EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Universe 10.");
            File.WriteAllText(Report, "SUCCESS\nPrefab: " + PrefabPath
                + "\nDoor connected in Universe 10.\nScene saved: " + save
                + "\nExisting closed sprite, transform and collider geometry preserved.\nOpen sprite: Full TileSet_155.");
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }
}
