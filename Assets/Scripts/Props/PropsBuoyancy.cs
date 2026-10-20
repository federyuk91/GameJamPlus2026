using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MousePlatformer
{
    [MovedFrom(true, sourceNamespace: "MousePlatformer", sourceAssembly: "Assembly-CSharp", sourceClassName: "CrateBuoyancy")]
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PropsBuoyancy : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Full-submersion lift strength used by PropsMotion. Flotation requires enough lift to balance the effective gravitational load, including supported bodies.")]
        private float buoyancyAcceleration = 100f;
        [SerializeField] private SimulatedBody2D body;
        private SimulatedBody2D Body => body != null ? body : body = GetComponent<SimulatedBody2D>();
        [SerializeField, Range(0.5f, 2f), Tooltip("1 critically damps vertical flotation near the water surface.")]
        private float verticalDampingRatio = 1f;
        public float LiftStrength => Mathf.Max(0f, buoyancyAcceleration);
        public float VerticalDampingRatio => verticalDampingRatio;
        public float Drag(float submergedFraction) => Body.WaterDrag * Mathf.Clamp01(submergedFraction);

        public Vector2 Integrate(Vector2 velocity, float gravity, float submergedFraction, float dt)
        {
            velocity.y += (buoyancyAcceleration * submergedFraction - gravity) * dt;
            return velocity * Mathf.Exp(-Drag(submergedFraction) * dt);
        }
    }
}
