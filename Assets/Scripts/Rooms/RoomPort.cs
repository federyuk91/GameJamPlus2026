using UnityEngine;

namespace MousePlatformer
{
    public enum RoomDirection { Up, Down, Left, Right }

    [DisallowMultipleComponent]
    public sealed class RoomPort : MonoBehaviour
    {
        [SerializeField] private RoomDirection direction;
        public RoomDirection Direction => direction;
        public Vector2 Position => transform.position;

        public static RoomDirection Opposite(RoomDirection value)
        {
            switch (value)
            {
                case RoomDirection.Up: return RoomDirection.Down;
                case RoomDirection.Down: return RoomDirection.Up;
                case RoomDirection.Left: return RoomDirection.Right;
                default: return RoomDirection.Left;
            }
        }

        public static Vector2 Axis(RoomDirection value)
        {
            switch (value)
            {
                case RoomDirection.Up: return Vector2.up;
                case RoomDirection.Down: return Vector2.down;
                case RoomDirection.Left: return Vector2.left;
                default: return Vector2.right;
            }
        }

        public bool CompatibleWith(RoomPort other) => other != null && other.Direction == Opposite(direction);

        private void OnDrawGizmosSelected()
        {
            Vector2 axis = Axis(direction);
            Vector2 position = transform.position;
            Vector2 end = position + axis * 0.6f;
            Vector2 side = new Vector2(-axis.y, axis.x);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(position, 0.1f);
            Gizmos.DrawLine(position, end);
            Gizmos.DrawLine(end, end - axis * 0.15f + side * 0.1f);
            Gizmos.DrawLine(end, end - axis * 0.15f - side * 0.1f);
        }
    }
}
