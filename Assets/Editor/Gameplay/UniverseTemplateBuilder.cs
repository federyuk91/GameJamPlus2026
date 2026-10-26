using System;
using System.Collections.Generic;
using System.IO;
using GameJamPlus.Gameplay;
using GameJamPlus.UI;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class UniverseTemplateBuilder
{
    private const string SourcePath = "Assets/Scenes/Universes/Universo_10_Composed.unity";
    private const string PrefabPath = "Assets/Prefabs/GameLoop/Universe Base.prefab";
    private const string CampaignPath = "Assets/Settings/UniverseCampaign.asset";
    private const string RequestPath = "Temp/PrepareUniverseTemplates.request";
    private const string AuditRequestPath = "Temp/AuditUniverseTemplates.request";
    private const string ReportPath = "Temp/UniverseTemplates-result.txt";
    private static readonly List<string> report = new List<string>();

    static UniverseTemplateBuilder() => EditorApplication.update += ProcessRequest;

    private static void ProcessRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(AuditRequestPath))
        {
            File.Delete(AuditRequestPath);
            try { AuditPrepared(); }
            catch (Exception exception) { File.AppendAllText(ReportPath, "\nAUDIT FAILED: " + exception); Debug.LogException(exception); }
        }
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        try { Prepare(); }
        catch (Exception exception)
        {
            report.Add("FAILED: " + exception);
            File.WriteAllLines(ReportPath, report);
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Gameplay/Audit Universe Base")]
    public static void AuditPrepared()
    {
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try { Validate(root); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
        for (int universe = 1; universe <= 10; universe++)
        {
            string text = File.ReadAllText(ScenePath(universe)).Replace("\r\n", "\n");
            if (!text.Contains(guid) || !text.Contains("propertyPath: startingUniverse\n      value: " + universe + "\n"))
                throw new InvalidOperationException("Incorrect universe prefab or number: " + ScenePath(universe));
        }
        File.AppendAllText(ReportPath, "\nUNITY AUDIT SUCCESS: shared prefab loaded; all ten scene instances reference it with their own universe number.\n");
    }

    [MenuItem("Tools/Gameplay/Prepare Universe Templates 1-10")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) throw new IOException("Universe Base already exists. Edit that shared prefab instead of recreating it.");
        report.Clear();
        UniverseCampaign campaign = AssetDatabase.LoadAssetAtPath<UniverseCampaign>(CampaignPath);
        if (campaign == null) throw new InvalidOperationException("Missing campaign.");
        Scene previous = SceneManager.GetActiveScene();
        Scene source = SceneManager.GetSceneByPath(SourcePath);
        bool sourceWasLoaded = source.IsValid() && source.isLoaded;
        Scene work = default;
        string workPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Universes/__UniverseTemplateWorking.unity");
        string archive = CreateArchive();
        try
        {
            BackupScenes(archive);
            if (sourceWasLoaded)
            {
                if (!EditorSceneManager.SaveScene(source, workPath, true)) throw new IOException("Could not snapshot Composed.");
            }
            else if (!AssetDatabase.CopyAsset(SourcePath, workPath)) throw new IOException("Could not copy Composed.");
            work = EditorSceneManager.OpenScene(workPath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(work);
            GameObject[] roots = work.GetRootGameObjects();
            if (roots.Length != 1) throw new InvalidOperationException("Composed must have one level root.");
            GameObject root = roots[0];
            root.name = "Universe Base";
            Clean(root, campaign);
            Validate(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            if (prefab == null) throw new IOException("Could not save Universe Base.");
            Validate(root);
            GameObject loadedPrefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { Validate(loadedPrefab); }
            finally { PrefabUtility.UnloadPrefabContents(loadedPrefab); }
            GameManager manager = One<GameManager>(root);
            List<EditorBuildSettingsScene> build = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int universe = 10; universe >= 1; universe--)
            {
                string path = ScenePath(universe);
                SetUniverse(manager, universe);
                if (!EditorSceneManager.SaveScene(work, path, true)) throw new IOException("Could not save " + path);
                EditorBuildSettingsScene entry = build.Find(candidate => candidate.path == path);
                if (entry == null) build.Add(new EditorBuildSettingsScene(path, true));
                else entry.enabled = true;
                report.Add("SCENE: " + path + " | startingUniverse=" + universe);
            }
            EditorBuildSettings.scenes = build.ToArray();
            report.Add("PREFAB: " + PrefabPath);
            report.Add("ARCHIVE: " + archive);
            report.Add("SUCCESS: 10 scenes; one shared prefab; empty tilemaps; no level-specific objects; no Play Mode executed.");
            File.WriteAllLines(ReportPath, report);
        }
        finally
        {
            if (work.IsValid() && work.isLoaded) EditorSceneManager.CloseScene(work, true);
            if (File.Exists(workPath)) AssetDatabase.DeleteAsset(workPath);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            else if (File.Exists(ScenePath(10))) EditorSceneManager.OpenScene(ScenePath(10), OpenSceneMode.Additive);
        }
    }

    private static string ScenePath(int universe) => "Assets/Scenes/Universes/Universo_" + universe + ".unity";

    private static string CreateArchive()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Archive")) AssetDatabase.CreateFolder("Assets/Scenes", "Archive");
        string folder = "Before Universe Templates";
        int suffix = 2;
        while (AssetDatabase.IsValidFolder("Assets/Scenes/Archive/" + folder)) folder = "Before Universe Templates " + suffix++;
        AssetDatabase.CreateFolder("Assets/Scenes/Archive", folder);
        return "Assets/Scenes/Archive/" + folder;
    }

    private static void BackupScenes(string archive)
    {
        // Snapshot unsaved scene edits too, before replacing any campaign scene.
        for (int universe = 1; universe <= 10; universe++)
        {
            string path = ScenePath(universe);
            string backup = archive + "/Universo_" + universe + "_PreTemplate.unity";
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded)
            {
                if (!EditorSceneManager.SaveScene(loaded, backup, true)) throw new IOException("Could not archive " + path);
            }
            else if (File.Exists(path) && !AssetDatabase.CopyAsset(path, backup)) throw new IOException("Could not archive " + path);
        }
        for (int universe = 1; universe <= 10; universe++)
        {
            Scene loaded = SceneManager.GetSceneByPath(ScenePath(universe));
            if (loaded.IsValid() && loaded.isLoaded) EditorSceneManager.CloseScene(loaded, true);
        }
        report.Add("All previous Universe scenes archived before replacement.");
    }

    private static void Clean(GameObject root, UniverseCampaign campaign)
    {
        ParallaxConfigurator configurator = One<ParallaxConfigurator>(root);
        PlayerMovement player = One<PlayerMovement>(root);
        WaterGeometryBaker baker = One<WaterGeometryBaker>(root);
        foreach (ParallaxConfigurator.DepthPlane plane in configurator.Depths)
        {
            Transform parent = plane.Parent.transform;
            switch (plane.Category)
            {
                case ParallaxConfigurator.Depth.Terrain:
                case ParallaxConfigurator.Depth.CollectiblesAndProps:
                    KeepEmptyTilemaps(parent);
                    break;
                case ParallaxConfigurator.Depth.WaterAndDiggableGround:
                    baker.transform.SetParent(parent, true);
                    ClearChildren(baker.transform);
                    foreach (Transform child in Children(parent))
                        if (child != baker.transform) Object.DestroyImmediate(child.gameObject);
                    break;
                case ParallaxConfigurator.Depth.GameplayDecorations:
                    ClearChildren(parent);
                    break;
                case ParallaxConfigurator.Depth.PlayerAndCharacters:
                    player.transform.SetParent(parent, true);
                    foreach (Transform child in Children(parent))
                        if (child != player.transform) Object.DestroyImmediate(child.gameObject);
                    break;
            }
        }
        baker.SetBake(default, 0, 0, Array.Empty<byte>());
        baker.SetGeneratedBasins(null, Array.Empty<WaterBasin>());
        SerializedObject bakeData = new SerializedObject(baker);
        bakeData.FindProperty("useCustomBounds").boolValue = false;
        bakeData.FindProperty("showWaterPreview").boolValue = false;
        bakeData.ApplyModifiedPropertiesWithoutUndo();

        SpawnPoint[] points = root.GetComponentsInChildren<SpawnPoint>(true);
        if (points.Length == 0) throw new InvalidOperationException("Missing spawn point.");
        SpawnPoint spawn = points[0];
        for (int index = 1; index < points.Length; index++) Object.DestroyImmediate(points[index].gameObject);
        spawn.gameObject.name = "Spawn 1";
        spawn.gameObject.SetActive(true);
        spawn.enabled = true;
        spawn.transform.position = new Vector3(0f, 0.3f, 0f);
        player.transform.position = spawn.transform.position;
        player.gameObject.SetActive(true);
        if (PrefabUtility.IsPartOfPrefabInstance(spawn))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawn.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawn.gameObject);
        }
        if (PrefabUtility.IsPartOfPrefabInstance(player))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.gameObject);
        }

        GameManager manager = One<GameManager>(root);
        SerializedObject managerData = new SerializedObject(manager);
        managerData.FindProperty("campaign").objectReferenceValue = campaign;
        SerializedProperty spawns = managerData.FindProperty("spawnPoints");
        spawns.arraySize = 1;
        spawns.GetArrayElementAtIndex(0).objectReferenceValue = spawn;
        managerData.ApplyModifiedPropertiesWithoutUndo();
        SetUniverse(manager, 10);
        SerializedObject goals = new SerializedObject(One<CollectionGoalController>(root));
        goals.FindProperty("rules").ClearArray();
        goals.ApplyModifiedPropertiesWithoutUndo();

        Camera camera = One<Camera>(root);
        Vector3 cameraPosition = camera.transform.position;
        camera.transform.position = new Vector3(0f, 1f, cameraPosition.z);
        HandReleaseIntro hand = One<HandReleaseIntro>(root);
        SerializedObject handData = new SerializedObject(hand);
        Vector2 offset = handData.FindProperty("spawnOffset").vector2Value;
        hand.transform.position = spawn.transform.position + (Vector3)offset;
        NullDeletedReferences(root);
    }

    private static void SetUniverse(GameManager manager, int universe)
    {
        SerializedObject data = new SerializedObject(manager);
        data.FindProperty("startingUniverse").intValue = universe;
        data.FindProperty("gameplaySceneName").stringValue = "Universo_" + universe;
        data.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(manager)) PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
    }

    private static void KeepEmptyTilemaps(Transform parent)
    {
        Tilemap[] maps = parent.GetComponentsInChildren<Tilemap>(true);
        HashSet<Transform> retained = new HashSet<Transform>();
        foreach (Tilemap map in maps)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(map))
            {
                GameObject owner = PrefabUtility.GetOutermostPrefabInstanceRoot(map);
                PrefabUtility.UnpackPrefabInstance(owner, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            }
            map.transform.SetParent(parent, true);
            map.transform.localPosition = Vector3.zero;
            map.ClearAllTiles();
            map.CompressBounds();
            TilemapCollider2D collider = map.GetComponent<TilemapCollider2D>();
            if (collider != null) collider.ProcessTilemapChanges();
            retained.Add(map.transform);
        }
        foreach (Transform child in Children(parent))
            if (!retained.Contains(child)) Object.DestroyImmediate(child.gameObject);
    }

    private static void NullDeletedReferences(GameObject root)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null) throw new InvalidOperationException("Missing script in template.");
            SerializedObject data = new SerializedObject(component);
            SerializedProperty property = data.GetIterator();
            bool changed = false;
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0) continue;
                property.objectReferenceValue = null;
                changed = true;
            }
            if (changed)
            {
                data.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
    }

    private static void Validate(GameObject root)
    {
        GameManager manager = One<GameManager>(root);
        SerializedObject managerData = new SerializedObject(manager);
        string[] required = { "hud", "dialogues", "stageEnds", "collectionGoals", "player", "playerCollider", "playerStats", "gameplayCamera", "handReleaseIntro", "campaign" };
        foreach (string field in required)
            if (managerData.FindProperty(field).objectReferenceValue == null) throw new InvalidOperationException("Missing GameManager reference: " + field);
        One<PlayerMovement>(root);
        One<GameplayHudController>(root);
        One<Camera>(root);
        One<DialogueManager>(root);
        One<StageEndManager>(root);
        One<SpawnPoint>(root);
        ParallaxConfigurator configurator = One<ParallaxConfigurator>(root);
        if (configurator.Depths.Count != 12) throw new InvalidOperationException("Expected 12 depth planes.");
        if (configurator.DeskSurface == null) throw new InvalidOperationException("Missing desk surface.");
        foreach (ParallaxConfigurator.DepthPlane plane in configurator.Depths)
            if (plane.Parent == null) throw new InvalidOperationException("Missing plane: " + plane.Category);
        foreach (Tilemap map in One<WaterGeometryBaker>(root).SourceTilemaps)
            if (map == null) throw new InvalidOperationException("Missing water source tilemap.");
        foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
            if (map.GetUsedTilesCount() != 0) throw new InvalidOperationException("Painted tilemap: " + map.name);
        string[] forbidden = { "Collectible", "InteractableButton", "ButtonPressurePlate", "MouseTrap", "ElectricTrap", "PiranhaBite", "RopeAttachment", "Pushable", "BalloonLift", "DroppingPlatform", "WaterVolume", "WaterBasin", "DiggableTerrain", "Turbine", "Goal" };
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || Array.IndexOf(forbidden, behaviour.GetType().Name) >= 0) throw new InvalidOperationException("Unexpected level component: " + behaviour);
        }
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            SerializedObject data = new SerializedObject(component);
            SerializedProperty property = data.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                Object reference = property.objectReferenceValue;
                GameObject target = reference as GameObject;
                if (reference is Component referencedComponent) target = referencedComponent.gameObject;
                if (target != null && target.scene.IsValid() && !target.transform.IsChildOf(root.transform))
                    throw new InvalidOperationException(component.name + "." + property.propertyPath + " points outside the template.");
            }
        }
        report.Add("Validated template: 12 planes, " + root.GetComponentsInChildren<Tilemap>(true).Length + " empty tilemaps; unique systems; internal references.");
    }

    private static T One<T>(GameObject root) where T : Component
    {
        T[] items = root.GetComponentsInChildren<T>(true);
        if (items.Length != 1) throw new InvalidOperationException("Expected one " + typeof(T).Name + ", found " + items.Length);
        return items[0];
    }

    private static List<Transform> Children(Transform root)
    {
        List<Transform> result = new List<Transform>();
        foreach (Transform child in root) result.Add(child);
        return result;
    }

    private static void ClearChildren(Transform root)
    {
        foreach (Transform child in Children(root)) Object.DestroyImmediate(child.gameObject);
    }
}
