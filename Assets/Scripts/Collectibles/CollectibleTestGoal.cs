using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class CollectibleTestGoal : MonoBehaviour
    {
        [SerializeField] private CollectibleCollector collector;
        [SerializeField] private CollectibleType type = CollectibleType.Cheese;
        [SerializeField, Min(1)] private int requiredCount = 5;
        [SerializeField] private TestEndEvent outcome;
        private bool completed;

        private void OnEnable()
        {
            if (collector != null) collector.CountChanged += OnCountChanged;
        }

        private void OnDisable()
        {
            if (collector != null) collector.CountChanged -= OnCountChanged;
        }

        private void OnCountChanged(CollectibleType collectedType, int totalCount)
        {
            if (completed || collectedType != type || outcome == null
                || GameSession.GetTestCollectibleCount(type) < Mathf.Max(1, requiredCount)) return;

            completed = true;
            outcome.EndTest();
        }
    }
}
