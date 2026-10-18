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
        [SerializeField, TextArea(2, 6)] private string[] resultMessages =
        {
            "The subject has completed the test."
        };

        public DialogueMessagePlayer ScientistComment => scientistComment;
        public bool SubjectDied => subjectDied;

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
