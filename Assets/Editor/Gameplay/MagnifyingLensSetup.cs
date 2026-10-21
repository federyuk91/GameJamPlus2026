using System.IO;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MagnifyingLensSetup
{
    private const string Request = "Temp/MagnifyingLensSetup.request";
    private const string Report = "Temp/MagnifyingLensSetup-result.txt";
    private const string LensSpriteGuid = "6a4cf2330ad18054f92a858b416219f3";
    private const string MaterialPath = "Assets/Materials/MagnifyingLens2D.mat";

    static MagnifyingLensSetup() => EditorApplication.delayCall += ProcessRequest;

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        Configure();
    }

    [MenuItem("Tools/Gameplay/Configure Magnifying Lens")]
    private static void Configure()
    {
        Scene scene = SceneManager.GetActiveScene();
        SpriteRenderer lens = null;
        Camera camera = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SpriteRenderer candidate in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (candidate.sprite != null && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(candidate.sprite)) == LensSpriteGuid)
                    lens = candidate;
            foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true))
                if (candidate.CompareTag("MainCamera")) camera = candidate;
        }
        if (lens == null || camera == null)
        {
            File.WriteAllText(Report, "Open the Welcome scene containing BG (1), then use Tools/Gameplay/Configure Magnifying Lens. The current scene was not modified.");
            return;
        }

        Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        SerializedObject tags = new SerializedObject(settings[0]);
        SerializedProperty layers = tags.FindProperty("m_SortingLayers");
        int layerID = 1190830421;
        bool found = false;
        for (int index = 0; index < layers.arraySize; index++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (layer.FindPropertyRelative("name").stringValue != MagnifyingLens2D.BackgroundLayer) continue;
            layerID = layer.FindPropertyRelative("uniqueID").intValue;
            found = true;
            break;
        }
        if (!found)
        {
            layers.InsertArrayElementAtIndex(0);
            SerializedProperty layer = layers.GetArrayElementAtIndex(0);
            layer.FindPropertyRelative("name").stringValue = MagnifyingLens2D.BackgroundLayer;
            layer.FindPropertyRelative("uniqueID").intValue = layerID;
            layer.FindPropertyRelative("locked").boolValue = false;
            tags.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(settings[0]);
        AssetDatabase.SaveAssetIfDirty(settings[0]);

        Object rendererData = AssetDatabase.LoadAssetAtPath<Object>("Assets/Settings/Renderer2D.asset");
        SerializedObject renderer = new SerializedObject(rendererData);
        renderer.FindProperty("m_UseCameraSortingLayersTexture").boolValue = true;
        renderer.FindProperty("m_CameraSortingLayersTextureBound").intValue = layerID;
        renderer.FindProperty("m_CameraSortingLayerDownsamplingMethod").intValue = 0;
        renderer.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(rendererData);

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MagnifyingLens2D.shader");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "MagnifyingLens2D" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        MagnifyingLens2D component = lens.GetComponent<MagnifyingLens2D>();
        if (component == null) component = Undo.AddComponent<MagnifyingLens2D>(lens.gameObject);
        Undo.RecordObject(component, "Configure magnifying lens");
        Undo.RecordObject(lens, "Configure magnifying lens material");
        SerializedObject lensSettings = new SerializedObject(component);
        lensSettings.FindProperty("lensRenderer").objectReferenceValue = lens;
        lensSettings.FindProperty("magnifyingMaterial").objectReferenceValue = material;
        lensSettings.FindProperty("viewCamera").objectReferenceValue = camera;
        lensSettings.ApplyModifiedProperties();
        component.ApplySettings();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Light2D light in root.GetComponentsInChildren<Light2D>(true))
            {
                SerializedObject lightSettings = new SerializedObject(light);
                SerializedProperty lightLayers = lightSettings.FindProperty("m_ApplyToSortingLayers");
                bool affectsLensLayer = false;
                bool affectsBackground = false;
                for (int index = 0; index < lightLayers.arraySize; index++)
                {
                    int existing = lightLayers.GetArrayElementAtIndex(index).intValue;
                    affectsLensLayer |= existing == layerID;
                    affectsBackground |= existing == lens.sortingLayerID;
                }
                if (!affectsLensLayer && affectsBackground)
                {
                    Undo.RecordObject(light, "Include lens background in 2D lighting");
                    lightLayers.InsertArrayElementAtIndex(lightLayers.arraySize);
                    lightLayers.GetArrayElementAtIndex(lightLayers.arraySize - 1).intValue = layerID;
                    lightSettings.ApplyModifiedProperties();
                    if (PrefabUtility.IsPartOfPrefabInstance(light)) PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                }
            }
        }
        if (PrefabUtility.IsPartOfPrefabInstance(lens)) PrefabUtility.RecordPrefabInstancePropertyModifications(lens);
        EditorUtility.SetDirty(component);
        EditorSceneManager.MarkSceneDirty(scene);
        File.WriteAllText(Report, "Configured lens on " + lens.name + " in " + scene.path + ". Magnification: 2. Capture layer ID: " + SortingLayer.NameToID(MagnifyingLens2D.BackgroundLayer) + ". Scene marked dirty, not saved. Shader errors: " + ShaderUtil.ShaderHasError(shader));
        Debug.Log("Magnifying lens configured. Save the Welcome scene to keep the component and lighting settings.", lens);
    }
}
