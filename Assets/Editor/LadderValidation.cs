using System;
using MousePlatformer;
using UnityEngine;

public static class LadderValidation
{
    public static void RunMotionChecks()
    {
        var settings = new LadderSettings();
        foreach (float dt in new[] { 0.01f, 0.02f, 1f / 30f })
        foreach (float scale in new[] { 0.5f, 1f, 2f })
        {
            float height = 0f;
            int frames = Mathf.RoundToInt(1f / dt);
            for (int i = 0; i < frames; i++) height = settings.Climb(height, 1f, scale, dt, -100f, 100f);
            Require(Mathf.Abs(height * scale - settings.climbSpeed) < 0.001f,
                "Climbing must use world units/s at every ladder scale");
            for (int i = 0; i < frames; i++) height = settings.Climb(height, -1f, scale, dt, -100f, 100f);
            Require(Mathf.Abs(height) < 0.001f, "Down input must reverse climbing");
            Require(settings.Climb(0.5f, 0f, scale, dt, -1f, 1f) == 0.5f, "Idle input must retain local attachment");
            Require(settings.Climb(1f, 1f, scale, dt, -1f, 1f) == 1f
                && settings.Climb(-1f, -1f, scale, dt, -1f, 1f) == -1f, "Climbing must respect endpoints");
        }
        Vector2 carrier = new Vector2(2f, -1f);
        Vector2 right = settings.JumpVelocity(carrier, 1f, -1f);
        Vector2 left = settings.JumpVelocity(carrier, -1f, 1f);
        Require(right == carrier + new Vector2(settings.jumpHorizontalSpeed, settings.jumpUpSpeed),
            "Right jump must inherit moving-ladder velocity");
        Require(left == carrier + new Vector2(-settings.jumpHorizontalSpeed, settings.jumpUpSpeed),
            "Left jump must inherit moving-ladder velocity");
        Require(settings.JumpVelocity(Vector2.zero, 0f, -1f).x < 0f, "Neutral jump must use last facing direction");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
