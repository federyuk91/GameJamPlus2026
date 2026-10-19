using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class CollectibleGravity : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayers = 65;
        [SerializeField] private Vector2 footprintSize = new Vector2(0.90625f, 0.25f);
        [SerializeField] private Vector2 footprintOffset = new Vector2(0.015625f, -0.375f);
        [SerializeField, Min(0f)] private float gravity = 30f;
        [SerializeField, Min(0.01f)] private float maximumFallSpeed = 8f;
        [SerializeField, Min(0.001f)] private float skin = 0.002f;

        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private Transform support;
        private Vector3 supportLocalPoint;
        private Vector3 supportWorldPoint;
        private float fallSpeed;

        private void OnDisable()
        {
            support = null;
            fallSpeed = 0f;
        }

        private void FixedUpdate()
        {
            if (support != null && support.gameObject.activeInHierarchy)
                transform.position += support.TransformPoint(supportLocalPoint) - supportWorldPoint;

            fallSpeed = Mathf.Min(maximumFallSpeed, fallSpeed + gravity * Time.fixedDeltaTime);
            float displacement = fallSpeed * Time.fixedDeltaTime;
            Vector3 scale = transform.lossyScale;
            Vector2 size = new Vector2(footprintSize.x * Mathf.Abs(scale.x), footprintSize.y * Mathf.Abs(scale.y));
            Vector2 origin = transform.TransformPoint(footprintOffset);
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            Physics2D.SyncTransforms();
            int count = Physics2D.BoxCast(origin, size, transform.eulerAngles.z, Vector2.down,
                filter, hits, displacement + skin);

            float allowed = displacement;
            Collider2D ground = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = hits[i];
                Collider2D candidate = hit.collider;
                if (candidate == null || candidate.transform.IsChildOf(transform) || hit.normal.y <= 0.01f)
                    continue;
                // The mouse must pass through the pickup instead of becoming its support.
                if (candidate.GetComponentInParent<PlayerMovement>() != null) continue;
                float distance = Mathf.Max(0f, hit.distance - skin);
                if (distance > allowed) continue;
                allowed = distance;
                ground = candidate;
            }

            transform.position += Vector3.down * allowed;
            support = ground != null ? ground.transform : null;
            if (support != null)
            {
                fallSpeed = 0f;
                supportWorldPoint = transform.position;
                supportLocalPoint = support.InverseTransformPoint(supportWorldPoint);
            }
            Physics2D.SyncTransforms();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(footprintOffset, footprintSize);
        }
    }
}
