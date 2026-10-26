using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MousePlatformer
{
    [ExecuteAlways, DefaultExecutionOrder(300), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MagnifyingLens2D : MonoBehaviour
    {
        public const string BackgroundLayer = "Lens Background";
        private static readonly int BackgroundTexture = Shader.PropertyToID("_LensBackgroundTexture");
        [SerializeField] private SpriteRenderer lensRenderer;
        [SerializeField] private Material magnifyingMaterial;
        [SerializeField, Tooltip("Optional gameplay camera. If unassigned, uses the MainCamera rendering this scene, without a scene search.")]
        private Camera viewCamera;
        [SerializeField, Range(1f, 5f)] private float magnification = 2f;
        [SerializeField] private Vector2 lensCenterUV = new Vector2(0.40608f, 0.69966f);
        [SerializeField, Tooltip("Half width and height of the circular distortion area, as fractions of the sprite dimensions. Lens Distortion is adjusted on the material.")]
        private Vector2 lensRadiusUV = new Vector2(0.5f, 0.5f);
        [SerializeField, Range(0.01f, 1f)] private float greenDominance = 0.2f;
        [SerializeField, HideInInspector] private Material previousMaterial;

        private sealed class CaptureTarget
        {
            public Renderer Renderer;
            public readonly List<SortingGroup> Groups = new List<SortingGroup>();
        }

        private readonly struct SortEntry
        {
            public readonly Object Source;
            public readonly int Layer;
            public readonly int Order;
            public readonly int Queue;
            public readonly Vector3 Position;

            public SortEntry(Object source, int layer, int order, int queue, Vector3 position)
            {
                Source = source;
                Layer = SortingLayer.GetLayerValueFromID(layer);
                Order = order;
                Queue = queue;
                Position = position;
            }
        }

        private readonly List<GameObject> roots = new List<GameObject>();
        private readonly List<Renderer> discovered = new List<Renderer>();
        private readonly List<CaptureTarget> targets = new List<CaptureTarget>();
        private readonly Dictionary<Renderer, CaptureTarget> targetCache = new Dictionary<Renderer, CaptureTarget>();
        private readonly List<Renderer> hidden = new List<Renderer>();
        private readonly List<SortEntry> lensPath = new List<SortEntry>();
        private readonly List<SortEntry> candidatePath = new List<SortEntry>();
        private readonly Dictionary<Camera, RenderTexture> textures = new Dictionary<Camera, RenderTexture>();
        private readonly UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
        private Camera captureCamera;
        private Camera automaticCamera;
        private UniversalAdditionalCameraData captureData;
        private MaterialPropertyBlock properties;
        private CaptureTarget lensTarget;
        private bool capturing;

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
            automaticCamera = null;
            if (lensRenderer == null) lensRenderer = GetComponent<SpriteRenderer>();
            RenderPipelineManager.beginCameraRendering += BeginCamera;
#if UNITY_EDITOR
            UnityEditor.SceneView.duringSceneGui += DuringSceneGui;
#endif
            ApplySettings();
        }

        private void LateUpdate()
        {
            Camera source = viewCamera != null ? viewCamera : automaticCamera;
            if (source != null) Capture(source);
        }

#if UNITY_EDITOR
        private void DuringSceneGui(UnityEditor.SceneView sceneView)
        {
            if (Event.current.type == EventType.Repaint && sceneView.camera != null) Capture(sceneView.camera);
        }
#endif

        public void ApplySettings()
        {
            if (lensRenderer == null || magnifyingMaterial == null || lensRenderer.sprite == null) return;
            if (lensRenderer.sharedMaterial != magnifyingMaterial)
            {
                previousMaterial = lensRenderer.sharedMaterial;
                lensRenderer.sharedMaterial = magnifyingMaterial;
            }
            if (properties == null) properties = new MaterialPropertyBlock();
            lensRenderer.GetPropertyBlock(properties);
            Sprite sprite = lensRenderer.sprite;
            Vector2 center = (Vector2.Scale(lensCenterUV, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
            properties.SetVector("_LensCenter", new Vector4(center.x, center.y, 0f, 0f));
            Vector2 radius = Vector2.Scale(lensRadiusUV, sprite.rect.size) / sprite.pixelsPerUnit;
            properties.SetVector("_LensRadius", new Vector4(Mathf.Max(0.0001f, radius.x), Mathf.Max(0.0001f, radius.y), 0f, 0f));
            properties.SetFloat("_Magnification", magnification);
            properties.SetFloat("_GreenDominance", greenDominance);
            lensRenderer.SetPropertyBlock(properties);
        }

        private void RefreshTargets()
        {
            targets.Clear();
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded) continue;
                scene.GetRootGameObjects(roots);
                foreach (GameObject root in roots)
                {
                    root.GetComponentsInChildren(true, discovered);
                    foreach (Renderer renderer in discovered)
                    {
                        if (!targetCache.TryGetValue(renderer, out CaptureTarget target))
                        {
                            target = new CaptureTarget { Renderer = renderer };
                            targetCache.Add(renderer, target);
                        }
                        renderer.GetComponentsInParent(true, target.Groups);
                        targets.Add(target);
                    }
                }
            }
            if (lensTarget == null) lensTarget = new CaptureTarget { Renderer = lensRenderer };
            lensRenderer.GetComponentsInParent(true, lensTarget.Groups);
        }

        private static void BuildPath(CaptureTarget target, List<SortEntry> path)
        {
            path.Clear();
            Renderer renderer = target.Renderer;
            Vector3 position = renderer.bounds.center;
            if (renderer is SpriteRenderer sprite && sprite.spriteSortPoint == SpriteSortPoint.Pivot) position = sprite.transform.position;
            Material material = renderer.sharedMaterial;
            path.Add(new SortEntry(renderer, renderer.sortingLayerID, renderer.sortingOrder, material != null ? material.renderQueue : 3000, position));
            foreach (SortingGroup group in target.Groups)
            {
                if (group == null || !group.isActiveAndEnabled) continue;
                path.Add(new SortEntry(group, group.sortingLayerID, group.sortingOrder, 3000, group.transform.position));
                if (group.sortAtRoot) break;
            }
            path.Reverse();
        }

        private bool IsBehind(Camera camera)
        {
            int count = Mathf.Min(candidatePath.Count, lensPath.Count);
            for (int index = 0; index < count; index++)
            {
                SortEntry candidate = candidatePath[index];
                SortEntry lens = lensPath[index];
                if (candidate.Source == lens.Source) continue;
                if (candidate.Layer != lens.Layer) return candidate.Layer < lens.Layer;
                if (candidate.Order != lens.Order) return candidate.Order < lens.Order;
                if (candidate.Queue != lens.Queue) return candidate.Queue < lens.Queue;
                // Children of the same group share its distance for transparent sorting.
                if (index > 0) return false;
                return SortDistance(camera, candidate.Position) > SortDistance(camera, lens.Position) + 0.0001f;
            }
            return false;
        }

        private static float SortDistance(Camera camera, Vector3 position)
        {
            if (camera.transparencySortMode == TransparencySortMode.CustomAxis)
                return Vector3.Dot(position, camera.transparencySortAxis);
            if (camera.transparencySortMode == TransparencySortMode.Perspective
                || (camera.transparencySortMode == TransparencySortMode.Default && !camera.orthographic))
                return (position - camera.transform.position).sqrMagnitude;
            return Vector3.Dot(position - camera.transform.position, camera.transform.forward);
        }

        private void PrepareCamera(Camera source)
        {
            if (captureCamera == null)
            {
                GameObject cameraObject = new GameObject("Lens Capture Camera") { hideFlags = HideFlags.HideAndDontSave };
                captureCamera = cameraObject.AddComponent<Camera>();
                captureData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                captureData.renderType = CameraRenderType.Base;
                captureData.renderPostProcessing = false;
                captureData.requiresColorOption = CameraOverrideOption.Off;
                captureData.requiresDepthOption = CameraOverrideOption.Off;
                captureData.allowXRRendering = false;
            }
            captureCamera.CopyFrom(source);
            captureCamera.enabled = false;
            captureCamera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            captureCamera.rect = new Rect(0f, 0f, 1f, 1f);
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = source.backgroundColor;
            captureCamera.allowMSAA = false;
            captureCamera.targetTexture = null;
            captureCamera.projectionMatrix = source.projectionMatrix;
            captureCamera.worldToCameraMatrix = source.worldToCameraMatrix;
        }

        private RenderTexture TextureFor(Camera source)
        {
            int width = Mathf.Max(1, source.scaledPixelWidth);
            int height = Mathf.Max(1, source.scaledPixelHeight);
            textures.TryGetValue(source, out RenderTexture texture);
            if (texture != null && texture.width == width && texture.height == height) return texture;
            if (texture != null) { texture.Release(); DestroyResource(texture); }
            texture = new RenderTexture(width, height, 24, source.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default)
            {
                name = "Lens Background Capture",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            texture.Create();
            textures[source] = texture;
            return texture;
        }

        private void Capture(Camera source)
        {
            if (capturing || lensRenderer == null || !lensRenderer.enabled || magnifyingMaterial == null
                || GraphicsSettings.currentRenderPipeline == null || !gameObject.activeInHierarchy
                || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
            PrepareCamera(source);
            if (!RenderPipeline.SupportsRenderRequest(captureCamera, request)) return;
            RefreshTargets();
            BuildPath(lensTarget, lensPath);
            RenderTexture texture = TextureFor(source);
            capturing = true;
            try
            {
                foreach (CaptureTarget target in targets)
                {
                    Renderer renderer = target.Renderer;
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff) continue;
                    BuildPath(target, candidatePath);
                    if (renderer != lensRenderer && IsBehind(source)) continue;
                    hidden.Add(renderer);
                    renderer.forceRenderingOff = true;
                }
                request.destination = texture;
                // SubmitRenderRequest completes synchronously. Restore visibility before the main camera renders.
                RenderPipeline.SubmitRenderRequest(captureCamera, request);
            }
            finally
            {
                foreach (Renderer renderer in hidden)
                    if (renderer != null) renderer.forceRenderingOff = false;
                hidden.Clear();
                capturing = false;
            }
            ApplySettings();
            Bind(texture);
        }

        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (capturing || lensRenderer == null) return;
            if (viewCamera == null && camera.cameraType == CameraType.Game && camera.CompareTag("MainCamera")
                && camera.gameObject.scene == gameObject.scene) automaticCamera = camera;
            if (textures.TryGetValue(camera, out RenderTexture texture)) Bind(texture);
            else if (camera == viewCamera || camera == automaticCamera || camera.cameraType == CameraType.SceneView)
            {
                if (properties == null) properties = new MaterialPropertyBlock();
                lensRenderer.GetPropertyBlock(properties);
                properties.SetFloat("_LensCaptureReady", 0f);
                lensRenderer.SetPropertyBlock(properties);
            }
        }

        private void Bind(RenderTexture texture)
        {
            if (properties == null) properties = new MaterialPropertyBlock();
            lensRenderer.GetPropertyBlock(properties);
            properties.SetTexture(BackgroundTexture, texture);
            properties.SetFloat("_LensCaptureReady", 1f);
            lensRenderer.SetPropertyBlock(properties);
        }

        private static void DestroyResource(Object resource)
        {
            if (Application.isPlaying) Destroy(resource);
            else DestroyImmediate(resource);
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
#if UNITY_EDITOR
            UnityEditor.SceneView.duringSceneGui -= DuringSceneGui;
#endif
            foreach (RenderTexture texture in textures.Values)
            {
                if (texture == null) continue;
                texture.Release();
                DestroyResource(texture);
            }
            textures.Clear();
            targetCache.Clear();
            targets.Clear();
            lensTarget = null;
            if (captureCamera != null) DestroyResource(captureCamera.gameObject);
            captureCamera = null;
            captureData = null;
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
