using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class CollectibleTestGoal : MonoBehaviour
    {
        [SerializeField] private CollectibleCollector collector;
        [SerializeField] private CollectibleType type = CollectibleType.Cheese;
        [SerializeField, Min(1)] private int requiredCount = 10;
        [SerializeField] private TestEndEvent outcome;
        [SerializeField, Tooltip("Off: counts only this level. On: uses the session total displayed in the HUD.")]
        private bool useSessionTotal = true;
        private bool completed;
        private bool completionPending;

        private void OnEnable()
        {
            if (collector != null) collector.CountChanged += OnCountChanged;
        }

        private void LateUpdate()
        {
            // Finish after the pickup's events, so its ordinary dialogue cannot replace the ending.
            if (!completionPending || completed || outcome == null || Time.timeScale <= 0f) return;
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.LoopState.Playing) return;
            completed = true;
            completionPending = false;
            outcome.EndTest();
        }

        private void OnDisable()
        {
            if (collector != null) collector.CountChanged -= OnCountChanged;
        }

        private void OnCountChanged(CollectibleType collectedType, int totalCount)
        {
            if (completed || collectedType != type || outcome == null
                || (useSessionTotal ? totalCount : GameSession.GetTestCollectibleCount(type)) < Mathf.Max(1, requiredCount)) return;

            completionPending = true;
        }
    }
}
