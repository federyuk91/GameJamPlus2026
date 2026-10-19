using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class CrateBuoyancy : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Acceleration at full submersion in units/s². Must exceed crate gravity for flotation.")]
        private float buoyancyAcceleration = 100f;
        [SerializeField, Min(0f)] private float waterDrag = 5f;
        [SerializeField, Range(0.5f, 2f), Tooltip("1 critically damps vertical flotation near the water surface.")]
        private float verticalDampingRatio = 1f;
        public float LiftStrength => Mathf.Max(0f, buoyancyAcceleration);
        public float VerticalDampingRatio => verticalDampingRatio;
        public float Drag(float submergedFraction) => Mathf.Max(0f, waterDrag) * submergedFraction;

        public Vector2 Integrate(Vector2 velocity, float gravity, float submergedFraction, float dt)
        {
            velocity.y += (buoyancyAcceleration * submergedFraction - gravity) * dt;
            return velocity * Mathf.Exp(-waterDrag * submergedFraction * dt);
        }
    }
}
