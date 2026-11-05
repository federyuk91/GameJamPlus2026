using System.IO;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

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
        ConfigureLayers(component);
        EditorUtility.SetDirty(component);
        if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);
        File.WriteAllText("Temp/ScientistParallaxSetup-result.txt", "Configured ScientistParallax on " + owner.name + " in " + scene.path + ". Scene not saved.");
    }

    public static void ConfigureLayers(ScientistParallax component)
    {
        if (component == null || EditorApplication.isPlaying) return;
        Undo.RecordObject(component, "Configure scientist parallax layers");
        component.InitializeLensLimits();
        SerializedObject settings = new SerializedObject(component);
        string[] fields = { "farLayer", "distantLayer", "middleLayer", "nearLayer" };
        string[] names = { "01 Far Background", "02 Distant Background", "03 Middle Background", "04 Near Background" };
        SpriteRenderer lensRenderer = component.Lens != null ? component.Lens.GetComponent<SpriteRenderer>() : null;
        for (int index = 0; index < fields.Length; index++)
        {
            SerializedProperty field = settings.FindProperty(fields[index]);
            Transform root = field.objectReferenceValue as Transform;
            if (root != null) continue;
            root = component.transform.Find(names[index]);
            if (root == null)
            {
                GameObject container = new GameObject(names[index]);
                Undo.RegisterCreatedObjectUndo(container, "Create parallax group");
                Undo.SetTransformParent(container.transform, component.transform, "Parent parallax group");
                container.transform.localPosition = Vector3.zero;
                container.transform.localRotation = Quaternion.identity;
                container.transform.localScale = Vector3.one;
                root = container.transform;
            }
            field.objectReferenceValue = root;
            SortingGroup sorting = root.GetComponent<SortingGroup>();
            if (sorting == null && lensRenderer != null)
            {
                sorting = Undo.AddComponent<SortingGroup>(root.gameObject);
                sorting.sortingLayerID = lensRenderer.sortingLayerID;
                sorting.sortingOrder = Mathf.Clamp(lensRenderer.sortingOrder - 4 + index, -32768, 32767);
            }
        }
        settings.ApplyModifiedProperties();
        EditorUtility.SetDirty(component);
        if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        if (component.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
    }
}
