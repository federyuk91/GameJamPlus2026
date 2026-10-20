using System;
using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(110), DisallowMultipleComponent]
    public sealed class PlayerWheelInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private CollectibleCollector collector;
        private StarvationDeathZone activeWheel;
        private bool movementWasEnabled;
        private bool visualWasEnabled;
        private bool collectorWasEnabled;
        private Vector2 entryPosition;
        private readonly RaycastHit2D[] obstacles = new RaycastHit2D[16];

        public StarvationDeathZone InteractionTarget { get; private set; }
        public bool IsUsing => activeWheel != null;
        public event Action UsageChanged;
        public float DrainInterval => IsUsing ? activeWheel.DrainInterval : 1f;
        public float TargetDistanceSquared => InteractionTarget != null && body != null
            ? (InteractionTarget.InteractionPoint - (Vector2)body.Shape.bounds.center).sqrMagnitude : float.PositiveInfinity;

        private void FixedUpdate() => RefreshInteractionTarget();

        public void RefreshInteractionTarget()
        {
            InteractionTarget = null;
            if (!isActiveAndEnabled || body == null || input == null || !input.isActiveAndEnabled) return;
            if (IsUsing) { InteractionTarget = activeWheel; return; }
            if (input.GameplayActionsLocked || movement == null || !movement.isActiveAndEnabled) return;
            GameManager manager = GameManager.Instance;
            if (manager != null && manager.State != GameManager.LoopState.Playing) return;
            Physics2D.SyncTransforms();
            float best = float.PositiveInfinity;
            foreach (StarvationDeathZone wheel in StarvationDeathZone.ActiveZones)
            {
                if (!wheel.CanUse || !wheel.Contains(body.Shape)) continue;
                Vector2 offset = wheel.InteractionPoint - (Vector2)body.Shape.bounds.center;
                if (offset.sqrMagnitude >= best || !HasLineOfSight(offset)) continue;
                best = offset.sqrMagnitude;
                InteractionTarget = wheel;
            }
        }

        private bool HasLineOfSight(Vector2 offset)
        {
            if (offset.sqrMagnitude < 0.0001f) return true;
            int count = Physics2D.Raycast(body.Shape.bounds.center, offset.normalized, body.SolidFilter, obstacles, offset.magnitude);
            for (int index = 0; index < count; index++)
            {
                Collider2D obstacle = obstacles[index].collider;
                if (obstacle == body.Shape || obstacle.transform.IsChildOf(transform)) continue;
                if (obstacles[index].distance < offset.magnitude - 0.01f) return false;
            }
            return true;
        }

        private void Update()
        {
            GameManager manager = GameManager.Instance;
            if (IsUsing && (!activeWheel.isActiveAndEnabled || (manager != null && manager.State != GameManager.LoopState.Playing)))
                StopUsing();
            if (Time.timeScale <= 0f || input == null || !input.isActiveAndEnabled || !input.WheelInteractionPressed) return;
            if (IsUsing)
            {
                input.ConsumeInteraction();
                StopUsing();
                return;
            }
            RefreshInteractionTarget();
            StarvationDeathZone wheel = InteractionTarget;
            if (wheel == null || movement == null || !wheel.TryUse(this, body.Shape)) return;
            input.ConsumeInteraction();
            activeWheel = wheel;
            entryPosition = transform.position;
            movementWasEnabled = movement.enabled;
            visualWasEnabled = visual != null && visual.enabled;
            collectorWasEnabled = collector != null && collector.enabled;
            input.SetGameplayActionsLocked(true);
            movement.TeleportTo(wheel.MountPosition(entryPosition));
            movement.enabled = false;
            if (visual != null) visual.enabled = false;
            if (collector != null) collector.enabled = false;
            GameSession.RecordObservation(TestObservation.Exercises);
            UsageChanged?.Invoke();
        }

        public void StopUsing()
        {
            if (!IsUsing) return;
            StarvationDeathZone wheel = activeWheel;
            activeWheel = null;
            InteractionTarget = null;
            wheel.Release(this);
            if (movement != null)
            {
                movement.TeleportTo(wheel.ExitPosition(entryPosition));
                movement.enabled = movementWasEnabled;
            }
            if (visual != null) visual.enabled = visualWasEnabled;
            if (collector != null) collector.enabled = collectorWasEnabled;
            if (input != null) input.SetGameplayActionsLocked(false);
            UsageChanged?.Invoke();
        }

        private void OnDisable() => StopUsing();
    }
}
