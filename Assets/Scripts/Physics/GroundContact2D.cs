using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class GroundContact2D : MonoBehaviour
    {
        [SerializeField, Tooltip("Body whose collision shape and filter are used to detect the supporting surface.")]
        private SimulatedBody2D body;
        private SimulatedBody2D Body => body != null ? body : body = GetComponent<SimulatedBody2D>();

        public bool HasContact { get; private set; }
        public bool IsWalkable { get; private set; }
        public bool IsSteep => HasContact && !IsWalkable;
        public Vector2 Normal { get; private set; } = Vector2.up;
        public Vector2 Point { get; private set; }
        public Collider2D Collider { get; private set; }
        public float Angle => Vector2.Angle(Normal, Vector2.up);

        public void Refresh(bool canGround = true, Collider2D ignored = null)
        {
            SimulatedBody2D owner = Body;
            // The vertical gap must be measured along the normal on steep ramps.
            Collider2D support = owner.Probe(Vector2.down, owner.Skin * 2f / 0.01f,
                out float distance, out Vector2 normal, out Vector2 point, ignored);
            HasContact = canGround && support != null && normal.y > 0.01f
                && distance * normal.y <= owner.Skin * 2f;
            Normal = HasContact ? normal : Vector2.up;
            Point = HasContact ? point : (Vector2)owner.transform.position;
            Collider = HasContact ? support : null;
            IsWalkable = HasContact && Angle <= owner.MaxSlopeAngle + 0.01f;
        }
    }
}
