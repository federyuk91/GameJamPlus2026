using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class SwimMotion
    {
        [InspectorName("Horizontal Speed (units/s)"), Min(0f)] public float speed = 5f;
        [Tooltip("Multiplier of the player's normal walking speed while grounded underwater.")]
        [Min(0f)] public float bottomWalkSpeedMultiplier = 0.5f;
        [InspectorName("Acceleration (units/s²)"), Min(0f)] public float acceleration = 10f;
        [Min(0f)] public float strokeAcceleration = 45f;
        [Min(0f)] public float strokeControlTime = 0.35f;
        [InspectorName("Braking (units/s²)"), Min(0f)] public float braking = 6f;
        [InspectorName("Sinking Gravity (units/s²)"), Min(0f)] public float gravity = 7f;
        [InspectorName("Max Sink Speed (units/s)"), Min(0f)] public float maxSinkSpeed = 3.5f;
        [InspectorName("Stroke Speed (units/s)"), Min(0f)] public float strokeSpeed = 4.8f;
        [InspectorName("Vertical Drag (1/s)"), Min(0f)] public float verticalDrag = 1.5f;

        public float StepHorizontal(float velocity, float horizontal, float dt, bool strokeControl)
        {
            if (dt <= 0f) return velocity;
            velocity = Mathf.Clamp(velocity, -speed, speed);
            float activeAcceleration = strokeControl ? Mathf.Max(acceleration, strokeAcceleration) : acceleration;
            return Mathf.MoveTowards(velocity, Mathf.Clamp(horizontal, -1f, 1f) * speed,
                (Mathf.Abs(horizontal) > 0.01f ? activeAcceleration : braking) * dt);
        }

        public Vector2 Step(Vector2 velocity, float horizontal, bool strokePressed, float dt, bool strokeControl = false)
        {
            if (dt <= 0f) return velocity;
            // Entering water immediately caps a fast fall or rope launch to swimming speeds.
            velocity.x = StepHorizontal(velocity.x, horizontal, dt, strokePressed || strokeControl);
            velocity.y = Mathf.Clamp(velocity.y * Mathf.Exp(-verticalDrag * dt) - gravity * dt,
                -maxSinkSpeed, strokeSpeed);
            // One press gives one stroke. Resetting upward speed avoids unbounded accumulation.
            if (strokePressed) velocity.y = strokeSpeed;
            return velocity;
        }
    }
}
