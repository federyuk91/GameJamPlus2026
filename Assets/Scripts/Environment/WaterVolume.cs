using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class WaterVolume : MonoBehaviour
    {
        private static readonly List<WaterVolume> activeVolumes = new List<WaterVolume>();
        [SerializeField] private BoxCollider2D shape;
        public BoxCollider2D Shape => shape != null ? shape : shape = GetComponent<BoxCollider2D>();

        public static bool ContainsPoint(Vector2 point)
        {
            foreach (WaterVolume volume in activeVolumes)
                if (volume != null && volume.Shape.enabled && volume.Shape.OverlapPoint(point)) return true;
            return false;
        }

        public static float SubmergedFraction(Bounds bodyBounds)
        {
            float fraction = 0f;
            foreach (WaterVolume volume in activeVolumes)
            {
                if (volume == null || !volume.Shape.enabled) continue;
                Bounds water = volume.Shape.bounds;
                float width = Mathf.Max(0f, Mathf.Min(bodyBounds.max.x, water.max.x) - Mathf.Max(bodyBounds.min.x, water.min.x));
                float height = Mathf.Max(0f, Mathf.Min(bodyBounds.max.y, water.max.y) - Mathf.Max(bodyBounds.min.y, water.min.y));
                float area = Mathf.Max(0.0001f, bodyBounds.size.x * bodyBounds.size.y);
                fraction = Mathf.Max(fraction, width * height / area);
            }
            return Mathf.Clamp01(fraction);
        }

        private void OnEnable() => activeVolumes.Add(this);
        private void OnDisable() => activeVolumes.Remove(this);
        private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
        private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
        private void OnValidate() => GetComponent<BoxCollider2D>().isTrigger = true;
    }
}
