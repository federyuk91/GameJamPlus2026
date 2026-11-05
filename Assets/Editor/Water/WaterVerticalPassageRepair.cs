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
public static class WaterVerticalPassageRepair
{
    private const string Request = "Library/WaterVerticalPassages.request";
    private const string Report = "Library/WaterVerticalPassages-result.txt";
    private const string Welcome = "Assets/Scenes/Levels/1_WelcomeToUniverse.unity";
    static WaterVerticalPassageRepair() => EditorApplication.update += ProcessRequest;

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Repair(); }
        catch (Exception exception)
        {
            File.WriteAllText(Report, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Mouse Platformer/Water/Rebake Welcome Vertical Passages")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetSceneByPath(Welcome);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        Scene previous = SceneManager.GetActiveScene();
        if (opened) scene = EditorSceneManager.OpenScene(Welcome, OpenSceneMode.Additive);
        try
        {
            WaterGeometryBaker baker = null;
            if (Selection.activeGameObject != null && Selection.activeGameObject.scene == scene)
                baker = Selection.activeGameObject.GetComponentInParent<WaterGeometryBaker>();
            if (baker == null)
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (WaterGeometryBaker candidate in root.GetComponentsInChildren<WaterGeometryBaker>())
                        if (baker == null || candidate.Basins.Count > baker.Basins.Count) baker = candidate;
            if (baker == null) throw new InvalidOperationException("No Water Geometry Baker found in Welcome.");
            WaterGeometryBakeTools.Bake(baker);
            int vertical = 0;
            int connections = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (WaterConnection connection in root.GetComponentsInChildren<WaterConnection>())
                {
                    SerializedObject data = new SerializedObject(connection);
                    WaterBasin first = data.FindProperty("first").objectReferenceValue as WaterBasin;
                    WaterBasin second = data.FindProperty("second").objectReferenceValue as WaterBasin;
                    if (first == null || second == null) continue;
                    connections++;
                    Vector3 direction = second.transform.position - first.transform.position;
                    if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x)) vertical++;
                }
            List<string> maps = new List<string>();
            foreach (Tilemap map in baker.SourceTilemaps) if (map != null) maps.Add(map.name);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Welcome could not be saved after rebaking.");
            File.WriteAllText(Report, "SUCCESS\nBaker: " + baker.name + "\nTilemaps: " + string.Join(", ", maps)
                + "\nBasins: " + baker.Basins.Count + "\nConnections: " + connections + "\nVertical connections: " + vertical
                + "\nWelcome saved. Initial water surfaces and matching basin references preserved.");
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }
}
