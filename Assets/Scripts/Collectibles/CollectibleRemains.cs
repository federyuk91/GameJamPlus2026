using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class CollectibleRemains : MonoBehaviour
    {
        [SerializeField, Tooltip("Unique ID for this placed collectible. Use a different ID for every instance, including duplicates.")]
        private string collectionId;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite consumedSprite;

        public bool IsConsumed => GameSession.HasConsumedCollectible(collectionId);

        public void MarkConsumed()
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                Debug.LogWarning("Assign a unique Collection Id to preserve this collectible across level loops.", this);
            GameSession.RememberConsumedCollectible(collectionId);
        }

        public void Show()
        {
            if (visual != null && consumedSprite != null) visual.sprite = consumedSprite;
        }
    }
}
