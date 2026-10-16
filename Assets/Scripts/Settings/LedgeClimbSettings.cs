using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class LedgeClimbSettings
    {
        public bool enabled = true;
        [Min(0.01f)] public float maxHeight = 0.75f;
        [Min(0.01f)] public float forwardReach = 0.12f;
        [Min(0.01f)] public float climbSpeed = 5f;
        [Min(0f)] public float retryDelay = 0.15f;
    }
}
