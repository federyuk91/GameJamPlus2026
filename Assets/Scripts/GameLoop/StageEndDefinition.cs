using System;
using GameJamPlus.UI;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [Serializable]
    public sealed class StageEndDefinition
    {
        [SerializeField, Tooltip("Scientist communication chosen from DialogueManager after the transition.")]
        private DialogueCase dialogueCase;
        [SerializeField, Tooltip("Whether this conclusion counts as a deceased subject. Does not calculate damage or change the animal's health.")]
        private bool subjectDied = true;
        [SerializeField, Tooltip("Use the exit tunnel image instead of the ordinary flash.")]
        private bool useExitImage;
        [SerializeField, Tooltip("Observations recorded in the notebook when this conclusion is accepted.")]
        private TestObservation completionObservations;
        [SerializeField, Min(-1f), Tooltip("Transition duration in seconds. Negative uses Exit Tunnel Duration for goals or the manager's Default Transition Duration for ordinary endings.")]
        private float flashDurationOverride = -1f;
        [SerializeField, TextArea(2, 6), Tooltip("Alternative report lines for the notebook, distinct from the scientist's communication.")]
        private string[] resultMessages = new string[0];

        public DialogueCase DialogueCase => dialogueCase;
        public bool SubjectDied => subjectDied;
        public bool UseExitImage => useExitImage;
        public TestObservation CompletionObservations => completionObservations;
        public float TransitionDurationOverride => flashDurationOverride;
        public string ChooseResult() => DialogueMessage.ChooseRandomMessage(resultMessages);
    }
}
