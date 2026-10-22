using System.IO;
using GameJamPlus.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class HandReleaseSetup
{
    private const string Request = "Temp/HandReleaseSetup.request";
    private const string Result = "Temp/HandReleaseSetup-result.txt";
    private const string PersistRequest = "Temp/PersistHandReleaseSetup.request";
    private const string WelcomePath = "Assets/Scenes/Levels/1_WelcomeToUniverse.unity";
    private const string ArtFolder = "Assets/Art/Pixel Art/Characters/Scientist/HandRelease";
    private const string AnimationFolder = "Assets/Animations/HandRelease";

    static HandReleaseSetup()
    {
        EditorApplication.delayCall += ProcessRequest;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += ProcessRequest;
    }

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ProcessRequest;
            return;
        }
        if (Configure(File.Exists(PersistRequest)))
        {
            File.Delete(Request);
            if (File.Exists(PersistRequest)) File.Delete(PersistRequest);
        }
    }

    [MenuItem("Tools/Gameplay/Configure Hand Release Intro")]
    private static void ConfigureFromMenu() => Configure();

    private static bool Configure(bool persist = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name.IndexOf("Welcome", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            File.WriteAllText(Result, "Make the Welcome scene containing HandRelease active outside Play Mode, then use Tools/Gameplay/Configure Hand Release Intro. No scene was opened or changed.");
            return false;
        }
        SpriteRenderer hand = null;
        GameManager manager = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SpriteRenderer candidate in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (candidate.name == "HandRelease" || candidate.name == "Hand release") hand = candidate;
            foreach (GameManager candidate in root.GetComponentsInChildren<GameManager>(true)) manager = candidate;
        }
        if (manager == null)
        {
            File.WriteAllText(Result, "The active Welcome scene must contain a configured Game Manager: " + scene.path);
            return false;
        }
        SerializedObject initialManagerSettings = new SerializedObject(manager);
        Transform configuredPlayer = initialManagerSettings.FindProperty("player").objectReferenceValue as Transform;
        if (configuredPlayer == null)
        {
            File.WriteAllText(Result, "Assign the Player on the active Welcome Game Manager before configuring the hand.");
            return false;
        }

        EnsureFolder(ArtFolder);
        EnsureFolder(AnimationFolder);
        Sprite[] frames = new Sprite[6];
        for (int index = 0; index < frames.Length; index++)
        {
            string path = ArtFolder + "/handRelease" + (index + 1) + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
            {
                string error = AssetDatabase.MoveAsset("Assets/mano sinistra" + (index + 1) + ".png", path);
                if (!string.IsNullOrEmpty(error))
                {
                    File.WriteAllText(Result, error);
                    return false;
                }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            frames[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (frames[index] == null)
            {
                File.WriteAllText(Result, "Missing single Sprite at " + path);
                return false;
            }
        }
        AnimationClip idle = CreateIdle(frames);
        AnimationClip release = CreateReleasePlaceholder(frames[5]);
        string controllerPath = AnimationFolder + "/HandRelease.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle");
            idleState.motion = idle;
            machine.defaultState = idleState;
            AnimatorState releaseState = machine.AddState("Release");
            releaseState.motion = release;
            EditorUtility.SetDirty(controller);
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure hand release intro");
        if (hand == null)
        {
            GameObject handObject = new GameObject("HandRelease");
            Undo.RegisterCreatedObjectUndo(handObject, "Create hand release");
            SceneManager.MoveGameObjectToScene(handObject, scene);
            Undo.SetTransformParent(handObject.transform, configuredPlayer.parent, "Parent hand release");
            handObject.transform.position = configuredPlayer.position + Vector3.up;
            Undo.SetTransformParent(configuredPlayer, handObject.transform, "Place player under hand");
            hand = Undo.AddComponent<SpriteRenderer>(handObject);
            SpriteRenderer playerVisual = configuredPlayer.GetComponentInChildren<SpriteRenderer>(true);
            if (playerVisual != null) hand.sharedMaterial = playerVisual.sharedMaterial;
        }
        Animator animator = hand.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(hand.gameObject);
        Undo.RecordObject(animator, "Configure hand animator");
        animator.runtimeAnimatorController = controller;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Undo.RecordObject(hand, "Configure hand sprite");
        hand.sprite = frames[0];
        HandReleaseIntro intro = hand.GetComponent<HandReleaseIntro>();
        if (intro == null) intro = Undo.AddComponent<HandReleaseIntro>(hand.gameObject);
        Undo.RecordObject(intro, "Configure hand release");
        SerializedObject settings = new SerializedObject(intro);
        settings.FindProperty("animator").objectReferenceValue = animator;
        settings.FindProperty("visual").objectReferenceValue = hand;
        if (settings.FindProperty("releaseAnimation").objectReferenceValue == null)
            settings.FindProperty("releaseAnimation").objectReferenceValue = release;
        settings.ApplyModifiedProperties();
        Undo.RecordObject(manager, "Assign spawn intro");
        SerializedObject managerSettings = new SerializedObject(manager);
        managerSettings.FindProperty("handReleaseIntro").objectReferenceValue = intro;
        Transform player = managerSettings.FindProperty("player").objectReferenceValue as Transform;
        managerSettings.ApplyModifiedProperties();
        if (player != null)
        {
            SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>(true);
            if (playerSprite != null)
            {
                hand.sortingLayerID = playerSprite.sortingLayerID;
                hand.sortingOrder = playerSprite.sortingOrder + 1;
            }
        }
        foreach (Object changed in new Object[] { hand, animator, intro, manager })
        {
            EditorUtility.SetDirty(changed);
            if (PrefabUtility.IsPartOfPrefabInstance(changed)) PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
        }
        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        SerializedObject verification = new SerializedObject(manager);
        if (verification.FindProperty("handReleaseIntro").objectReferenceValue != intro)
        {
            File.WriteAllText(Result, "Failed to assign Hand Release Intro on the loaded Game Manager. Scene not saved.");
            return false;
        }
        string persistence = "Scene marked dirty, not saved or reloaded.";
        if (persist)
        {
            Scene destination = SceneManager.GetSceneByPath(WelcomePath);
            if (destination.IsValid() && destination.isLoaded && destination != scene)
            {
                File.WriteAllText(Result, "The canonical Welcome is also loaded. Keep only the intended Welcome scene loaded before saving the repair. Current changes remain open and unsaved.");
                return false;
            }
            string backupFolder = "Recovery/HandRelease_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            Directory.CreateDirectory(backupFolder);
            if (File.Exists(WelcomePath)) File.Copy(WelcomePath, Path.Combine(backupFolder, Path.GetFileName(WelcomePath)), false);
            if (scene.path != WelcomePath && File.Exists(scene.path))
                File.Copy(scene.path, Path.Combine(backupFolder, Path.GetFileName(scene.path)), false);
            if (!EditorSceneManager.SaveScene(scene, WelcomePath))
            {
                File.WriteAllText(Result, "Reference verified in the loaded scene but saving failed. Scene remains open and dirty.");
                return false;
            }
            persistence = "Loaded scene saved through Unity without reloading. Previous file copied to " + backupFolder + ".";
        }
        File.WriteAllText(Result, System.DateTime.Now.ToString("s") + " - Verified Game Manager Hand Release Intro = "
            + intro.name + ", Animator = " + animator.runtimeAnimatorController.name + ". " + persistence);
        return true;
    }

    private static AnimationClip CreateIdle(Sprite[] frames)
    {
        string path = AnimationFolder + "/HandRelease_Idle.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = "HandRelease_Idle" };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = 8f;
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int index = 0; index < keys.Length; index++)
            keys[index] = new ObjectReferenceKeyframe { time = index / clip.frameRate, value = frames[index % frames.Length] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip CreateReleasePlaceholder(Sprite frame)
    {
        string path = AnimationFolder + "/HandRelease_Release.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null) return clip;
        string spritePath = AnimationFolder + "/HandRelease_HandOnly.asset";
        Sprite hand = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (hand == null)
        {
            // Keep the hand in exactly the same place; exclude the drawn tail and mouse below row 58.
            Rect original = frame.rect;
            const float bottom = 38f;
            Rect rect = new Rect(original.x, original.y + bottom, original.width, original.height - bottom);
            Vector2 pivot = new Vector2(frame.pivot.x / rect.width, (frame.pivot.y - bottom) / rect.height);
            hand = Sprite.Create(frame.texture, rect, pivot, frame.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            hand.name = "HandRelease_HandOnly";
            AssetDatabase.CreateAsset(hand, spritePath);
        }
        clip = new AnimationClip { name = "HandRelease_Release", frameRate = 12f };
        AssetDatabase.CreateAsset(clip, path);
        ObjectReferenceKeyframe[] keys =
        {
            new ObjectReferenceKeyframe { time = 0f, value = hand },
            new ObjectReferenceKeyframe { time = 1f / 12f, value = hand }
        };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), keys);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
