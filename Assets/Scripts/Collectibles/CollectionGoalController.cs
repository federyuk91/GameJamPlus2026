using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class CollectionGoalController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField, Tooltip("Collection conditions for this universe. Evaluated in list order; the first accepted ending wins.")]
        private CollectionGoalRule[] rules = new CollectionGoalRule[0];

        public void Evaluate(CollectibleType type, CollectionGoalTiming timing)
        {
            if (!isActiveAndEnabled || gameManager == null || !gameManager.isActiveAndEnabled
                || gameManager.State != GameManager.LoopState.Playing || Time.timeScale <= 0f) return;
            foreach (CollectionGoalRule rule in rules)
            {
                if (rule != null && rule.Matches(type, timing) && StageEndManager.Request(rule.Ending, this)) return;
            }
        }
    }
}
