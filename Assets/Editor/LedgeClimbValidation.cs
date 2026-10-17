using System;
using MousePlatformer;
using UnityEditor;
using UnityEngine;

public static class LedgeClimbValidation
{
    [MenuItem("Tools/Mouse Platformer/Verifica risalita bordi")]
    public static void RunChecks()
    {
        foreach (float direction in new[] { -1f, 1f })
        foreach (float dt in new[] { 0.01f, 0.02f, 1f / 30f })
            Check(direction, dt, false, false, false);
        Check(1f, 0.02f, true, false, false);
        Check(1f, 0.02f, false, true, false);
        Check(1f, 0.02f, false, false, true);
        Debug.Log("LEDGE_CLIMB_VALIDATION_PASSED: both sides, 30/50/100 Hz, solid landing, ceiling, tall wall, trigger exclusion.");
    }

    private static void Check(float direction, float dt, bool ceiling, bool tall, bool trigger)
    {
        var root = new GameObject("Ledge validation temporary objects");
        root.transform.position = new Vector3(1000f, 1000f, 0f);
        try
        {
            var platform = new GameObject("Platform");
            platform.transform.SetParent(root.transform, false);
            platform.transform.localPosition = new Vector3(direction, tall ? 0.8f : -0.3f, 0f);
            var box = platform.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.isTrigger = trigger;
            var player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            player.transform.localPosition = Vector3.left * (direction * 0.005f);
            var body = player.AddComponent<SimulatedBody2D>();
            var climb = player.AddComponent<PlayerLedgeClimb>();
            if (ceiling)
            {
                var roof = new GameObject("Low ceiling");
                roof.transform.SetParent(root.transform, false);
                roof.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                roof.AddComponent<BoxCollider2D>().size = new Vector2(2f, 0.2f);
            }
            bool started = climb.TryBegin(direction);
            if (ceiling || tall || trigger)
            {
                Require(!started, "Invalid/blocked ledge must not begin a climb");
                return;
            }
            Require(started, "Reachable ledge must start climbing");
            int ticks = 0;
            while (climb.Active && ticks++ < 300) climb.Tick(dt, out _);
            Require(ticks < 300 && body.Grounded, "Climb must complete grounded on the ledge");
            Require(body.Shape.bounds.min.y >= box.bounds.max.y - 0.001f,
                "Landing must not overlap the supporting collider");
            Require((player.transform.position.x - root.transform.position.x) * direction > 0.5f,
                "Player must move across the corner");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            Physics2D.SyncTransforms();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
