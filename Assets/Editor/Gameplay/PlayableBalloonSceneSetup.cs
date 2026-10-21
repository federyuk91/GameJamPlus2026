using System.IO;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayableBalloonSceneSetup
{
    private const string Path = "Assets/Scenes/BalloonAscent.unity";
    private const string Request = "Temp/ConfigurePlayableBalloon.request";
    static PlayableBalloonSceneSetup() => EditorApplication.delayCall += ProcessRequest;

    private static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        ConfigureSavedScene();
    }

    [MenuItem("Tools/Gameplay/Use Real Player In Balloon Ascent")]
    private static void ConfigureSavedScene()
    {
        Scene active = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(Path);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;
        if (!wasLoaded) scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
        try
        {
            ConfigureScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasLoaded) EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Temp/PlayableBalloonSceneSetup-result.txt", "Real player and balloon restored. Player Rope Interaction.Initial Rope assigned at fraction 1; Balloon Lift capacity 3; existing PlatformCameraController follows player; height-based flash. Dedicated sequence removed. " + (wasLoaded ? "Open scene marked dirty, not saved." : "Scene saved."));
        }
        finally
        {
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        }
    }

    public static void ConfigureScene(Scene scene)
    {
        PlayerMovement player = null;
        BalloonLift balloon = null;
        Camera camera = null;
        Sprite pixel = null;
        Transform exit = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PlayerMovement candidate in root.GetComponentsInChildren<PlayerMovement>(true)) player = candidate;
            foreach (BalloonLift candidate in root.GetComponentsInChildren<BalloonLift>(true)) balloon = candidate;
            foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true)) camera = candidate;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == "Tunnel Exit") exit = child;
            foreach (SpriteRenderer candidate in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (candidate.name != "Shaft Wall") continue;
                pixel = candidate.sprite;
                if (candidate.GetComponent<BoxCollider2D>() == null) Undo.AddComponent<BoxCollider2D>(candidate.gameObject);
            }
        }
        if (player == null || balloon == null || camera == null || pixel == null || exit == null)
            throw new System.InvalidOperationException("Balloon ascent scene is missing its player, balloon, camera or tunnel references.");

        RestorePrefabComponents(player.gameObject);
        RestorePrefabComponents(balloon.gameObject);
        PlayerSatiety satiety = player.GetComponent<PlayerSatiety>();
        if (satiety != null) satiety.enabled = false;
        PlayerOxygen oxygen = player.GetComponent<PlayerOxygen>();
        if (oxygen != null) oxygen.enabled = false;
        RopeSimulation rope = balloon.GetComponentInChildren<RopeSimulation>(true);
        PlayerRopeInteraction grip = player.GetComponent<PlayerRopeInteraction>();
        Undo.RecordObject(grip, "Set initial balloon grip");
        SerializedObject gripSettings = new SerializedObject(grip);
        gripSettings.FindProperty("initialRope").objectReferenceValue = rope;
        gripSettings.FindProperty("initialGripFraction").floatValue = 1f;
        gripSettings.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(grip);
        SerializedObject lift = new SerializedObject(balloon);
        lift.FindProperty("liftCapacity").floatValue = 3f;
        lift.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(balloon);
        SerializedObject movementSettings = new SerializedObject(player);
        Vector2 handOffset = movementSettings.FindProperty("movementSettings.handOffset").vector2Value;
        Vector3 ropeEnd = rope.transform.position + Vector3.down * rope.Length;
        Undo.RecordObject(player.transform, "Align initial rope grip");
        Vector3 playerPosition = ropeEnd - (Vector3)handOffset;
        playerPosition.z = player.transform.position.z;
        player.transform.position = playerPosition;
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
        foreach (Animator animator in player.GetComponentsInChildren<Animator>(true)) animator.updateMode = AnimatorUpdateMode.Normal;
        foreach (Animator animator in balloon.GetComponentsInChildren<Animator>(true)) animator.updateMode = AnimatorUpdateMode.Normal;

        PlatformCameraController follow = camera.GetComponent<PlatformCameraController>();
        if (follow == null) follow = Undo.AddComponent<PlatformCameraController>(camera.gameObject);
        SerializedObject followSettings = new SerializedObject(follow);
        followSettings.FindProperty("target").objectReferenceValue = player.transform;
        followSettings.FindProperty("movement").objectReferenceValue = player;
        followSettings.FindProperty("input").objectReferenceValue = player.GetComponent<PlayerInputReader>();
        followSettings.FindProperty("offset").vector2Value = new Vector2(0f, 1.3f);
        followSettings.FindProperty("lookAheadDistance").floatValue = 0f;
        followSettings.FindProperty("fallingLookAhead").floatValue = 0f;
        followSettings.ApplyModifiedProperties();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Whiteout" || root.name == "Balloon Ascent Sequence") Undo.DestroyObjectImmediate(root);
        }

        const string materialPath = "Assets/Materials/BalloonExitFlash.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/HeightWhiteout.shader");
            material = new Material(shader) { name = "BalloonExitFlash" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        float endHeight = exit.position.y - 3.5f + 0.175f + 1.3f - 0.5f;
        material.SetFloat("_EndHeight", endHeight);
        material.SetFloat("_StartHeight", endHeight - 3f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        Transform existingFlash = camera.transform.Find("Exit Whiteout");
        GameObject flash = existingFlash != null ? existingFlash.gameObject : new GameObject("Exit Whiteout", typeof(SpriteRenderer));
        flash.transform.SetParent(camera.transform, false);
        flash.transform.localPosition = Vector3.forward;
        flash.transform.localScale = new Vector3(100f, 100f, 1f);
        SpriteRenderer visual = flash.GetComponent<SpriteRenderer>();
        visual.sprite = pixel;
        visual.sharedMaterial = material;
        visual.sortingOrder = 32767;
        EditorUtility.SetDirty(follow);
        Physics2D.SyncTransforms();
    }

    private static void RestorePrefabComponents(GameObject root)
    {
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null) continue;
            MonoBehaviour source = PrefabUtility.GetCorrespondingObjectFromSource(behaviour);
            Undo.RecordObject(behaviour, "Restore player and balloon behaviour");
            behaviour.enabled = source == null || source.enabled;
            PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
        }
        foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>(true))
        {
            Collider2D source = PrefabUtility.GetCorrespondingObjectFromSource(collider);
            Undo.RecordObject(collider, "Restore physics collider");
            collider.enabled = source == null || source.enabled;
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }
        foreach (AudioSource audio in root.GetComponentsInChildren<AudioSource>(true))
        {
            AudioSource source = PrefabUtility.GetCorrespondingObjectFromSource(audio);
            Undo.RecordObject(audio, "Restore audio source");
            audio.enabled = source == null || source.enabled;
            if (source != null) audio.playOnAwake = source.playOnAwake;
            PrefabUtility.RecordPrefabInstancePropertyModifications(audio);
        }
    }
}
