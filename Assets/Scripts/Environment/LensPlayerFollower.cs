using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent]
    public sealed class LensPlayerFollower : MonoBehaviour
    {
        [SerializeField, Tooltip("Player transform partially followed by the scientist's hand.")]
        private Transform target;
        [SerializeField, Tooltip("Bust whose local space defines the arm's reachable area. Its parallax and spawn alignment are inherited through this reference.")]
        private SpriteRenderer scientist;
        [SerializeField, Tooltip("Lens effect exposing the actual glass center, rather than the sprite pivot.")]
        private MagnifyingLens2D lens;
        [SerializeField, Tooltip("Lens sprite renderer. Depth and sorting remain controlled by its independent scene plane.")]
        private SpriteRenderer lensRenderer;
        [SerializeField, Tooltip("Minimum glass-center coordinates in the bust's local space. Drag the Scene view handle to edit.")]
        private Vector2 minimumLocalPosition = new Vector2(-1f, 0.1f);
        [SerializeField, Tooltip("Maximum glass-center coordinates in the bust's local space. Drag the Scene view handle to edit.")]
        private Vector2 maximumLocalPosition = new Vector2(1.5f, 1.25f);
        [SerializeField, Tooltip("Limit the hand to an ellipse inside the movement bounds, producing a rounded arm reach.")]
        private bool ellipticalReach = true;
        [SerializeField, Tooltip("Fraction of player displacement followed relative to the center of the arm area, on each axis. Zero locks the axis; one tracks fully until the reach limit.")]
        private Vector2 followScale = new Vector2(0.35f, 0.15f);
        [SerializeField, Min(0f), Tooltip("Seconds of smoothing relative to the moving bust. Zero applies the reachable target immediately.")]
        private float smoothTime = 0.2f;
        [SerializeField, Min(1f), Tooltip("World pixel grid used to align the glass center with the artwork.")]
        private float pixelsPerUnit = 32f;
        [SerializeField, Tooltip("Show the arm's reachable area in Scene view.")]
        private bool showGizmos = true;

        private Vector2 followedLocalPosition;
        private Vector2 velocity;
        private bool initialized;

        public SpriteRenderer Scientist => scientist;
        public SpriteRenderer LensRenderer => lensRenderer;
        public Transform LensTransform => lens != null ? lens.transform : null;
        public Transform BoundsSpace => scientist != null ? scientist.transform : null;
        public Vector2 MinimumLocalPosition => Vector2.Min(minimumLocalPosition, maximumLocalPosition);
        public Vector2 MaximumLocalPosition => Vector2.Max(minimumLocalPosition, maximumLocalPosition);
        public bool ShowGizmos => showGizmos;

        private void OnEnable()
        {
            initialized = false;
            velocity = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (target == null || scientist == null || lens == null || Time.deltaTime <= 0f) return;
            if (!initialized)
            {
                followedLocalPosition = ClampToReach(scientist.transform.InverseTransformPoint(lens.LensCenterWorld));
                initialized = true;
            }
            Vector2 center = (MinimumLocalPosition + MaximumLocalPosition) * 0.5f;
            Vector2 playerLocal = scientist.transform.InverseTransformPoint(target.position);
            Vector2 desired = ClampToReach(center + Vector2.Scale(playerLocal - center, followScale));
            followedLocalPosition = smoothTime > 0f
                ? Vector2.SmoothDamp(followedLocalPosition, desired, ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime)
                : desired;
            followedLocalPosition = ClampToReach(followedLocalPosition);
            Vector3 worldCenter = scientist.transform.TransformPoint(followedLocalPosition);
            float grid = Mathf.Max(1f, pixelsPerUnit);
            worldCenter.x = Mathf.Round(worldCenter.x * grid) / grid;
            worldCenter.y = Mathf.Round(worldCenter.y * grid) / grid;
            // Pixel snapping must not push the glass outside the arm's current reach.
            Vector2 boundedLocal = ClampToReach(scientist.transform.InverseTransformPoint(worldCenter));
            MoveGlassCenter(scientist.transform.TransformPoint(boundedLocal));
        }

        public void SetLocalLimit(bool minimum, Vector2 position)
        {
            if (minimum) minimumLocalPosition = position;
            else maximumLocalPosition = position;
        }

        public void CenterInBounds()
        {
            if (scientist == null || lens == null) return;
            Vector2 center = (MinimumLocalPosition + MaximumLocalPosition) * 0.5f;
            MoveGlassCenter(scientist.transform.TransformPoint(center));
            initialized = false;
            velocity = Vector2.zero;
        }

        private void MoveGlassCenter(Vector3 worldCenter)
        {
            Vector3 correction = worldCenter - lens.LensCenterWorld;
            correction.z = 0f;
            lens.transform.position += correction;
        }

        private Vector2 ClampToReach(Vector2 point)
        {
            Vector2 minimum = MinimumLocalPosition;
            Vector2 maximum = MaximumLocalPosition;
            Vector2 clamped = new Vector2(Mathf.Clamp(point.x, minimum.x, maximum.x),
                Mathf.Clamp(point.y, minimum.y, maximum.y));
            if (!ellipticalReach) return clamped;
            Vector2 center = (minimum + maximum) * 0.5f;
            Vector2 radii = (maximum - minimum) * 0.5f;
            Vector2 normalized = new Vector2(radii.x > 0f ? (clamped.x - center.x) / radii.x : 0f,
                radii.y > 0f ? (clamped.y - center.y) / radii.y : 0f);
            normalized = Vector2.ClampMagnitude(normalized, 1f);
            return center + Vector2.Scale(normalized, radii);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || BoundsSpace == null) return;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = BoundsSpace.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Vector2 center = (MinimumLocalPosition + MaximumLocalPosition) * 0.5f;
            Vector2 size = MaximumLocalPosition - MinimumLocalPosition;
            if (!ellipticalReach) Gizmos.DrawWireCube(center, size);
            else
            {
                const int segments = 48;
                Vector2 radii = size * 0.5f;
                Vector2 previousPoint = center + new Vector2(radii.x, 0f);
                for (int index = 1; index <= segments; index++)
                {
                    float angle = index * Mathf.PI * 2f / segments;
                    Vector2 point = center + new Vector2(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y);
                    Gizmos.DrawLine(previousPoint, point);
                    previousPoint = point;
                }
            }
            Gizmos.matrix = previous;
        }
    }
}
