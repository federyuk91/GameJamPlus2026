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
        [SerializeField] private PlayerWheelInteraction wheel;
        [SerializeField, Min(1)] private int maximumNotches = 5;
        [SerializeField, Min(0.1f)] private float drainInterval = 60f;
        [SerializeField, Min(0.1f), Tooltip("Seconds between satiety losses caused by green cheese poisoning. Pauses with gameplay.")]
        private float poisonInterval = 3f;
        [SerializeField, Min(1), Tooltip("Number of one-point satiety losses caused by green cheese. Another green cheese refreshes this count; normal cheese cures it.")]
        private int poisonTicks = 5;
        private float drainProgress;
        private float wheelDrainProgress;
        private float poisonElapsed;
        private int poisonTicksRemaining;

        public int Current { get; private set; }
        public int Maximum => Mathf.Max(1, maximumNotches);
        public bool IsPoisoned => poisonTicksRemaining > 0;
        public bool LastLossWasPoison { get; private set; }
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
            if (wheel != null) wheel.UsageChanged += ResetWheelDrain;
        }

        private void OnDisable()
        {
            if (collector != null) collector.CountChanged -= OnCollected;
            if (wheel != null) wheel.UsageChanged -= ResetWheelDrain;
        }

        private void ResetWheelDrain() => wheelDrainProgress = 0f;

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (Current <= 0 || manager == null || manager.State != GameManager.LoopState.Playing) return;
            TickPoison(Time.fixedDeltaTime);
            if (Current <= 0 || manager.State != GameManager.LoopState.Playing) return;
            float interval = Mathf.Max(0.1f, drainInterval);
            if (wheel != null && wheel.IsUsing)
            {
                wheelDrainProgress += Time.fixedDeltaTime;
                if (wheelDrainProgress < wheel.DrainInterval) return;
                wheelDrainProgress -= wheel.DrainInterval;
            }
            else
            {
                wheelDrainProgress = 0f;
                drainProgress += Time.fixedDeltaTime / interval;
                if (drainProgress < 1f) return;
                drainProgress -= 1f;
            }
            LoseNotch();
        }

        private void TickPoison(float deltaTime)
        {
            if (!IsPoisoned) return;
            poisonElapsed += deltaTime;
            if (poisonElapsed < Mathf.Max(0.1f, poisonInterval)) return;
            poisonElapsed -= Mathf.Max(0.1f, poisonInterval);
            poisonTicksRemaining--;
            LoseNotch(true);
        }

        private void LoseNotch(bool fromPoison = false)
        {
            LastLossWasPoison = fromPoison;
            Current = Mathf.Max(0, Current - 1);
            Changed?.Invoke(Current, Maximum);
            NotchLost?.Invoke();
            if (Current == 0 && starvationOutcome != null) starvationOutcome.EndTest();
        }

        private void OnCollected(CollectibleType type, int count)
        {
            if (type == CollectibleType.Cheese) RestoreFull();
            else if (type == CollectibleType.GreenCheese) RestoreAndPoison();
        }

        public void RestoreFull()
        {
            if (Current <= 0) return;
            poisonTicksRemaining = 0;
            poisonElapsed = 0f;
            Refill();
        }

        private void RestoreAndPoison()
        {
            if (Current <= 0) return;
            poisonTicksRemaining = Mathf.Max(1, poisonTicks);
            poisonElapsed = 0f;
            Refill();
        }

        private void Refill()
        {
            Current = Maximum;
            drainProgress = 0f;
            wheelDrainProgress = 0f;
            Changed?.Invoke(Current, Maximum);
        }
    }
}
