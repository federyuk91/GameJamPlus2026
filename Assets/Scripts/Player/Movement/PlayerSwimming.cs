using System.Collections.Generic;
using System;
using UnityEngine;
using GameJamPlus.Gameplay;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PlayerMovement))]
    public sealed class PlayerSwimming : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerMovement playerMovement;
        [Tooltip("Multiplies this animal's Move Speed to determine horizontal swimming speed. 1 uses normal movement speed; 0.5 halves it. The body's Max Speed still limits motion.")]
        [SerializeField, Min(0f)] private float swimSpeedMultiplier = 1f;
        [SerializeField] private SwimControlSettings controls = new SwimControlSettings();
        [SerializeField, Min(0.1f)] private float poolObservationDuration = 20f;
        private float consecutiveWaterTime;
        private PhysicsSettings WorldSettings => (body != null ? body : body = GetComponent<SimulatedBody2D>()).Settings;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        public bool InWater { get; private set; }
        public event Action StrokePerformed;
        private float strokeControlRemaining;
        private bool nearSurface;
        private bool surfaceJumpActive;
        private SwimMotion Settings => WorldSettings != null ? WorldSettings.water : PhysicsSettings.DefaultWater;
        public bool HasStrokeControl => isActiveAndEnabled && strokeControlRemaining > 0f;
        public float SwimSpeed => Mathf.Max(0f, playerMovement.MovementSettings.moveSpeed) * Mathf.Max(0f, swimSpeedMultiplier);
        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        }
        public void UpdateControlTimers(float dt) => strokeControlRemaining = Mathf.Max(0f, strokeControlRemaining - dt);
        public void ResetStrokeControl() => strokeControlRemaining = 0f;

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.LoopState.Playing || body == null || !body.Shape.enabled) return;
            if (!WaterVolume.ContainsPoint(body.Shape.bounds.center))
            {
                consecutiveWaterTime = 0f;
                return;
            }
            float duration = Mathf.Max(0.1f, poolObservationDuration);
            if (consecutiveWaterTime >= duration) return;
            consecutiveWaterTime += Time.fixedDeltaTime;
            if (consecutiveWaterTime >= duration) GameSession.RecordObservation(TestObservation.LovesPool);
        }

        public bool CheckWater()
        {
            nearSurface = false;
            if (!isActiveAndEnabled) { InWater = false; surfaceJumpActive = false; return false; }
            if (body == null) body = GetComponent<SimulatedBody2D>();
            Physics2D.SyncTransforms();
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            // Point queries work for simulated Transform bodies without trigger callbacks.
            Physics2D.OverlapPoint(body.Shape.bounds.center, filter, overlaps);
            InWater = false;
            foreach (Collider2D candidate in overlaps)
            {
                WaterVolume volume = candidate.GetComponent<WaterVolume>();
                if (volume == null || !volume.isActiveAndEnabled || !candidate.isTrigger) continue;
                InWater = true;
                Bounds playerBounds = body.Shape.bounds;
                float surfaceY = volume.Shape.bounds.max.y;
                if (playerBounds.max.y >= surfaceY - Settings.surfaceJumpDepth
                    && !WaterVolume.ContainsPoint(new Vector2(playerBounds.center.x, surfaceY + 0.001f)))
                    nearSurface = true;
            }
            if (!InWater) surfaceJumpActive = false;
            return InWater;
        }

        public Vector2 Step(Vector2 velocity, float horizontal, bool strokePressed, float dt)
        {
            if (velocity.y <= 0f) surfaceJumpActive = false;
            if (strokePressed)
            {
                surfaceJumpActive = InWater && nearSurface;
                strokeControlRemaining = Mathf.Max(0f, controls.strokeControlTime);
                if (InWater && dt > 0f) StrokePerformed?.Invoke();
            }
            // Preserve the surface impulse until the body leaves water instead of capping it on the next tick.
            return Settings.Step(velocity, horizontal, strokePressed, dt, body.GravityAcceleration, SwimSpeed, body.WaterDrag, controls, HasStrokeControl, surfaceJumpActive);
        }

        public float SurfaceHorizontal(float velocity, float horizontal, float dt)
            => Settings.StepHorizontal(velocity, horizontal, dt, SwimSpeed, HasStrokeControl, controls);

        private void OnDisable() { consecutiveWaterTime = 0f; InWater = false; nearSurface = surfaceJumpActive = false; ResetStrokeControl(); }
    }
}
