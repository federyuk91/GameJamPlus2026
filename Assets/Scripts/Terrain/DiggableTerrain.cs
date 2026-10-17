using System;
using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    public sealed class DiggableTerrain : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D area;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Material maskedMaterial;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField, Range(4, 32)] private int cellsPerUnit = 16;
        [SerializeField] private int foregroundSortingOrder = 10;
        private readonly List<BoxCollider2D> colliders = new List<BoxCollider2D>();
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        private bool[] solid;
        private bool[] visited;
        private Color32[] pixels;
        private Texture2D mask;
        private Material material;
        private Material originalMaterial;
        private int originalSortingOrder;
        private bool originalColliderEnabled;
        private Rect rect;
        private int width;
        private int height;
        private Vector2 cellSize;
        private DiggingSettings Settings => physicsSettings != null ? physicsSettings.digging : PhysicsSettings.DefaultDigging;
        public float CellWorldRadius => Vector2.Scale(cellSize,
            new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))).magnitude * 0.5f;
        private static readonly int MaskId = Shader.PropertyToID("_DigMask");
        private static readonly int RectId = Shader.PropertyToID("_DigRect");
        private static readonly int MatrixId = Shader.PropertyToID("_DigWorldToLocal");
        private static readonly int AlphaId = Shader.PropertyToID("_DugAlpha");
        private static readonly int TintId = Shader.PropertyToID("_DugTint");

        private void Awake()
        {
            if (area == null || visual == null || maskedMaterial == null)
            {
                Debug.LogError("DiggableTerrain: assegnare Area, Visual e Masked Material.", this);
                enabled = false;
                return;
            }
            rect = new Rect(area.offset - area.size * 0.5f, area.size);
            Vector3 scale = transform.lossyScale;
            width = Mathf.Clamp(Mathf.CeilToInt(rect.width * Mathf.Abs(scale.x) * cellsPerUnit), 1, 512);
            height = Mathf.Clamp(Mathf.CeilToInt(rect.height * Mathf.Abs(scale.y) * cellsPerUnit), 1, 512);
            cellSize = new Vector2(rect.width / width, rect.height / height);
            solid = new bool[width * height];
            visited = new bool[solid.Length];
            pixels = new Color32[solid.Length];
            for (int i = 0; i < solid.Length; i++)
            {
                solid[i] = true;
                pixels[i] = new Color32(255, 255, 255, 255);
            }
            mask = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = name + " excavation mask",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            mask.SetPixels32(pixels);
            mask.Apply(false, false);
            originalMaterial = visual.sharedMaterial;
            originalSortingOrder = visual.sortingOrder;
            originalColliderEnabled = area.enabled;
            material = new Material(maskedMaterial);
            material.SetTexture(MaskId, mask);
            material.SetVector(RectId, new Vector4(rect.xMin, rect.yMin, 1f / rect.width, 1f / rect.height));
            visual.sharedMaterial = material;
            visual.sortingOrder = foregroundSortingOrder;
            UpdateMaterial();
            area.enabled = false;
            RebuildColliders();
        }

        private void LateUpdate() => UpdateMaterial();

        private void UpdateMaterial()
        {
            if (material == null) return;
            material.SetMatrix(MatrixId, transform.worldToLocalMatrix);
            material.SetFloat(AlphaId, Mathf.Clamp(Settings.excavatedAlpha, 0, 255) / 255f);
            material.SetColor(TintId, Settings.excavatedTint);
        }

        public bool Intersects(Vector2 center, float radius)
        {
            if (solid == null) return false;
            Vector2 local = transform.InverseTransformPoint(center);
            Vector2 closest = new Vector2(Mathf.Clamp(local.x, rect.xMin, rect.xMax),
                Mathf.Clamp(local.y, rect.yMin, rect.yMax));
            return ((Vector2)transform.TransformPoint(closest) - center).sqrMagnitude <= radius * radius;
        }

        public bool HasEarth(Vector2 start, Vector2 end, float radius)
        {
            return Paint(start, end, radius, false);
        }

        public void Excavate(Vector2 start, Vector2 end, float radius)
        {
            if (!Paint(start, end, radius, true)) return;
            mask.SetPixels32(pixels);
            mask.Apply(false, false);
            RebuildColliders();
            Physics2D.SyncTransforms();
        }

        private bool Paint(Vector2 start, Vector2 end, float radius, bool remove)
        {
            if (solid == null) return false;
            Vector2 a = transform.InverseTransformPoint(start);
            Vector2 b = transform.InverseTransformPoint(end);
            Vector3 scale = transform.lossyScale;
            float rx = radius / Mathf.Max(0.0001f, Mathf.Abs(scale.x));
            float ry = radius / Mathf.Max(0.0001f, Mathf.Abs(scale.y));
            int minX = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x, b.x) - rx - rect.xMin) / cellSize.x), 0, width - 1);
            int maxX = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.x, b.x) + rx - rect.xMin) / cellSize.x), 0, width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.y, b.y) - ry - rect.yMin) / cellSize.y), 0, height - 1);
            int maxY = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.y, b.y) + ry - rect.yMin) / cellSize.y), 0, height - 1);
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            bool changed = false;
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int index = y * width + x;
                if (!solid[index]) continue;
                Vector2 local = new Vector2(rect.xMin + (x + 0.5f) * cellSize.x, rect.yMin + (y + 0.5f) * cellSize.y);
                Vector2 point = transform.TransformPoint(local);
                float t = lengthSquared > 0.000001f ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared) : 0f;
                if ((point - (start + segment * t)).sqrMagnitude > radius * radius) continue;
                if (!remove) return true;
                solid[index] = false;
                pixels[index] = new Color32(0, 0, 0, 255);
                changed = true;
            }
            return changed;
        }

        private void RebuildColliders()
        {
            Array.Clear(visited, 0, visited.Length);
            int used = 0;
            // Merge filled rectangles rather than creating one physics shape per pixel.
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (!solid[index] || visited[index]) continue;
                int right = x;
                while (right < width && solid[y * width + right] && !visited[y * width + right]) right++;
                int top = y + 1;
                while (top < height && RowAvailable(x, right, top)) top++;
                for (int row = y; row < top; row++)
                for (int col = x; col < right; col++) visited[row * width + col] = true;
                if (used == colliders.Count) colliders.Add(gameObject.AddComponent<BoxCollider2D>());
                BoxCollider2D collider = colliders[used++];
                collider.isTrigger = false;
                collider.sharedMaterial = area.sharedMaterial;
                collider.size = new Vector2((right - x) * cellSize.x, (top - y) * cellSize.y);
                collider.offset = new Vector2(rect.xMin + (x + right) * 0.5f * cellSize.x,
                    rect.yMin + (y + top) * 0.5f * cellSize.y);
                collider.enabled = true;
            }
            for (int i = used; i < colliders.Count; i++) colliders[i].enabled = false;
        }

        private bool RowAvailable(int left, int right, int y)
        {
            for (int x = left; x < right; x++)
                if (!solid[y * width + x] || visited[y * width + x]) return false;
            return true;
        }

        public bool BlockedByOtherSolid(SimulatedBody2D body, Vector2 displacement, out Vector2 allowed)
        {
            allowed = displacement;
            float distance = displacement.magnitude;
            if (distance <= 0.00001f) return false;
            Physics2D.SyncTransforms();
            Vector2 direction = displacement / distance;
            Bounds bounds = body.Shape.bounds;
            Physics2D.BoxCast(bounds.center, bounds.size, 0f, direction, body.SolidFilter, hits, distance + body.Skin);
            float nearest = distance;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(body.transform)) continue;
                if (Vector2.Dot(hit.normal, direction) >= -0.01f) continue;
                nearest = Mathf.Min(nearest, Mathf.Max(0f, hit.distance - body.Skin));
            }
            allowed = direction * nearest;
            return nearest < distance;
        }

        private void OnEnable()
        {
            if (solid == null) return;
            area.enabled = false;
            RebuildColliders();
        }

        private void OnDisable()
        {
            foreach (BoxCollider2D collider in colliders)
                if (collider != null) collider.enabled = false;
            if (solid != null && area != null) area.enabled = originalColliderEnabled;
        }

        private void OnDestroy()
        {
            if (visual != null && material != null)
            {
                visual.sharedMaterial = originalMaterial;
                visual.sortingOrder = originalSortingOrder;
            }
            foreach (BoxCollider2D collider in colliders)
                if (collider != null) Destroy(collider);
            if (material != null) Destroy(material);
            if (mask != null) Destroy(mask);
        }
    }
}
