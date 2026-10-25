using GameJamPlus.Gameplay;
using UnityEngine;

namespace GameJamPlus.UI
{
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField, Tooltip("Central dialogue case requested by this object's events. Text, portrait and timing are configured on the DialogueManager in Universe Rig.")]
        private DialogueCase dialogueCase;

        public void Play()
        {
            GameManager manager = GameManager.Instance;
            if (manager != null && manager.Dialogues != null) manager.Dialogues.Play(dialogueCase);
        }
    }
}
