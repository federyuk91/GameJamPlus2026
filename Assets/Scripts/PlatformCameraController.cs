using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class PlatformCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerMovement movement;
        [InspectorName("Offset (units)")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.75f);
        [InspectorName("Dead Zone Half Size (units)")]
        [SerializeField] private Vector2 deadZone = new Vector2(0.75f, 0.5f);
        [InspectorName("Horizontal Smooth Time (s)")]
        [SerializeField, Min(0.01f)] private float horizontalSmoothTime = 0.12f;
        [InspectorName("Vertical Smooth Time (s)")]
        [SerializeField, Min(0.01f)] private float verticalSmoothTime = 0.18f;
        [InspectorName("Falling Smooth Time (s)")]
        [SerializeField, Min(0.01f)] private float fallingSmoothTime = 0.08f;
        [InspectorName("Look Ahead Distance (units)")]
        [SerializeField, Min(0f)] private float lookAheadDistance = 1.25f;
        [InspectorName("Falling Look Ahead (units)")]
        [SerializeField, Min(0f)] private float fallingLookAhead = 1f;
        [SerializeField, Min(1)] private int pixelsPerUnit = 32;

        // Keep the smoothing state off the pixel grid to avoid rounding stalls.
        private Vector2 position;
        private Vector2 smoothVelocity;
        private float lookAhead;
        private float depth;

        private void OnEnable()
        {
            depth = transform.position.z;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            position = target != null ? (Vector2)target.position + offset : (Vector2)transform.position;
            smoothVelocity = Vector2.zero;
            lookAhead = 0f;
            ApplyPosition();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector2 velocity = movement != null ? movement.Velocity : Vector2.zero;
            float dt = Time.deltaTime;
            lookAhead = Mathf.MoveTowards(lookAhead,
                Mathf.Clamp(velocity.x / 5f, -1f, 1f) * lookAheadDistance, 5f * dt);
            Vector2 focus = (Vector2)target.position + offset + new Vector2(lookAhead,
                -Mathf.Clamp01(-velocity.y / 15f) * fallingLookAhead);
            Vector2 desired = position;
            desired.x += OutsideDeadZone(focus.x - position.x, deadZone.x);
            desired.y += OutsideDeadZone(focus.y - position.y, deadZone.y);
            position.x = Mathf.SmoothDamp(position.x, desired.x, ref smoothVelocity.x,
                horizontalSmoothTime, Mathf.Infinity, dt);
            position.y = Mathf.SmoothDamp(position.y, desired.y, ref smoothVelocity.y,
                velocity.y < -1f ? fallingSmoothTime : verticalSmoothTime, Mathf.Infinity, dt);
            ApplyPosition();
        }

        private static float OutsideDeadZone(float distance, float halfSize)
        {
            return Mathf.Sign(distance) * Mathf.Max(0f, Mathf.Abs(distance) - Mathf.Max(0f, halfSize));
        }

        private void ApplyPosition()
        {
            float ppu = Mathf.Max(1, pixelsPerUnit);
            transform.position = new Vector3(Mathf.Round(position.x * ppu) / ppu,
                Mathf.Round(position.y * ppu) / ppu, depth);
        }
    }
}
