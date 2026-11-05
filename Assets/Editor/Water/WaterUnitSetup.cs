using System;
using System.Collections.Generic;
using System.IO;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class WaterUnitSetup
{
    private const string Request = "Library/WaterUnitSetup.request";
    private const string Report = "Library/WaterUnitSetup-result.txt";
    private const string Welcome = "Assets/Scenes/Levels/1_WelcomeToUniverse.unity";

    static WaterUnitSetup() => EditorApplication.update += ProcessRequest;

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { ConfigureWelcome(); }
        catch (Exception exception)
        {
            File.WriteAllText(Report, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Mouse Platformer/Water/Prepare Welcome Water Unit Geometry")]
    public static void ConfigureWelcome()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetSceneByPath(Welcome);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        Scene previous = SceneManager.GetActiveScene();
        if (opened) scene = EditorSceneManager.OpenScene(Welcome, OpenSceneMode.Additive);
        try
        {
            GameObject network = null;
            List<Tilemap> maps = new List<Tilemap>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Communicating Water - Welcome") network = root;
                foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>())
                    if (map.GetUsedTilesCount() > 0 && WaterGeometryBakeTools.HasSolidCollider(map)) maps.Add(map);
            }
            if (network == null || maps.Count == 0) throw new InvalidOperationException("Welcome water network or solid Tilemaps are missing.");
            WaterGeometryBaker baker = network.GetComponent<WaterGeometryBaker>();
            if (baker == null) baker = Undo.AddComponent<WaterGeometryBaker>(network);
            Undo.RecordObject(baker, "Configure Welcome water geometry");
            baker.Configure(maps.ToArray(), AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Water Section.prefab"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlatformWater.mat"), network.GetComponentsInChildren<WaterBasin>());
            WaterGeometryBakeTools.Bake(baker, false);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Welcome could not be saved.");
            GameObject unit = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Water Unit.prefab");
            if (unit == null || unit.GetComponent<WaterUnit>() == null) throw new InvalidOperationException("Water Unit prefab import is incomplete.");
            File.WriteAllText(Report, "SUCCESS\nWelcome collision geometry baked and saved.\nAssigned solid Tilemaps: " + maps.Count
                + "\nArea: " + baker.BakedBounds + "\nExisting basins and levels preserved.\nPrefab: Assets/Prefabs/Environment/Water Unit.prefab");
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }
}
