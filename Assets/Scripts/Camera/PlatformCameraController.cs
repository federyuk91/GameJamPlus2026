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
        [SerializeField, Min(0f)] private float shockShakeAmplitude = 0.07f;
        private Camera viewCamera;
        private InputAction inspectCamera;
        private InputAction lockCamera;
        private bool positionLocked;
        private bool inspectionFrozen;
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
        public bool IsHeightLocked => positionLocked;
        public bool IsPositionLocked => positionLocked;
        public bool IsInspectionFrozen => Mode == CameraMode.Inspect && inspectionFrozen;
        public event Action<bool> HeightLockChanged;
        public event Action<CameraMode> ModeChanged;
        public event Action LockedMovementAttempted;

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            if (input == null && movement != null) input = movement.GetComponent<PlayerInputReader>();
            inspectCamera = new InputAction("Hold Camera Control", InputActionType.Button);
            inspectCamera.AddBinding("<Keyboard>/c");
            inspectCamera.AddBinding("<Gamepad>/rightShoulder");
            lockCamera = new InputAction("Lock or Recenter Camera", InputActionType.Button);
            lockCamera.AddBinding("<Keyboard>/v");
            lockCamera.AddBinding("<Gamepad>/rightTrigger");
        }

        private void OnEnable()
        {
            depth = transform.position.z;
            SnapToTarget();
            inspectCamera.Enable();
            lockCamera.Enable();
        }

        private void OnDisable()
        {
            inspectCamera?.Disable();
            lockCamera?.Disable();
            positionLocked = false;
            SetMode(CameraMode.Follow);
        }

        private void OnDestroy()
        {
            inspectCamera?.Dispose();
            lockCamera?.Dispose();
        }

        private void Update()
        {
            GameManager manager = GameManager.Instance;
            if (Mode == CameraMode.Inspect && !inspectCamera.IsPressed()) EndInspection();
            if (Time.timeScale <= 0f || (manager != null && manager.State != GameManager.LoopState.Playing)
                || input == null || input.ExternalGameplayActionsLocked)
            {
                if (Mode == CameraMode.Inspect) EndInspection();
                return;
            }
            if (inspectCamera.WasPressedThisFrame() && Mode != CameraMode.Inspect)
            {
                if (movement != null && movement.CanStartCameraControl)
                {
                    position += panOffset;
                    panOffset = panTarget = panVelocity = smoothVelocity = Vector2.zero;
                    inspectionFrozen = false;
                    SetMode(CameraMode.Inspect);
                }
                else if (positionLocked && Time.unscaledTime >= nextReminderTime)
                {
                    nextReminderTime = Time.unscaledTime + 1f;
                    LockedMovementAttempted?.Invoke();
                }
            }
            if (lockCamera.WasPressedThisFrame()) TogglePositionLock();
        }

        public void ToggleHeightLock() => TogglePositionLock();

        public void TogglePositionLock()
        {
            if (positionLocked)
            {
                bool inspecting = Mode == CameraMode.Inspect;
                positionLocked = false;
                inspectionFrozen = false;
                position = target != null ? (Vector2)target.position + offset : position;
                smoothVelocity = panOffset = panTarget = panVelocity = Vector2.zero;
                lookAhead = shakeRemaining = 0f;
                SetMode(inspecting ? CameraMode.Inspect : CameraMode.Follow, true);
                ApplyPosition();
                return;
            }
            if (Mode != CameraMode.Inspect) return;
            positionLocked = true;
            inspectionFrozen = true;
            panTarget = panOffset;
            panVelocity = Vector2.zero;
            NotifyModeChanged();
        }

        private void EndInspection()
        {
            inspectionFrozen = false;
            if (positionLocked)
            {
                position += panOffset;
                panOffset = panTarget = panVelocity = Vector2.zero;
                SetMode(CameraMode.Fixed);
            }
            else
            {
                panVelocity = Vector2.zero;
                SetMode(CameraMode.Follow);
            }
        }

        private void SetMode(CameraMode mode, bool notifyIfUnchanged = false)
        {
            if (Mode == mode)
            {
                if (notifyIfUnchanged) NotifyModeChanged();
                return;
            }
            Mode = mode;
            bool controllingCamera = mode == CameraMode.Inspect;
            if (input != null) input.SetCameraControlsLocked(controllingCamera);
            if (movement != null) movement.SetCameraControlActive(controllingCamera);
            if (mode == CameraMode.Fixed)
            {
                panTarget = panOffset;
                panVelocity = Vector2.zero;
                nextReminderTime = 0f;
            }
            NotifyModeChanged();
        }

        private void NotifyModeChanged()
        {
            HeightLockChanged?.Invoke(IsHeightLocked);
            ModeChanged?.Invoke(Mode);
        }

        public void SnapToTarget()
        {
            bool wasLocked = positionLocked;
            positionLocked = false;
            inspectionFrozen = false;
            SetMode(CameraMode.Follow, wasLocked);
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
            if (inspectionFrozen || viewCamera == null || input == null
                || !viewCamera.orthographic || input.ExternalGameplayActionsLocked) return;
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
            if (input != null && input.IsParalyzed)
            {
                float shockPhase = Time.time * 90f;
                shake += new Vector2(Mathf.Sin(shockPhase), Mathf.Sin(shockPhase * 1.37f)) * shockShakeAmplitude;
            }
            Vector2 displayedPosition = position + panOffset + shake;
            transform.position = new Vector3(Mathf.Round(displayedPosition.x * ppu) / ppu,
                Mathf.Round(displayedPosition.y * ppu) / ppu, depth);
        }
    }
}
