using System.Collections;
using System.Collections.Generic;
using GameJamPlus.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(-200), DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public enum LoopState
        {
            Playing,
            Flash,
            ScientistComment,
            Results,
            Loading,
            DeathAnimation,
            Spawning
        }

        [SerializeField] private GameplayHudController hud;
        [Header("Player spawn")]
        [SerializeField] private Transform player;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private MousePlatformer.PlayerRecovery playerRecovery;
        [SerializeField] private MousePlatformer.PlayerCrushVisual playerCrushVisual;
        [SerializeField] private MousePlatformer.PlatformCameraController gameplayCamera;
        [SerializeField, Tooltip("Scene instances. Random selection among assigned active points. Position denotes the player pivot.")]
        private SpawnPoint[] spawnPoints = new SpawnPoint[0];
        [SerializeField] private HandReleaseIntro handReleaseIntro;
        [Header("Game loop")]
        [SerializeField, Min(1)] private int startingUniverse = 10;
        [SerializeField] private string gameplaySceneName = "LoopScene";
        [SerializeField] private string endGameSceneName = "EndGame";
        [SerializeField, Min(0f), Tooltip("Seconds approaching the tunnel exit until the light fills the screen, before the scientist comment.")]
        private float flashDuration = 1.5f;
        [SerializeField, TextArea] private string fallbackComment = "Excellent. We can consider this stage complete.";
        private double completedTime;
        private string completedResult;
        private float previousTimeScale;
        private bool ownsPause;
        private TestEndEvent animatedOutcome;
        private string pendingComment;
        private Sprite pendingAvatar;
        private float pendingFlashDuration;

        public static GameManager Instance { get; private set; }
        public LoopState State { get; private set; } = LoopState.Playing;
        public int Universe => GameSession.Universe;
        public GameplayHudController Hud => hud;
        public Collider2D PlayerCollider => playerCollider;
        public MousePlatformer.PlayerRecovery PlayerRecovery => playerRecovery;
        public MousePlatformer.PlayerCrushVisual PlayerCrushVisual => playerCrushVisual;
        public IReadOnlyList<SpawnPoint> SpawnPoints => spawnPoints;
        public void SnapCameraToPlayer() => gameplayCamera?.SnapToTarget();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("The scene must contain exactly one Game Manager.", this);
                enabled = false;
                return;
            }
            Instance = this;
            if (playerCrushVisual == null && player != null)
                player.TryGetComponent(out playerCrushVisual);
            GameSession.EnsureStarted(startingUniverse);
            GameSession.EnterGameplayScene(gameObject.scene.path);
            if (hud == null)
            {
                Debug.LogError("Assign the Gameplay HUD to the Game Manager.", this);
                enabled = false;
                return;
            }
            SpawnPlayer();
            // The hand's OnEnable may not have run yet at the manager's earlier execution order.
            if (handReleaseIntro != null && handReleaseIntro.enabled && handReleaseIntro.gameObject.activeInHierarchy)
                State = LoopState.Spawning;
        }

        private void OnEnable()
        {
            if (hud == null) return;
            hud.TestCommentCompleted += OnTestCommentCompleted;
            hud.TestContinueRequested += OnTestContinueRequested;
        }

        private void Start()
        {
            hud.SetUniverse(Universe);
            if (gameplayCamera != null) gameplayCamera.SnapToTarget();
            if (State == LoopState.Spawning)
            {
                if (handReleaseIntro.Begin(player, CompleteSpawnIntro)) hud.BeginSpawnPresentation();
                else State = LoopState.Playing;
            }
        }

        private void CompleteSpawnIntro()
        {
            if (State != LoopState.Spawning) return;
            State = LoopState.Playing;
            hud.CompleteSpawnPresentation();
        }

        private void SpawnPlayer()
        {
            if (player == null)
            {
                Debug.LogWarning("Assign the Player to the Game Manager to use Spawn Points.", this);
                return;
            }
            if (GameSession.TryGetCheckpoint(out string checkpointScene, out bool hasPosition, out Vector2 checkpointPosition)
                && hasPosition && checkpointScene == gameObject.scene.path)
            {
                ApplySpawnPosition(checkpointPosition);
                return;
            }
            int validCount = 0;
            foreach (SpawnPoint point in spawnPoints ?? System.Array.Empty<SpawnPoint>())
                if (point != null && point.CanSpawn) validCount++;
            if (validCount == 0)
            {
                Debug.LogWarning("No active Spawn Point assigned: the player keeps the scene position.", this);
                return;
            }

            int selected = Random.Range(0, validCount);
            foreach (SpawnPoint point in spawnPoints)
            {
                if (point == null || !point.CanSpawn) continue;
                if (selected-- != 0) continue;
                Vector2 position = point.Position;
                ApplySpawnPosition(position);
                return;
            }
        }

        private void ApplySpawnPosition(Vector2 position)
        {
            player.position = new Vector3(position.x, position.y, player.position.z);
            Physics2D.SyncTransforms();
        }

        public void EndTest(TestEndEvent outcome)
        {
            PrepareTestEnding(outcome, false);
        }

        public bool BeginTestAnimation(TestEndEvent outcome) => PrepareTestEnding(outcome, true);

        public void CompleteTestAnimation(TestEndEvent outcome)
        {
            if (!isActiveAndEnabled || State != LoopState.DeathAnimation || animatedOutcome != outcome) return;
            animatedOutcome = null;
            State = LoopState.Flash;
            StartCoroutine(EndSequence(pendingComment, pendingAvatar));
        }

        private bool PrepareTestEnding(TestEndEvent outcome, bool waitForAnimation)
        {
            if (!isActiveAndEnabled || State != LoopState.Playing || outcome == null) return false;
            State = waitForAnimation ? LoopState.DeathAnimation : LoopState.Flash;
            hud.HidePhoto();
            completedTime = hud.ElapsedSeconds;
            completedResult = outcome.ChooseResult();
            pendingFlashDuration = outcome.FlashDurationOverride >= 0f
                ? outcome.FlashDurationOverride : flashDuration;
            GameSession.RecordTestResult(outcome.SubjectDied);
            previousTimeScale = Time.timeScale;
            ownsPause = true;
            Time.timeScale = 0f;
            hud.BeginTestEnding();

            DialogueMessagePlayer dialogue = outcome.ScientistComment;
            string comment = dialogue != null ? dialogue.ChooseMessage() : string.Empty;
            Sprite avatar = dialogue != null ? dialogue.Avatar : null;
            pendingComment = string.IsNullOrWhiteSpace(comment) ? fallbackComment : comment;
            pendingAvatar = avatar;
            if (waitForAnimation) animatedOutcome = outcome;
            else StartCoroutine(EndSequence(pendingComment, pendingAvatar));
            return true;
        }

        private IEnumerator EndSequence(string comment, Sprite avatar)
        {
            float elapsed = 0f;
            while (elapsed < pendingFlashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                hud.SetTestCoverOpacity(Mathf.Clamp01(elapsed / pendingFlashDuration));
                yield return null;
            }
            hud.SetTestCoverOpacity(1f);
            State = LoopState.ScientistComment;
            hud.ShowTestComment(comment, avatar);
        }

        private void OnTestCommentCompleted()
        {
            if (State != LoopState.ScientistComment) return;
            State = LoopState.Results;
            hud.ShowTestResult(Universe, completedTime, completedResult);
        }

        private void OnTestContinueRequested()
        {
            if (State != LoopState.Results) return;
            string scene = Universe <= 1 ? endGameSceneName : gameplaySceneName;
            if (Universe > 1 && GameSession.TryGetCheckpoint(out string checkpointScene, out _, out _))
                scene = checkpointScene;
            if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("The next test or ending scene is not included in Build Profiles: " + scene, this);
                return;
            }
            State = LoopState.Loading;
            GameSession.Advance();
            RestoreTimeScale();
            SceneManager.LoadScene(scene);
        }

        private void RestoreTimeScale()
        {
            if (!ownsPause) return;
            Time.timeScale = previousTimeScale;
            ownsPause = false;
        }

        private void OnDisable()
        {
            if (hud != null)
            {
                hud.TestCommentCompleted -= OnTestCommentCompleted;
                hud.TestContinueRequested -= OnTestContinueRequested;
            }
            StopAllCoroutines();
            RestoreTimeScale();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
