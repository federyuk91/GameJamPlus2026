using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class DiggingSettings
    {
        public bool enabled = true;
        [Min(0.01f)] public float radius = 0.45f;
        [Min(0f)] public float reach = 0.08f;
        [Range(0, 255)] public int excavatedAlpha = 254;
        public Color excavatedTint = new Color(0.04f, 0.025f, 0.015f, 1f);
    }
}
