using GameJamPlus.Gameplay;
using GameJamPlus.UI;
using UnityEngine;
using UnityEngine.Events;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class CollectiblePhoto : MonoBehaviour
    {
        [SerializeField] private GameplayHudController hud;
        [SerializeField] private Collectible collectible;
        [SerializeField, Tooltip("Photo displayed on collection. Leave empty to show a placeholder.")]
        private Texture2D photo;
        [SerializeField] private UnityEvent onContinue = new UnityEvent();
        private bool continued;

        private void Awake()
        {
            if (collectible == null) collectible = GetComponent<Collectible>();
        }

        public void Play()
        {
            GameManager manager = GameManager.Instance;
            GameplayHudController target = hud != null ? hud : manager != null ? manager.Hud : null;
            if (target == null)
            {
                Debug.LogWarning("Assign the Gameplay HUD or add the Game Manager to display the photo.", this);
                return;
            }
            continued = false;
            target.ShowPhoto(photo, ContinuePhoto);
        }

        private void ContinuePhoto()
        {
            if (continued) return;
            continued = true;
            onContinue.Invoke();
            // The collectible is already hidden, but its collector owns the run's close event.
            if (collectible != null && collectible.Collector != null)
                collectible.Collector.NotifyPhotoClosed(collectible);
        }
    }
}
