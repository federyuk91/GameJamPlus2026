using System.Collections.Generic;
using System.IO;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BalloonAscentSceneSetup
{
    private const string ScenePath = "Assets/Scenes/BalloonAscent.unity";
    private const string Request = "Temp/CreateBalloonAscent.request";
    private const string Report = "Temp/BalloonAscentSetup-result.txt";
    static BalloonAscentSceneSetup() => EditorApplication.delayCall += ProcessRequest;

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        CreateScene();
    }

    [MenuItem("Tools/Gameplay/Create Balloon Ascent Scene")]
    public static void CreateScene()
    {
        if (File.Exists(ScenePath))
        {
            File.WriteAllText(Report, "Scene already exists; preserved: " + ScenePath);
            return;
        }
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            Sprite pixel = CreatePixelSprite();
            Material lit = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));
            Material unlit = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("9dfc825aed78fcd4ba02077103263b40"));
            if (lit == null) lit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            if (unlit == null) unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2.8125f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 1.3f, -10f);
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            PixelPerfectCamera pixels = cameraObject.AddComponent<PixelPerfectCamera>();
            pixels.assetsPPU = 32;
            pixels.refResolutionX = 320;
            pixels.refResolutionY = 180;
            pixels.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;

            GameObject tunnel = new GameObject("Vertical Tunnel");
            const float exitHeight = 40f;
            for (int side = -1; side <= 1; side += 2)
            {
                Bar("Shaft Wall", tunnel.transform, new Vector2(side * 3.25f, 19f), new Vector2(1.5f, 46f),
                    new Color(0.15f, 0.18f, 0.21f), pixel, lit, -30);
                Bar("Inner Rim", tunnel.transform, new Vector2(side * 2.51f, 19f), new Vector2(0.0625f, 46f),
                    new Color(0.22f, 0.26f, 0.29f), pixel, lit, -29);
                for (int level = -2; level < 28; level++)
                    Bar("Wall Joint " + level, tunnel.transform, new Vector2(side * 3.25f, level * 1.5f), new Vector2(1.5f, 0.0625f),
                        new Color(0.06f, 0.07f, 0.085f), pixel, lit, -28);
            }
            GameObject exit = new GameObject("Tunnel Exit");
            exit.transform.SetParent(tunnel.transform, false);
            exit.transform.position = new Vector3(0f, exitHeight, 0f);
            Bar("Opening Glow", tunnel.transform, new Vector2(0f, exitHeight + 0.15f), new Vector2(5f, 0.3f),
                new Color(1f, 0.98f, 0.9f), pixel, unlit, -15);
            GameObject shafts = new GameObject("Light From Above");
            shafts.transform.SetParent(tunnel.transform, false);
            for (int band = 0; band < 40; band++)
            {
                float progress = (band + 0.5f) / 40f;
                Bar("Shaft Haze " + band, shafts.transform, new Vector2(0f, band + 0.5f),
                    new Vector2(Mathf.Lerp(4.9f, 0.8f, progress), 1f),
                    new Color(1f, 0.96f, 0.83f, 0.008f + Mathf.Pow(progress, 3f) * 0.08f), pixel, unlit, -20);
            }
            GameObject ambientObject = new GameObject("Dim Ambient Light");
            Light2D ambient = ambientObject.AddComponent<Light2D>();
            ambient.lightType = Light2D.LightType.Global;
            ambient.color = new Color(0.65f, 0.72f, 0.86f);
            ambient.intensity = 0.3f;
            GameObject topLightObject = new GameObject("Tunnel Opening Light");
            topLightObject.transform.position = new Vector3(0f, exitHeight + 0.5f, 0f);
            topLightObject.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            Light2D topLight = topLightObject.AddComponent<Light2D>();
            topLight.lightType = Light2D.LightType.Point;
            topLight.color = new Color(1f, 0.96f, 0.85f);
            topLight.intensity = 2.2f;
            topLight.pointLightOuterRadius = 45f;
            topLight.pointLightInnerRadius = 0.5f;
            topLight.pointLightOuterAngle = 22f;
            topLight.pointLightInnerAngle = 12f;
            topLight.falloffIntensity = 0.6f;

            GameObject rig = new GameObject("Balloon And Mouse");
            GameObject balloon = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/Balloon.prefab"), scene);
            balloon.transform.SetParent(rig.transform, false);
            balloon.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            GameObject mouse = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"), scene);
            mouse.name = "Mouse Hanging From Balloon";
            mouse.transform.SetParent(rig.transform, false);
            mouse.transform.localPosition = new Vector3(0f, 0.175f, 0f);
            Animator mouseAnimator = mouse.GetComponentInChildren<Animator>(true);
            foreach (Animator animator in balloon.GetComponentsInChildren<Animator>(true)) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            GameObject leadingPoint = new GameObject("Balloon Top");
            leadingPoint.transform.SetParent(rig.transform, false);
            leadingPoint.transform.localPosition = new Vector3(0f, 3.5f, 0f);

            PlayableBalloonSceneSetup.ConfigureScene(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save balloon ascent scene.");
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!buildScenes.Exists(entry => entry.path == ScenePath)) buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            File.WriteAllText(Report, "Created " + ScenePath + ". Mouse and balloon assigned, real player initially grabbed to the physical balloon rope, black shaft, overhead light, existing camera controller and height-based whiteout. Previous scene preserved. Added to build settings.");
        }
        catch (System.Exception exception)
        {
            File.WriteAllText(Report, exception.ToString());
            Debug.LogException(exception);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static Sprite CreatePixelSprite()
    {
        const string directory = "Assets/Art/Cinematics";
        if (!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder("Assets/Art", "Cinematics");
        const string path = directory + "/TunnelPixel.asset";
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite existing) return existing;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "TunnelPixel", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
        sprite.name = "TunnelPixel";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssetIfDirty(texture);
        return sprite;
    }

    private static void Bar(string name, Transform parent, Vector2 position, Vector2 size, Color color, Sprite sprite, Material material, int order)
    {
        GameObject bar = new GameObject(name, typeof(SpriteRenderer));
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = position;
        bar.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer visual = bar.GetComponent<SpriteRenderer>();
        visual.sprite = sprite;
        visual.sharedMaterial = material;
        visual.color = color;
        visual.sortingOrder = order;
    }
}
