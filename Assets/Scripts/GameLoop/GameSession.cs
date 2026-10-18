using System.Collections.Generic;
using MousePlatformer;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    public static class GameSession
    {
        private static readonly Dictionary<CollectibleType, int> collectibleCounts = new Dictionary<CollectibleType, int>();
        public static int Universe { get; private set; }
        public static int TotalTests { get; private set; }
        public static int CompletedTests { get; private set; }
        public static int SubjectsUsed { get; private set; }
        public static int DeceasedSubjects { get; private set; }
        public static int RecoveredSubjects => SubjectsUsed - DeceasedSubjects;
        private static bool active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Universe = 0;
            TotalTests = 0;
            CompletedTests = 0;
            SubjectsUsed = 0;
            DeceasedSubjects = 0;
            active = false;
            collectibleCounts.Clear();
        }

        public static void EnsureStarted(int startingUniverse)
        {
            if (active && Universe > 0) return;
            Universe = Mathf.Max(1, startingUniverse);
            collectibleCounts.Clear();
            TotalTests = Universe;
            CompletedTests = 0;
            SubjectsUsed = 0;
            DeceasedSubjects = 0;
            active = true;
        }

        public static void Advance()
        {
            Universe = Mathf.Max(0, Universe - 1);
        }

        public static int GetCollectibleCount(CollectibleType type)
        {
            return collectibleCounts.TryGetValue(type, out int count) ? count : 0;
        }

        public static int AddCollectibleCount(CollectibleType type, int amount)
        {
            int count = GetCollectibleCount(type) + Mathf.Max(0, amount);
            collectibleCounts[type] = count;
            return count;
        }

        public static void RecordTestResult(bool subjectDied)
        {
            if (!active || CompletedTests >= TotalTests) return;
            CompletedTests++;
            SubjectsUsed++;
            if (subjectDied) DeceasedSubjects++;
        }
    }
}
