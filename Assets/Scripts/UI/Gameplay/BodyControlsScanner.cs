using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [UxmlElement]
    public sealed partial class BodyControlsScanner : VisualElement
    {
        private static readonly int SourceRectId = Shader.PropertyToID("_SourceRect");
        private static readonly int ContentRectId = Shader.PropertyToID("_ContentRect");
        private static readonly int ScanPositionId = Shader.PropertyToID("_ScanPosition");
        private static readonly int ScanDirectionId = Shader.PropertyToID("_ScanDirection");
        private static readonly int BandWidthId = Shader.PropertyToID("_BandWidth");
        private static readonly int PixelOffsetId = Shader.PropertyToID("_PixelOffset");
        private static readonly int LineWidthId = Shader.PropertyToID("_LineWidth");
        private static readonly int LineColorId = Shader.PropertyToID("_LineColor");
        private IVisualElementScheduledItem animation;
        private Material material;
        private RenderTexture target;
        private double startTime;
        private Texture source;
        private Rect sourceRect;
        private Vector2 canvasSize;
        private Vector2 contentOffset;

        [UxmlAttribute] public bool ScanEnabled { get; set; } = true;
        [UxmlAttribute] public Sprite ScanSprite { get; set; }
        [UxmlAttribute] public float PixelOffset { get; set; } = 4f;
        [UxmlAttribute] public float ScanDuration { get; set; } = 3f;
        [UxmlAttribute] public float ScanAngle { get; set; } = 15f;
        [UxmlAttribute] public float PauseDuration { get; set; } = 0.8f;
        [UxmlAttribute] public float BandWidth { get; set; } = 0.18f;
        [UxmlAttribute] public float LineWidth { get; set; } = 0.008f;
        [UxmlAttribute] public Color LineColor { get; set; } = new Color(240f / 255f, 229f / 255f, 202f / 255f, 0.7f);

        public BodyControlsScanner()
        {
            pickingMode = PickingMode.Ignore;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        private void OnAttach(AttachToPanelEvent attachEvent)
        {
            startTime = Time.realtimeSinceStartupAsDouble;
            if (animation == null) animation = schedule.Execute(Tick).Every(33);
            else animation.Resume();
        }

        private void Tick()
        {
            // Real time keeps the presentation running while gameplay is paused.
            if (!ScanEnabled)
            {
                style.backgroundImage = new StyleBackground(StyleKeyword.None);
                return;
            }
            for (VisualElement ancestor = this; ancestor != null; ancestor = ancestor.parent)
                if (ancestor.resolvedStyle.display == DisplayStyle.None || !ancestor.visible) return;
            if (parent == null || parent.contentRect.width <= 0f || parent.contentRect.height <= 0f) return;
            Background background = parent.resolvedStyle.backgroundImage;
            Sprite sprite = ScanSprite != null ? ScanSprite : background.sprite;
            Texture texture = sprite != null ? sprite.texture
                : background.texture != null ? (Texture)background.texture : background.renderTexture;
            if (texture == null) return;
            Rect rect = sprite != null ? sprite.textureRect
                : new Rect(0f, 0f, texture.width, texture.height);
            Vector2 size = sprite != null ? sprite.rect.size : rect.size;
            Vector2 offset = sprite != null ? sprite.textureRectOffset : Vector2.zero;
            if (!Prepare(texture, rect, size, offset)) return;
            float duration = Mathf.Max(0.1f, ScanDuration);
            float pause = Mathf.Max(0f, PauseDuration);
            float elapsed = (float)((Time.realtimeSinceStartupAsDouble - startTime) % (duration + pause));
            float width = Mathf.Clamp(BandWidth, 0.01f, 1f);
            float pixelOffset = Mathf.Max(0f, PixelOffset) / size.y;
            float radians = ScanAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(-Mathf.Sin(radians) * size.x / size.y, Mathf.Cos(radians));
            float extent = (Mathf.Abs(direction.x) + Mathf.Abs(direction.y)) * 0.5f;
            float margin = width + pixelOffset;
            float minimum = 0.5f - extent - margin;
            float maximum = 0.5f + extent + margin;
            float position = elapsed < duration ? Mathf.Lerp(minimum, maximum, elapsed / duration) : maximum;
            material.SetFloat(ScanPositionId, position);
            material.SetVector(ScanDirectionId, new Vector4(direction.x, direction.y, 0f, 0f));
            material.SetFloat(BandWidthId, width);
            material.SetFloat(PixelOffsetId, pixelOffset);
            material.SetFloat(LineWidthId, Mathf.Clamp(LineWidth, 0f, width));
            material.SetColor(LineColorId, LineColor);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, target, material);
            RenderTexture.active = previous;
            style.backgroundImage = new StyleBackground(Background.FromRenderTexture(target));
            MarkDirtyRepaint();
        }

        private bool Prepare(Texture texture, Rect rect, Vector2 size, Vector2 offset)
        {
            if (material == null)
            {
                Shader shader = Resources.Load<Shader>("BodyControlsScanner");
                if (shader == null || !shader.isSupported) return false;
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            if (source == texture && sourceRect == rect && canvasSize == size && contentOffset == offset && target != null) return true;
            ReleaseTarget();
            source = texture;
            sourceRect = rect;
            canvasSize = size;
            contentOffset = offset;
            // Keep the full sprite canvas; stretching a trimmed texture would enlarge the scan overlay.
            target = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(size.x)),
                Mathf.Max(1, Mathf.RoundToInt(size.y)), 0, RenderTextureFormat.ARGB32)
            {
                name = "Body Controls Scanner",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            target.Create();
            material.SetVector(SourceRectId, new Vector4(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height));
            material.SetVector(ContentRectId, new Vector4(offset.x / size.x, offset.y / size.y,
                rect.width / size.x, rect.height / size.y));
            return true;
        }

        private void OnDetach(DetachFromPanelEvent detachEvent)
        {
            animation?.Pause();
            ReleaseTarget();
            DestroyResource(material);
            material = null;
            source = null;
        }

        private void ReleaseTarget()
        {
            style.backgroundImage = new StyleBackground(StyleKeyword.None);
            if (target == null) return;
            target.Release();
            DestroyResource(target);
            target = null;
        }

        private static void DestroyResource(Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) Object.Destroy(resource);
            else Object.DestroyImmediate(resource);
        }
    }
}
