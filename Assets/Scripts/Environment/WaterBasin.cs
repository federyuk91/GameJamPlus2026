using System;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(-600), DisallowMultipleComponent]
    public sealed class WaterBasin : MonoBehaviour
    {
        [Serializable]
        public struct Section
        {
            public Rect bounds;
            public WaterVolume water;
        }

        [SerializeField, Tooltip("Non-overlapping rectangular sections, relative to this basin. Keep the basin unrotated and unscaled.")]
        private Section[] sections = Array.Empty<Section>();
        // Keep the serialized pivot-relative height so existing scene and prefab levels remain unchanged.
        [SerializeField, HideInInspector]
        private float initialSurfaceHeight;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showSectionBounds;
        [SerializeField] private bool showWaterPreview;
        [SerializeField, Range(0f, 100f), Tooltip("Geometric Scene View preview from this basin's floor to ceiling; does not change its initial level.")]
        private float waterPreview;
        [SerializeField, HideInInspector] private float visualOverlapOffset;
        private WaterBasinModel model;
        private float visibleSurface;
        private float visibleFlow;
        private Vector2 flowOrigin;
        private Vector2 flowDirection;
        public WaterBasinModel Model => model;
        public bool IsSimulationPreviewActive { get; set; }
        public float SurfaceHeight => Application.isPlaying && model != null ? model.Surface : transform.position.y + initialSurfaceHeight;
        public float MaximumWaterDepth => GeometryBounds.height;
        public float InitialWaterDepth => Mathf.Clamp(transform.position.y + initialSurfaceHeight - GeometryBounds.yMin, 0f, MaximumWaterDepth);

        public Rect GeometryBounds
        {
            get
            {
                if (sections == null || sections.Length == 0) return new Rect(transform.position, Vector2.zero);
                Rect first = WorldBounds(sections[0]);
                Vector2 minimum = first.min;
                Vector2 maximum = first.max;
                foreach (Section section in sections)
                {
                    Rect bounds = WorldBounds(section);
                    minimum = Vector2.Min(minimum, bounds.min);
                    maximum = Vector2.Max(maximum, bounds.max);
                }
                return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
            }
        }

        private void Awake() => ResetWater();

        public WaterBasinModel CreatePreviewModel()
        {
            Rect[] worldSections = new Rect[sections.Length];
            for (int index = 0; index < sections.Length; index++) worldSections[index] = WorldBounds(sections[index]);
            return new WaterBasinModel(worldSections, transform.position.y + initialSurfaceHeight);
        }

        public void SetVisualOverlapOffset(float offset) => visualOverlapOffset = Mathf.Max(0f, offset);

        public void Configure(Section[] basinSections, float initialWorldSurface)
        {
            sections = basinSections;
            initialSurfaceHeight = initialWorldSurface - transform.position.y;
        }

        public void ResetWater()
        {
            if (sections.Length == 0) return;
            Rect[] worldSections = new Rect[sections.Length];
            for (int index = 0; index < sections.Length; index++) worldSections[index] = WorldBounds(sections[index]);
            model = new WaterBasinModel(worldSections, transform.position.y + initialSurfaceHeight);
            visibleSurface = model.Surface;
            RefreshWater();
        }

        public void AddWater(float amount)
        {
            if (model == null) return;
            ReceiveWater(amount);
        }

        public bool ContainsGeometryPoint(Vector2 point)
        {
            foreach (Section section in sections)
                if (WorldBounds(section).Contains(point)) return true;
            return false;
        }

        public float ReceiveWater(float amount)
        {
            if (model == null) return 0f;
            double before = model.Volume;
            model.SetVolume(before + Mathf.Max(0f, amount));
            visibleFlow = 1f;
            flowOrigin = transform.position;
            RefreshWater();
            return (float)(model.Volume - before);
        }

        public void RemoveWater(float amount)
        {
            if (model == null) return;
            model.SetVolume(model.Volume - Mathf.Max(0f, amount));
            RefreshWater();
        }

        internal void RefreshWater()
        {
            if (model == null) return;
            foreach (Section section in sections)
            {
                if (section.water == null) continue;
                Rect capacity = WorldBounds(section);
                Rect fill = capacity;
                fill.height = Mathf.Clamp(model.Surface - fill.yMin, 0f, fill.height);
                section.water.SetWorldBounds(fill, capacity, visualOverlapOffset);
                section.water.SetPresentation(visibleSurface + visualOverlapOffset, model.Bottom - visualOverlapOffset, visibleFlow, flowOrigin, flowDirection);
            }
        }

        internal void NotifyFlow(float flow, Vector2 origin, Vector2 direction = default)
        {
            visibleFlow = flow;
            flowOrigin = origin;
            flowDirection = direction;
        }

        private void LateUpdate()
        {
            if (model == null) return;
            visibleSurface = Mathf.Lerp(visibleSurface, model.Surface, 1f - Mathf.Exp(-12f * Time.deltaTime));
            visibleFlow = Mathf.MoveTowards(visibleFlow, 0f, Time.deltaTime * 2f);
            foreach (Section section in sections)
                if (section.water != null) section.water.SetPresentation(visibleSurface + visualOverlapOffset, model.Bottom - visualOverlapOffset, visibleFlow, flowOrigin, flowDirection);
        }

        private Rect WorldBounds(Section section)
        {
            Vector2 origin = (Vector2)transform.position + section.bounds.position;
            return new Rect(origin, section.bounds.size);
        }

        public void DrawVolumeGizmos(Color color, float surfaceHeight = float.PositiveInfinity)
        {
            if (sections == null) return;
            Color previousColor = Gizmos.color;
            Gizmos.color = color;
            foreach (Section section in sections)
            {
                Rect bounds = WorldBounds(section);
                bounds.height = Mathf.Clamp(surfaceHeight - bounds.yMin, 0f, bounds.height);
                if (bounds.height <= 0f) continue;
                bounds = Rect.MinMaxRect(bounds.xMin - visualOverlapOffset, bounds.yMin - visualOverlapOffset,
                    bounds.xMax + visualOverlapOffset, bounds.yMax + visualOverlapOffset);
                Gizmos.DrawCube(new Vector3(bounds.center.x, bounds.center.y, transform.position.z),
                    new Vector3(bounds.width, bounds.height, 0.01f));
            }
            Gizmos.color = previousColor;
        }

        private void OnDrawGizmos()
        {
            if (!IsSimulationPreviewActive && showWaterPreview && sections != null && sections.Length > 0)
            {
                Rect bounds = GeometryBounds;
                DrawVolumeGizmos(new Color(0.1f, 0.8f, 1f, 0.55f), Mathf.Lerp(bounds.yMin, bounds.yMax, waterPreview / 100f));
            }
            if (IsSimulationPreviewActive || !showGizmos || sections == null || sections.Length == 0) return;
            Rect first = WorldBounds(sections[0]);
            Bounds capacity = new Bounds(first.center, first.size);
            foreach (Section section in sections)
            {
                Rect area = WorldBounds(section);
                capacity.Encapsulate(new Vector3(area.xMin, area.yMin));
                capacity.Encapsulate(new Vector3(area.xMax, area.yMax));
            }
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.45f);
            Gizmos.DrawWireCube(capacity.center, capacity.size);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(transform.position.x,
                Mathf.Clamp(SurfaceHeight, capacity.min.y, capacity.max.y), transform.position.z), 0.08f);
            foreach (Section section in sections)
            {
                Rect bounds = WorldBounds(section);
                if (showSectionBounds)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
                float surface = SurfaceHeight;
                if (surface <= bounds.yMin + 0.0001f || surface > bounds.yMax + 0.0001f) continue;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(new Vector3(bounds.xMin, surface), new Vector3(bounds.xMax, surface));
            }
        }
    }
}
