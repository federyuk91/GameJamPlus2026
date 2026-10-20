using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class LedgeClimbSettings
    {
        public bool enabled = true;
        [Min(0.01f)] public float maxHeight = 0.75f;
        [Range(0.1f, 1f)] public float hangHeightFraction = 0.85f;
        [Min(0f)] public float retryDelay = 0.15f;
    }
}
