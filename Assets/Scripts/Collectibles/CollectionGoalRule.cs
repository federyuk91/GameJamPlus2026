using System;
using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    public enum CollectionGoalTiming { Collected = 0, PhotoClosed = 1 }
    public enum CollectionGoalMatch { Threshold = 0, EveryMultiple = 1 }

    [Serializable]
    public sealed class CollectionGoalRule
    {
        [SerializeField, Tooltip("Enable this collection goal for this scene or universe rig.")]
        private bool enabled = true;
        [SerializeField, Tooltip("Counter to observe. Normal and green cheese share the cheese counter.")]
        private CollectibleType type;
        [SerializeField, Min(1), Tooltip("Required quantity, or interval when matching every multiple.")]
        private int requiredCount = 10;
        [SerializeField, Tooltip("On uses the whole run; off uses the current attempt's count.")]
        private bool useSessionTotal = true;
        [SerializeField, Tooltip("Threshold accepts counts at or above the target; Every Multiple accepts N, 2N, 3N and so on.")]
        private CollectionGoalMatch match;
        [SerializeField, Tooltip("Evaluate immediately after collection, or only after the collected photo is closed.")]
        private CollectionGoalTiming timing;
        [SerializeField, Tooltip("Central StageEndManager conclusion requested when the rule matches. None disables the ending.")]
        private StageEndCase ending;

        public StageEndCase Ending => ending;

        public bool Matches(CollectibleType collectedType, CollectionGoalTiming eventTiming)
        {
            if (!enabled || ending == StageEndCase.None || timing != eventTiming
                || CollectibleSession.CounterType(type) != CollectibleSession.CounterType(collectedType)) return false;
            int count = GameSession.Collections.GetCount(type, useSessionTotal);
            int target = Mathf.Max(1, requiredCount);
            return count > 0 && (match == CollectionGoalMatch.EveryMultiple ? count % target == 0 : count >= target);
        }
    }
}
