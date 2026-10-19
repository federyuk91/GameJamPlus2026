using System.Collections.Generic;
using System;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerSwimming : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PhysicsSettings physicsSettings;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        public bool InWater { get; private set; }
        public event Action StrokePerformed;
        private float strokeControlRemaining;
        private SwimMotion Settings => physicsSettings != null ? physicsSettings.water : PhysicsSettings.DefaultWater;
        public bool HasStrokeControl => isActiveAndEnabled && strokeControlRemaining > 0f;
        public void UpdateControlTimers(float dt) => strokeControlRemaining = Mathf.Max(0f, strokeControlRemaining - dt);
        public void ResetStrokeControl() => strokeControlRemaining = 0f;

        public bool CheckWater()
        {
            if (!isActiveAndEnabled) { InWater = false; return false; }
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
                break;
            }
            return InWater;
        }

        public Vector2 Step(Vector2 velocity, float horizontal, bool strokePressed, float dt)
        {
            if (strokePressed)
            {
                strokeControlRemaining = Settings.strokeControlTime;
                if (InWater && dt > 0f) StrokePerformed?.Invoke();
            }
            return Settings.Step(velocity, horizontal, strokePressed, dt, HasStrokeControl);
        }

        public float SurfaceHorizontal(float velocity, float horizontal, float dt)
            => Settings.StepHorizontal(velocity, horizontal, dt, HasStrokeControl);

        private void OnDisable() { InWater = false; ResetStrokeControl(); }
    }
}
