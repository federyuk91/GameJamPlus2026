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
        [SerializeField, Min(1), Tooltip("Photos collected by the current subject before curiosity ends the test, after closing the photo.")]
        private int photosToEndTest = 3;
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
            bool endsTest = GameSession.PhotosCollectedThisTest >= Mathf.Max(1, photosToEndTest);
            target.ShowPhoto(photo, () => ContinuePhoto(endsTest));
        }

        private void ContinuePhoto(bool endsTest)
        {
            onContinue.Invoke();
            if (endsTest && curiosityEnding != null) curiosityEnding.EndTest();
        }
    }
}
