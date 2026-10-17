using UnityEngine;

namespace GameJamPlus.Gameplay
{
    public static class GameSession
    {
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
        }

        public static void EnsureStarted(int startingUniverse)
        {
            if (active && Universe > 0) return;
            Universe = Mathf.Max(1, startingUniverse);
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

        public static void RecordTestResult(bool subjectDied)
        {
            if (!active || CompletedTests >= TotalTests) return;
            CompletedTests++;
            SubjectsUsed++;
            if (subjectDied) DeceasedSubjects++;
        }
    }
}
