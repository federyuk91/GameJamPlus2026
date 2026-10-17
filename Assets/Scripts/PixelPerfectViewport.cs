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

        private Vector2Int lastScreenSize;
        private PixelPerfectCamera.CropFrame originalCropFrame;
        private Rect visibleGameRect;

        public event Action ViewportChanged;
        public Rect FrameRect { get; private set; }
        public float DisplayScale { get; private set; }
        public Vector2Int ReferenceResolution => new Vector2Int(
            pixelPerfectCamera.refResolutionX, pixelPerfectCamera.refResolutionY);
        public int VerticalBorderPixels => verticalBorderPixels;

        private void OnEnable()
        {
            if (pixelPerfectCamera == null)
                pixelPerfectCamera = GetComponent<PixelPerfectCamera>();

            originalCropFrame = pixelPerfectCamera.cropFrame;
            ApplyViewport();
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
            bool belowReference = Screen.width < pixelPerfectCamera.refResolutionX
                || Screen.height < pixelPerfectCamera.refResolutionY;

            // Below 1x, preserve the full game frame rather than cropping it; some detail is inevitably lost.
            pixelPerfectCamera.cropFrame = belowReference
                ? PixelPerfectCamera.CropFrame.StretchFill
                : PixelPerfectCamera.CropFrame.Windowbox;

            float scale = Mathf.Min((float)Screen.width / pixelPerfectCamera.refResolutionX,
                (float)Screen.height / pixelPerfectCamera.refResolutionY);
            if (!belowReference)
                scale = Mathf.Floor(scale);

            float frameWidth = pixelPerfectCamera.refResolutionX * scale;
            float frameHeight = pixelPerfectCamera.refResolutionY * scale;
            DisplayScale = scale;
            FrameRect = new Rect(Mathf.Floor((Screen.width - frameWidth) * 0.5f),
                Mathf.Floor((Screen.height - frameHeight) * 0.5f), frameWidth, frameHeight);
            float borderHeight = Mathf.Clamp(verticalBorderPixels, 0,
                pixelPerfectCamera.refResolutionY / 2) * scale;
            visibleGameRect = new Rect(
                Mathf.Floor((Screen.width - frameWidth) * 0.5f),
                Mathf.Floor((Screen.height - frameHeight) * 0.5f) + borderHeight,
                frameWidth, frameHeight - 2f * borderHeight);
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
                pixelPerfectCamera.cropFrame = originalCropFrame;
        }
    }
}
