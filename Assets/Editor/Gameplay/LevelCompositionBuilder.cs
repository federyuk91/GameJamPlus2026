using System;
using System.Collections.Generic;
using System.IO;
using GameJamPlus.Gameplay;
using GameJamPlus.UI;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class LevelCompositionBuilder
{
    private const string RequestPath = "Temp/CreateComposedUniverse.request";
    private const string FinalizeRequestPath = "Temp/FinalizeComposedUniverse.request";
    private const string LensOrderRequestPath = "Temp/RepairComposedLensOrder.request";
    private const string LensCaptureRequestPath = "Temp/RepairComposedLensCapture.request";
    private const string GameplayPath = "Assets/Scenes/Universes/Universo_10.unity";
    private const string ArtPath = "Assets/Scenes/Parallax Test.unity";
    private const string OutputPath = "Assets/Scenes/Universes/Universo_10_Composed.unity";
    private const string CampaignPath = "Assets/Settings/UniverseCampaign_Composed.asset";
    private const string ReportPath = "Temp/LevelComposition-result.txt";
    private static readonly Dictionary<ParallaxConfigurator.Depth, Transform> planes = new Dictionary<ParallaxConfigurator.Depth, Transform>();
    private static readonly Dictionary<Object, Object> copiedObjects = new Dictionary<Object, Object>();
    private static readonly Dictionary<string, Transform> folders = new Dictionary<string, Transform>();
    private static Scene destination;
    private static Transform systems;

    static LevelCompositionBuilder()
    {
        EditorApplication.update += ProcessRequest;
    }

    private static void ProcessRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(LensCaptureRequestPath))
        {
            File.Delete(LensCaptureRequestPath);
            try { FinalizeComposition(); }
            catch (Exception exception) { File.AppendAllText(ReportPath, "\nLENS CAPTURE ERROR: " + exception); Debug.LogException(exception); }
        }
        if (File.Exists(LensOrderRequestPath))
        {
            File.Delete(LensOrderRequestPath);
            try { FinalizeComposition(); }
            catch (Exception exception) { File.AppendAllText(ReportPath, "\nLENS ORDER ERROR: " + exception); Debug.LogException(exception); }
        }
        if (File.Exists(FinalizeRequestPath))
        {
            File.Delete(FinalizeRequestPath);
            try { FinalizeComposition(); }
            catch (Exception exception) { File.AppendAllText(ReportPath, "\nFINALIZE ERROR: " + exception); Debug.LogException(exception); }
        }
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        try { Create(); }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    [MenuItem("Tools/Gameplay/Create Composed Universe 10")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before composing a level.");
        if (File.Exists(OutputPath) || File.Exists(CampaignPath)) throw new IOException("The composed scene or campaign already exists; nothing will be overwritten.");
        planes.Clear();
        copiedObjects.Clear();
        folders.Clear();
        Scene originalActive = SceneManager.GetActiveScene();
        Scene artScene = SceneManager.GetSceneByPath(ArtPath);
        bool artWasLoaded = artScene.IsValid() && artScene.isLoaded;
        try
        {
            if (!AssetDatabase.CopyAsset(GameplayPath, OutputPath)) throw new IOException("Could not copy the gameplay source scene.");
            destination = EditorSceneManager.OpenScene(OutputPath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(destination);
            if (!artWasLoaded) artScene = EditorSceneManager.OpenScene(ArtPath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(destination);
            Compose(artScene);
            if (!EditorSceneManager.SaveScene(destination, OutputPath)) throw new IOException("Could not save the composed scene.");
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<UniverseCampaign>(CampaignPath));
            RegisterBuildScene();
            File.AppendAllText(ReportPath, "\nSUCCESS: " + OutputPath + "\nSource scenes were not saved or replaced.\n");
        }
        catch (Exception exception)
        {
            File.WriteAllText(ReportPath, exception.ToString());
            throw;
        }
        finally
        {
            if (!artWasLoaded && artScene.IsValid() && artScene.isLoaded) EditorSceneManager.CloseScene(artScene, true);
            if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
        }
    }

    private static void FinalizeComposition()
    {
        Scene scene = SceneManager.GetSceneByPath(OutputPath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(OutputPath, OpenSceneMode.Additive);
        destination = scene;
        ParallaxConfigurator configurator = One<ParallaxConfigurator>(scene);
        Transform scientists = null;
        foreach (ParallaxConfigurator.DepthPlane plane in configurator.Depths)
            if (plane.Category == ParallaxConfigurator.Depth.BackgroundScientists) scientists = plane.Parent.transform;
        foreach (BackgroundParallaxLayer layer in All<BackgroundParallaxLayer>(scene))
        {
            if (layer.name != "Scientist - Desk Background Decoration") continue;
            Move(layer.transform, scientists);
        }

        HashSet<Object> referenced = new HashSet<Object>();
        foreach (Component component in All<Component>(scene))
        {
            if (component == null || component is Transform || component is Grid) continue;
            SerializedObject data = new SerializedObject(component);
            SerializedProperty property = data.GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null) referenced.Add(property.objectReferenceValue);
        }
        foreach (Transform transform in All<Transform>(scene))
        {
            if (transform == null || transform.name != "Authoring Helpers") continue;
            foreach (Transform child in Children(transform))
            {
                if (child.childCount != 0 || referenced.Contains(child) || referenced.Contains(child.gameObject)) continue;
                bool structuralOnly = true;
                foreach (Component component in child.GetComponents<Component>())
                    if (!(component is Transform) && !(component is Grid)) structuralOnly = false;
                if (structuralOnly) Object.DestroyImmediate(child.gameObject);
            }
            if (transform.childCount == 0) Object.DestroyImmediate(transform.gameObject);
        }
        ApplyGameplayLensOrder(configurator);
        if (!ParallaxConfiguratorEditor.Configure(configurator)) throw new InvalidOperationException("Final configuration rejected.");
        AuditSceneReferences();
        EditorSceneManager.SaveScene(scene, OutputPath);
        File.AppendAllText(ReportPath, "\nFINALIZED: redundant empty containers removed; scientist assigned to plane 10; lens behind gameplay (Z 1.5, order 50).\n");
    }

    private static void Compose(Scene artScene)
    {
        GameObject[] oldRoots = destination.GetRootGameObjects();
        GameObject rig = Array.Find(oldRoots, root => root.name == "Universe Rig");
        if (rig != null && PrefabUtility.IsPartOfPrefabInstance(rig))
            PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        GameManager manager = One<GameManager>(destination);
        PlayerMovement player = One<PlayerMovement>(destination);
        GameplayHudController hud = One<GameplayHudController>(destination);
        Camera camera = One<Camera>(destination);
        Dictionary<string, int> gameplayBefore = Inventory(destination);
        Transform level = NewGroup("Level_Universe_10_Composed", null);
        systems = NewGroup("Systems", level);
        Transform spawnRoot = NewGroup("Spawn Points", level);
        Transform world = NewGroup("World", level);

        Move(manager.transform, systems);
        Move(camera.transform, systems);
        Move(hud.transform, systems);
        if (rig != null)
        {
            foreach (Transform child in Children(rig.transform))
                if (child.name == "HandRelease" || child.name == "Global Light" || child.name == "Gameplay Music") Move(child, systems);
        }
        foreach (SpawnPoint point in All<SpawnPoint>(destination)) Move(point.transform, spawnRoot);

        // Only scenery is replaced. Gameplay remains the copy of Universo_10.
        foreach (ScientistParallax legacy in All<ScientistParallax>(destination))
            Object.DestroyImmediate(legacy.gameObject);
        foreach (GameObject root in oldRoots)
        {
            if (root == null) continue;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child != null && child.name == "Laboratory Desk - Parallax Background") Object.DestroyImmediate(child.gameObject);
        }

        ParallaxConfigurator artConfig = One<ParallaxConfigurator>(artScene);
        List<GameObject> artClones = new List<GameObject>();
        foreach (ParallaxConfigurator.DepthPlane setting in artConfig.Depths)
        {
            Transform root;
            if (IsScenery(setting.Category))
            {
                GameObject source = setting.Parent;
                if (source == null) throw new InvalidOperationException("Missing art parent: " + setting.Category);
                GameObject clone = Object.Instantiate(source);
                SceneManager.MoveGameObjectToScene(clone, destination);
                clone.name = source.name;
                clone.transform.SetParent(world, true);
                MapHierarchy(source, clone);
                artClones.Add(clone);
                root = clone.transform;
            }
            else
            {
                root = NewGroup(NameFor(setting.Category), world);
                if (setting.Category == ParallaxConfigurator.Depth.Terrain
                    || setting.Category == ParallaxConfigurator.Depth.WaterAndDiggableGround
                    || setting.Category == ParallaxConfigurator.Depth.CollectiblesAndProps) root.gameObject.AddComponent<Grid>();
            }
            planes.Add(setting.Category, root);
        }
        Move(player.transform, planes[ParallaxConfigurator.Depth.PlayerAndCharacters]);
        copiedObjects[One<GameManager>(artScene)] = manager;
        copiedObjects[One<GameManager>(artScene).gameObject] = manager.gameObject;
        copiedObjects[One<Camera>(artScene)] = camera;
        copiedObjects[One<Camera>(artScene).transform] = camera.transform;
        copiedObjects[One<PlayerMovement>(artScene).transform] = player.transform;
        copiedObjects[One<PlayerMovement>(artScene).gameObject] = player.gameObject;
        copiedObjects[One<PlayerMovement>(artScene)] = player;
        foreach (GameObject clone in artClones) Remap(clone);

        foreach (GameObject root in oldRoots)
            if (root != null && !root.transform.IsChildOf(level)) Organize(root.transform, level);

        ParallaxConfigurator configurator = world.gameObject.AddComponent<ParallaxConfigurator>();
        EditorUtility.CopySerialized(artConfig, configurator);
        SerializedObject configData = new SerializedObject(configurator);
        SerializedProperty settings = configData.FindProperty("depths");
        for (int index = 0; index < settings.arraySize; index++)
        {
            SerializedProperty item = settings.GetArrayElementAtIndex(index);
            ParallaxConfigurator.Depth category = (ParallaxConfigurator.Depth)item.FindPropertyRelative("depth").intValue;
            item.FindPropertyRelative("parent").objectReferenceValue = planes[category].gameObject;
        }
        configData.FindProperty("deskSurface").objectReferenceValue = copiedObjects[artConfig.DeskSurface];
        configData.ApplyModifiedPropertiesWithoutUndo();
        ApplyGameplayLensOrder(configurator);
        if (!ParallaxConfiguratorEditor.Configure(configurator)) throw new InvalidOperationException("Depth configuration was rejected.");
        CreateCampaign(manager);
        Dictionary<string, int> gameplayAfter = Inventory(destination);
        foreach (KeyValuePair<string, int> entry in gameplayBefore)
            if (!gameplayAfter.TryGetValue(entry.Key, out int count) || count != entry.Value)
                throw new InvalidOperationException("Gameplay inventory changed: " + entry.Key);
        One<GameManager>(destination);
        One<PlayerMovement>(destination);
        One<Camera>(destination);
        One<GameplayHudController>(destination);
        AuditSceneReferences();
        File.WriteAllText(ReportPath, "Gameplay from: " + GameplayPath + "\nScenery from: " + ArtPath + "\nPlanes: " + planes.Count + "\nPreserved gameplay:\n");
        foreach (KeyValuePair<string, int> entry in gameplayAfter) File.AppendAllText(ReportPath, entry.Key + ": " + entry.Value + "\n");
        EditorSceneManager.MarkSceneDirty(destination);
    }

    private static void ApplyGameplayLensOrder(ParallaxConfigurator configurator)
    {
        SerializedObject data = new SerializedObject(configurator);
        SerializedProperty depths = data.FindProperty("depths");
        int lensIndex = -1;
        for (int index = 0; index < depths.arraySize; index++)
        {
            SerializedProperty plane = depths.GetArrayElementAtIndex(index);
            ParallaxConfigurator.Depth category = (ParallaxConfigurator.Depth)plane.FindPropertyRelative("depth").intValue;
            if (category != ParallaxConfigurator.Depth.Lens) continue;
            plane.FindPropertyRelative("worldZ").floatValue = 1.5f;
            plane.FindPropertyRelative("sortingLayer").stringValue = "Default";
            plane.FindPropertyRelative("sortingOrder").intValue = 50;
            plane.FindPropertyRelative("useSortingGroup").boolValue = true;
            lensIndex = index;
        }
        if (lensIndex >= 0) depths.MoveArrayElement(lensIndex, 6);
        data.ApplyModifiedPropertiesWithoutUndo();
        for (int index = 0; index < configurator.Depths.Count; index++)
        {
            ParallaxConfigurator.DepthPlane plane = configurator.Depths[index];
            if (plane.Parent == null) continue;
            switch (plane.Category)
            {
                case ParallaxConfigurator.Depth.Lens: plane.Parent.name = "07 Lens - Behind Gameplay"; break;
                case ParallaxConfigurator.Depth.WaterAndDiggableGround: plane.Parent.name = "03 Water And Diggable Ground"; break;
                case ParallaxConfigurator.Depth.GameplayDecorations: plane.Parent.name = "04 Gameplay Decorations"; break;
                case ParallaxConfigurator.Depth.PlayerAndCharacters: plane.Parent.name = "05 Player And Characters"; break;
                case ParallaxConfigurator.Depth.CollectiblesAndProps: plane.Parent.name = "06 Collectibles And Props"; break;
            }
            plane.Parent.transform.SetSiblingIndex(index);
        }
    }

    private static void Organize(Transform node, Transform level)
    {
        if (node == null || node.IsChildOf(level)) return;
        GameObject go = node.gameObject;
        if (go.GetComponent<Tilemap>() != null)
        {
            bool backing = go.name.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0;
            Move(node, backing ? Folder(ParallaxConfigurator.Depth.CollectiblesAndProps, "Room Backing") : planes[ParallaxConfigurator.Depth.Terrain]);
            return;
        }
        bool prefabRoot = PrefabUtility.IsAnyPrefabInstanceRoot(go);
        bool containsTilemaps = go.GetComponentInChildren<Tilemap>(true) != null;
        if (prefabRoot && containsTilemaps)
        {
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            prefabRoot = false;
        }
        if (prefabRoot || go.GetComponent<Renderer>() != null || go.GetComponent<Collider2D>() != null || go.GetComponents<MonoBehaviour>().Length > 0)
        {
            if (Has(go, "WaterVolume") || Has(go, "DiggableTerrain")) Move(node, planes[ParallaxConfigurator.Depth.WaterAndDiggableGround]);
            else if (Has(go, "PiranhaBite") || Has(go, "PiranhaDeath")) Move(node, planes[ParallaxConfigurator.Depth.PlayerAndCharacters]);
            else
            {
                string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                if (prefab.Contains("/Decorations/") || go.name.StartsWith("Algae", StringComparison.OrdinalIgnoreCase)) Move(node, planes[ParallaxConfigurator.Depth.GameplayDecorations]);
                else if (Has(go, "Collectible")) Move(node, Folder(ParallaxConfigurator.Depth.CollectiblesAndProps, "Collectibles"));
                else if (Has(go, "InteractableButton") || Has(go, "ButtonPressurePlate") || Has(go, "Door")) Move(node, Folder(ParallaxConfigurator.Depth.CollectiblesAndProps, "Buttons And Doors"));
                else if (Has(go, "MouseTrap") || Has(go, "ElectricTrap")) Move(node, Folder(ParallaxConfigurator.Depth.CollectiblesAndProps, "Traps"));
                else if (go.name.IndexOf("Goal", StringComparison.OrdinalIgnoreCase) >= 0 || go.name.IndexOf("Checkpoint", StringComparison.OrdinalIgnoreCase) >= 0) Move(node, Folder(ParallaxConfigurator.Depth.CollectiblesAndProps, "Goals And Checkpoints"));
                else Move(node, Folder(ParallaxConfigurator.Depth.CollectiblesAndProps, "Props And Volumes"));
            }
            return;
        }
        foreach (Transform child in Children(node)) Organize(child, level);
        if (node.childCount == 0) Move(node, NewGroupOnce("Authoring Helpers", systems));
    }

    private static bool Has(GameObject go, string name)
    {
        foreach (MonoBehaviour behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour != null && behaviour.GetType().Name == name) return true;
        return false;
    }

    private static bool IsScenery(ParallaxConfigurator.Depth category)
    {
        return category == ParallaxConfigurator.Depth.ForegroundDeskDecorations || category == ParallaxConfigurator.Depth.Lens
            || category == ParallaxConfigurator.Depth.BackgroundDeskDecorations || category == ParallaxConfigurator.Depth.FixedDesk
            || category == ParallaxConfigurator.Depth.BackgroundScientists || category == ParallaxConfigurator.Depth.WallElements
            || category == ParallaxConfigurator.Depth.BehindWindow;
    }

    private static string NameFor(ParallaxConfigurator.Depth category)
    {
        switch (category)
        {
            case ParallaxConfigurator.Depth.Terrain: return "02 Terrain";
            case ParallaxConfigurator.Depth.WaterAndDiggableGround: return "04 Water And Diggable Ground";
            case ParallaxConfigurator.Depth.GameplayDecorations: return "05 Gameplay Decorations";
            case ParallaxConfigurator.Depth.PlayerAndCharacters: return "06 Player And Characters";
            default: return "07 Collectibles And Props";
        }
    }

    private static Transform Folder(ParallaxConfigurator.Depth category, string name) => NewGroupOnce(name, planes[category]);

    private static Transform NewGroupOnce(string name, Transform parent)
    {
        string key = parent.GetInstanceID() + "/" + name;
        if (!folders.TryGetValue(key, out Transform group)) { group = NewGroup(name, parent); folders.Add(key, group); }
        return group;
    }

    private static Transform NewGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, destination);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void Move(Transform item, Transform parent)
    {
        item.SetParent(parent, true);
    }

    private static List<Transform> Children(Transform root)
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in root) children.Add(child);
        return children;
    }

    private static List<T> All<T>(Scene scene) where T : Component
    {
        List<T> result = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
        return result;
    }

    private static T One<T>(Scene scene) where T : Component
    {
        List<T> found = All<T>(scene);
        if (found.Count != 1) throw new InvalidOperationException(scene.path + " needs exactly one " + typeof(T).Name + ", found " + found.Count);
        return found[0];
    }

    private static void MapHierarchy(GameObject source, GameObject clone)
    {
        copiedObjects[source] = clone;
        Component[] originals = source.GetComponents<Component>();
        Component[] copies = clone.GetComponents<Component>();
        for (int index = 0; index < originals.Length; index++)
            if (originals[index] != null && copies[index] != null) copiedObjects[originals[index]] = copies[index];
        for (int index = 0; index < source.transform.childCount; index++)
            MapHierarchy(source.transform.GetChild(index).gameObject, clone.transform.GetChild(index).gameObject);
    }

    private static void Remap(GameObject root)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            SerializedObject data = new SerializedObject(component);
            SerializedProperty property = data.GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null
                    && copiedObjects.TryGetValue(property.objectReferenceValue, out Object replacement)) property.objectReferenceValue = replacement;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Dictionary<string, int> Inventory(Scene scene)
    {
        string[] names = { "Collectible", "InteractableButton", "ButtonPressurePlate", "MouseTrap", "ElectricTrap", "PiranhaBite", "Rope", "RopeAttachment", "Pushable", "BalloonLift", "DroppingPlatform", "WaterVolume", "DiggableTerrain", "Turbine" };
        Dictionary<string, int> result = new Dictionary<string, int>();
        foreach (string name in names) result[name] = 0;
        foreach (MonoBehaviour behaviour in All<MonoBehaviour>(scene))
            if (behaviour != null && result.ContainsKey(behaviour.GetType().Name)) result[behaviour.GetType().Name]++;
        return result;
    }

    private static void AuditSceneReferences()
    {
        foreach (MonoBehaviour behaviour in All<MonoBehaviour>(destination))
        {
            if (behaviour == null) throw new InvalidOperationException("Missing script in the composed scene.");
            SerializedObject data = new SerializedObject(behaviour);
            SerializedProperty property = data.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                Object reference = property.objectReferenceValue;
                GameObject go = reference as GameObject;
                if (reference is Component component) go = component.gameObject;
                if (go != null && go.scene.IsValid() && go.scene != destination)
                    throw new InvalidOperationException(behaviour.name + "." + property.propertyPath + " still points to another scene: " + go.scene.path);
            }
        }
    }

    private static void CreateCampaign(GameManager manager)
    {
        SerializedObject settings = new SerializedObject(manager);
        UniverseCampaign original = settings.FindProperty("campaign").objectReferenceValue as UniverseCampaign;
        if (original == null) return;
        if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), CampaignPath)) throw new IOException("Could not create the composed scene's campaign copy.");
        UniverseCampaign copy = AssetDatabase.LoadAssetAtPath<UniverseCampaign>(CampaignPath);
        SerializedObject campaign = new SerializedObject(copy);
        SerializedProperty entries = campaign.FindProperty("universes");
        for (int index = 0; index < entries.arraySize; index++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            if (entry.FindPropertyRelative("number").intValue == 10) entry.FindPropertyRelative("sceneName").stringValue = Path.GetFileNameWithoutExtension(OutputPath);
        }
        campaign.ApplyModifiedPropertiesWithoutUndo();
        settings.FindProperty("campaign").objectReferenceValue = copy;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RegisterBuildScene()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.Add(new EditorBuildSettingsScene(OutputPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
