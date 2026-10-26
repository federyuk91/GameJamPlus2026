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
        [SerializeField, Min(0.001f)] private float emptyPlateWeight = 0.2f;
        [SerializeField] private Vector2 consumedShapeSize = new Vector2(0.90625f, 0.25f);
        [SerializeField] private Vector2 consumedShapeOffset = new Vector2(0.015625f, -0.375f);
        [SerializeField, Tooltip("Frames for one through five empty plates, with their collider height in local units.")]
        private PlateFrame[] plateFrames = new PlateFrame[0];

        [System.Serializable]
        private struct PlateFrame
        {
            public Sprite sprite;
            [Min(0.001f)] public float height;
        }

        private const int MaximumPlates = 5;
        public int PlateCount { get; private set; }

        private bool CanStack => PlateCount > 0 && body != null && consumedPushable != null
            && consumedPushable.isActiveAndEnabled && plateFrames.Length >= MaximumPlates;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
        }

        public void Show()
        {
            if (PlateCount > 0) return;
            PlateCount = 1;
            if (visual != null && consumedSprite != null) visual.sprite = consumedSprite;
            if (body != null)
            {
                body.SetWeight(emptyPlateWeight);
                body.Shape.size = consumedShapeSize;
                body.Shape.offset = consumedShapeOffset;
            }
            if (consumedPushable != null)
            {
                consumedPushable.Body.Shape.isTrigger = true;
                consumedPushable.enabled = true;
                consumedPushable.EnableStacking();
            }
            RefreshPlateVisual();
            Physics2D.SyncTransforms();
        }

        private void FixedUpdate()
        {
            if (!CanStack) return;
            // PropsMotion has already resolved this tick: only merge through a supporting top contact.
            body.RefreshGround();
            Collider2D support = body.GroundCollider;
            if (!body.Grounded || support == null || !support.TryGetComponent(out CollectibleRemains lower)
                || lower == this || !lower.isActiveAndEnabled || !lower.CanStack
                || lower.body.Shape != support || lower.PlateCount >= MaximumPlates) return;

            Bounds incomingBounds = body.Shape.bounds;
            Bounds lowerBounds = support.bounds;
            if (incomingBounds.center.y <= lowerBounds.center.y
                || Mathf.Abs(incomingBounds.center.x - lowerBounds.center.x) > lowerBounds.extents.x) return;

            int transferred = Mathf.Min(PlateCount, MaximumPlates - lower.PlateCount);
            float transferredWeight = body.Weight * transferred / PlateCount;
            float previousTop = lowerBounds.max.y;
            lower.PlateCount += transferred;
            lower.body.SetWeight(lower.body.Weight + transferredWeight);
            lower.RefreshPlateVisual();
            PlateCount -= transferred;

            if (PlateCount == 0)
            {
                // Disable immediately so later movement queries cannot collide with the absorbed plate.
                gameObject.SetActive(false);
            }
            else
            {
                body.SetWeight(body.Weight - transferredWeight);
                RefreshPlateVisual();
                transform.position += Vector3.up * (lower.body.Shape.bounds.max.y - previousTop);
            }
            Physics2D.SyncTransforms();
        }

        private void RefreshPlateVisual()
        {
            if (plateFrames.Length < PlateCount || PlateCount <= 0) return;
            PlateFrame frame = plateFrames[PlateCount - 1];
            if (visual != null && frame.sprite != null) visual.sprite = frame.sprite;
            if (body == null) return;
            float height = Mathf.Max(0.001f, frame.height);
            float bottom = consumedShapeOffset.y - consumedShapeSize.y * 0.5f;
            body.Shape.size = new Vector2(consumedShapeSize.x, height);
            body.Shape.offset = new Vector2(consumedShapeOffset.x, bottom + height * 0.5f);
        }
    }
}
