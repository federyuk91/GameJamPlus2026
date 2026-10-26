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
        [SerializeField] private DialogueManager dialogues;
        [SerializeField] private StageEndManager stageEnds;
        [SerializeField] private MousePlatformer.CollectionGoalController collectionGoals;
        [Header("Player spawn")]
        [SerializeField] private Transform player;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private MousePlatformer.PlayerRecovery playerRecovery;
        [SerializeField] private MousePlatformer.PlayerCrushVisual playerCrushVisual;
        [SerializeField] private MousePlatformer.PlayerStats playerStats;
        [SerializeField] private MousePlatformer.PlatformCameraController gameplayCamera;
        [SerializeField, Tooltip("Assigned active spawn points are shuffled without replacement across retries. All are used before a new cycle, avoiding immediate repetition between cycles.")]
        private SpawnPoint[] spawnPoints = new SpawnPoint[0];
        [SerializeField] private HandReleaseIntro handReleaseIntro;
        [Header("Game loop")]
        [SerializeField] private UniverseCampaign campaign;
        [SerializeField, Range(1, 10)] private int startingUniverse = 10;
        [SerializeField] private string gameplaySceneName = "LoopScene";
        [SerializeField] private string endGameSceneName = "EndGame";
        public static GameManager Instance { get; private set; }
        public LoopState State { get; private set; } = LoopState.Playing;
        public int Universe => GameSession.Universe;
        public string SubjectId => GameSession.SubjectId;
        public int Attempt => GameSession.Attempt;
        public GameplayHudController Hud => hud;
        public DialogueManager Dialogues => dialogues;
        public StageEndManager StageEnds => stageEnds;
        public MousePlatformer.CollectionGoalController CollectionGoals => collectionGoals;
        public StageEndDefinition EndingOutcome => stageEnds != null ? stageEnds.Outcome : null;
        public int SurvivingHealth => playerStats != null ? playerStats.Health : MousePlatformer.PlayerStats.MaximumHealth;
        internal void SetStageState(LoopState state) => State = state;
        public Collider2D PlayerCollider => playerCollider;
        public MousePlatformer.PlayerRecovery PlayerRecovery => playerRecovery;
        public MousePlatformer.PlayerCrushVisual PlayerCrushVisual => playerCrushVisual;
        public IReadOnlyList<SpawnPoint> SpawnPoints => spawnPoints;
        public Vector2 SpawnPosition { get; private set; }
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
            if (playerStats == null && player != null) player.TryGetComponent(out playerStats);
            GameSession.EnsureStarted(startingUniverse, campaign != null ? campaign.AttemptsPerUniverse : 10,
                campaign != null ? campaign.MaximumDeaths : 100);
            if (playerStats != null) playerStats.SetHealth(GameSession.SubjectHealth);
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

        private void Start()
        {
            hud.SetUniverse(Universe);
            hud.SetSubjectIdentity(SubjectId, Attempt, GameSession.AttemptsPerUniverse);
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
            SpawnPosition = player.position;
            if (campaign == null && GameSession.TryGetCheckpoint(out string checkpointScene, out bool hasPosition, out Vector2 checkpointPosition)
                && hasPosition && checkpointScene == gameObject.scene.path)
            {
                ApplySpawnPosition(checkpointPosition);
                return;
            }
            List<int> available = new List<int>();
            for (int index = 0; spawnPoints != null && index < spawnPoints.Length; index++)
                if (spawnPoints[index] != null && spawnPoints[index].CanSpawn) available.Add(index);
            if (available.Count == 0)
            {
                Debug.LogWarning("No active Spawn Point assigned: the player keeps the scene position.", this);
                return;
            }

            int selected = GameSession.SelectSpawn(gameObject.scene.path, available);
            ApplySpawnPosition(spawnPoints[selected].Position);
        }

        private void ApplySpawnPosition(Vector2 position)
        {
            SpawnPosition = position;
            player.position = new Vector3(position.x, position.y, player.position.z);
            Physics2D.SyncTransforms();
        }

        internal void ContinueAfterStage()
        {
            if (State != LoopState.Results) return;
            string scene;
            if (campaign != null)
            {
                int nextUniverse = Universe - (GameSession.WillAdvanceUniverse ? 1 : 0);
                scene = GameSession.WillEndCampaign ? campaign.EndGameScene : campaign.SceneForUniverse(nextUniverse);
            }
            else scene = Universe <= 1 ? endGameSceneName : gameplaySceneName;
            if (campaign == null && Universe > 1 && GameSession.TryGetCheckpoint(out string checkpointScene, out _, out _))
                scene = checkpointScene;
            if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("The next test or ending scene is not included in Build Profiles: " + scene, this);
                return;
            }
            State = LoopState.Loading;
            GameSession.Advance(campaign != null);
            stageEnds?.ReleasePause();
            SceneManager.LoadScene(scene);
        }

        private void OnDisable() => stageEnds?.Cancel();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
