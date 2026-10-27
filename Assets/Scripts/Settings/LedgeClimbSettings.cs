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
        [Tooltip("Distance above and below a candidate edge used to check the local grip geometry, in world units.")]
        [Min(0.001f)] public float gripProbeClearance = 0.03125f;
        [Tooltip("Minimum exposed shelf depth required for hanging, in world units.")]
        [Min(0.001f)] public float minimumGripDepth = 0.03125f;
        [Min(0f)] public float retryDelay = 0.15f;
    }
}
