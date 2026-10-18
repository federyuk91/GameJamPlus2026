using System;
using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(300), DisallowMultipleComponent]
    public sealed class PlayerSatiety : MonoBehaviour
    {
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private CollectibleCollector collector;
        [SerializeField] private TestEndEvent starvationOutcome;
        [SerializeField, Min(1)] private int maximumNotches = 5;
        [SerializeField, Min(0.1f)] private float drainInterval = 60f;
        private float drainProgress;

        public int Current { get; private set; }
        public int Maximum => Mathf.Max(1, maximumNotches);
        public event Action<int, int> Changed;
        public event Action NotchLost;

        private void Awake()
        {
            if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
            if (collector == null) collector = GetComponent<CollectibleCollector>();
            Current = Maximum;
        }

        private void OnEnable()
        {
            if (collector != null) collector.CountChanged += OnCollected;
        }

        private void OnDisable()
        {
            if (collector != null) collector.CountChanged -= OnCollected;
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (Current <= 0 || manager == null || manager.State != GameManager.LoopState.Playing) return;
            float interval = Mathf.Max(0.1f, drainInterval);
            if (StarvationDeathZone.ActiveZones.Count > 0)
            {
                Physics2D.SyncTransforms();
                for (int index = 0; index < StarvationDeathZone.ActiveZones.Count; index++)
                {
                    StarvationDeathZone zone = StarvationDeathZone.ActiveZones[index];
                    if (zone.Contains(playerCollider)) interval = Mathf.Min(interval, zone.DrainInterval);
                }
            }
            // Preserve partial consumption when changing zones; overlapping zones use the fastest rate.
            drainProgress += Time.fixedDeltaTime / interval;
            if (drainProgress < 1f) return;
            drainProgress -= 1f;
            Current--;
            Changed?.Invoke(Current, Maximum);
            NotchLost?.Invoke();
            if (Current == 0 && starvationOutcome != null) starvationOutcome.EndTest();
        }

        private void OnCollected(CollectibleType type, int count)
        {
            if (type == CollectibleType.Cheese) RestoreFull();
        }

        public void RestoreFull()
        {
            if (Current <= 0) return;
            Current = Maximum;
            drainProgress = 0f;
            Changed?.Invoke(Current, Maximum);
        }
    }
}
