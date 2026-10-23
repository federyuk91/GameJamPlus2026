using GameJamPlus.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PixelPerfectViewport))]
    public sealed class PixelPerfectZoom : MonoBehaviour
    {
        [SerializeField] private PixelPerfectViewport viewport;
        private InputAction scroll;
        private InputAction zoomIn;
        private InputAction zoomOut;

        private void Awake()
        {
            if (viewport == null) viewport = GetComponent<PixelPerfectViewport>();
            scroll = new InputAction("CameraZoomScroll", InputActionType.Value, "<Mouse>/scroll/y");
            zoomIn = new InputAction("CameraZoomIn", InputActionType.Button, "<Gamepad>/leftShoulder");
            zoomOut = new InputAction("CameraZoomOut", InputActionType.Button, "<Gamepad>/leftTrigger");
        }

        private void OnEnable()
        {
            scroll.Enable();
            zoomIn.Enable();
            zoomOut.Enable();
        }

        private void Update()
        {
            GameManager manager = GameManager.Instance;
            if (viewport == null || Time.timeScale <= 0f
                || (manager != null && manager.State != GameManager.LoopState.Playing)) return;
            float wheel = scroll.ReadValue<float>();
            bool closer = wheel > 0f || zoomIn.WasPressedThisFrame();
            bool farther = wheel < 0f || zoomOut.WasPressedThisFrame();
            if (closer != farther) viewport.StepZoom(closer ? 1 : -1);
        }

        private void OnDisable()
        {
            scroll.Disable();
            zoomIn.Disable();
            zoomOut.Disable();
        }

        private void OnDestroy()
        {
            scroll.Dispose();
            zoomIn.Dispose();
            zoomOut.Dispose();
        }
    }
}
