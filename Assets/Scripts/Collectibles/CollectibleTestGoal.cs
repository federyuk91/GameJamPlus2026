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
            bool matchesType = collectedType == type
                || (type == CollectibleType.Cheese && collectedType == CollectibleType.GreenCheese);
            if (completed || !matchesType || outcome == null
                || (useSessionTotal ? totalCount : GameSession.GetTestCollectibleCount(type)) < Mathf.Max(1, requiredCount)) return;

            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.LoopState.Playing || Time.timeScale <= 0f) return;
            // Commit during collection: pickup events can release pressure buttons and activate hazards.
            // GameplayHudController already suppresses ordinary pickup dialogue once an ending begins.
            completed = true;
            outcome.EndTest();
        }
    }
}
