using GameJamPlus.UI;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TestEndEvent : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private DialogueMessagePlayer scientistComment;
        [SerializeField, Tooltip("Disattiva per una prova conclusa con il soggetto ancora vivo.")]
        private bool subjectDied = true;
        [SerializeField, TextArea(2, 6)] private string[] resultMessages =
        {
            "Il soggetto ha completato la prova."
        };

        public DialogueMessagePlayer ScientistComment => scientistComment;
        public bool SubjectDied => subjectDied;

        public void EndTest()
        {
            // Prefabs cannot hold a reference to the scene's manager.
            GameManager manager = gameManager != null ? gameManager : GameManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("Nessun Game Manager disponibile per terminare il test.", this);
                return;
            }
            manager.EndTest(this);
        }

        public string ChooseResult()
        {
            return DialogueMessagePlayer.ChooseRandomMessage(resultMessages);
        }
    }
}
