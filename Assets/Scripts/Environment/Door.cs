using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class Door : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private bool startsOpen;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (blockingCollider == null) blockingCollider = GetComponent<Collider2D>();
            if (closedSprite == null && visual != null) closedSprite = visual.sprite;
            SetOpen(startsOpen);
        }

        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);
        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool value)
        {
            IsOpen = value;
            Sprite sprite = value ? openSprite : closedSprite;
            if (visual != null && sprite != null) visual.sprite = sprite;
            if (blockingCollider != null) blockingCollider.enabled = !value;
            if (Application.isPlaying) Physics2D.SyncTransforms();
        }
    }
}
