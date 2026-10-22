using System.Collections.Generic;
using MousePlatformer;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    public static class GameSession
    {
        private static readonly Dictionary<CollectibleType, int> collectibleCounts = new Dictionary<CollectibleType, int>();
        private static readonly Dictionary<CollectibleType, int> testCollectibleCounts = new Dictionary<CollectibleType, int>();
        private static readonly HashSet<string> collectedPhotoIds = new HashSet<string>();
        private static readonly HashSet<string> consumedCollectibleIds = new HashSet<string>();
        public static int Universe { get; private set; }
        public static int TotalTests { get; private set; }
        public static int CompletedTests { get; private set; }
        public static int SubjectsUsed { get; private set; }
        public static int DeceasedSubjects { get; private set; }
        public static int PhotosCollectedThisTest => GetTestCollectibleCount(CollectibleType.Photo);
        public static int RecoveredSubjects => SubjectsUsed - DeceasedSubjects;
        private static bool active;
        private static TestObservation testObservations;
        private static string checkpointScenePath;
        private static string checkpointSourceScenePath;
        private static bool checkpointHasPosition;
        private static Vector2 checkpointPosition;

        public static void SetCheckpoint(string sourceScenePath, string scenePath, bool hasPosition, Vector2 position)
        {
            checkpointSourceScenePath = sourceScenePath;
            checkpointScenePath = scenePath;
            checkpointHasPosition = hasPosition;
            checkpointPosition = position;
        }

        public static void EnterGameplayScene(string scenePath)
        {
            if (!string.IsNullOrEmpty(checkpointScenePath) && checkpointSourceScenePath != scenePath)
                ClearCheckpoint();
        }

        public static bool TryGetCheckpoint(out string scenePath, out bool hasPosition, out Vector2 position)
        {
            scenePath = checkpointScenePath;
            hasPosition = checkpointHasPosition;
            position = checkpointPosition;
            return !string.IsNullOrEmpty(scenePath);
        }

        private static void ClearCheckpoint()
        {
            checkpointScenePath = null;
            checkpointSourceScenePath = null;
            checkpointHasPosition = false;
            checkpointPosition = Vector2.zero;
        }

        public static void RecordObservation(TestObservation observation)
        {
            if (active) testObservations |= observation;
        }

        public static TestObservation GetTestObservations(double elapsedSeconds)
        {
            TestObservation observations = testObservations;
            if (GetTestCollectibleCount(CollectibleType.Cheese) > 0
                || GetTestCollectibleCount(CollectibleType.GreenCheese) > 0)
                observations |= TestObservation.EnjoysFood;
            if (PhotosCollectedThisTest > 0) observations |= TestObservation.EnjoysGardening;
            if (elapsedSeconds > 60d) observations |= TestObservation.ExtendedStay;
            return observations;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            ClearCheckpoint();
            Universe = 0;
            TotalTests = 0;
            CompletedTests = 0;
            SubjectsUsed = 0;
            DeceasedSubjects = 0;
            testCollectibleCounts.Clear();
            active = false;
            testObservations = TestObservation.None;
            collectibleCounts.Clear();
            collectedPhotoIds.Clear();
            consumedCollectibleIds.Clear();
        }

        public static void EnsureStarted(int startingUniverse)
        {
            if (active && Universe > 0) return;
            ClearCheckpoint();
            Universe = Mathf.Max(1, startingUniverse);
            collectibleCounts.Clear();
            collectedPhotoIds.Clear();
            consumedCollectibleIds.Clear();
            TotalTests = Universe;
            CompletedTests = 0;
            SubjectsUsed = 0;
            DeceasedSubjects = 0;
            testCollectibleCounts.Clear();
            active = true;
            testObservations = TestObservation.None;
        }

        public static void Advance()
        {
            Universe = Mathf.Max(0, Universe - 1);
            testCollectibleCounts.Clear();
            testObservations = TestObservation.None;
        }

        public static int GetCollectibleCount(CollectibleType type)
        {
            return collectibleCounts.TryGetValue(type, out int count) ? count : 0;
        }

        public static int GetTestCollectibleCount(CollectibleType type)
        {
            return testCollectibleCounts.TryGetValue(type, out int count) ? count : 0;
        }

        public static bool HasCollectedPhoto(string collectionId)
        {
            return !string.IsNullOrWhiteSpace(collectionId) && collectedPhotoIds.Contains(collectionId);
        }

        public static void RememberPhoto(string collectionId)
        {
            if (!string.IsNullOrWhiteSpace(collectionId)) collectedPhotoIds.Add(collectionId);
        }

        public static bool HasConsumedCollectible(string collectionId)
        {
            return !string.IsNullOrWhiteSpace(collectionId) && consumedCollectibleIds.Contains(collectionId);
        }

        public static void RememberConsumedCollectible(string collectionId)
        {
            if (!string.IsNullOrWhiteSpace(collectionId)) consumedCollectibleIds.Add(collectionId);
        }

        public static int AddCollectibleCount(CollectibleType type, int amount)
        {
            int added = Mathf.Max(0, amount);
            int count = GetCollectibleCount(type) + added;
            collectibleCounts[type] = count;
            testCollectibleCounts[type] = GetTestCollectibleCount(type) + added;
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
