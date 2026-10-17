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
        [SerializeField, Tooltip("Foto da mostrare alla raccolta. Vuoto mostra un segnaposto.")]
        private Texture2D photo;
        [SerializeField] private UnityEvent onContinue = new UnityEvent();

        public void Play()
        {
            GameManager manager = GameManager.Instance;
            GameplayHudController target = hud != null ? hud : manager != null ? manager.Hud : null;
            if (target == null)
            {
                Debug.LogWarning("Assegna il Gameplay HUD o aggiungi il Game Manager per mostrare la foto.", this);
                return;
            }
            target.ShowPhoto(photo, onContinue.Invoke);
        }
    }
}
