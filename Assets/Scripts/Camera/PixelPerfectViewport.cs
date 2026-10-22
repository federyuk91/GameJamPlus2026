using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MousePlatformer
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(PixelPerfectCamera))]
    public sealed class PixelPerfectViewport : MonoBehaviour
    {
        [SerializeField] private PixelPerfectCamera pixelPerfectCamera;
        [SerializeField, Min(0)] private int verticalBorderPixels = 20;
        [SerializeField, Min(-1)] private int bottomBorderPixels = -1;

        private Vector2Int lastScreenSize;
        private PixelPerfectCamera.CropFrame originalCropFrame;
        private Vector2Int originalReferenceResolution;
        private int zoomLevel;
        private Rect visibleGameRect;

        public event Action ViewportChanged;
        public Rect FrameRect { get; private set; }
        public float DisplayScale { get; private set; }
        public Vector2Int ReferenceResolution => originalReferenceResolution.x > 0 ? originalReferenceResolution
            : new Vector2Int(pixelPerfectCamera.refResolutionX, pixelPerfectCamera.refResolutionY);
        public int ZoomLevel => zoomLevel;
        public int VerticalBorderPixels => verticalBorderPixels;
        public int BottomBorderPixels => bottomBorderPixels < 0 ? verticalBorderPixels : bottomBorderPixels;

        private void OnEnable()
        {
            if (pixelPerfectCamera == null)
                pixelPerfectCamera = GetComponent<PixelPerfectCamera>();

            originalCropFrame = pixelPerfectCamera.cropFrame;
            originalReferenceResolution = new Vector2Int(pixelPerfectCamera.refResolutionX, pixelPerfectCamera.refResolutionY);
            zoomLevel = 0;
            ApplyViewport();
        }

        public void StepZoom(int direction)
        {
            if (direction != 0) SetZoomLevel(zoomLevel + (direction > 0 ? 1 : -1));
        }

        public void SetZoomLevel(int level)
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            int nextLevel = Mathf.Clamp(level, -1, 1);
            if (nextLevel < 0 && NormalPixelScale() <= 1) nextLevel = 0;
            if (nextLevel == zoomLevel) return;
            zoomLevel = nextLevel;
            ApplyViewport();
        }

        private int NormalPixelScale()
        {
            Vector2Int reference = ReferenceResolution;
            return Mathf.Max(1, Mathf.Min(Screen.width / reference.x, Screen.height / reference.y));
        }

        private void LateUpdate()
        {
            // Unity has no cross-platform window resize callback; only reconfigure when dimensions change.
            if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
                ApplyViewport();
        }

        private void ApplyViewport()
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            Vector2Int reference = ReferenceResolution;
            bool belowReference = Screen.width < reference.x || Screen.height < reference.y;
            if (zoomLevel < 0 && NormalPixelScale() <= 1) zoomLevel = 0;

            // Below 1x, preserve the full game frame rather than cropping it; some detail is inevitably lost.
            if (zoomLevel == 0)
            {
                pixelPerfectCamera.refResolutionX = reference.x;
                pixelPerfectCamera.refResolutionY = reference.y;
                pixelPerfectCamera.cropFrame = belowReference
                    ? PixelPerfectCamera.CropFrame.StretchFill : PixelPerfectCamera.CropFrame.Windowbox;
            }
            else
            {
                int pixelScale = Mathf.Max(1, NormalPixelScale() + zoomLevel);
                // Integer scale, full-screen rendering; the existing masks keep the HUD frame fixed.
                pixelPerfectCamera.refResolutionX = Mathf.Max(1, Screen.width / pixelScale);
                pixelPerfectCamera.refResolutionY = Mathf.Max(1, Screen.height / pixelScale);
                pixelPerfectCamera.cropFrame = PixelPerfectCamera.CropFrame.None;
            }

            float scale = Mathf.Min((float)Screen.width / reference.x,
                (float)Screen.height / reference.y);
            if (!belowReference)
                scale = Mathf.Floor(scale);

            float frameWidth = reference.x * scale;
            float frameHeight = reference.y * scale;
            DisplayScale = scale;
            FrameRect = new Rect(Mathf.Floor((Screen.width - frameWidth) * 0.5f),
                Mathf.Floor((Screen.height - frameHeight) * 0.5f), frameWidth, frameHeight);
            float topBorderHeight = Mathf.Clamp(verticalBorderPixels, 0,
                reference.y / 2) * scale;
            float bottomBorderHeight = Mathf.Clamp(BottomBorderPixels * scale, 0f,
                frameHeight - topBorderHeight);
            visibleGameRect = new Rect(
                Mathf.Floor((Screen.width - frameWidth) * 0.5f),
                Mathf.Floor((Screen.height - frameHeight) * 0.5f) + topBorderHeight,
                frameWidth, frameHeight - topBorderHeight - bottomBorderHeight);
            ViewportChanged?.Invoke();
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // Edit Mode updates are not continuous; the Game View repaint must also refresh resized borders.
            if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
                ApplyViewport();

            // Mask the reserved rows after camera rendering, without changing the pixel grid or intercepting input.
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.color = Color.white;
            GUI.depth = int.MinValue;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, visibleGameRect.yMin), Texture2D.blackTexture);
            GUI.DrawTexture(new Rect(0f, visibleGameRect.yMax, Screen.width,
                Screen.height - visibleGameRect.yMax), Texture2D.blackTexture);
            GUI.DrawTexture(new Rect(0f, visibleGameRect.yMin, visibleGameRect.xMin,
                visibleGameRect.height), Texture2D.blackTexture);
            GUI.DrawTexture(new Rect(visibleGameRect.xMax, visibleGameRect.yMin,
                Screen.width - visibleGameRect.xMax, visibleGameRect.height), Texture2D.blackTexture);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        private void OnDisable()
        {
            if (pixelPerfectCamera != null)
            {
                pixelPerfectCamera.cropFrame = originalCropFrame;
                if (originalReferenceResolution.x > 0)
                {
                    pixelPerfectCamera.refResolutionX = originalReferenceResolution.x;
                    pixelPerfectCamera.refResolutionY = originalReferenceResolution.y;
                }
            }
        }
    }
}
