using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class CrateBuoyancy : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Spinta a immersione completa in unità/s². Deve superare la gravità della cassa per galleggiare.")]
        private float buoyancyAcceleration = 100f;
        [SerializeField, Min(0f)] private float waterDrag = 5f;

        public Vector2 Integrate(Vector2 velocity, float gravity, float submergedFraction, float dt)
        {
            velocity.y += (buoyancyAcceleration * submergedFraction - gravity) * dt;
            return velocity * Mathf.Exp(-waterDrag * submergedFraction * dt);
        }
    }
}
