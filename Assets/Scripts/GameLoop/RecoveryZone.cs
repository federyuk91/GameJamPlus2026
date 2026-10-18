using MousePlatformer;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(225), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class RecoveryZone : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D area;
        [SerializeField] private PlayerRecovery playerRecovery;
        [SerializeField, Min(1f)] private float searchRadius = 24f;
        [SerializeField, Min(0.05f)] private float sampleSpacing = 0.25f;
        [SerializeField, Range(0f, 10f)] private float flatSurfaceAngle = 1f;
        [SerializeField] private bool showGizmos = true;
        private bool wasInside;

        private void Awake()
        {
            if (area == null) area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.LoopState.Playing || !area.enabled) return;
            if (playerRecovery == null) playerRecovery = manager.PlayerRecovery;
            Collider2D playerCollider = playerRecovery != null ? playerRecovery.Shape : null;
            if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy) return;
            Physics2D.SyncTransforms();
            ColliderDistance2D contact = area.Distance(playerCollider);
            bool inside = contact.isValid && contact.distance <= 0f;
            if (inside && !wasInside)
            {
                wasInside = true;
                playerRecovery.TryRecover(searchRadius, sampleSpacing, flatSurfaceAngle);
            }
            else if (!inside) wasInside = false;
        }

        private void OnDisable() => wasInside = false;

        private void Reset()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }

        private void OnValidate()
        {
            if (area != null) area.isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || area == null) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = area.transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.06f);
            Gizmos.DrawCube(area.offset, area.size);
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.5f);
            Gizmos.DrawWireCube(area.offset, area.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
