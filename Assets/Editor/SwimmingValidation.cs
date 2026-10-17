using System;
using MousePlatformer;
using UnityEngine;

public static class SwimmingValidation
{
    public static void RunMotionChecks()
    {
        foreach (float dt in new[] { 0.01f, 0.02f, 1f / 30f })
        {
            var motion = new SwimMotion();
            Vector2 velocity = Vector2.zero;
            for (int i = 0; i < Mathf.RoundToInt(5f / dt); i++)
                velocity = motion.Step(velocity, 0f, false, dt);
            Require(Mathf.Abs(velocity.y + motion.maxSinkSpeed) < 0.001f, "Idle swimming must sink at capped speed");
            velocity = motion.Step(velocity, 0f, true, dt);
            Require(Mathf.Abs(velocity.y - motion.strokeSpeed) < 0.001f, "One jump press must give an upward stroke");
            for (int i = 0; i < Mathf.RoundToInt(0.1f / dt); i++)
                velocity = motion.Step(velocity, 0f, false, dt);
            Require(velocity.y > 0f && velocity.y < motion.strokeSpeed, "Stroke must decay gradually after a press");
            velocity = motion.Step(velocity, 0f, true, dt);
            Require(Mathf.Abs(velocity.y - motion.strokeSpeed) < 0.001f, "Repeated presses must renew the stroke");
            for (int i = 0; i < Mathf.RoundToInt(2f / dt); i++)
                velocity = motion.Step(velocity, 0f, false, dt);
            Require(velocity.y < 0f, "Without a new press the player must resume sinking");
            velocity = motion.Step(new Vector2(50f, -50f), 1f, false, dt);
            Require(velocity.x <= motion.speed && velocity.y >= -motion.maxSinkSpeed,
                "Water entry must limit fast fall and horizontal momentum");
            velocity = Vector2.zero;
            for (int i = 0; i < Mathf.CeilToInt((motion.speed / motion.acceleration + 0.1f) / dt); i++)
                velocity = motion.Step(velocity, 1f, false, dt);
            Require(Mathf.Abs(velocity.x - motion.speed) < 0.001f, "Horizontal swimming uses acceleration in units/s²");
            for (int i = 0; i < Mathf.CeilToInt((motion.speed / motion.braking + 0.1f) / dt); i++)
                velocity = motion.Step(velocity, 0f, false, dt);
            Require(Mathf.Abs(velocity.x) < 0.001f, "Releasing direction must brake horizontal swimming");
            for (int i = 0; i < 100; i++) velocity = motion.Step(velocity, -1f, true, dt);
            Require(velocity.x >= -motion.speed && velocity.y <= motion.strokeSpeed,
                "Repeated strokes must never accumulate unbounded velocity");
            velocity = motion.Step(new Vector2(motion.speed, -1f), 1f, true, dt);
            Require(Mathf.Abs(velocity.x - motion.speed) < 0.001f,
                "A stroke must preserve existing lateral speed");
            velocity = Vector2.zero;
            int responseTicks = Mathf.CeilToInt((motion.speed / motion.strokeAcceleration + dt) / dt);
            for (int i = 0; i < responseTicks; i++)
                velocity = motion.Step(velocity, 1f, i == 0, dt, true);
            Require(Mathf.Abs(velocity.x - motion.speed) < 0.001f,
                "After a stroke lateral input must quickly reach swimming speed");
            float x = motion.StepHorizontal(motion.speed, 1f, dt, true);
            Require(Mathf.Abs(x - motion.speed) < 0.001f, "Surface control must not lose water momentum");
            for (int i = 0; i < responseTicks * 2; i++) x = motion.StepHorizontal(x, -1f, dt, true);
            Require(x < 0f, "Stroke/surface control must allow changing horizontal direction");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
