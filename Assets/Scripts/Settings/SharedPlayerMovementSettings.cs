using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class SharedPlayerMovementSettings
    {
        [Tooltip("Maximum step height in world units while grounded. Zero disables stepping.")]
        [Min(0f)] public float maxStepHeight = 0.15f;
        [Tooltip("Seconds after leaving walkable ground during which a normal jump is still accepted.")]
        [Min(0f)] public float coyoteTime = 0.1f;
        [Tooltip("Seconds for which a Jump press is remembered while waiting for a valid ground jump.")]
        [Min(0f)] public float jumpBuffer = 0.12f;
        [Tooltip("Seconds of modified air control after leaving a rope, ladder, ledge or moving support jump.")]
        [Min(0f)] public float launchControlTime = 0.25f;
        [Tooltip("Multiplier applied to local Air Acceleration during Launch Control Time. 0 removes horizontal steering; 1 keeps normal control.")]
        [Range(0f, 1f)] public float launchControlMultiplier = 0.15f;
        [Tooltip("Footsteps per second while running. 0 disables footsteps.")]
        [Min(0f)] public float runFootstepsPerSecond = 6f;
        [Tooltip("Footsteps per second while walking. 0 disables footsteps.")]
        [Min(0f)] public float walkFootstepsPerSecond = 3f;
    }
}
