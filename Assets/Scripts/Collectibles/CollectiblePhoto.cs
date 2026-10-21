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
        [SerializeField, Tooltip("Unique ID for this photo instance across the run. Assign a different ID to each photo placed in a scene.")]
        private string collectionId;
        [SerializeField, Tooltip("Photo displayed on collection. Leave empty to show a placeholder.")]
        private Texture2D photo;
        [SerializeField, Min(1), Tooltip("Number of collected photos needed to end the trial after closing the photo. Counts quantities independently of collection IDs.")]
        private int photosToEndTest = 3;
        [SerializeField, Tooltip("Off: counts only this level. On: uses the session total displayed in the HUD.")]
        private bool useSessionTotal = true;
        [SerializeField] private TestEndEvent curiosityEnding;
        [SerializeField] private UnityEvent onContinue = new UnityEvent();

        private void Start()
        {
            // All scene Awake calls, including session initialization, have completed.
            if (GameSession.HasCollectedPhoto(collectionId)) gameObject.SetActive(false);
            else if (string.IsNullOrWhiteSpace(collectionId))
                Debug.LogWarning("Assign a unique Collection Id to keep this photo collected across level loops.", this);
        }

        public void Play()
        {
            GameSession.RememberPhoto(collectionId);
            GameManager manager = GameManager.Instance;
            GameplayHudController target = hud != null ? hud : manager != null ? manager.Hud : null;
            if (target == null)
            {
                Debug.LogWarning("Assign the Gameplay HUD or add the Game Manager to display the photo.", this);
                return;
            }
            target.ShowPhoto(photo, ContinuePhoto);
        }

        private void ContinuePhoto()
        {
            onContinue.Invoke();
            int count = useSessionTotal ? GameSession.GetCollectibleCount(CollectibleType.Photo)
                : GameSession.PhotosCollectedThisTest;
            if (count < Mathf.Max(1, photosToEndTest)) return;
            if (curiosityEnding == null)
            {
                Debug.LogError("Assign Curiosity Ending to end the trial at the photo quantity threshold.", this);
                return;
            }
            curiosityEnding.EndTest();
        }
    }
}
