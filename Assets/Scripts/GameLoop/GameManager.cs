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
            Loading
        }

        [SerializeField] private GameplayHudController hud;
        [Header("Player spawn")]
        [SerializeField] private Transform player;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private MousePlatformer.PlayerRecovery playerRecovery;
        [SerializeField] private MousePlatformer.PlatformCameraController gameplayCamera;
        [SerializeField, Tooltip("Istanze della scena. Selezione casuale fra i punti assegnati e attivi. La posizione indica il pivot del player.")]
        private SpawnPoint[] spawnPoints = new SpawnPoint[0];
        [Header("Game loop")]
        [SerializeField, Min(1)] private int startingUniverse = 10;
        [SerializeField] private string gameplaySceneName = "LoopScene";
        [SerializeField] private string endGameSceneName = "EndGame";
        [SerializeField, Min(0f)] private float flashDuration = 0.18f;
        [SerializeField, TextArea] private string fallbackComment = "Ottimo. Possiamo considerare conclusa questa prova.";
        private double completedTime;
        private string completedResult;
        private float previousTimeScale;
        private bool ownsPause;

        public static GameManager Instance { get; private set; }
        public LoopState State { get; private set; } = LoopState.Playing;
        public int Universe => GameSession.Universe;
        public GameplayHudController Hud => hud;
        public Collider2D PlayerCollider => playerCollider;
        public MousePlatformer.PlayerRecovery PlayerRecovery => playerRecovery;
        public IReadOnlyList<SpawnPoint> SpawnPoints => spawnPoints;
        public void SnapCameraToPlayer() => gameplayCamera?.SnapToTarget();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("La scena deve contenere un solo Game Manager.", this);
                enabled = false;
                return;
            }
            Instance = this;
            GameSession.EnsureStarted(startingUniverse);
            if (hud == null)
            {
                Debug.LogError("Assegna il Gameplay HUD al Game Manager.", this);
                enabled = false;
                return;
            }
            SpawnPlayer();
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
        }

        private void SpawnPlayer()
        {
            if (player == null)
            {
                Debug.LogWarning("Assegna il Player al Game Manager per usare gli Spawn Point.", this);
                return;
            }
            int validCount = 0;
            foreach (SpawnPoint point in spawnPoints ?? System.Array.Empty<SpawnPoint>())
                if (point != null && point.CanSpawn) validCount++;
            if (validCount == 0)
            {
                Debug.LogWarning("Nessuno Spawn Point attivo assegnato: il player mantiene la posizione della scena.", this);
                return;
            }

            int selected = Random.Range(0, validCount);
            foreach (SpawnPoint point in spawnPoints)
            {
                if (point == null || !point.CanSpawn) continue;
                if (selected-- != 0) continue;
                Vector2 position = point.Position;
                player.position = new Vector3(position.x, position.y, player.position.z);
                Physics2D.SyncTransforms();
                return;
            }
        }

        public void EndTest(TestEndEvent outcome)
        {
            if (!isActiveAndEnabled || State != LoopState.Playing || outcome == null) return;
            State = LoopState.Flash;
            hud.HidePhoto();
            completedTime = hud.ElapsedSeconds;
            completedResult = outcome.ChooseResult();
            GameSession.RecordTestResult(outcome.SubjectDied);
            previousTimeScale = Time.timeScale;
            ownsPause = true;
            Time.timeScale = 0f;
            hud.BeginTestEnding();

            DialogueMessagePlayer dialogue = outcome.ScientistComment;
            string comment = dialogue != null ? dialogue.ChooseMessage() : string.Empty;
            Sprite avatar = dialogue != null ? dialogue.Avatar : null;
            StartCoroutine(EndSequence(string.IsNullOrWhiteSpace(comment) ? fallbackComment : comment, avatar));
        }

        private IEnumerator EndSequence(string comment, Sprite avatar)
        {
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                hud.SetTestCoverOpacity(Mathf.Clamp01(elapsed / flashDuration));
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
            if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("La scena del prossimo test o di fine gioco non e inclusa nei Build Profiles: " + scene, this);
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
