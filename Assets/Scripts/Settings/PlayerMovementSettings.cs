using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class PlayerMovementSettings
    {
        [Min(0f)] public float speed = 5f;
        [Min(0f)] public float groundAcceleration = 45f;
        [Min(0f)] public float airAcceleration = 28f;
        [Tooltip("Maximum step height in world units while grounded. Zero disables stepping.")]
        [Min(0f)] public float maxStepHeight = 0.15f;
        [Range(0f, 89f)] public float maxSlopeAngle = 45f;
        [Range(0f, 89f)] public float slopeSlideAngle = 30f;
        public bool slideOnSteepSlopes = true;
        [Min(0f)] public float slopeSlideAcceleration = 20f;
        [Min(0f)] public float slopeSlideMaxSpeed = 8f;
        [Min(0f)] public float coyoteTime = 0.1f;
        [Min(0f)] public float jumpBuffer = 0.12f;
        [Min(0f)] public float launchControlTime = 0.25f;
        [Range(0f, 1f)] public float launchControlMultiplier = 0.15f;
    }

    [Serializable]
    public sealed class PlayerInteractionSettings
    {
        [Min(0.01f)] public float crateMoveSpeed = 2.5f;
        [Min(0.01f)] public float crateGrabReach = 0.12f;
        [Min(0f)] public float ropeClimbSpeed = 2f;
        [Min(0f)] public float ropeSwingAcceleration = 70f;
        [Min(0.01f)] public float ropeGrabRadius = 0.45f;
        [Min(0f)] public float ropeRegrabDelay = 0.3f;
    }
}
