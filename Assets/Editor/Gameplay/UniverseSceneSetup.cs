using System;
using System.Collections.Generic;
using System.IO;
using GameJamPlus.Gameplay;
using GameJamPlus.UI;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class UniverseSceneSetup
{
    private const string Request = "Library/CreateUniverseScenes.request";
    private const string Report = "Library/UniverseScenes-result.txt";
    private const string Folder = "Assets/Scenes/Universes";
    private const string RigPath = "Assets/Prefabs/GameLoop/Universe Rig.prefab";
    private const string CampaignPath = "Assets/Settings/UniverseCampaign.asset";
    private const string FloorPath = "Assets/Art/Generated/UniverseFloor.png";

    static UniverseSceneSetup() => EditorApplication.update += ProcessRequest;

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { CreateMissingScenes(); }
        catch (Exception exception)
        {
            File.WriteAllText(Report, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Gameplay/Create Missing Universe Scenes")]
    public static void CreateMissingScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureFolder(Folder);
        UniverseCampaign campaign = AssetDatabase.LoadAssetAtPath<UniverseCampaign>(CampaignPath);
        if (campaign == null) throw new InvalidOperationException("Missing campaign: " + CampaignPath);
        Sprite floorSprite = CreateFloorSprite();
        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
        if (rigPrefab == null) rigPrefab = CreateRig(campaign);
        List<EditorBuildSettingsScene> build = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        List<string> created = new List<string>();
        Scene previous = SceneManager.GetActiveScene();
        try
        {
            for (int universe = 10; universe >= 1; universe--)
            {
                string path = Folder + "/Universo_" + universe + ".unity";
                if (!File.Exists(path))
                {
                    Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    try
                    {
                        SceneManager.SetActiveScene(scene);
                        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
                        rig.name = "Universe Rig";
                        GameManager manager = rig.GetComponentInChildren<GameManager>(true);
                        SerializedObject settings = new SerializedObject(manager);
                        settings.FindProperty("startingUniverse").intValue = universe;
                        settings.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
                        CreateEnvironment(floorSprite);
                        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save " + path);
                        created.Add(path);
                    }
                    finally
                    {
                        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
                EditorBuildSettingsScene existing = build.Find(entry => entry.path == path);
                if (existing != null) existing.enabled = true;
                else build.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = build.ToArray();
            File.WriteAllText(Report, "SUCCESS\nCreated: " + created.Count + "\n" + string.Join("\n", created)
                + "\nExisting scenes and the open scene were preserved.\nRig: " + RigPath);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static GameObject CreateRig(UniverseCampaign campaign)
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            GameObject rig = new GameObject("Universe Rig");
            GameObject player = Instantiate("Assets/Prefabs/Characters/Player.prefab", rig.transform, scene);
            player.name = "Player";
            player.transform.localPosition = Vector3.up * 0.3f;
            GameObject hudObject = Instantiate("Assets/Prefabs/UI/Gameplay HUD.prefab", rig.transform, scene);
            GameplayHudController hud = hudObject.GetComponent<GameplayHudController>();
            PlayerInputReader input = player.GetComponent<PlayerInputReader>();
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1f, -10f);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.625f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.13f, 0.18f);
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            PixelPerfectCamera pixelCamera = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelCamera.assetsPPU = 32;
            pixelCamera.refResolutionX = 640;
            pixelCamera.refResolutionY = 360;
            pixelCamera.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            PixelPerfectViewport viewport = cameraObject.AddComponent<PixelPerfectViewport>();
            SetReferences(viewport, ("pixelPerfectCamera", pixelCamera));
            PlatformCameraController follow = cameraObject.AddComponent<PlatformCameraController>();
            SetReferences(follow, ("target", player.transform), ("movement", movement), ("input", input));
            PixelPerfectZoom zoom = cameraObject.AddComponent<PixelPerfectZoom>();
            SetReferences(zoom, ("viewport", viewport));
            GameplayInteractionAvailability availability = hudObject.GetComponent<GameplayInteractionAvailability>();
            SetReferences(availability, ("input", input), ("collector", player.GetComponent<CollectibleCollector>()),
                ("ropeBite", player.GetComponent<PlayerRopeBite>()), ("digging", player.GetComponent<PlayerDigging>()),
                ("movement", movement), ("crates", player.GetComponent<PlayerCrateInteraction>()),
                ("buttons", player.GetComponent<PlayerButtonInteraction>()), ("wheel", player.GetComponent<PlayerWheelInteraction>()));
            SetReferences(hud, ("playerInput", input), ("collectibleCollector", player.GetComponent<CollectibleCollector>()),
                ("interactionAvailability", availability), ("viewport", viewport), ("cameraController", follow));
            SetReferences(hudObject.GetComponent<SatietyHud>(), ("satiety", player.GetComponent<PlayerSatiety>()),
                ("oxygen", player.GetComponent<PlayerOxygen>()), ("electricity", player.GetComponent<PlayerElectricity>()),
                ("health", player.GetComponent<PlayerHealth>()), ("oxygenBubbles", player.GetComponent<PlayerOxygenBubbles>()),
                ("gameplayCamera", follow));
            SetReferences(hudObject.GetComponent<RopeBiteProgressView>(), ("gameplayCamera", camera), ("bite", player.GetComponent<PlayerRopeBite>()));
            GameObject managerObject = new GameObject("Game Manager", typeof(GameManager));
            managerObject.transform.SetParent(rig.transform, false);
            GameManager manager = managerObject.GetComponent<GameManager>();
            SetReferences(manager, ("hud", hud), ("player", player.transform), ("playerCollider", player.GetComponent<BoxCollider2D>()),
                ("playerRecovery", player.GetComponent<PlayerRecovery>()), ("playerCrushVisual", player.GetComponent<PlayerCrushVisual>()),
                ("playerHealth", player.GetComponent<PlayerHealth>()), ("gameplayCamera", follow), ("campaign", campaign));
            CreateHandIntro(rig.transform, manager);
            GameObject spawnGroup = new GameObject("Spawn Points");
            spawnGroup.transform.SetParent(rig.transform, false);
            SerializedObject managerSettings = new SerializedObject(manager);
            SerializedProperty points = managerSettings.FindProperty("spawnPoints");
            points.arraySize = 5;
            for (int index = 0; index < 5; index++)
            {
                GameObject point = Instantiate("Assets/Prefabs/GameLoop/Spawn Point.prefab", spawnGroup.transform, scene);
                point.name = "Spawn " + (index + 1);
                point.transform.localPosition = new Vector3((index - 2) * 4f, 0.3f, 0f);
                points.GetArrayElementAtIndex(index).objectReferenceValue = point.GetComponent<SpawnPoint>();
            }
            managerSettings.ApplyModifiedPropertiesWithoutUndo();
            GameObject lighting = new GameObject("Global Light", typeof(Light2D));
            lighting.transform.SetParent(rig.transform, false);
            Light2D light = lighting.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            GameObject musicObject = new GameObject("Gameplay Music", typeof(AudioSource), typeof(BackgroundMusic));
            musicObject.transform.SetParent(rig.transform, false);
            musicObject.GetComponent<AudioSource>().volume = 0.5f;
            SetReferences(musicObject.GetComponent<BackgroundMusic>(), ("source", musicObject.GetComponent<AudioSource>()),
                ("clip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Music/Music_Gameplay.wav")));
            return PrefabUtility.SaveAsPrefabAsset(rig, RigPath);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void CreateHandIntro(Transform parent, GameManager manager)
    {
        GameObject hand = new GameObject("HandRelease", typeof(SpriteRenderer), typeof(Animator), typeof(HandReleaseIntro));
        hand.transform.SetParent(parent, false);
        hand.transform.localPosition = new Vector3(0.2f, 1.05f, 0f);
        SpriteRenderer visual = hand.GetComponent<SpriteRenderer>();
        visual.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Pixel Art/Characters/Scientist/HandRelease/handRelease1.png");
        Animator animator = hand.GetComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/HandRelease/HandRelease.controller");
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        HandReleaseIntro intro = hand.GetComponent<HandReleaseIntro>();
        SetReferences(intro, ("animator", animator), ("visual", visual),
            ("releaseAnimation", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/HandRelease/HandRelease_Release.anim")));
        SerializedObject settings = new SerializedObject(intro);
        settings.FindProperty("spawnOffset").vector2Value = new Vector2(0.2f, 0.75f);
        settings.ApplyModifiedPropertiesWithoutUndo();
        SetReferences(manager, ("handReleaseIntro", intro));
    }

    private static void CreateEnvironment(Sprite floorSprite)
    {
        GameObject level = new GameObject("Level");
        GameObject floor = new GameObject("Authoring Floor - Replace", typeof(SpriteRenderer), typeof(BoxCollider2D));
        floor.transform.SetParent(level.transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        SpriteRenderer renderer = floor.GetComponent<SpriteRenderer>();
        renderer.sprite = floorSprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(24f, 1f);
        renderer.color = new Color(0.2f, 0.3f, 0.35f);
        floor.GetComponent<BoxCollider2D>().size = new Vector2(24f, 1f);
        GameObject puzzle = new GameObject("Puzzle");
        puzzle.transform.SetParent(level.transform, false);
    }

    private static Sprite CreateFloorSprite()
    {
        if (!File.Exists(FloorPath))
        {
            EnsureFolder("Assets/Art/Generated");
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            File.WriteAllBytes(FloorPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(FloorPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(FloorPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            TextureImporterSettings textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(FloorPath);
    }

    private static GameObject Instantiate(string path, Transform parent, Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new InvalidOperationException("Missing prefab: " + path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static void SetReferences(Object target, params (string name, Object value)[] references)
    {
        if (target == null) throw new InvalidOperationException("Missing component while wiring universe rig.");
        SerializedObject settings = new SerializedObject(target);
        foreach ((string name, Object value) reference in references)
        {
            SerializedProperty property = settings.FindProperty(reference.name);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + reference.name);
            property.objectReferenceValue = reference.value;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void EnsureFolder(string path)
    {
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(path)) return;
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
