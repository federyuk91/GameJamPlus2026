using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [Serializable]
    public sealed class ExitTunnelSettings
    {
        [SerializeField] private Texture2D image;
        [SerializeField] private Shader shader;
        [SerializeField, Min(0f), Tooltip("Transition duration in seconds unless the ending event supplies an override.")]
        private float duration = 4f;
        [SerializeField, Min(1f)] private float finalZoom = 5f;
        [SerializeField, Range(0f, 16f), Tooltip("Exposure stops: each stop doubles brightness.")]
        private float exposureStops = 1f;
        [SerializeField, Range(0f, 0.94f)] private float whiteoutStart = 0.88f;
        [SerializeField, Range(1f, 6f), Tooltip("Strength of the warm halo around the expanding white opening.")]
        private float lightStrength = 1.25f;

        public Texture2D Image => image;
        public Shader Shader => shader;
        public float Duration => Mathf.Max(0f, duration);
        public float FinalZoom => Mathf.Max(1f, finalZoom);
        public float ExposureStops => exposureStops;
        public float WhiteoutStart => Mathf.Clamp(whiteoutStart, 0f, 0.94f);
        public float LightStrength => Mathf.Max(1f, lightStrength);
    }

    public sealed class ExitTunnelImage : Image, IDisposable
    {
        private readonly ExitTunnelSettings settings;
        private Material material;
        private RenderTexture output;
        private float progress;
        public bool IsAvailable => settings != null && settings.Image != null
            && settings.Shader != null && settings.Shader.isSupported;

        public ExitTunnelImage(ExitTunnelSettings settings)
        {
            this.settings = settings;
            pickingMode = PickingMode.Ignore;
            scaleMode = ScaleMode.StretchToFill;
            style.position = Position.Absolute;
            style.left = style.right = style.top = style.bottom = 0f;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            if (!IsAvailable) return;
            if (material == null)
                material = new Material(settings.Shader) { hideFlags = HideFlags.HideAndDontSave };
            int width = Mathf.Clamp(Screen.width, 1, 1920);
            int height = Mathf.Max(1, Mathf.RoundToInt(width * (float)Screen.height / Mathf.Max(1, Screen.width)));
            if (output == null || output.width != width || output.height != height)
            {
                ReleaseOutput();
                output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    name = "Exit tunnel transition", hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
                };
                output.Create();
                image = output;
            }
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            float targetAspect = contentRect.height > 0f ? contentRect.width / contentRect.height : (float)width / height;
            float sourceAspect = (float)settings.Image.width / settings.Image.height;
            Vector2 crop = targetAspect > sourceAspect
                ? new Vector2(1f, sourceAspect / targetAspect) : new Vector2(targetAspect / sourceAspect, 1f);
            float zoom = Mathf.Lerp(1f, settings.FinalZoom, eased);
            material.SetVector("_Crop", new Vector4(crop.x / zoom, crop.y / zoom, 0f, 0f));
            material.SetFloat("_Exposure", Mathf.Pow(2f, settings.ExposureStops * eased));
            material.SetFloat("_LightStrength", settings.LightStrength);
            material.SetFloat("_Progress", progress);
            material.SetFloat("_ScreenAspect", targetAspect);
            material.SetFloat("_Whiteout", Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(settings.WhiteoutStart, 0.95f, progress)));
            // Blit changes the active render target; restore it before UI Toolkit renders its panel.
            RenderTexture previous = RenderTexture.active;
            try { Graphics.Blit(settings.Image, output, material); }
            finally { RenderTexture.active = previous; }
            MarkDirtyRepaint();
        }

        private void OnGeometryChanged(GeometryChangedEvent change)
        {
            if (output != null) SetProgress(progress);
        }

        private void ReleaseOutput()
        {
            image = null;
            if (output == null) return;
            output.Release();
            UnityEngine.Object.Destroy(output);
            output = null;
        }

        public void Dispose()
        {
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ReleaseOutput();
            if (material != null) UnityEngine.Object.Destroy(material);
            material = null;
            RemoveFromHierarchy();
        }
    }
}
