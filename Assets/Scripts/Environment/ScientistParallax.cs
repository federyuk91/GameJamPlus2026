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
        [SerializeField, Tooltip("Eye positions in the bust sprite, normalized from bottom-left.")]
        private Vector2 firstEyeUV = new Vector2(0.421875f, 0.5703125f);
        [SerializeField] private Vector2 secondEyeUV = new Vector2(0.59375f, 0.5703125f);
        [SerializeField, Min(0.01f), Tooltip("Horizontal camera travel in world units needed to move the lens from the first eye to the second.")]
        private float cameraTravelForSweep = 6f;
        [SerializeField, Range(0f, 1f)] private float initialSweepPosition;
        [SerializeField] private bool reverseSweep;
        [SerializeField, Min(0f)] private float sweepSmoothTime = 0.2f;
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
                sweepPosition = initialSweepPosition;
                initialized = true;
            }
            Vector3 cameraDelta = viewCamera.transform.position - cameraOrigin;
            bust.transform.position = bustOrigin + new Vector3(cameraDelta.x * parallaxFactor.x,
                cameraDelta.y * parallaxFactor.y, 0f);
            float direction = reverseSweep ? -1f : 1f;
            float desiredSweep = Mathf.Clamp01(initialSweepPosition + direction * cameraDelta.x / Mathf.Max(0.01f, cameraTravelForSweep));
            sweepPosition = sweepSmoothTime > 0f
                ? Mathf.SmoothDamp(sweepPosition, desiredSweep, ref sweepVelocity, sweepSmoothTime)
                : desiredSweep;
            Vector2 eyeUV = Vector2.Lerp(firstEyeUV, secondEyeUV, sweepPosition);
            Sprite sprite = bust.sprite;
            Vector2 eyeLocal = (Vector2.Scale(eyeUV, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
            if (bust.flipX) eyeLocal.x = -eyeLocal.x;
            if (bust.flipY) eyeLocal.y = -eyeLocal.y;
            Vector3 eyeWorld = bust.transform.TransformPoint(eyeLocal);
            Vector3 correction = eyeWorld - lens.LensCenterWorld;
            correction.z = 0f;
            lens.transform.position += correction;
        }

        private void OnDisable()
        {
            if (!initialized) return;
            if (bust != null) bust.transform.position = bustOrigin;
            if (lens != null) lens.transform.position = lensOrigin;
            initialized = false;
            sweepVelocity = 0f;
        }
    }
}
