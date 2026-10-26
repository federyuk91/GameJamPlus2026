using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(220), DisallowMultipleComponent]
    public sealed class BackgroundParallaxLayer : MonoBehaviour
    {
        [SerializeField, Tooltip("Gameplay camera whose movement drives this background plane.")]
        private Camera viewCamera;
        [SerializeField, Tooltip("Fraction of camera movement followed on each axis. Zero anchors the plane to the world; one fixes it on screen.")]
        private Vector2 followFactor = new Vector2(0.8f, 0.8f);
        [SerializeField, Min(1f), Tooltip("Pixel grid used to keep background translation aligned with the sprite artwork.")]
        private float pixelsPerUnit = 32f;

        [Header("Optional Spawn Alignment")]
        [SerializeField, Tooltip("When assigned, aligns this background horizontally with the selected player spawn before starting parallax.")]
        private GameManager spawnManager;
        [SerializeField, Tooltip("Point to align with the spawn, for example the center of a sprite child. Uses this transform when unassigned.")]
        private Transform horizontalAlignmentPoint;
        [SerializeField, Tooltip("World-space horizontal offset from the selected spawn. Does not change height or depth.")]
        private float spawnOffsetX;

        private Vector3 layerOrigin;
        private Vector3 cameraOrigin;
        private bool initialized;

        private void OnEnable() => initialized = false;

        private void LateUpdate()
        {
            if (viewCamera == null || Time.deltaTime <= 0f) return;
            if (!initialized)
            {
                if (spawnManager != null)
                {
                    Transform alignment = horizontalAlignmentPoint != null ? horizontalAlignmentPoint : transform;
                    Vector3 position = transform.position;
                    position.x += spawnManager.SpawnPosition.x + spawnOffsetX - alignment.position.x;
                    transform.position = position;
                }
                layerOrigin = transform.position;
                cameraOrigin = viewCamera.transform.position;
                initialized = true;
            }

            Vector3 delta = viewCamera.transform.position - cameraOrigin;
            float grid = Mathf.Max(1f, pixelsPerUnit);
            Vector3 offset = new Vector3(
                Mathf.Round(delta.x * followFactor.x * grid) / grid,
                Mathf.Round(delta.y * followFactor.y * grid) / grid, 0f);
            transform.position = layerOrigin + offset;
        }
    }
}
