using GameJamPlus.UI;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TestEndEvent : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private DialogueMessagePlayer scientistComment;
        [SerializeField, Tooltip("Disable for a test that ends with the subject still alive.")]
        private bool subjectDied = true;
        [SerializeField, Tooltip("Show the full-screen exit image with zoom and exposure before the scientist comment.")]
        private bool useExitImage;
        [SerializeField, InspectorName("Transition Duration Override"), Min(-1f), Tooltip("Duration in seconds for this ending. Negative: use Gameplay HUD / Exit Tunnel / Duration for the exit image, or Game Manager / Transition Duration for other endings.")]
        private float flashDurationOverride = -1f;
        [SerializeField, TextArea(2, 6)] private string[] resultMessages =
        {
            "The subject has completed the stage."
        };

        public DialogueMessagePlayer ScientistComment => scientistComment;
        public bool SubjectDied => subjectDied;
        public bool UseExitImage => useExitImage;
        public float FlashDurationOverride => flashDurationOverride;

        public void EndTest()
        {
            // Prefabs cannot hold a reference to the scene's manager.
            GameManager manager = gameManager != null ? gameManager : GameManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("No Game Manager is available to end the test.", this);
                return;
            }
            manager.EndTest(this);
        }

        public string ChooseResult()
        {
            return DialogueMessagePlayer.ChooseRandomMessage(resultMessages);
        }

        public bool BeginAnimatedEnd()
        {
            GameManager manager = gameManager != null ? gameManager : GameManager.Instance;
            return manager != null && manager.BeginTestAnimation(this);
        }

        public void CompleteAnimatedEnd()
        {
            GameManager manager = gameManager != null ? gameManager : GameManager.Instance;
            if (manager != null) manager.CompleteTestAnimation(this);
        }
    }
}
