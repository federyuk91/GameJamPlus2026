using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    public enum ButtonSpawnPositionMode { Fixed, Sequential, Shuffle }

    internal sealed class ButtonSpawnPositionSequence
    {
        private readonly List<Transform> points = new List<Transform>();
        private readonly List<Transform> candidates = new List<Transform>();
        private readonly List<Transform> order = new List<Transform>();
        private ButtonSpawnPositionMode mode;
        private Transform previous;
        private int next;
        public bool HasPoints => points.Count > 0;

        public void Configure(IReadOnlyList<Transform> assignedPoints, ButtonSpawnPositionMode selectionMode)
        {
            candidates.Clear();
            for (int index = 0; assignedPoints != null && index < assignedPoints.Count; index++)
            {
                Transform point = assignedPoints[index];
                if (point != null && !candidates.Contains(point)) candidates.Add(point);
            }
            bool changed = mode != selectionMode || points.Count != candidates.Count;
            for (int index = 0; !changed && index < points.Count; index++)
                changed = points[index] != candidates[index];
            if (!changed) return;
            mode = selectionMode;
            points.Clear();
            points.AddRange(candidates);
            Reset();
        }

        public bool TryGetNext(bool repeat, out Transform point)
        {
            point = null;
            if (!HasPoints) return false;
            if (order.Count == 0) BeginCycle();
            else if (next >= order.Count)
            {
                if (!repeat) return false;
                BeginCycle();
            }
            point = order[next];
            return point != null;
        }

        public void Advance()
        {
            if (next >= order.Count) return;
            previous = order[next];
            next++;
        }

        public void Reset()
        {
            order.Clear();
            previous = null;
            next = 0;
        }

        private void BeginCycle()
        {
            order.Clear();
            order.AddRange(points);
            next = 0;
            if (mode != ButtonSpawnPositionMode.Shuffle) return;
            for (int index = order.Count - 1; index > 0; index--)
            {
                int other = Random.Range(0, index + 1);
                Transform value = order[index];
                order[index] = order[other];
                order[other] = value;
            }
            // Keep the first point of a new shuffle different from the last successful spawn.
            if (order.Count > 1 && order[0] == previous)
            {
                int other = Random.Range(1, order.Count);
                order[0] = order[other];
                order[other] = previous;
            }
        }
    }
}
