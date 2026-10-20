using System;
using GameJamPlus.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class PlatformCameraController : MonoBehaviour
    {
        public enum CameraMode { Follow, Inspect, Fixed, FollowOffset }

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
        [SerializeField, Range(0f, 1f)] private float horizontalLookFraction = 0.5f;
        [SerializeField, Min(0.01f)] private float cameraMoveSpeed = 6f;
        [SerializeField, Min(0.01f)] private float verticalLookSmoothTime = 0.2f;
        [SerializeField, Min(1)] private int pixelsPerUnit = 32;
        private Camera viewCamera;
        private InputAction toggleCamera;
        private Vector2 position;
        private Vector2 smoothVelocity;
        private Vector2 panOffset;
        private Vector2 panTarget;
        private Vector2 panVelocity;
        private float lookAhead;
        private float depth;
        private float shakeRemaining;
        private float shakeDuration;
        private float shakeAmplitude;
        private float nextReminderTime;

        public CameraMode Mode { get; private set; }
        public bool IsHeightLocked => Mode == CameraMode.Fixed;
        public event Action<bool> HeightLockChanged;
        public event Action<CameraMode> ModeChanged;
        public event Action LockedMovementAttempted;

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            if (input == null && movement != null) input = movement.GetComponent<PlayerInputReader>();
            toggleCamera = new InputAction("Toggle Camera Control", InputActionType.Button);
            toggleCamera.AddBinding("<Keyboard>/c");
            toggleCamera.AddBinding("<Gamepad>/rightShoulder");
        }

        private void OnEnable()
        {
            depth = transform.position.z;
            SnapToTarget();
            toggleCamera.Enable();
        }

        private void OnDisable()
        {
            toggleCamera?.Disable();
            SetMode(CameraMode.Follow);
        }

        private void OnDestroy() => toggleCamera?.Dispose();

        private void Update()
        {
            GameManager manager = GameManager.Instance;
            if (Time.timeScale <= 0f || (manager != null && manager.State != GameManager.LoopState.Playing)
                || input == null || input.ExternalGameplayActionsLocked) return;
            if (toggleCamera.WasPressedThisFrame())
            {
                ToggleHeightLock();
                return;
            }
            if ((Mode == CameraMode.Inspect || Mode == CameraMode.Fixed) && input.CameraJumpPressed)
            {
                SetMode(Mode == CameraMode.Inspect ? CameraMode.Fixed : CameraMode.Inspect);
                return;
            }
            if (Mode == CameraMode.Fixed && input.CameraMove.sqrMagnitude > 0.01f
                && Time.unscaledTime >= nextReminderTime)
            {
                nextReminderTime = Time.unscaledTime + 1f;
                LockedMovementAttempted?.Invoke();
            }
        }

        public void ToggleHeightLock()
        {
            switch (Mode)
            {
                case CameraMode.Follow:
                case CameraMode.FollowOffset:
                    if (movement == null || !movement.CanStartCameraControl) return;
                    panTarget = panOffset;
                    smoothVelocity = Vector2.zero;
                    SetMode(CameraMode.Inspect);
                    break;
                case CameraMode.Inspect:
                    panTarget = Vector2.zero;
                    panVelocity = Vector2.zero;
                    SetMode(CameraMode.Follow);
                    break;
                case CameraMode.Fixed:
                    SetMode(CameraMode.FollowOffset);
                    break;
            }
        }

        private void SetMode(CameraMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            bool controllingCamera = mode == CameraMode.Inspect || mode == CameraMode.Fixed;
            if (input != null) input.SetCameraControlsLocked(controllingCamera);
            if (movement != null) movement.SetCameraControlActive(controllingCamera);
            if (mode == CameraMode.Fixed)
            {
                panTarget = panOffset;
                panVelocity = Vector2.zero;
                nextReminderTime = 0f;
            }
            HeightLockChanged?.Invoke(IsHeightLocked);
            ModeChanged?.Invoke(mode);
        }

        public void SnapToTarget()
        {
            SetMode(CameraMode.Follow);
            position = target != null ? (Vector2)target.position + offset : (Vector2)transform.position;
            smoothVelocity = panOffset = panTarget = panVelocity = Vector2.zero;
            lookAhead = shakeRemaining = 0f;
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
            GameManager manager = GameManager.Instance;
            bool canUpdate = Time.deltaTime > 0f && (manager == null || manager.State == GameManager.LoopState.Playing);
            if (canUpdate)
            {
                float dt = Time.deltaTime;
                if (Mode == CameraMode.Inspect) UpdateInspection(dt);
                else if (Mode != CameraMode.Fixed)
                {
                    FollowTarget(dt);
                    if (Mode == CameraMode.Follow)
                        panOffset = Vector2.SmoothDamp(panOffset, Vector2.zero, ref panVelocity,
                            verticalLookSmoothTime, Mathf.Infinity, dt);
                }
            }
            shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
            ApplyPosition();
        }

        private void UpdateInspection(float dt)
        {
            if (viewCamera == null || input == null || !viewCamera.orthographic || input.ExternalGameplayActionsLocked) return;
            float height = viewCamera.orthographicSize * 2f;
            Vector2 limits = new Vector2(height * viewCamera.aspect * horizontalLookFraction, height * verticalLookFraction);
            panTarget += Vector2.ClampMagnitude(input.CameraMove, 1f) * (cameraMoveSpeed * dt);
            panTarget = new Vector2(Mathf.Clamp(panTarget.x, -limits.x, limits.x), Mathf.Clamp(panTarget.y, -limits.y, limits.y));
            panOffset = Vector2.SmoothDamp(panOffset, panTarget, ref panVelocity, verticalLookSmoothTime, Mathf.Infinity, dt);
            panOffset = new Vector2(Mathf.Clamp(panOffset.x, -limits.x, limits.x), Mathf.Clamp(panOffset.y, -limits.y, limits.y));
        }

        private void FollowTarget(float dt)
        {
            Vector2 velocity = movement != null ? movement.Velocity : Vector2.zero;
            lookAhead = Mathf.MoveTowards(lookAhead,
                Mathf.Clamp(velocity.x / 5f, -1f, 1f) * lookAheadDistance, 5f * dt);
            Vector2 focus = (Vector2)target.position + offset + new Vector2(lookAhead,
                -Mathf.Clamp01(-velocity.y / 15f) * fallingLookAhead);
            Vector2 desired = position;
            desired.x += OutsideDeadZone(focus.x - position.x, deadZone.x);
            desired.y += OutsideDeadZone(focus.y - position.y, deadZone.y);
            position.x = Mathf.SmoothDamp(position.x, desired.x, ref smoothVelocity.x, horizontalSmoothTime, Mathf.Infinity, dt);
            position.y = Mathf.SmoothDamp(position.y, desired.y, ref smoothVelocity.y,
                velocity.y < -1f ? fallingSmoothTime : verticalSmoothTime, Mathf.Infinity, dt);
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
            Vector2 displayedPosition = position + panOffset + shake;
            transform.position = new Vector3(Mathf.Round(displayedPosition.x * ppu) / ppu,
                Mathf.Round(displayedPosition.y * ppu) / ppu, depth);
        }
    }
}
