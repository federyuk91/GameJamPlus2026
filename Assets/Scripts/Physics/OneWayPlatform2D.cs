using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class OneWayPlatform2D : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Tolleranza sotto la superficie da cui il corpo può ancora trovare appoggio.")]
        private float surfaceTolerance = 0.02f;
        [SerializeField] private bool allowDropThrough = true;

        internal bool AllowsDropThrough => allowDropThrough;

        internal bool Blocks(BoxCollider2D platform, Vector2 center, Vector2 halfSize, Vector2 right,
            Vector2 up, Vector2 direction, Vector2 hitNormal, float skin)
        {
            Vector2 normal = platform.transform.up;
            if (Vector2.Dot(direction, normal) >= -0.01f || Vector2.Dot(hitNormal, normal) < 0.99f)
                return false;

            float bottom = Vector2.Dot(center, normal) - ProjectRadius(halfSize, right, up, normal);
            return bottom >= SurfaceHeight(platform, normal) - Mathf.Max(surfaceTolerance, skin);
        }

        internal bool HasCleared(BoxCollider2D platform, BoxCollider2D movingShape, float skin)
        {
            Vector2 normal = platform.transform.up;
            Bounds bodyBounds = movingShape.bounds;
            float top = Vector2.Dot(bodyBounds.center, normal)
                + ProjectRadius(bodyBounds.extents, Vector2.right, Vector2.up, normal);
            if (top < SurfaceHeight(platform, normal) - skin) return true;

            Vector2 tangent = platform.transform.right;
            float separation = Mathf.Abs(Vector2.Dot(bodyBounds.center - platform.bounds.center, tangent));
            float bodyRadius = ProjectRadius(bodyBounds.extents, Vector2.right, Vector2.up, tangent);
            return separation > bodyRadius + Mathf.Abs(platform.transform.lossyScale.x) * platform.size.x * 0.5f + skin;
        }

        private static float SurfaceHeight(BoxCollider2D platform, Vector2 normal)
        {
            Vector2 center = platform.transform.TransformPoint(platform.offset);
            return Vector2.Dot(center, normal) + Mathf.Abs(platform.transform.lossyScale.y) * platform.size.y * 0.5f;
        }

        private static float ProjectRadius(Vector2 halfSize, Vector2 right, Vector2 up, Vector2 axis)
            => halfSize.x * Mathf.Abs(Vector2.Dot(right, axis)) + halfSize.y * Mathf.Abs(Vector2.Dot(up, axis));
    }
}
