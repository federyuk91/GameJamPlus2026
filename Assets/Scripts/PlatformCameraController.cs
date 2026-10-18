using UnityEngine;
using System;
using GameJamPlus.Gameplay;
using UnityEngine.InputSystem;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class PlatformCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.75f);
        [SerializeField] private Vector2 deadZone = new Vector2(0.75f, 0.5f);
        [SerializeField, Min(0.01f)] private float horizontalSmoothTime = 0.12f;
        [SerializeField, Min(0.01f)] private float verticalSmoothTime = 0.18f;
        [SerializeField, Min(0.01f)] private float fallingSmoothTime = 0.08f;
        [SerializeField, Min(0f)] private float lookAheadDistance = 1.25f;
        [SerializeField, Min(0f)] private float fallingLookAhead = 1f;
        [SerializeField, Range(0f, 1f)] private float verticalLookFraction = 0.5f;
        [SerializeField, Min(0.01f)] private float verticalLookSmoothTime = 0.2f;
        [SerializeField, Min(1)] private int pixelsPerUnit = 32;
        private Camera viewCamera;
        private float verticalLook;
        private float verticalLookVelocity;
        private InputAction toggleHeightLock;
        private float lockedHeight;
        public bool IsHeightLocked { get; private set; }
        public event Action<bool> HeightLockChanged;

        // Keep the smoothing state off the pixel grid to avoid rounding stalls.
        private Vector2 position;
        private Vector2 smoothVelocity;
        private float lookAhead;
        private float depth;
        private float shakeRemaining;
        private float shakeDuration;
        private float shakeAmplitude;

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            if (input == null && movement != null) input = movement.GetComponent<PlayerInputReader>();
            toggleHeightLock = new InputAction("Toggle Camera Height Lock", InputActionType.Button);
            toggleHeightLock.AddBinding("<Keyboard>/c");
            toggleHeightLock.AddBinding("<Gamepad>/rightStickPress");
        }

        private void OnEnable()
        {
            depth = transform.position.z;
            SnapToTarget();
            toggleHeightLock.Enable();
        }

        private void OnDisable() => toggleHeightLock?.Disable();
        private void OnDestroy() => toggleHeightLock?.Dispose();

        private void Update()
        {
            GameManager manager = GameManager.Instance;
            if (Time.timeScale <= 0f || (manager != null && manager.State != GameManager.LoopState.Playing)) return;
            if (toggleHeightLock.WasPressedThisFrame()) ToggleHeightLock();
        }

        public void ToggleHeightLock()
        {
            if (IsHeightLocked)
            {
                position.y = lockedHeight;
                verticalLook = verticalLookVelocity = smoothVelocity.y = 0f;
            }
            else lockedHeight = position.y + verticalLook;
            IsHeightLocked = !IsHeightLocked;
            HeightLockChanged?.Invoke(IsHeightLocked);
        }

        public void SnapToTarget()
        {
            position = target != null ? (Vector2)target.position + offset : (Vector2)transform.position;
            smoothVelocity = Vector2.zero;
            lookAhead = 0f;
            verticalLook = 0f;
            verticalLookVelocity = 0f;
            shakeRemaining = 0f;
            if (IsHeightLocked)
            {
                IsHeightLocked = false;
                HeightLockChanged?.Invoke(false);
            }
            ApplyPosition();
        }

        public void PlayShake(float duration, float amplitude)
        {
            shakeDuration = Mathf.Max(0f, duration);
            shakeRemaining = shakeDuration;
            shakeAmplitude = Mathf.Max(0f, amplitude);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector2 velocity = movement != null ? movement.Velocity : Vector2.zero;
            float dt = Time.deltaTime;
            float desiredVerticalLook = 0f;
            if (movement != null && movement.CanLookVertically && input != null && input.isActiveAndEnabled
                && viewCamera != null && viewCamera.orthographic && Mathf.Abs(input.Vertical) > 0.1f)
                desiredVerticalLook = Mathf.Sign(input.Vertical) * viewCamera.orthographicSize * 2f * verticalLookFraction;
            if (dt > 0f && !IsHeightLocked)
                verticalLook = Mathf.SmoothDamp(verticalLook, desiredVerticalLook, ref verticalLookVelocity,
                    verticalLookSmoothTime, Mathf.Infinity, dt);
            lookAhead = Mathf.MoveTowards(lookAhead,
                Mathf.Clamp(velocity.x / 5f, -1f, 1f) * lookAheadDistance, 5f * dt);
            Vector2 focus = (Vector2)target.position + offset + new Vector2(lookAhead,
                -Mathf.Clamp01(-velocity.y / 15f) * fallingLookAhead);
            Vector2 desired = position;
            desired.x += OutsideDeadZone(focus.x - position.x, deadZone.x);
            desired.y += OutsideDeadZone(focus.y - position.y, deadZone.y);
            position.x = Mathf.SmoothDamp(position.x, desired.x, ref smoothVelocity.x,
                horizontalSmoothTime, Mathf.Infinity, dt);
            if (!IsHeightLocked)
                position.y = Mathf.SmoothDamp(position.y, desired.y, ref smoothVelocity.y,
                    velocity.y < -1f ? fallingSmoothTime : verticalSmoothTime, Mathf.Infinity, dt);
            shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
            ApplyPosition();
        }

        private static float OutsideDeadZone(float distance, float halfSize)
        {
            return Mathf.Sign(distance) * Mathf.Max(0f, Mathf.Abs(distance) - Mathf.Max(0f, halfSize));
        }

        private void ApplyPosition()
        {
            float ppu = Mathf.Max(1, pixelsPerUnit);
            float envelope = shakeDuration > 0f ? shakeRemaining / shakeDuration : 0f;
            float phase = (shakeDuration - shakeRemaining) * 90f;
            Vector2 shake = new Vector2(Mathf.Sin(phase), Mathf.Sin(phase * 1.37f)) * (shakeAmplitude * envelope);
            // Keep manual looking separate from follow/dead-zone state so release returns to the same framing.
            Vector2 displayedPosition = new Vector2(position.x,
                IsHeightLocked ? lockedHeight : position.y + verticalLook) + shake;
            transform.position = new Vector3(Mathf.Round(displayedPosition.x * ppu) / ppu,
                Mathf.Round(displayedPosition.y * ppu) / ppu, depth);
        }
    }
}
