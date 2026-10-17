using System;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class PlatformerValidation
{
    public static void Run()
    {
        try
        {
            RopeValidation.RunChainChecks();
            SwimmingValidation.RunMotionChecks();
            LadderValidation.RunMotionChecks();
            JumpValidation.RunChecks();
            LedgeClimbValidation.RunChecks();
            EditorSceneManager.OpenScene("Assets/Scenes/LoopScene.unity");
            var player = GameObject.Find("Player");
            Require(player != null, "Player missing");
            Require(player.GetComponent<Rigidbody2D>() == null, "Player must not have Rigidbody2D");
            Require(player.GetComponent<PlayerInputReader>() != null && player.GetComponent<PlayerMovement>() != null
                && player.GetComponent<MouseFacing>() != null, "Composition missing");
            var pixel = Camera.main.GetComponent<PixelPerfectCamera>();
            Require(pixel != null && pixel.assetsPPU == 32 && pixel.refResolutionX == 640
                && pixel.refResolutionY == 320, "Pixel camera settings");
            var sprite = player.GetComponent<SpriteRenderer>().sprite;
            Require(sprite.rect.size == new Vector2(40, 40) && sprite.pixelsPerUnit == 32, "Sprite scale");
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0, "Missing script: " + go.name);
            var body = player.GetComponent<SimulatedBody2D>();
            body.SendMessage("Awake");
            // Fall onto the actual room floor, then walk without catching on the floor.
            for (int i = 0; i < 100; i++) body.Move(new Vector2(0, -0.2f));
            Require(body.Grounded, "Floor grounding");
            float floorY = player.transform.position.y;
            for (int i = 0; i < 10; i++) body.Move(new Vector2(0.1f, -0.01f));
            Require(player.transform.position.x > 0.9f && Mathf.Abs(player.transform.position.y - floorY) < 0.01f,
                "Horizontal movement along floor");
            body.Move(new Vector2(-100, 0));
            Require(body.HitWall && player.transform.position.x > -5f, "Swept wall collision");
            player.transform.position = Vector3.zero;
            body.Move(new Vector2(0, 100));
            float ceilingBottom = GameObject.Find("Square (2)").GetComponent<BoxCollider2D>().bounds.min.y;
            Require(body.HitCeiling && body.Shape.bounds.max.y <= ceilingBottom, "Swept ceiling collision");
            body.Move(new Vector2(0, -100));
            Require(body.Grounded && player.transform.position.y > -5f, "Swept floor collision");
            ValidateCrates();
            ValidateSwimmingVolumes();
            ValidateLadder();
            Debug.Log("PLATFORMER_VALIDATION_PASSED: composition, sprite, camera, grounding, floor traversal, wall/ceiling/floor sweeps.");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void ValidateCrates()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var floor = new GameObject("Test floor");
        floor.transform.position = new Vector3(0, -1, 0);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(30, 1);
        var actor = new GameObject("Test player");
        actor.transform.position = new Vector3(0, 0.005f, 0);
        var actorBody = actor.AddComponent<SimulatedBody2D>();
        var interaction = actor.AddComponent<PlayerCrateInteraction>();
        var box = new GameObject("Test crate");
        box.transform.position = new Vector3(1.01f, 0.005f, 0);
        var crate = box.AddComponent<PushableCrate>();
        crate.Body.Move(Vector2.down * 0.01f);
        actorBody.Move(Vector2.down * 0.01f);
        float initialX = box.transform.position.x;
        StepCrate(actorBody, interaction, 0.04f, false);
        Require(Mathf.Abs(box.transform.position.x - initialX) < 0.0001f && interaction.HeldCrate == null,
            "Crate must not push without grab");
        StepCrate(actorBody, interaction, 0.04f, true);
        Require(box.transform.position.x > initialX && interaction.HeldCrate == crate, "Crate must push with grab");
        float pushedX = box.transform.position.x;
        // A pull can temporarily cross the player's old position; the pair must move together.
        StepCrate(actorBody, interaction, -0.04f, true);
        Require(box.transform.position.x < pushedX && interaction.HeldCrate == crate, "Crate must pull with grab");
        Require(box.transform.position.x - actor.transform.position.x >= 1f, "Pull must preserve separation");
        float releasedX = box.transform.position.x;
        StepCrate(actorBody, interaction, -0.04f, false);
        Require(Mathf.Abs(box.transform.position.x - releasedX) < 0.0001f && interaction.HeldCrate == null,
            "Release must stop pulling");
        var wall = new GameObject("Crate wall");
        wall.transform.position = new Vector3(3, 0, 0);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(1, 5);
        for (int i = 0; i < 100; i++) StepCrate(actorBody, interaction, 0.04f, true);
        Require(box.transform.position.x <= 1.996f && box.transform.position.x > 1.98f,
            "Pushed crate must stop at wall");
        Require(box.transform.position.x - actor.transform.position.x >= 1f, "Blocked push must not overlap");
        var behind = new GameObject("Player wall");
        behind.transform.position = actor.transform.position + Vector3.left * 1.005f;
        behind.AddComponent<BoxCollider2D>().size = new Vector2(1, 5);
        float blockedX = box.transform.position.x;
        StepCrate(actorBody, interaction, -0.04f, true);
        Require(Mathf.Abs(box.transform.position.x - blockedX) < 0.001f, "Pull blocked by player wall must stop crate");
        interaction.ResolveHorizontal(-0.04f, true, false);
        Require(interaction.HeldCrate == null, "Jump/airborne must release grab");
        Debug.Log("CRATE_VALIDATION_PASSED: push, pull, release, separation, both wall constraints, airborne release.");
    }

    private static void StepCrate(SimulatedBody2D body, PlayerCrateInteraction interaction, float amount, bool grab)
    {
        float resolved = interaction.ResolveHorizontal(amount, grab, true);
        body.Move(new Vector2(resolved, -0.01f), interaction.HeldCrate != null ? interaction.HeldCrate.Body.Shape : null);
    }

    private static void ValidateSwimmingVolumes()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var water = new GameObject("Test water");
        var waterShape = water.AddComponent<BoxCollider2D>();
        waterShape.size = new Vector2(4f, 4f);
        waterShape.isTrigger = true;
        var volume = water.AddComponent<WaterVolume>();
        var actor = new GameObject("Test swimmer");
        var body = actor.AddComponent<SimulatedBody2D>();
        var swimming = actor.AddComponent<PlayerSwimming>();
        Require(actor.GetComponent<Rigidbody2D>() == null && swimming.CheckWater(),
            "Water detection must work without Rigidbody or trigger callbacks");
        volume.enabled = false;
        Require(!swimming.CheckWater(), "Disabled water volume must not enable swimming");
        volume.enabled = true;
        var secondWater = new GameObject("Overlapping water");
        secondWater.AddComponent<BoxCollider2D>().isTrigger = true;
        secondWater.AddComponent<WaterVolume>();
        volume.enabled = false;
        Require(swimming.CheckWater(), "Overlapping active water must remain swimmable");
        volume.enabled = true;
        actor.transform.position = new Vector3(0f, 2.01f, 0f);
        Require(!swimming.CheckWater(), "Leaving water surface must restore dry movement");
        actor.transform.position = Vector3.zero;
        body.Move(Vector2.right * 10f);
        Require(actor.transform.position.x > 9.99f && !swimming.CheckWater(),
            "Water must never block body sweeps, and leaving sideways must end swimming");
        Debug.Log("SWIMMING_VOLUME_VALIDATION_PASSED: no Rigidbody, disabled/overlapping volumes, surface and sideways exit.");
    }

    private static void ValidateLadder()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("Test ladder");
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 5f);
        var ladder = go.AddComponent<Ladder>();
        ladder.Shape.isTrigger = true;
        var actor = new GameObject("Test climber");
        actor.transform.position = new Vector3(0f, -1f, 0f);
        actor.AddComponent<SimulatedBody2D>();
        var interaction = actor.AddComponent<PlayerLadderInteraction>();
        const float dt = 0.02f;
        bool handled = interaction.Tick(0f, 1f, false, Vector2.zero, dt, out _, out bool released);
        Require(handled && !released && interaction.Attached, "Up must attach to a ladder without Rigidbody");
        float localY = go.transform.InverseTransformPoint(actor.transform.position).y;
        Vector3 carriedStart = actor.transform.position;
        go.transform.position += new Vector3(0.2f, 0.1f, 0f);
        interaction.Tick(0f, 0f, false, Vector2.zero, dt, out _, out released);
        Require(!released && Vector2.Distance(actor.transform.position, carriedStart + new Vector3(0.2f, 0.1f, 0f)) < 0.001f,
            "Idle attached player must follow moving ladder");
        Require(Mathf.Abs(go.transform.InverseTransformPoint(actor.transform.position).y - localY) < 0.001f,
            "Moving ladder must retain local climbing coordinate");
        float beforeDown = actor.transform.position.y;
        interaction.Tick(0f, -1f, false, Vector2.zero, dt, out _, out _);
        Require(actor.transform.position.y < beforeDown, "Down must descend");
        go.transform.position += Vector3.right * 0.04f;
        interaction.Tick(-1f, 0f, true, Vector2.zero, dt, out Vector2 launch, out released);
        Require(released && !interaction.Attached && launch.x < 0f && launch.y > 0f,
            "Jump must release left with ladder momentum");
        Require(Mathf.Abs(launch.x - (-6f + 2f)) < 0.001f, "Jump must add carrier velocity");
        interaction.Tick(0f, 1f, false, launch, dt, out _, out _);
        Require(!interaction.Attached, "Jump cooldown must prevent immediate reattachment");
        Debug.Log("LADDER_VALIDATION_PASSED: attach, descend, moving ladder, local coordinate, jump and cooldown.");
    }
}
