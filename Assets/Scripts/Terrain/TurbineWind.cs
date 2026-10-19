using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class TurbineWind : MonoBehaviour
    {
        private static readonly List<TurbineWind> activeTurbines = new List<TurbineWind>();
        [SerializeField] private BoxCollider2D windArea;
        [SerializeField] private Transform outlet;
        [SerializeField] private Animator fanAnimator;
        [SerializeField] private bool running = true;
        [SerializeField, Min(0f), Tooltip("Acceleration for a weight-1 crate or a balloon. Heavier crates accelerate less.")]
        private float acceleration = 12f;
        [SerializeField, Range(0f, 1f)] private float farEndStrength = 0.5f;
        [SerializeField] private bool blockedByWalls = true;
        [SerializeField] private LayerMask blockingLayers = 65;
        [SerializeField] private bool showWindGizmos = true;
        [SerializeField, Range(4, 64)] private int gizmoColumns = 24;
        [SerializeField, Range(2, 32)] private int gizmoRows = 8;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];

        public bool Running => running;
        public void SetRunning(bool value)
        {
            running = value;
            UpdateAnimationState();
        }

        public void ToggleRunning() => SetRunning(!running);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => activeTurbines.Clear();

        private void OnEnable()
        {
            activeTurbines.Add(this);
            UpdateAnimationState();
        }

        private void OnDisable()
        {
            activeTurbines.Remove(this);
            UpdateAnimationState();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) UpdateAnimationState();
        }

        private void UpdateAnimationState()
        {
            if (fanAnimator != null) fanAnimator.speed = running && isActiveAndEnabled ? 1f : 0f;
        }

        public static Vector2 AccelerationAt(Collider2D target)
        {
            Vector2 total = Vector2.zero;
            if (target == null || !target.enabled) return total;
            foreach (TurbineWind turbine in activeTurbines)
                total += turbine.Sample(target);
            return total;
        }

        private Vector2 Sample(Collider2D target)
        {
            if (!running || windArea == null || !windArea.enabled || outlet == null
                || !windArea.gameObject.activeInHierarchy || !windArea.bounds.Intersects(target.bounds)
                || !windArea.Distance(target).isOverlapped) return Vector2.zero;
            Vector2 direction = outlet.right;
            Vector2 point = target.ClosestPoint(outlet.position);
            if (TryGetBlocker(point, target, out RaycastHit2D blocker)) return Vector2.zero;

            Vector2 localPoint = windArea.transform.InverseTransformPoint(target.bounds.center);
            float fraction = Mathf.InverseLerp(windArea.offset.x - windArea.size.x * 0.5f,
                windArea.offset.x + windArea.size.x * 0.5f, localPoint.x);
            return direction * (acceleration * Mathf.Lerp(1f, farEndStrength, fraction));
        }

        private bool TryGetBlocker(Vector2 point, Collider2D target, out RaycastHit2D nearest)
        {
            nearest = default;
            if (!blockedByWalls) return false;
            Vector2 start = outlet.position;
            Vector2 delta = point - start;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(blockingLayers);
            filter.useTriggers = false;
            int count = Physics2D.Raycast(start, delta.normalized, filter, hits, delta.magnitude);
            bool blocked = false;
            for (int i = 0; i < count; i++)
            {
                Collider2D obstacle = hits[i].collider;
                if (obstacle == null || obstacle == target || obstacle.transform.IsChildOf(transform)) continue;
                if (blocked && hits[i].distance >= nearest.distance) continue;
                nearest = hits[i];
                blocked = true;
            }
            return blocked;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showWindGizmos || windArea == null) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            bool active = running && isActiveAndEnabled && windArea.enabled && windArea.gameObject.activeInHierarchy;
            Color clearColor = new Color(0.25f, 1f, 0.6f, 0.15f);
            Color blockedColor = new Color(1f, 0.25f, 0.2f, 0.22f);
            Gizmos.color = active ? new Color(0.4f, 0.85f, 1f, 0.8f) : Color.gray;
            Gizmos.matrix = windArea.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(windArea.offset, windArea.size);
            if (outlet != null && active)
            {
                Physics2D.SyncTransforms();
                int columns = Mathf.Clamp(gizmoColumns, 4, 64);
                int rows = Mathf.Clamp(gizmoRows, 2, 32);
                Vector2 cell = new Vector2(windArea.size.x / columns, windArea.size.y / rows);
                Vector2 minimum = windArea.offset - windArea.size * 0.5f;
                // Sample the same outlet-to-target visibility used by actual wind, including partial shadows.
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        Vector2 center = minimum + new Vector2((column + 0.5f) * cell.x, (row + 0.5f) * cell.y);
                        Vector2 worldPoint = windArea.transform.TransformPoint(center);
                        Gizmos.color = TryGetBlocker(worldPoint, null, out RaycastHit2D blocker) ? blockedColor : clearColor;
                        Gizmos.DrawCube(center, new Vector3(cell.x * 0.95f, cell.y * 0.95f, 0.001f));
                    }
                }
                Gizmos.matrix = Matrix4x4.identity;
                for (int row = 0; row < rows; row++)
                {
                    Vector2 edge = minimum + new Vector2(windArea.size.x, (row + 0.5f) * cell.y);
                    Vector3 end = windArea.transform.TransformPoint(edge);
                    bool blocked = TryGetBlocker(end, null, out RaycastHit2D hit);
                    Vector3 stop = blocked ? new Vector3(hit.point.x, hit.point.y, outlet.position.z) : end;
                    Gizmos.color = new Color(0.25f, 1f, 0.6f, 0.6f);
                    Gizmos.DrawLine(outlet.position, stop);
                    if (!blocked) continue;
                    Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.75f);
                    Gizmos.DrawLine(stop, end);
                    Gizmos.DrawWireSphere(stop, 0.045f);
                }
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(outlet.position, outlet.right);
            }
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
