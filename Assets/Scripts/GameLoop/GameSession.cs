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
        private static readonly Dictionary<string, SpawnShuffleBag> spawnBags = new Dictionary<string, SpawnShuffleBag>();
        public static int Universe { get; private set; }
        public static int TotalTests { get; private set; }
        public static int CompletedTests { get; private set; }
        public static int SubjectsUsed { get; private set; }
        public static int DeceasedSubjects { get; private set; }
        public static int PhotosCollectedThisTest => GetTestCollectibleCount(CollectibleType.Photo);
        public static int RecoveredSubjects => SubjectsUsed - DeceasedSubjects;
        public static int Attempt { get; private set; }
        public static int AttemptsPerUniverse { get; private set; } = 10;
        public static int MaximumDeaths { get; private set; } = 100;
        public static int SubjectUniverse { get; private set; }
        public static int SubjectRepetition { get; private set; }
        public static int GuestIndex { get; private set; }
        public static int SubjectHealth { get; private set; } = PlayerHealth.Maximum;
        public static string SubjectId => $"U{SubjectUniverse}-R{SubjectRepetition}-G{GuestIndex}";
        public static bool LastSubjectDied { get; private set; }
        public static int CompletedUniverses { get; private set; }
        public static bool HasPendingResult { get; private set; }
        public static bool WillAdvanceUniverse => HasPendingResult && (!LastSubjectDied || Attempt >= AttemptsPerUniverse);
        public static bool WillEndCampaign => HasPendingResult
            && (DeceasedSubjects >= MaximumDeaths || (Universe <= 1 && WillAdvanceUniverse));
        private static bool active;
        private static TestObservation testObservations;
        private static int subjectPhotoCount;
        private static bool clearObservationsOnAdvance;
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

        public static int SelectSpawn(string scenePath, IReadOnlyList<int> available)
        {
            string key = Universe + ":" + scenePath;
            if (!spawnBags.TryGetValue(key, out SpawnShuffleBag bag))
            {
                bag = new SpawnShuffleBag();
                spawnBags.Add(key, bag);
            }
            return bag.Next(available);
        }

        public static TestObservation GetTestObservations(double elapsedSeconds)
        {
            TestObservation observations = testObservations;
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
            CompletedUniverses = 0;
            Attempt = SubjectUniverse = SubjectRepetition = GuestIndex = 0;
            SubjectHealth = PlayerHealth.Maximum;
            AttemptsPerUniverse = 10;
            MaximumDeaths = 100;
            LastSubjectDied = HasPendingResult = false;
            spawnBags.Clear();
            testCollectibleCounts.Clear();
            active = false;
            testObservations = TestObservation.None;
            subjectPhotoCount = 0;
            clearObservationsOnAdvance = false;
            collectibleCounts.Clear();
            collectedPhotoIds.Clear();
            consumedCollectibleIds.Clear();
        }

        public static void EnsureStarted(int startingUniverse, int attemptsPerUniverse = 10, int maximumDeaths = 100)
        {
            if (active && Universe > 0) return;
            ClearCheckpoint();
            Universe = Mathf.Clamp(startingUniverse, 1, 10);
            collectibleCounts.Clear();
            collectedPhotoIds.Clear();
            consumedCollectibleIds.Clear();
            TotalTests = Universe;
            CompletedTests = 0;
            SubjectsUsed = 1;
            DeceasedSubjects = 0;
            CompletedUniverses = 0;
            AttemptsPerUniverse = Mathf.Max(1, attemptsPerUniverse);
            MaximumDeaths = Mathf.Max(1, maximumDeaths);
            Attempt = 1;
            SubjectUniverse = Universe;
            SubjectRepetition = GuestIndex = 0;
            SubjectHealth = PlayerHealth.Maximum;
            LastSubjectDied = HasPendingResult = false;
            spawnBags.Clear();
            testCollectibleCounts.Clear();
            active = true;
            testObservations = TestObservation.None;
            subjectPhotoCount = 0;
            clearObservationsOnAdvance = false;
        }

        public static void Advance(bool campaignProgression = true)
        {
            if (!HasPendingResult) return;
            bool ending = campaignProgression && WillEndCampaign;
            bool nextUniverse = !campaignProgression || WillAdvanceUniverse;
            if (ending) Universe = 0;
            else if (nextUniverse) Universe = Mathf.Max(0, Universe - 1);
            if (nextUniverse)
            {
                Attempt = 1;
                spawnBags.Clear();
                if (campaignProgression) ClearCheckpoint();
            }
            else Attempt++;
            if (LastSubjectDied && Universe > 0)
            {
                SubjectRepetition = SubjectUniverse == Universe ? SubjectRepetition + 1 : 0;
                SubjectUniverse = Universe;
                GuestIndex++;
                SubjectsUsed++;
                SubjectHealth = PlayerHealth.Maximum;
            }
            testCollectibleCounts.Clear();
            // Keep the final observations visible in Results; clear them only when starting the next subject.
            if (clearObservationsOnAdvance)
            {
                testObservations = TestObservation.None;
                subjectPhotoCount = 0;
            }
            clearObservationsOnAdvance = false;
            HasPendingResult = false;
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
            if (added > 0)
            {
                if (type == CollectibleType.Cheese || type == CollectibleType.GreenCheese)
                    RecordObservation(TestObservation.EnjoysFood);
                if (type == CollectibleType.Photo)
                {
                    subjectPhotoCount += added;
                    if (subjectPhotoCount >= 3) RecordObservation(TestObservation.VeryCurious);
                }
            }
            return count;
        }

        public static void RecordTestResult(bool subjectDied, double elapsedSeconds = 0d, int survivingHealth = PlayerHealth.Maximum)
        {
            if (!active || HasPendingResult) return;
            testObservations = GetTestObservations(elapsedSeconds);
            clearObservationsOnAdvance = subjectDied;
            CompletedTests++;
            LastSubjectDied = subjectDied;
            HasPendingResult = true;
            SubjectHealth = subjectDied ? 0 : Mathf.Clamp(survivingHealth, 1, PlayerHealth.Maximum);
            if (!subjectDied) CompletedUniverses++;
            if (subjectDied) DeceasedSubjects++;
        }
    }
}
