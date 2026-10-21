using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class SwimMotion
    {
        [Tooltip("Multiplier of the player's normal walking speed while grounded underwater.")]
        [Min(0f)] public float bottomWalkSpeedMultiplier = 0.5f;
        [Min(0f)] public float acceleration = 10f;
        [Min(0f)] public float strokeAcceleration = 45f;
        [Min(0f)] public float strokeControlTime = 0.35f;
        [Min(0f)] public float braking = 6f;
        [Tooltip("Swimmer-only multiplier of the body's effective gravity. Controls downward swimming acceleration, not prop buoyancy. The default 7/30 preserves 7 units/s² for a body with gravity 30.")]
        [Min(0f)] public float gravityMultiplier = 7f / 30f;
        [Min(0f)] public float maxSinkSpeed = 3.5f;
        [Min(0f)] public float strokeSpeed = 4.8f;
        [Tooltip("Upward speed when Jump is pressed near the water surface. Does not affect underwater strokes.")]
        [Min(0f)] public float surfaceJumpSpeed = 4.8f;
        [Tooltip("Maximum distance from the top of the player collider to the water surface that allows a surface jump.")]
        [Min(0f)] public float surfaceJumpDepth = 0.1f;

        public float StepHorizontal(float velocity, float horizontal, float dt, float speed, bool strokeControl)
        {
            if (dt <= 0f) return velocity;
            speed = Mathf.Max(0f, speed);
            velocity = Mathf.Clamp(velocity, -speed, speed);
            float activeAcceleration = strokeControl ? Mathf.Max(acceleration, strokeAcceleration) : acceleration;
            return Mathf.MoveTowards(velocity, Mathf.Clamp(horizontal, -1f, 1f) * speed,
                (Mathf.Abs(horizontal) > 0.01f ? activeAcceleration : braking) * dt);
        }

        public float GravityAcceleration(float bodyGravity) => Mathf.Max(0f, bodyGravity) * Mathf.Max(0f, gravityMultiplier);

        public Vector2 Step(Vector2 velocity, float horizontal, bool strokePressed, float dt, float bodyGravity, float speed, float waterDrag, bool strokeControl = false, bool surfaceJump = false)
        {
            if (dt <= 0f) return velocity;
            // Entering water immediately caps a fast fall or rope launch to swimming speeds.
            velocity.x = StepHorizontal(velocity.x, horizontal, dt, speed, strokePressed || strokeControl);
            float upwardSpeed = surfaceJump ? surfaceJumpSpeed : strokeSpeed;
            velocity.y = Mathf.Clamp(velocity.y * Mathf.Exp(-Mathf.Max(0f, waterDrag) * dt) - GravityAcceleration(bodyGravity) * dt,
                -maxSinkSpeed, upwardSpeed);
            // One press gives one stroke. Resetting upward speed avoids unbounded accumulation.
            if (strokePressed) velocity.y = upwardSpeed;
            return velocity;
        }
    }
}
