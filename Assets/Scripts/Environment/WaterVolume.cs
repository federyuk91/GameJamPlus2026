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

        [SerializeField] private SpriteRenderer visual;
        private MaterialPropertyBlock presentation;
        private static readonly int SurfaceProperty = Shader.PropertyToID("_SurfaceHeight");
        private static readonly int BottomProperty = Shader.PropertyToID("_BottomHeight");
        private static readonly int FlowProperty = Shader.PropertyToID("_Flow");
        private static readonly int FlowDirectionProperty = Shader.PropertyToID("_FlowDirection");
        private static readonly int FlowOriginProperty = Shader.PropertyToID("_FlowOrigin");

        public void SetPresentation(float surface, float bottom, float flow, Vector2 flowOrigin, Vector2 direction = default)
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (visual == null) return;
            if (presentation == null) presentation = new MaterialPropertyBlock();
            visual.GetPropertyBlock(presentation);
            presentation.SetFloat(SurfaceProperty, surface);
            presentation.SetFloat(BottomProperty, bottom);
            presentation.SetFloat(FlowProperty, flow);
            if (direction.sqrMagnitude < 0.0001f) direction = new Vector2(Mathf.Sign(flow), 0f);
            presentation.SetVector(FlowDirectionProperty, new Vector4(direction.x, direction.y, 0f, 0f));
            presentation.SetVector(FlowOriginProperty, new Vector4(flowOrigin.x, flowOrigin.y, 0f, 0f));
            visual.SetPropertyBlock(presentation);
        }

        public void SetWorldBounds(Rect bounds) => SetWorldBounds(bounds, bounds);

        public void SetWorldBounds(Rect fillBounds, Rect capacityBounds, float visualOverlapOffset = 0f)
        {
            float offset = Mathf.Max(0f, visualOverlapOffset);
            Rect visualBounds = Rect.MinMaxRect(capacityBounds.xMin - offset, capacityBounds.yMin - offset,
                capacityBounds.xMax + offset, capacityBounds.yMax + offset);
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            bool filled = fillBounds.width > 0.0001f && fillBounds.height > 0.0001f;
            Shape.enabled = filled;
            if (visual != null) visual.enabled = filled;
            Bounds spriteBounds = visual != null && visual.sprite != null
                ? visual.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            // Even a dry section needs its own baked transform, never the prefab's previous placement.
            transform.localScale = new Vector3(Mathf.Max(0.0001f, visualBounds.width) / Mathf.Max(0.0001f, spriteBounds.size.x * Mathf.Abs(parentScale.x)),
                Mathf.Max(0.0001f, visualBounds.height) / Mathf.Max(0.0001f, spriteBounds.size.y * Mathf.Abs(parentScale.y)), transform.localScale.z);
            Vector3 centerOffset = transform.TransformVector(spriteBounds.center);
            transform.position = new Vector3(visualBounds.center.x - centerOffset.x, visualBounds.center.y - centerOffset.y, transform.position.z);
            Vector3 scale = transform.lossyScale;
            Shape.size = new Vector2(Mathf.Max(0.0001f, fillBounds.width / Mathf.Max(0.0001f, Mathf.Abs(scale.x))),
                Mathf.Max(0.0001f, fillBounds.height / Mathf.Max(0.0001f, Mathf.Abs(scale.y))));
            Vector3 center = transform.InverseTransformPoint(new Vector3(fillBounds.center.x, fillBounds.center.y, transform.position.z));
            Shape.offset = new Vector2(center.x, center.y);
        }

        public static bool ContainsPoint(Vector2 point)
        {
            foreach (WaterVolume volume in activeVolumes)
                if (volume != null && volume.Shape.enabled && volume.Shape.OverlapPoint(point)) return true;
            return false;
        }

        private static readonly List<Rect> coverage = new List<Rect>();
        private static readonly List<float> coverageEdges = new List<float>();
        private static readonly List<Vector2> coverageIntervals = new List<Vector2>();

        public static float SubmergedFraction(Bounds bodyBounds)
        {
            coverage.Clear();
            coverageEdges.Clear();
            foreach (WaterVolume volume in activeVolumes)
            {
                if (volume == null || !volume.Shape.enabled) continue;
                Bounds water = volume.Shape.bounds;
                float left = Mathf.Max(bodyBounds.min.x, water.min.x);
                float right = Mathf.Min(bodyBounds.max.x, water.max.x);
                float bottom = Mathf.Max(bodyBounds.min.y, water.min.y);
                float top = Mathf.Min(bodyBounds.max.y, water.max.y);
                if (right <= left || top <= bottom) continue;
                coverage.Add(Rect.MinMaxRect(left, bottom, right, top));
                coverageEdges.Add(left);
                coverageEdges.Add(right);
            }
            // Union coverage across basin sections; taking the largest section undercounts a body crossing their seams.
            coverageEdges.Sort();
            float area = 0f;
            for (int index = 1; index < coverageEdges.Count; index++)
            {
                float left = coverageEdges[index - 1];
                float right = coverageEdges[index];
                if (right <= left) continue;
                float middle = (left + right) * 0.5f;
                coverageIntervals.Clear();
                foreach (Rect rectangle in coverage)
                    if (middle >= rectangle.xMin && middle <= rectangle.xMax)
                        coverageIntervals.Add(new Vector2(rectangle.yMin, rectangle.yMax));
                coverageIntervals.Sort(CompareIntervals);
                float length = 0f;
                float end = float.NegativeInfinity;
                foreach (Vector2 interval in coverageIntervals)
                {
                    length += Mathf.Max(0f, interval.y - Mathf.Max(end, interval.x));
                    end = Mathf.Max(end, interval.y);
                }
                area += (right - left) * length;
            }
            return Mathf.Clamp01(area / Mathf.Max(0.0001f, bodyBounds.size.x * bodyBounds.size.y));
        }

        private static int CompareIntervals(Vector2 first, Vector2 second) => first.x.CompareTo(second.x);

        private void OnEnable() => activeVolumes.Add(this);
        private void OnDisable() => activeVolumes.Remove(this);
        private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
        private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
        private void OnValidate() => GetComponent<BoxCollider2D>().isTrigger = true;
    }
}
