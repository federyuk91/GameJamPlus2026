using System.Collections.Generic;

namespace MousePlatformer
{
    public sealed class CollectibleSession
    {
        private readonly Dictionary<CollectibleType, int> totals = new Dictionary<CollectibleType, int>();
        private readonly Dictionary<CollectibleType, int> attemptCounts = new Dictionary<CollectibleType, int>();
        private readonly HashSet<string> collectedIds = new HashSet<string>();

        public static CollectibleType CounterType(CollectibleType type)
            => type == CollectibleType.GreenCheese ? CollectibleType.Cheese : type;

        public int GetCount(CollectibleType type, bool sessionTotal = true)
        {
            Dictionary<CollectibleType, int> counts = sessionTotal ? totals : attemptCounts;
            return counts.TryGetValue(CounterType(type), out int count) ? count : 0;
        }

        public bool HasCollected(string id) => !string.IsNullOrWhiteSpace(id) && collectedIds.Contains(id);

        public bool TryRemember(string id)
            => string.IsNullOrWhiteSpace(id) || collectedIds.Add(id);

        public int Add(CollectibleType type, int amount)
        {
            int added = System.Math.Max(0, amount);
            CollectibleType counter = CounterType(type);
            int total = GetCount(counter) + added;
            totals[counter] = total;
            attemptCounts[counter] = GetCount(counter, false) + added;
            return total;
        }

        public void BeginAttempt() => attemptCounts.Clear();

        public void Reset()
        {
            totals.Clear();
            attemptCounts.Clear();
            collectedIds.Clear();
        }
    }
}
