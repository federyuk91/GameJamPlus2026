using System;
using GameJamPlus.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace MousePlatformer
{
    [DefaultExecutionOrder(310), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class PlayerOxygen : MonoBehaviour
    {
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private TestEndEvent drowningOutcome;
        [SerializeField, Min(1)] private int maximumNotches = 5;
        [SerializeField, Min(0.1f)] private float drainInterval = 5f;
        [SerializeField, Min(0.1f)] private float recoveryInterval = 1f;
        [SerializeField, Range(0f, 1f)] private float breathingPointHeight = 0.85f;
        [SerializeField, Min(0f)] private float warningSeconds = 10f;
        [SerializeField] private UnityEvent onLowOxygen = new UnityEvent();
        private float drainElapsed;
        private float recoveryElapsed;
        private bool warningIssued;

        public int Current { get; private set; }
        public int Maximum => Mathf.Max(1, maximumNotches);
        public bool IsUnderwater { get; private set; }
        public float SecondsRemaining => Mathf.Max(0f, Current * Mathf.Max(0.1f, drainInterval) - drainElapsed);
        public event Action<int, int> Changed;
        public event Action NotchLost;

        private void Awake()
        {
            if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
            Current = Maximum;
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (Current <= 0 || manager == null || manager.State != GameManager.LoopState.Playing
                || playerCollider == null || !playerCollider.enabled) return;
            Physics2D.SyncTransforms();
            Bounds bounds = playerCollider.bounds;
            Vector2 breathingPoint = new Vector2(bounds.center.x,
                Mathf.Lerp(bounds.min.y, bounds.max.y, breathingPointHeight));
            bool underwater = WaterVolume.ContainsPoint(breathingPoint);
            bool changed = underwater != IsUnderwater;
            IsUnderwater = underwater;
            if (underwater)
            {
                recoveryElapsed = 0f;
                drainElapsed += Time.fixedDeltaTime;
                if (drainElapsed >= Mathf.Max(0.1f, drainInterval))
                {
                    drainElapsed -= Mathf.Max(0.1f, drainInterval);
                    Current--;
                    changed = true;
                    NotchLost?.Invoke();
                }
            }
            else if (Current < Maximum || drainElapsed > 0f)
            {
                recoveryElapsed += Time.fixedDeltaTime;
                if (recoveryElapsed >= Mathf.Max(0.1f, recoveryInterval))
                {
                    recoveryElapsed -= Mathf.Max(0.1f, recoveryInterval);
                    Current = Mathf.Min(Maximum, Current + 1);
                    // Briefly surfacing cannot reset an almost-consumed notch; a full recovery interval can.
                    drainElapsed = 0f;
                    if (Current == Maximum) warningIssued = false;
                    changed = true;
                }
            }
            else recoveryElapsed = 0f;
            if (changed) Changed?.Invoke(Current, Maximum);
            if (IsUnderwater && Current > 0 && !warningIssued && SecondsRemaining <= warningSeconds)
            {
                warningIssued = true;
                onLowOxygen?.Invoke();
            }
            if (Current == 0 && drowningOutcome != null) drowningOutcome.EndTest();
        }
    }
}
