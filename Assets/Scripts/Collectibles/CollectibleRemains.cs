using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class CollectibleRemains : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite consumedSprite;
        [SerializeField, Tooltip("Optional pushing behaviour enabled after consumption. The empty plate stacks with props but remains pass-through for the animal.")]
        private Pushable consumedPushable;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private Vector2 consumedShapeSize = new Vector2(0.90625f, 0.25f);
        [SerializeField] private Vector2 consumedShapeOffset = new Vector2(0.015625f, -0.375f);

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
        }

        public void Show()
        {
            if (visual != null && consumedSprite != null) visual.sprite = consumedSprite;
            if (body != null)
            {
                body.SetCanPressButtons(false);
                body.Shape.size = consumedShapeSize;
                body.Shape.offset = consumedShapeOffset;
            }
            if (consumedPushable != null)
            {
                consumedPushable.Body.Shape.isTrigger = true;
                consumedPushable.enabled = true;
                consumedPushable.EnableStacking();
            }
            Physics2D.SyncTransforms();
        }
    }
}
