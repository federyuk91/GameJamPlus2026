using System;
using MousePlatformer;
using UnityEngine;

public static class RopeValidation
{
    public static void RunChainChecks()
    {
        RunLimitChecks();
        foreach (float dt in new[] { 0.01f, 0.02f, 1f / 30f })
        {
            var rope = new RopeChain(Vector2.zero, 5, 0.5f);
            for (int frame = 0; frame < 300; frame++) rope.Step(Vector2.zero, dt, 30f, 0.35f, -1f, 0f);
            Require(Mathf.Abs(rope[5].x) < 0.001f, "Resting rope must not drift horizontally");
            Require(Vector2.Distance(rope[0], Vector2.zero) < 0.00001f, "Anchor must stay fixed");
            rope.AddVelocity(2.5f, Vector2.right * 5f, dt);
            bool right = false, left = false;
            float lastX = rope[5].x;
            for (int frame = 0; frame < 600; frame++)
            {
                rope.Step(Vector2.zero, dt, 30f, 0.35f, 2.5f, frame < 40 ? 70f : 0f);
                right |= rope[5].x > 0.2f;
                left |= rope[5].x < -0.2f;
                for (int node = 0; node < rope.SegmentCount; node++)
                    Require(Mathf.Abs(Vector2.Distance(rope[node], rope[node + 1]) - 0.5f) < 0.02f,
                        "Distance joint must retain segment length");
                Require(Mathf.Abs(rope.Velocity(2.5f).x - (rope[5].x - lastX) / dt) < 0.001f,
                    "Release velocity must match rope displacement per second");
                Require(!float.IsNaN(rope[5].x) && rope[5].magnitude < 2.6f, "Solver must stay finite and bounded");
                lastX = rope[5].x;
            }
            Require(right && left, "Rope must swing in both directions after pumping");
            float along = rope.ClosestDistance(rope.Point(1.25f), out float separation);
            Require(Mathf.Abs(along - 1.25f) < 0.001f && separation < 0.001f, "Grip projection on rope");
            Require(Vector2.Distance(rope.Point(-5f), rope[0]) < 0.001f
                && Vector2.Distance(rope.Point(10f), rope[5]) < 0.001f, "Climbing must clamp to rope ends");
        }
    }

    private static void RunLimitChecks()
    {
        // Check the stopping-distance envelope independently of gravity and pumping.
        var limits = new RopeSwingConstraint();
        var positions = new[] { Vector2.zero, AtAngle(60f) };
        var history = new[] { Vector2.zero, AtAngle(59f) };
        bool stopped = limits.Apply(positions, history, Vector2.zero, 59f, 0.1f);
        float first = RopeSwingConstraint.Angle(positions[1]);
        Require(!stopped && first > 59f && first < 60f, "Braking must reduce outward travel before outer");
        positions[1] = AtAngle(61f);
        stopped = limits.Apply(positions, history, Vector2.zero, first, 0.1f);
        Require(stopped && Mathf.Abs(RopeSwingConstraint.Angle(positions[1]) - 60f) < 0.001f,
            "Outer must stop exactly at the configured angle");
        Require((positions[1] - history[1]).sqrMagnitude < 0.000001f, "Outer stop must clear Verlet velocity");

        foreach (float dt in new[] { 0.01f, 0.02f, 1f / 30f })
        foreach (float direction in new[] { -1f, 1f })
        {
            limits = new RopeSwingConstraint
            {
                minInnerAngle = -20f, minOuterAngle = -35f,
                maxInnerAngle = 25f, maxOuterAngle = 50f
            };
            var rope = new RopeChain(Vector2.zero, 5, 0.5f);
            bool hitOuter = false;
            for (int frame = 0; frame < 1000; frame++)
            {
                rope.Step(Vector2.zero, dt, 30f, 0.35f, 2.5f, direction * 700f, swingLimits: limits);
                float angle = RopeSwingConstraint.Angle(rope[5]);
                Require(angle >= -35.001f && angle <= 50.001f, "Asymmetric outer limits must never be exceeded");
                for (int node = 0; node < rope.SegmentCount; node++)
                    Require(Mathf.Abs(Vector2.Distance(rope[node], rope[node + 1]) - 0.5f) < 0.02f,
                        $"Angular braking must preserve distance joints: dt={dt}, direction={direction}, frame={frame}, node={node}, length={Vector2.Distance(rope[node], rope[node + 1])}");
                float boundary = direction < 0f ? -35f : 50f;
                if (Mathf.Abs(angle - boundary) < 0.001f && rope.Velocity(2.5f).sqrMagnitude < 0.000001f)
                {
                    hitOuter = true;
                    for (int node = 0; node <= rope.SegmentCount; node++)
                        Require(rope.Velocity(node * rope.SegmentLength).sqrMagnitude < 0.000001f,
                            "Every link must stop at outer before reversal");
                    rope.Step(Vector2.zero, dt, 30f, 0.35f, 2.5f, direction * 700f, swingLimits: limits);
                    Require(RopeSwingConstraint.Angle(rope[5]) * direction < boundary * direction,
                        "After zero speed gravity must reverse the swing");
                    break;
                }
            }
            Require(hitOuter, "Sustained strong pumping must reach a zero-speed outer stop");
        }
    }

    private static Vector2 AtAngle(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
