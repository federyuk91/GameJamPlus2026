using System;
using UnityEngine;

namespace GameJamPlus.UI
{
    [Serializable]
    public sealed class DialogueMessage
    {
        [SerializeField, Tooltip("Portrait used by this communication. Empty uses the HUD's default scientist.")]
        private Sprite avatar;
        [SerializeField, TextArea(2, 6), Tooltip("Alternative lines: one non-empty entry is chosen randomly for each occurrence. {universe} uses the current universe number.")]
        private string[] messages = new string[0];
        [SerializeField, Range(1f, 30f), Tooltip("Maximum time shown before automatic advance or closing, in seconds.")]
        private float duration = 7f;

        public Sprite Avatar => avatar;
        public float Duration => Mathf.Clamp(duration, 1f, 30f);
        public string ChooseMessage() => ChooseRandomMessage(messages);

        public static string ChooseRandomMessage(string[] alternatives)
        {
            if (alternatives == null) return string.Empty;
            int count = 0;
            foreach (string text in alternatives)
                if (!string.IsNullOrWhiteSpace(text)) count++;
            if (count == 0) return string.Empty;
            int selected = UnityEngine.Random.Range(0, count);
            foreach (string text in alternatives)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (selected-- == 0) return text;
            }
            return string.Empty;
        }
    }
}
