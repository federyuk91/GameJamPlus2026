using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(200), DisallowMultipleComponent]
    public sealed class ScientistParallax : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private SpriteRenderer bust;
        [SerializeField] private MagnifyingLens2D lens;
        [SerializeField, Tooltip("Fraction of camera translation followed by the bust. 0 stays in world space, 1 stays at the same screen position.")]
        private Vector2 parallaxFactor = new Vector2(0.8f, 0.85f);
        [SerializeField, HideInInspector]
        private Vector2 firstEyeUV = new Vector2(0.421875f, 0.5703125f);
        [SerializeField, HideInInspector] private Vector2 secondEyeUV = new Vector2(0.59375f, 0.5703125f);
        [SerializeField, Min(0.01f), Tooltip("Horizontal camera travel in world units needed to move the lens between its left and right limits.")]
        private float cameraTravelForSweep = 6f;
        [SerializeField, Range(0f, 1f)] private float initialSweepPosition;
        [SerializeField] private bool reverseSweep;
        [SerializeField, Min(0f)] private float sweepSmoothTime = 0.2f;
        [Header("Lens Limits")]
        [SerializeField, HideInInspector] private bool customLensLimits;
        [SerializeField, Tooltip("Lens center limits in the bust transform's local space. Drag the handles in Scene view.")]
        private Vector2 lensLeftLimit;
        [SerializeField] private Vector2 lensRightLimit;
        [SerializeField, Tooltip("Additional world-space height above the configured limits, relative to the bust.")]
        private float lensVerticalOffset;
        [SerializeField] private bool showGizmos = true;

        [Header("Background Layers - Far To Near")]
        [SerializeField] private Transform farLayer;
        [SerializeField] private Transform distantLayer;
        [SerializeField] private Transform middleLayer;
        [SerializeField] private Transform nearLayer;
        [SerializeField, Tooltip("Camera translation followed by each group. 1 stays fixed on screen (far); 0 stays in world space (near).")]
        private Vector2 farParallaxFactor = new Vector2(0.95f, 0.95f);
        [SerializeField] private Vector2 distantParallaxFactor = new Vector2(0.9f, 0.9f);
        [SerializeField] private Vector2 middleParallaxFactor = new Vector2(0.85f, 0.85f);
        [SerializeField] private Vector2 nearParallaxFactor = new Vector2(0.8f, 0.8f);
        private Transform[] layerRoots;
        private Vector3[] layerOrigins;

        public bool ShowGizmos => showGizmos;
        public MagnifyingLens2D Lens => lens;
        public Vector3 LeftLimitWorld => LimitWorld(true);
        public Vector3 RightLimitWorld => LimitWorld(false);

        private Vector3 cameraOrigin;
        private Vector3 bustOrigin;
        private Vector3 lensOrigin;
        private float sweepPosition;
        private float sweepVelocity;
        private bool initialized;

        private void LateUpdate()
        {
            if (viewCamera == null || bust == null || bust.sprite == null || lens == null || Time.deltaTime <= 0f) return;
            if (!initialized)
            {
                cameraOrigin = viewCamera.transform.position;
                bustOrigin = bust.transform.position;
                lensOrigin = lens.transform.position;
                layerRoots = new Transform[] { farLayer, distantLayer, middleLayer, nearLayer };
                layerOrigins = new Vector3[layerRoots.Length];
                for (int index = 0; index < layerRoots.Length; index++)
                    if (layerRoots[index] != null) layerOrigins[index] = layerRoots[index].position;
                sweepPosition = initialSweepPosition;
                initialized = true;
            }
            Vector3 cameraDelta = viewCamera.transform.position - cameraOrigin;
            MoveLayer(0, cameraDelta, farParallaxFactor);
            MoveLayer(1, cameraDelta, distantParallaxFactor);
            MoveLayer(2, cameraDelta, middleParallaxFactor);
            MoveLayer(3, cameraDelta, nearParallaxFactor);
            bust.transform.position = bustOrigin + new Vector3(cameraDelta.x * parallaxFactor.x,
                cameraDelta.y * parallaxFactor.y, 0f);
            float direction = reverseSweep ? -1f : 1f;
            float desiredSweep = Mathf.Clamp01(initialSweepPosition + direction * cameraDelta.x / Mathf.Max(0.01f, cameraTravelForSweep));
            sweepPosition = sweepSmoothTime > 0f
                ? Mathf.SmoothDamp(sweepPosition, desiredSweep, ref sweepVelocity, sweepSmoothTime)
                : desiredSweep;
            Vector3 target = Vector3.Lerp(LeftLimitWorld, RightLimitWorld, sweepPosition);
            Vector3 correction = target - lens.LensCenterWorld;
            correction.z = 0f;
            lens.transform.position += correction;
        }

        private void MoveLayer(int index, Vector3 cameraDelta, Vector2 factor)
        {
            Transform root = layerRoots[index];
            if (root == null || root == bust.transform || root == lens.transform) return;
            root.position = layerOrigins[index] + new Vector3(cameraDelta.x * factor.x, cameraDelta.y * factor.y, 0f);
        }

        private Vector2 EyeLocal(Vector2 uv)
        {
            Sprite sprite = bust.sprite;
            Vector2 point = (Vector2.Scale(uv, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
            if (bust.flipX) point.x = -point.x;
            if (bust.flipY) point.y = -point.y;
            return point;
        }

        private Vector3 LimitWorld(bool left)
        {
            if (bust == null || bust.sprite == null) return transform.position;
            Vector2 point = customLensLimits ? (left ? lensLeftLimit : lensRightLimit)
                : EyeLocal(left ? firstEyeUV : secondEyeUV);
            return bust.transform.TransformPoint(point) + Vector3.up * lensVerticalOffset;
        }

        public void InitializeLensLimits()
        {
            if (customLensLimits || bust == null || bust.sprite == null) return;
            lensLeftLimit = EyeLocal(firstEyeUV);
            lensRightLimit = EyeLocal(secondEyeUV);
            customLensLimits = true;
        }

        public void SetLensLimitWorld(bool left, Vector3 position)
        {
            if (bust == null) return;
            InitializeLensLimits();
            Vector2 local = bust.transform.InverseTransformPoint(position - Vector3.up * lensVerticalOffset);
            if (left) lensLeftLimit = local;
            else lensRightLimit = local;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || bust == null) return;
            Vector3 left = LeftLimitWorld;
            Vector3 right = RightLimitWorld;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawWireSphere(left, 0.06f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(right, 0.06f);
        }

        private void OnDisable()
        {
            if (!initialized) return;
            for (int index = 0; index < layerRoots.Length; index++)
                if (layerRoots[index] != null) layerRoots[index].position = layerOrigins[index];
            if (bust != null) bust.transform.position = bustOrigin;
            if (lens != null) lens.transform.position = lensOrigin;
            initialized = false;
            sweepVelocity = 0f;
        }
    }
}
