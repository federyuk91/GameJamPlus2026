using GameJamPlus.Gameplay;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class TheatreSpotlight : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D trackingArea;
        [SerializeField] private Transform aimingPivot;
        [SerializeField] private Light2D spotlight;
        [SerializeField, Tooltip("Optional player collider. When empty, uses the player assigned to the scene's Game Manager.")]
        private Collider2D targetCollider;
        [SerializeField, Min(0f), Tooltip("Maximum spotlight rotation speed in degrees per second while following the player or returning to its rest pose.")]
        private float rotationSpeed = 120f;
        [SerializeField, Min(0f), Tooltip("Seconds fading the spotlight on when the player enters its area, or off when the player leaves. Brightness is configured on the child Light 2D.")]
        private float fadeDuration = 0.3f;
        [SerializeField] private bool showGizmos = true;
        private Quaternion restRotation;
        private float activeIntensity;

        public bool IsTracking { get; private set; }

        private void Awake()
        {
            if (trackingArea == null || aimingPivot == null || spotlight == null)
            {
                Debug.LogError("Assign Tracking Area, Aiming Pivot and Spotlight on the theatre spotlight.", this);
                enabled = false;
                return;
            }
            trackingArea.isTrigger = true;
            restRotation = aimingPivot.localRotation;
            activeIntensity = spotlight.intensity;
            spotlight.intensity = 0f;
            spotlight.enabled = false;
        }

        private void LateUpdate()
        {
            if (Time.deltaTime <= 0f) return;
            if (targetCollider == null && GameManager.Instance != null)
                targetCollider = GameManager.Instance.PlayerCollider;
            IsTracking = ContainsPlayer();
            Quaternion desiredRotation = aimingPivot.parent != null
                ? aimingPivot.parent.rotation * restRotation : restRotation;
            if (IsTracking)
            {
                Vector2 direction = targetCollider.bounds.center - aimingPivot.position;
                if (direction.sqrMagnitude > 0.0001f)
                    desiredRotation = Quaternion.Euler(0f, 0f,
                        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            }
            aimingPivot.rotation = Quaternion.RotateTowards(aimingPivot.rotation,
                desiredRotation, rotationSpeed * Time.deltaTime);
            float desiredIntensity = IsTracking ? activeIntensity : 0f;
            spotlight.intensity = fadeDuration > 0f
                ? Mathf.MoveTowards(spotlight.intensity, desiredIntensity, activeIntensity * Time.deltaTime / fadeDuration)
                : desiredIntensity;
            spotlight.enabled = spotlight.intensity > 0.001f;
        }

        private bool ContainsPlayer()
        {
            if (!trackingArea.enabled || !trackingArea.gameObject.activeInHierarchy
                || targetCollider == null || !targetCollider.enabled
                || !targetCollider.gameObject.activeInHierarchy) return false;
            // Custom character movement does not produce Rigidbody2D trigger callbacks.
            Physics2D.SyncTransforms();
            ColliderDistance2D contact = trackingArea.Distance(targetCollider);
            return contact.isValid && contact.distance <= 0f;
        }

        private void OnDisable()
        {
            IsTracking = false;
            if (spotlight == null) return;
            spotlight.intensity = 0f;
            spotlight.enabled = false;
        }

        private void OnValidate()
        {
            if (trackingArea != null) trackingArea.isTrigger = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            if (trackingArea != null)
            {
                Gizmos.matrix = trackingArea.transform.localToWorldMatrix;
                Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.1f);
                Gizmos.DrawCube(trackingArea.offset, trackingArea.size);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(trackingArea.offset, trackingArea.size);
            }
            Gizmos.matrix = Matrix4x4.identity;
            if (spotlight != null)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.6f);
                float halfAngle = spotlight.pointLightOuterAngle * 0.5f;
                Vector3 origin = spotlight.transform.position;
                Vector3 forward = spotlight.transform.up;
                Vector3 previous = origin + Quaternion.AngleAxis(-halfAngle, Vector3.forward)
                    * forward * spotlight.pointLightOuterRadius;
                Gizmos.DrawLine(origin, previous);
                for (int segment = 1; segment <= 24; segment++)
                {
                    float angle = Mathf.Lerp(-halfAngle, halfAngle, segment / 24f);
                    Vector3 point = origin + Quaternion.AngleAxis(angle, Vector3.forward)
                        * forward * spotlight.pointLightOuterRadius;
                    Gizmos.DrawLine(previous, point);
                    previous = point;
                }
                Gizmos.DrawLine(previous, origin);
            }
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
