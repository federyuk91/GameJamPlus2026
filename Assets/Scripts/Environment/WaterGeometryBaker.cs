using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MousePlatformer
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-700)]
    public sealed class WaterGeometryBaker : MonoBehaviour
    {
        [SerializeField] private Tilemap[] sourceTilemaps = Array.Empty<Tilemap>();
        [SerializeField, Range(4, 32)] private int pixelsPerUnit = 32;
        [SerializeField] private bool useCustomBounds;
        [SerializeField] private Rect customBounds = new Rect(-10f, -10f, 20f, 20f);
        [SerializeField, Min(0.1f), Tooltip("Narrower passages separate automatic room cores. Reduce for small rooms.")]
        private float roomSeparationRadius = 1f;
        [SerializeField, Min(0f)] private float passageReach = 4f;
        [SerializeField] private GameObject waterVolumePrefab;
        [SerializeField] private Material waterMaterial;
        [SerializeField, Min(0f), Tooltip("Visual expansion in world units on every side of baked water sections. Does not change capacity, swimming colliders or links.")]
        private float visualOverlapOffset;
        [SerializeField, Tooltip("Existing basins can be reused. If empty, the editor bake can generate rooms automatically.")]
        private WaterBasin[] basins = Array.Empty<WaterBasin>();
        [SerializeField, Tooltip("Show the full baked basin capacity, including dry sections, as translucent gizmos.")]
        private bool showVolume;
        [SerializeField, Tooltip("Preview a common water height in Scene View without changing initial water levels or colliders.")]
        private bool showWaterPreview;
        [SerializeField, Range(0f, 100f), Tooltip("Common height from the lowest basin floor (0) to the highest basin ceiling (100). Enable Show Water Preview to display it.")]
        private float waterPreview;
        [SerializeField, HideInInspector] private Transform generatedRoot;
        [SerializeField, HideInInspector] private Rect bakedBounds;
        [SerializeField, HideInInspector] private int columns;
        [SerializeField, HideInInspector] private int rows;
        [SerializeField, HideInInspector] private byte[] solidMask = Array.Empty<byte>();
        private static readonly List<WaterGeometryBaker> active = new List<WaterGeometryBaker>();

        public IReadOnlyList<Tilemap> SourceTilemaps => sourceTilemaps;
        public IReadOnlyList<WaterBasin> Basins => basins;
        public int PixelsPerUnit => pixelsPerUnit;
        public float RoomSeparationRadius => roomSeparationRadius;
        public float PassageReach => passageReach;
        public GameObject WaterVolumePrefab => waterVolumePrefab;
        public Material WaterMaterial => waterMaterial;
        public float VisualOverlapOffset => Mathf.Max(0f, visualOverlapOffset);
        public Transform GeneratedRoot => generatedRoot;
        public Rect BakedBounds => bakedBounds;
        public bool HasBake => columns > 0 && rows > 0 && solidMask.Length == (columns * rows + 7) / 8;
        public bool UseCustomBounds => useCustomBounds;
        public Rect CustomBounds => customBounds;

        private void OnDrawGizmos()
        {
            if ((!showVolume && !showWaterPreview) || basins == null) return;
            float bottom = float.PositiveInfinity;
            float top = float.NegativeInfinity;
            foreach (WaterBasin basin in basins)
            {
                if (basin == null || !basin.gameObject.activeInHierarchy) continue;
                Rect bounds = basin.GeometryBounds;
                if (bounds.width <= 0f || bounds.height <= 0f) continue;
                bottom = Mathf.Min(bottom, bounds.yMin);
                top = Mathf.Max(top, bounds.yMax);
            }
            if (float.IsInfinity(bottom)) return;
            float previewSurface = Mathf.Lerp(bottom, top, waterPreview / 100f);
            for (int index = 0; index < basins.Length; index++)
            {
                WaterBasin basin = basins[index];
                if (basin == null || !basin.gameObject.activeInHierarchy) continue;
                Color color = Color.HSVToRGB(Mathf.Repeat(index * 0.618034f, 1f), 0.65f, 1f);
                color.a = 0.25f;
                if (showVolume) basin.DrawVolumeGizmos(color);
                if (showWaterPreview && !basin.IsSimulationPreviewActive) basin.DrawVolumeGizmos(new Color(0.1f, 0.8f, 1f, 0.55f), previewSurface);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => active.Clear();
        private void OnEnable() { if (!active.Contains(this)) active.Add(this); }
        private void OnDisable() => active.Remove(this);

        public void Configure(Tilemap[] maps, GameObject volumePrefab, Material material, WaterBasin[] existingBasins)
        {
            sourceTilemaps = maps;
            waterVolumePrefab = volumePrefab;
            waterMaterial = material;
            basins = existingBasins;
        }

        public void SetBake(Rect bounds, int width, int height, byte[] mask)
        {
            bakedBounds = bounds;
            columns = width;
            rows = height;
            solidMask = mask;
        }

        public void SetGeneratedBasins(Transform root, WaterBasin[] generatedBasins)
        {
            generatedRoot = root;
            basins = generatedBasins;
        }

        public static WaterGeometryBaker ForPoint(Vector2 point, UnityEngine.SceneManagement.Scene scene)
        {
            WaterGeometryBaker nearest = null;
            float distance = float.PositiveInfinity;
            foreach (WaterGeometryBaker candidate in active)
            {
                if (candidate == null || candidate.gameObject.scene != scene || !candidate.HasBake) continue;
                Vector2 closest = new Vector2(Mathf.Clamp(point.x, candidate.bakedBounds.xMin, candidate.bakedBounds.xMax),
                    Mathf.Clamp(point.y, candidate.bakedBounds.yMin, candidate.bakedBounds.yMax));
                float current = (point - closest).sqrMagnitude;
                if (current >= distance) continue;
                nearest = candidate;
                distance = current;
            }
            return nearest;
        }

        public WaterBasin BasinAt(Vector2 point)
        {
            foreach (WaterBasin basin in basins)
                if (basin != null && basin.isActiveAndEnabled && basin.ContainsGeometryPoint(point)) return basin;
            return null;
        }

        private bool IsSolid(int x, int y)
        {
            if (x < 0 || y < 0 || x >= columns || y >= rows) return false;
            int index = y * columns + x;
            return (solidMask[index >> 3] & (1 << (index & 7))) != 0;
        }

        public float HorizontalTravel(Rect bounds, float requested)
        {
            if (!HasBake || Mathf.Approximately(requested, 0f)) return 0f;
            float dx = bakedBounds.width / columns;
            float dy = bakedBounds.height / rows;
            int firstY = Mathf.Max(0, Mathf.FloorToInt((bounds.yMin + 0.001f - bakedBounds.yMin) / dy));
            int lastY = Mathf.Min(rows - 1, Mathf.FloorToInt((bounds.yMax - 0.001f - bakedBounds.yMin) / dy));
            float minimum = requested > 0f ? bounds.xMax : bounds.xMin + requested;
            float maximum = requested > 0f ? bounds.xMax + requested : bounds.xMin;
            int firstX = Mathf.Max(0, Mathf.FloorToInt((minimum - bakedBounds.xMin) / dx));
            int lastX = Mathf.Min(columns - 1, Mathf.FloorToInt((maximum - bakedBounds.xMin) / dx));
            float allowed = Mathf.Abs(requested);
            for (int y = firstY; y <= lastY; y++)
                for (int x = firstX; x <= lastX; x++)
                {
                    if (!IsSolid(x, y)) continue;
                    float distance = requested > 0f ? bakedBounds.xMin + x * dx - bounds.xMax
                        : bounds.xMin - (bakedBounds.xMin + (x + 1) * dx);
                    allowed = Mathf.Min(allowed, Mathf.Max(0f, distance));
                }
            return Mathf.Sign(requested) * allowed;
        }

        public float DownwardTravel(Rect bounds, float requested)
        {
            if (!HasBake || requested <= 0f) return 0f;
            float cellWidth = bakedBounds.width / columns;
            float cellHeight = bakedBounds.height / rows;
            int firstX = Mathf.Max(0, Mathf.FloorToInt((bounds.xMin + 0.001f - bakedBounds.xMin) / cellWidth));
            int lastX = Mathf.Min(columns - 1, Mathf.FloorToInt((bounds.xMax - 0.001f - bakedBounds.xMin) / cellWidth));
            int firstY = Mathf.Max(0, Mathf.FloorToInt((bounds.yMin - requested - bakedBounds.yMin) / cellHeight));
            int lastY = Mathf.Min(rows - 1, Mathf.FloorToInt((bounds.yMin + 0.001f - bakedBounds.yMin) / cellHeight));
            float allowed = requested;
            for (int y = firstY; y <= lastY; y++)
                for (int x = firstX; x <= lastX; x++)
                {
                    if (!IsSolid(x, y)) continue;
                    float top = bakedBounds.yMin + (y + 1) * cellHeight;
                    allowed = Mathf.Min(allowed, Mathf.Max(0f, bounds.yMin - top));
                }
            return allowed;
        }

        private void OnDrawGizmosSelected()
        {
            Color previousColor = Gizmos.color;
            if (HasBake)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
                Gizmos.DrawWireCube(bakedBounds.center, bakedBounds.size);
            }
            if (useCustomBounds)
            {
                Gizmos.color = new Color(1f, 0.65f, 0.15f, 1f);
                Gizmos.DrawWireCube(customBounds.center, customBounds.size);
            }
            Gizmos.color = previousColor;
        }
    }
}
