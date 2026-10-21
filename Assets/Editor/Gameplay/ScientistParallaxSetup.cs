using System.IO;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ScientistParallaxSetup
{
    private const string Request = "Temp/ScientistParallaxSetup.request";
    static ScientistParallaxSetup() => EditorApplication.delayCall += ProcessRequest;

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        Configure();
    }

    [MenuItem("Tools/Gameplay/Configure Scientist Parallax")]
    private static void Configure()
    {
        Scene scene = SceneManager.GetActiveScene();
        SpriteRenderer bust = null;
        MagnifyingLens2D lens = null;
        Camera camera = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SpriteRenderer candidate in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (candidate.name == "Busto") bust = candidate;
            foreach (MagnifyingLens2D candidate in root.GetComponentsInChildren<MagnifyingLens2D>(true))
                if (candidate.name == "Lente") lens = candidate;
            foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true))
                if (candidate.CompareTag("MainCamera")) camera = candidate;
        }
        if (bust == null || lens == null || camera == null)
        {
            File.WriteAllText("Temp/ScientistParallaxSetup-result.txt", "Open the Welcome scene with Busto, Lente and Main Camera, then use Tools/Gameplay/Configure Scientist Parallax.");
            return;
        }
        GameObject owner = bust.transform.parent != null && lens.transform.parent == bust.transform.parent
            ? bust.transform.parent.gameObject : bust.gameObject;
        ScientistParallax component = owner.GetComponent<ScientistParallax>();
        if (component == null) component = Undo.AddComponent<ScientistParallax>(owner);
        Undo.RecordObject(component, "Configure scientist parallax");
        SerializedObject settings = new SerializedObject(component);
        settings.FindProperty("viewCamera").objectReferenceValue = camera;
        settings.FindProperty("bust").objectReferenceValue = bust;
        settings.FindProperty("lens").objectReferenceValue = lens;
        settings.ApplyModifiedProperties();
        EditorUtility.SetDirty(component);
        if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);
        File.WriteAllText("Temp/ScientistParallaxSetup-result.txt", "Configured ScientistParallax on " + owner.name + " in " + scene.path + ". Scene not saved.");
    }
}
