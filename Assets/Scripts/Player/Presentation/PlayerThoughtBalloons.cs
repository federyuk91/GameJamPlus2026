using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(320), DisallowMultipleComponent]
    public sealed class PlayerThoughtBalloons : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer playerVisual;
        [SerializeField] private SpriteRenderer balloonVisual;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private MouseFacing facing;
        [SerializeField] private Vector2 offset = new Vector2(0.45f, 0.34f);

        private readonly Queue<TimedBalloon> queued = new Queue<TimedBalloon>();
        private TimedBalloon current;
        private double expiresAt;
        private Sprite persistentLeft;
        private Sprite persistentRight;

        private readonly struct TimedBalloon
        {
            public readonly Sprite Left;
            public readonly Sprite Right;
            public readonly float Duration;

            public TimedBalloon(Sprite left, Sprite right, float duration)
            {
                Left = left;
                Right = right;
                Duration = duration;
            }
        }

        public void Show(Sprite left) => Show(left, null);

        public void Show(Sprite left, Sprite right, float duration = 1.5f)
        {
            if (isActiveAndEnabled && left != null && duration > 0f)
                queued.Enqueue(new TimedBalloon(left, right, duration));
        }

        public void SetPersistent(Sprite left, Sprite right = null)
        {
            persistentLeft = left;
            persistentRight = right;
        }

        public void Clear()
        {
            queued.Clear();
            current = default;
            SetPersistent(null);
            if (balloonVisual != null) balloonVisual.enabled = false;
        }

        private void LateUpdate()
        {
            if (balloonVisual == null || playerVisual == null) return;

            double now = Time.timeAsDouble;
            if (current.Left != null && now >= expiresAt) current = default;
            if (current.Left == null && queued.Count > 0)
            {
                current = queued.Dequeue();
                expiresAt = now + current.Duration;
            }

            bool hasTimedBalloon = current.Left != null;
            Sprite left = hasTimedBalloon ? current.Left : persistentLeft;
            Sprite right = hasTimedBalloon ? current.Right : persistentRight;
            balloonVisual.enabled = left != null && playerVisual.enabled && playerVisual.gameObject.activeInHierarchy;
            if (!balloonVisual.enabled) return;

            float horizontalSpeed = movement != null ? movement.Velocity.x : 0f;
            bool onRight = Mathf.Abs(horizontalSpeed) > 0.05f
                ? horizontalSpeed > 0f
                : facing != null && facing.FacingRight;
            balloonVisual.sprite = onRight && right != null ? right : left;
            balloonVisual.flipX = onRight && right == null;
            balloonVisual.sortingLayerID = playerVisual.sortingLayerID;
            balloonVisual.sortingOrder = playerVisual.sortingOrder + 1;

            Bounds bounds = playerVisual.bounds;
            float x = bounds.center.x + (onRight ? offset.x : -offset.x);
            balloonVisual.transform.position = new Vector3(x, bounds.max.y + offset.y, bounds.center.z);
        }

        private void OnDisable() => Clear();
    }
}
