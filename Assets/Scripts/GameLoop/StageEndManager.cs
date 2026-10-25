using System.Collections;
using GameJamPlus.UI;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(-190), DisallowMultipleComponent]
    public sealed class StageEndManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameplayHudController hud;
        [SerializeField] private DialogueManager dialogues;
        [SerializeField, Min(0f), Tooltip("Default transition duration in seconds for endings without the exit tunnel image.")]
        private float defaultTransitionDuration = 1.5f;
        [SerializeField] private StageEndDefinition starvation = new StageEndDefinition();
        [SerializeField] private StageEndDefinition drowning = new StageEndDefinition();
        [SerializeField] private StageEndDefinition poisoning = new StageEndDefinition();
        [SerializeField] private StageEndDefinition crushing = new StageEndDefinition();
        [SerializeField] private StageEndDefinition electricShock = new StageEndDefinition();
        [SerializeField] private StageEndDefinition mouseTrap = new StageEndDefinition();
        [SerializeField] private StageEndDefinition fall = new StageEndDefinition();
        [SerializeField] private StageEndDefinition goal = new StageEndDefinition();
        [SerializeField] private StageEndDefinition cheeseGoal = new StageEndDefinition();
        [SerializeField] private StageEndDefinition photoGoal = new StageEndDefinition();
        [SerializeField] private StageEndDefinition piranha = new StageEndDefinition();
        private long nextRequest;
        private long pendingAnimation;
        private double completedTime;
        private string result;
        private string comment;
        private Sprite avatar;
        private float commentDuration;
        private float transitionDuration;
        private float previousTimeScale;
        private bool ownsPause;
        public StageEndDefinition Outcome { get; private set; }

        public static bool Request(StageEndCase ending, Object source = null)
        {
            GameManager manager = GameManager.Instance;
            return manager != null && manager.StageEnds != null
                && manager.StageEnds.Accept(ending, source, false, out _);
        }

        public static bool TryBeginAnimated(StageEndCase ending, Object source, out StageEndRequest request)
        {
            request = default;
            GameManager manager = GameManager.Instance;
            return manager != null && manager.StageEnds != null
                && manager.StageEnds.Accept(ending, source, true, out request);
        }

        private StageEndDefinition GetDefinition(StageEndCase ending)
        {
            switch (ending)
            {
                case StageEndCase.Starvation: return starvation;
                case StageEndCase.Drowning: return drowning;
                case StageEndCase.Poisoning: return poisoning;
                case StageEndCase.Crushing: return crushing;
                case StageEndCase.ElectricShock: return electricShock;
                case StageEndCase.MouseTrap: return mouseTrap;
                case StageEndCase.Fall: return fall;
                case StageEndCase.Goal: return goal;
                case StageEndCase.CheeseGoal: return cheeseGoal;
                case StageEndCase.PhotoGoal: return photoGoal;
                case StageEndCase.Piranha: return piranha;
                default: return null;
            }
        }

        private void OnEnable()
        {
            if (hud == null) return;
            hud.TestCommentCompleted += OnCommentCompleted;
            hud.TestContinueRequested += OnContinueRequested;
        }

        private bool Accept(StageEndCase ending, Object source, bool waitForAnimation, out StageEndRequest request)
        {
            request = default;
            StageEndDefinition definition = GetDefinition(ending);
            if (!isActiveAndEnabled || gameManager == null || !gameManager.isActiveAndEnabled
                || gameManager.State != GameManager.LoopState.Playing || definition == null || hud == null) return false;
            // Claim the conclusion before UI callbacks or session changes can request another ending.
            gameManager.SetStageState(waitForAnimation ? GameManager.LoopState.DeathAnimation : GameManager.LoopState.Flash);
            Outcome = definition;
            hud.HidePhoto();
            completedTime = hud.ElapsedSeconds;
            result = definition.ChooseResult();
            transitionDuration = definition.TransitionDurationOverride >= 0f ? definition.TransitionDurationOverride
                : definition.UseExitImage ? hud.ExitTransitionDuration : defaultTransitionDuration;
            GameSession.RecordObservation(definition.CompletionObservations);
            GameSession.RecordTestResult(definition.SubjectDied, completedTime, gameManager.SurvivingHealth);
            previousTimeScale = Time.timeScale;
            ownsPause = true;
            Time.timeScale = 0f;
            hud.BeginTestEnding(definition.UseExitImage);
            comment = string.Empty;
            DialogueMessage message = dialogues != null ? dialogues.ChooseEnding(definition.DialogueCase, out comment) : null;
            avatar = message != null ? message.Avatar : null;
            commentDuration = message != null ? message.Duration : 7f;
            long identifier = ++nextRequest;
            if (waitForAnimation)
            {
                pendingAnimation = identifier;
                request = new StageEndRequest(this, identifier);
            }
            else StartCoroutine(PlayTransition());
            Debug.Log($"Stage ending: {ending}, source: {source}, subject died: {definition.SubjectDied}. Scientist comment: {comment}", source);
            return true;
        }

        internal void CompleteAnimation(long identifier)
        {
            if (!isActiveAndEnabled || pendingAnimation == 0 || identifier != pendingAnimation
                || gameManager == null || !gameManager.isActiveAndEnabled
                || gameManager.State != GameManager.LoopState.DeathAnimation) return;
            pendingAnimation = 0;
            gameManager.SetStageState(GameManager.LoopState.Flash);
            StartCoroutine(PlayTransition());
        }

        private IEnumerator PlayTransition()
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                hud.SetTestCoverOpacity(Mathf.Clamp01(elapsed / transitionDuration));
                yield return null;
            }
            hud.SetTestCoverOpacity(1f);
            gameManager.SetStageState(GameManager.LoopState.ScientistComment);
            hud.ShowTestComment(comment, avatar, commentDuration);
        }

        private void OnCommentCompleted()
        {
            if (gameManager == null || !gameManager.isActiveAndEnabled
                || gameManager.State != GameManager.LoopState.ScientistComment) return;
            gameManager.SetStageState(GameManager.LoopState.Results);
            hud.ShowTestResult(gameManager.Universe, completedTime, result);
        }

        private void OnContinueRequested()
        {
            if (gameManager != null && gameManager.isActiveAndEnabled) gameManager.ContinueAfterStage();
        }

        internal void ReleasePause()
        {
            if (!ownsPause) return;
            Time.timeScale = previousTimeScale;
            ownsPause = false;
        }

        internal void Cancel()
        {
            StopAllCoroutines();
            pendingAnimation = 0;
            ReleasePause();
        }

        private void OnDisable()
        {
            if (hud != null)
            {
                hud.TestCommentCompleted -= OnCommentCompleted;
                hud.TestContinueRequested -= OnContinueRequested;
            }
            Cancel();
        }
    }
}
