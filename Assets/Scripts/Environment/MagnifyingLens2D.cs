using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MousePlatformer
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MagnifyingLens2D : MonoBehaviour
    {
        public const string BackgroundLayer = "Lens Background";
        [SerializeField] private SpriteRenderer lensRenderer;
        [SerializeField] private Material magnifyingMaterial;
        [SerializeField] private Camera viewCamera;
        [SerializeField, Range(1f, 5f)] private float magnification = 2f;
        [SerializeField] private Vector2 lensCenterUV = new Vector2(0.40608f, 0.69966f);
        [SerializeField, Range(0.01f, 1f)] private float greenDominance = 0.2f;
        private readonly List<GameObject> roots = new List<GameObject>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<SortingGroup> groups = new List<SortingGroup>();
        private readonly List<Renderer> discoveredRenderers = new List<Renderer>();
        private readonly List<SortingGroup> discoveredGroups = new List<SortingGroup>();
        private readonly List<Renderer> changedRenderers = new List<Renderer>();
        private readonly List<int> rendererLayers = new List<int>();
        private readonly List<int> rendererOrders = new List<int>();
        private readonly List<SortingGroup> changedGroups = new List<SortingGroup>();
        private readonly List<int> groupLayers = new List<int>();
        private readonly List<int> groupOrders = new List<int>();
        private readonly List<BackgroundTarget> backgroundTargets = new List<BackgroundTarget>();

        private struct BackgroundTarget
        {
            public Renderer Renderer;
            public SortingGroup Group;
            public int Layer;
            public int Order;
            public float Distance;
        }
        [SerializeField, HideInInspector] private Material previousMaterial;
        private MaterialPropertyBlock properties;
        private float nextRefresh;

        public Vector3 LensCenterWorld
        {
            get
            {
                if (lensRenderer == null || lensRenderer.sprite == null) return transform.position;
                Sprite sprite = lensRenderer.sprite;
                Vector2 center = (Vector2.Scale(lensCenterUV, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
                if (lensRenderer.flipX) center.x = -center.x;
                if (lensRenderer.flipY) center.y = -center.y;
                return lensRenderer.transform.TransformPoint(center);
            }
        }

        private void OnEnable()
        {
            if (lensRenderer == null) lensRenderer = GetComponent<SpriteRenderer>();
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
            nextRefresh = 0f;
            ApplySettings();
        }

        public void ApplySettings()
        {
            if (lensRenderer == null || magnifyingMaterial == null) return;
            if (lensRenderer.sharedMaterial != magnifyingMaterial)
            {
                previousMaterial = lensRenderer.sharedMaterial;
                lensRenderer.sharedMaterial = magnifyingMaterial;
            }
            Sprite sprite = lensRenderer.sprite;
            if (sprite == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            lensRenderer.GetPropertyBlock(properties);
            Vector2 center = (Vector2.Scale(lensCenterUV, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
            properties.SetVector("_LensCenter", new Vector4(center.x, center.y, 0f, 0f));
            properties.SetFloat("_Magnification", magnification);
            properties.SetFloat("_GreenDominance", greenDominance);
            lensRenderer.SetPropertyBlock(properties);
        }

        private void RefreshRenderers()
        {
            renderers.Clear();
            groups.Clear();
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded) continue;
                scene.GetRootGameObjects(roots);
                foreach (GameObject root in roots)
                {
                    root.GetComponentsInChildren(true, discoveredRenderers);
                    foreach (Renderer candidate in discoveredRenderers)
                        if (candidate.GetComponentInParent<SortingGroup>() == null) renderers.Add(candidate);
                    root.GetComponentsInChildren(true, discoveredGroups);
                    foreach (SortingGroup group in discoveredGroups)
                        if (group.transform.parent == null || group.transform.parent.GetComponentInParent<SortingGroup>() == null)
                            groups.Add(group);
                }
            }
            nextRefresh = Time.realtimeSinceStartup + 0.25f;
        }

        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (lensRenderer == null || !lensRenderer.enabled || magnifyingMaterial == null
                || (camera != viewCamera && camera.cameraType != CameraType.SceneView)) return;
            RestoreLayers();
            ApplySettings();
            if (Time.realtimeSinceStartup >= nextRefresh) RefreshRenderers();
            int backgroundID = SortingLayer.NameToID(BackgroundLayer);
            if (backgroundID == 0) return;
            backgroundTargets.Clear();
            foreach (Renderer candidate in renderers)
            {
                if (candidate == null || candidate == lensRenderer || !candidate.enabled
                    || !BehindLens(candidate.sortingLayerID, candidate.sortingOrder, candidate.transform.position, camera)) continue;
                backgroundTargets.Add(new BackgroundTarget
                {
                    Renderer = candidate, Layer = candidate.sortingLayerID, Order = candidate.sortingOrder,
                    Distance = Vector3.Dot(candidate.transform.position, camera.transform.forward)
                });
            }
            foreach (SortingGroup group in groups)
            {
                if (group == null || !group.enabled || lensRenderer.transform.IsChildOf(group.transform)
                    || !BehindLens(group.sortingLayerID, group.sortingOrder, group.transform.position, camera)) continue;
                backgroundTargets.Add(new BackgroundTarget
                {
                    Group = group, Layer = group.sortingLayerID, Order = group.sortingOrder,
                    Distance = Vector3.Dot(group.transform.position, camera.transform.forward)
                });
            }
            // Flatten only the capture layer, preserving each source layer's original priority.
            backgroundTargets.Sort(CompareTargets);
            for (int index = 0; index < backgroundTargets.Count; index++)
            {
                BackgroundTarget target = backgroundTargets[index];
                int order = Mathf.Clamp(short.MinValue + index, short.MinValue, short.MaxValue);
                if (target.Renderer != null)
                {
                    changedRenderers.Add(target.Renderer);
                    rendererLayers.Add(target.Layer);
                    rendererOrders.Add(target.Order);
                    target.Renderer.sortingLayerID = backgroundID;
                    target.Renderer.sortingOrder = order;
                }
                else
                {
                    changedGroups.Add(target.Group);
                    groupLayers.Add(target.Layer);
                    groupOrders.Add(target.Order);
                    target.Group.sortingLayerID = backgroundID;
                    target.Group.sortingOrder = order;
                }
            }
        }

        private static int CompareTargets(BackgroundTarget left, BackgroundTarget right)
        {
            int layer = SortingLayer.GetLayerValueFromID(left.Layer).CompareTo(SortingLayer.GetLayerValueFromID(right.Layer));
            if (layer != 0) return layer;
            int order = left.Order.CompareTo(right.Order);
            return order != 0 ? order : right.Distance.CompareTo(left.Distance);
        }

        private bool BehindLens(int layer, int order, Vector3 position, Camera camera)
        {
            int layerValue = SortingLayer.GetLayerValueFromID(layer);
            int lensValue = SortingLayer.GetLayerValueFromID(lensRenderer.sortingLayerID);
            if (layerValue != lensValue) return layerValue < lensValue;
            if (order != lensRenderer.sortingOrder) return order < lensRenderer.sortingOrder;
            return Vector3.Dot(position - lensRenderer.transform.position, camera.transform.forward) > 0.0001f;
        }

        private void EndCamera(ScriptableRenderContext context, Camera camera) => RestoreLayers();

        private void RestoreLayers()
        {
            for (int index = 0; index < changedRenderers.Count; index++)
                if (changedRenderers[index] != null)
                {
                    changedRenderers[index].sortingLayerID = rendererLayers[index];
                    changedRenderers[index].sortingOrder = rendererOrders[index];
                }
            for (int index = 0; index < changedGroups.Count; index++)
                if (changedGroups[index] != null)
                {
                    changedGroups[index].sortingLayerID = groupLayers[index];
                    changedGroups[index].sortingOrder = groupOrders[index];
                }
            changedRenderers.Clear();
            rendererLayers.Clear();
            rendererOrders.Clear();
            changedGroups.Clear();
            groupLayers.Clear();
            groupOrders.Clear();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            RestoreLayers();
            if (lensRenderer != null && lensRenderer.sharedMaterial == magnifyingMaterial)
            {
                lensRenderer.sharedMaterial = previousMaterial;
                if (properties != null)
                {
                    properties.Clear();
                    lensRenderer.SetPropertyBlock(properties);
                }
            }
        }
    }
}
