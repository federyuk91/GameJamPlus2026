using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(150), DisallowMultipleComponent, RequireComponent(typeof(PiranhaPatrol))]
    public sealed class PiranhaChase : MonoBehaviour
    {
        [SerializeField] private PiranhaPatrol patrol;
        [SerializeField] private PlayerSwimming targetSwimming;
        [SerializeField] private Collider2D targetCollider;
        [SerializeField, Min(0f)] private float speed = 3f;
        [SerializeField, Min(0.01f)] private float viewDistance = 5f;
        [SerializeField, Range(1f, 360f)] private float viewAngle = 100f;
        [SerializeField] private Vector2 eyeOffset = new Vector2(0.15f, 0f);
        [SerializeField] private LayerMask occluderLayers = 65;
        [SerializeField] private bool chasing;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        public bool IsChasing => chasing;
        public float ViewDistance => viewDistance;
        public float ViewAngle => viewAngle;
        public Vector2 ViewDirection => patrol != null ? patrol.FacingDirection : Vector2.left;
        public Vector2 EyePosition
        {
            get
            {
                Vector2 forward = ViewDirection;
                Vector2 up = new Vector2(-forward.y, forward.x);
                return (Vector2)transform.position + forward * eyeOffset.x + up * eyeOffset.y;
            }
        }

        private void Awake()
        {
            if (patrol == null) patrol = GetComponent<PiranhaPatrol>();
            if (targetSwimming == null || targetCollider == null)
            {
                Debug.LogError("PiranhaChase: assign the player's Target Swimming and Target Collider.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            chasing = CanSeePlayer();
            if (chasing) patrol.Chase(targetCollider.bounds.center, speed);
            else if (patrol != null) patrol.ResumePatrol();
        }

        private bool CanSeePlayer()
        {
            if (patrol == null || !patrol.isActiveAndEnabled || targetCollider == null || !targetCollider.enabled
                || !targetCollider.gameObject.activeInHierarchy || targetSwimming == null || !targetSwimming.CheckWater()) return false;
            Vector2 eye = EyePosition;
            Vector2 offset = (Vector2)targetCollider.bounds.center - eye;
            float distance = offset.magnitude;
            if (distance > viewDistance) return false;
            if (distance <= 0.0001f) return true;
            Vector2 direction = offset / distance;
            if (Vector2.Dot(ViewDirection, direction) < Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad)) return false;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(occluderLayers);
            filter.useTriggers = false;
            Physics2D.SyncTransforms();
            Physics2D.Raycast(eye, direction, filter, hits, distance);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)
                    || hit.collider.transform.IsChildOf(targetCollider.transform)) continue;
                if (hit.distance < distance) return false;
            }
            return true;
        }

        private void OnDisable()
        {
            chasing = false;
            if (patrol != null) patrol.ResumePatrol();
        }

        public void SetVision(float distance, float angle)
        {
            viewDistance = Mathf.Max(0.01f, distance);
            viewAngle = Mathf.Clamp(angle, 1f, 360f);
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 eye = EyePosition;
            Vector2 forward = ViewDirection;
            float start = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - viewAngle * 0.5f;
            Vector2 previous = eye;
            Gizmos.color = chasing ? Color.red : Color.yellow;
            for (int i = 0; i <= 40; i++)
            {
                float angle = (start + viewAngle * i / 40f) * Mathf.Deg2Rad;
                Vector2 point = eye + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * viewDistance;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
            Gizmos.DrawLine(previous, eye);
            if (chasing && targetCollider != null) Gizmos.DrawLine(eye, targetCollider.bounds.center);
        }
    }
}
