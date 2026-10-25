using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class SwimMotion
    {
        [Tooltip("Multiplier of the player's normal walking speed while grounded underwater.")]
        [Min(0f)] public float bottomWalkSpeedMultiplier = 0.5f;
        [Tooltip("Shared water velocity damping in inverse seconds, on both axes. Higher values dissipate momentum faster.")]
        [Min(0f)] public float waterDrag = DefaultWaterDrag;
        public const float DefaultWaterDrag = 1.5f;
        [Tooltip("Swimmer-only multiplier of the body's effective gravity. Controls downward swimming acceleration, not prop buoyancy. The default 7/30 preserves 7 units/s² for a body with gravity 30.")]
        [Min(0f)] public float gravityMultiplier = 7f / 30f;
        [Min(0f)] public float maxSinkSpeed = 3.5f;
        [Min(0f)] public float strokeSpeed = 4.8f;
        [Tooltip("Upward speed when Jump is pressed near the water surface. Does not affect underwater strokes.")]
        [Min(0f)] public float surfaceJumpSpeed = 4.8f;
        [Tooltip("Maximum distance from the top of the player collider to the water surface that allows a surface jump.")]
        [Min(0f)] public float surfaceJumpDepth = 0.1f;

        public float StepHorizontal(float velocity, float horizontal, float dt, float speed, bool strokeControl,
            SwimControlSettings controls, float drag = 0f)
        {
            if (dt <= 0f) return velocity;
            speed = Mathf.Max(0f, speed);
            velocity = Mathf.Clamp(velocity, -speed, speed) * Mathf.Exp(-Mathf.Max(0f, drag) * dt);
            float activeAcceleration = strokeControl ? Mathf.Max(controls.acceleration, controls.strokeAcceleration) : controls.acceleration;
            bool hasInput = Mathf.Abs(horizontal) > 0.01f;
            float target = hasInput ? Mathf.Clamp(horizontal, -1f, 1f) * speed : 0f;
            velocity = Mathf.MoveTowards(velocity, target,
                Mathf.Max(0f, hasInput ? activeAcceleration : controls.braking) * dt);
            // Exponential drag approaches rest asymptotically; stop imperceptible residual drift.
            return !hasInput && Mathf.Abs(velocity) < 0.01f ? 0f : velocity;
        }

        public float GravityAcceleration(float bodyGravity) => Mathf.Max(0f, bodyGravity) * Mathf.Max(0f, gravityMultiplier);

        public Vector2 Step(Vector2 velocity, float horizontal, bool strokePressed, float dt, float bodyGravity, float speed, float waterDrag, SwimControlSettings controls, bool strokeControl = false, bool surfaceJump = false)
        {
            if (dt <= 0f) return velocity;
            // Entering water immediately caps a fast fall or rope launch to swimming speeds.
            velocity.x = StepHorizontal(velocity.x, horizontal, dt, speed, strokePressed || strokeControl, controls, waterDrag);
            float upwardSpeed = surfaceJump ? surfaceJumpSpeed : strokeSpeed;
            velocity.y = Mathf.Clamp(velocity.y * Mathf.Exp(-Mathf.Max(0f, waterDrag) * dt) - GravityAcceleration(bodyGravity) * dt,
                -maxSinkSpeed, upwardSpeed);
            // One press gives one stroke. Resetting upward speed avoids unbounded accumulation.
            if (strokePressed) velocity.y = upwardSpeed;
            return velocity;
        }
    }

    [Serializable]
    public sealed class SwimControlSettings
    {
        [Tooltip("Horizontal acceleration while swimming, in world units/s². Water Drag also opposes motion while input is held.")]
        [Min(0f)] public float acceleration = 9f;
        [Tooltip("Horizontal acceleration after a swim stroke, also used for brief surface control.")]
        [Min(0f)] public float strokeAcceleration = 14f;
        [Min(0f)] public float strokeControlTime = 0.3f;
        [Tooltip("Additional horizontal deceleration when directional input is released. 0 leaves only water resistance.")]
        [Min(0f)] public float braking;
    }

}
