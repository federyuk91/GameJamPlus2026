using UnityEngine;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent]
    public sealed class DialogueMessagePlayer : MonoBehaviour
    {
        [SerializeField] private GameplayHudController hud;
        [SerializeField] private Sprite avatar;
        [SerializeField, TextArea(2, 6)] private string[] messages = new string[0];
        [SerializeField, Range(1f, 30f)] private float duration = 7f;

        public Sprite Avatar => avatar;
        public string ChooseMessage() => ChooseRandomMessage(messages);

        public void Play()
        {
            GameJamPlus.Gameplay.GameManager manager = GameJamPlus.Gameplay.GameManager.Instance;
            GameplayHudController target = hud != null ? hud : manager != null ? manager.Hud : null;
            if (target == null)
            {
                Debug.LogWarning("Assign the Gameplay HUD to the message component.", this);
                return;
            }
            string message = ChooseMessage();
            if (!string.IsNullOrWhiteSpace(message)) target.ShowScientistComment(message, avatar, duration);
        }

        public static string ChooseRandomMessage(string[] messages)
        {
            if (messages == null) return string.Empty;

            int validCount = 0;
            foreach (string message in messages)
                if (!string.IsNullOrWhiteSpace(message)) validCount++;
            if (validCount == 0) return string.Empty;

            int selected = Random.Range(0, validCount);
            foreach (string message in messages)
            {
                if (string.IsNullOrWhiteSpace(message)) continue;
                if (selected-- != 0) continue;
                return message;
            }
            return string.Empty;
        }
    }
}
