using System;
using MousePlatformer;
using UnityEngine;

public static class JumpValidation
{
    public static void RunChecks()
    {
        foreach (float dt in new[] { 0.01f, 0.02f, 1f / 30f, 0.1f })
        foreach (float gravity in new[] { 10f, 30f, 66f })
        foreach (float height in new[] { 0.01f, 0.25f, 1f, 2f, 3.75f })
        {
            float velocity = JumpPhysics.LaunchSpeed(height, gravity);
            float y = 0f;
            int ticks = 0;
            while (velocity > 0f && ticks++ < 1000)
                y += JumpPhysics.Integrate(ref velocity, gravity, 26f, dt);
            Require(ticks < 1000 && Mathf.Abs(y - height) < 0.0001f,
                $"Jump apex must equal height: H={height}, g={gravity}, dt={dt}, apex={y}");
            Require(velocity == 0f, "Velocity must be zero at apex");
            Require(JumpPhysics.Integrate(ref velocity, gravity * 2.2f, 26f, dt) < 0f,
                "Descent must start after apex");
            velocity = JumpPhysics.LaunchSpeed(height, gravity) * 0.45f;
            y = 0f;
            while (velocity > 0f) y += JumpPhysics.Integrate(ref velocity, gravity, 26f, dt);
            Require(y < height, "Early release must keep the shorter jump");
        }
        Require(JumpPhysics.LaunchSpeed(0f, 30f) == 0f, "Zero height must not launch upward");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
