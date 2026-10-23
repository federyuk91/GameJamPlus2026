using System.Collections.Generic;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    internal sealed class SpawnShuffleBag
    {
        private readonly List<int> candidates = new List<int>();
        private readonly List<int> remaining = new List<int>();
        private int previous = -1;

        public int Next(IReadOnlyList<int> available)
        {
            if (available.Count == 0) return -1;
            bool changed = candidates.Count != available.Count;
            for (int index = 0; !changed && index < available.Count; index++)
                changed = candidates[index] != available[index];
            if (changed)
            {
                candidates.Clear();
                for (int index = 0; index < available.Count; index++) candidates.Add(available[index]);
                remaining.Clear();
            }
            if (remaining.Count == 0)
            {
                remaining.AddRange(candidates);
                for (int index = remaining.Count - 1; index > 0; index--)
                {
                    int other = Random.Range(0, index + 1);
                    int value = remaining[index];
                    remaining[index] = remaining[other];
                    remaining[other] = value;
                }
                // Avoid repeating the last spawn at the boundary between two complete cycles.
                int last = remaining.Count - 1;
                if (last > 0 && remaining[last] == previous)
                {
                    int other = Random.Range(0, last);
                    remaining[last] = remaining[other];
                    remaining[other] = previous;
                }
            }
            int selected = remaining.Count - 1;
            previous = remaining[selected];
            remaining.RemoveAt(selected);
            return previous;
        }
    }
}
