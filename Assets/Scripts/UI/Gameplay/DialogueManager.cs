using GameJamPlus.Gameplay;
using UnityEngine;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent]
    public sealed class DialogueManager : MonoBehaviour
    {
        [SerializeField] private GameplayHudController hud;
        [Header("General")]
        [SerializeField] private DialogueMessage defaultEnding = new DialogueMessage();
        [SerializeField] private DialogueMessage welcome = new DialogueMessage();
        [Header("Player conditions")]
        [SerializeField] private DialogueMessage starvation = new DialogueMessage();
        [SerializeField] private DialogueMessage drowning = new DialogueMessage();
        [SerializeField] private DialogueMessage lowOxygen = new DialogueMessage();
        [Header("Collectibles")]
        [SerializeField] private DialogueMessage cheeseCollected = new DialogueMessage();
        [SerializeField] private DialogueMessage poisoned = new DialogueMessage();
        [SerializeField] private DialogueMessage cheeseGoal = new DialogueMessage();
        [SerializeField] private DialogueMessage photoGoal = new DialogueMessage();
        [Header("Environment")]
        [SerializeField] private DialogueMessage piranha = new DialogueMessage();
        [SerializeField] private DialogueMessage fallDeath = new DialogueMessage();
        [SerializeField] private DialogueMessage goal = new DialogueMessage();
        [SerializeField] private DialogueMessage electricShock = new DialogueMessage();
        [SerializeField] private DialogueMessage mouseTrap = new DialogueMessage();

        public DialogueMessage GetMessage(DialogueCase dialogueCase)
        {
            switch (dialogueCase)
            {
                case DialogueCase.DefaultEnding: return defaultEnding;
                case DialogueCase.Welcome: return welcome;
                case DialogueCase.Starvation: return starvation;
                case DialogueCase.Drowning: return drowning;
                case DialogueCase.LowOxygen: return lowOxygen;
                case DialogueCase.CheeseCollected: return cheeseCollected;
                case DialogueCase.Poisoned: return poisoned;
                case DialogueCase.CheeseGoal: return cheeseGoal;
                case DialogueCase.PhotoGoal: return photoGoal;
                case DialogueCase.Piranha: return piranha;
                case DialogueCase.FallDeath: return fallDeath;
                case DialogueCase.Goal: return goal;
                case DialogueCase.ElectricShock: return electricShock;
                case DialogueCase.MouseTrap: return mouseTrap;
                default: return null;
            }
        }

        public void Play(DialogueCase dialogueCase)
        {
            DialogueMessage message = GetMessage(dialogueCase);
            if (message == null || hud == null) return;
            string text = message.ChooseMessage();
            if (string.IsNullOrWhiteSpace(text)) return;
            hud.ShowScientistComment(Format(text), message.Avatar, message.Duration);
        }

        public DialogueMessage ChooseEnding(DialogueCase dialogueCase, out string text)
        {
            DialogueMessage message = GetMessage(dialogueCase);
            text = message != null ? message.ChooseMessage() : string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                message = defaultEnding;
                text = message != null ? message.ChooseMessage() : string.Empty;
            }
            text = Format(text);
            return message;
        }

        public string Format(string text) => text.Replace("{universe}", GameSession.Universe.ToString());
    }
}
