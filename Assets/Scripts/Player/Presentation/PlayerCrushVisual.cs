using System;
using System.Collections;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class PlayerCrushVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private BoxCollider2D playerCollider;
        [SerializeField, Min(0f), Tooltip("Seconds flattening the mouse sprite and collider vertically after a crushing impact.")]
        private float crushDuration = 0.25f;
        [SerializeField, Min(0f), Tooltip("Seconds keeping the fully flattened mouse visible before the ending transition.")]
        private float crushedHoldDuration = 0.1f;
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private bool crushing;
        private Vector2 originalColliderSize;
        private Vector2 originalColliderOffset;
        private bool originalColliderEnabled;
        private Bounds originalBounds;

        private void Awake()
        {
            if (playerCollider == null) playerCollider = GetComponent<BoxCollider2D>();
        }

        public IEnumerator Play(Action<float> headHeightChanged = null)
        {
            if (visual == null || crushing) yield break;
            crushing = true;
            Transform spriteTransform = visual.transform;
            originalScale = spriteTransform.localScale;
            originalPosition = spriteTransform.localPosition;
            if (playerCollider != null)
            {
                originalColliderSize = playerCollider.size;
                originalColliderOffset = playerCollider.offset;
                originalColliderEnabled = playerCollider.enabled;
                originalBounds = playerCollider.bounds;
            }
            float bottom = visual.sprite != null ? visual.sprite.bounds.min.y : 0f;
            float elapsed = 0f;
            while (elapsed < crushDuration)
            {
                SetCompression(Mathf.SmoothStep(0f, 1f, elapsed / crushDuration), bottom);
                NotifyHeadHeight(headHeightChanged);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            SetCompression(1f, bottom);
            NotifyHeadHeight(headHeightChanged);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, crushedHoldDuration));
        }

        private void SetCompression(float progress, float bottom)
        {
            Vector3 scale = originalScale;
            scale.y *= 1f - progress;
            visual.transform.localScale = scale;
            // Preserve the sprite's bottom edge when its pivot is above its feet.
            Vector3 position = originalPosition;
            position.y += bottom * (originalScale.y - scale.y);
            visual.transform.localPosition = position;
            if (playerCollider != null)
            {
                Vector2 size = originalColliderSize;
                size.y = Mathf.Max(0.001f, size.y * (1f - progress));
                Vector2 offset = originalColliderOffset;
                offset.y -= (originalColliderSize.y - size.y) * 0.5f;
                playerCollider.size = size;
                playerCollider.offset = offset;
                // Unity does not support a zero-height box; remove the fully flattened obstacle.
                playerCollider.enabled = originalColliderEnabled && progress < 1f;
            }
        }

        private void NotifyHeadHeight(Action<float> headHeightChanged)
        {
            if (playerCollider == null) return;
            Physics2D.SyncTransforms();
            float height = playerCollider.enabled ? playerCollider.bounds.max.y : originalBounds.min.y;
            headHeightChanged?.Invoke(height);
        }

        private void OnDisable()
        {
            if (crushing && visual != null)
            {
                visual.transform.localScale = originalScale;
                visual.transform.localPosition = originalPosition;
            }
            if (crushing && playerCollider != null)
            {
                playerCollider.size = originalColliderSize;
                playerCollider.offset = originalColliderOffset;
                playerCollider.enabled = originalColliderEnabled;
            }
            crushing = false;
        }
    }
}
