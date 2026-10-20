using UnityEngine;

namespace MousePlatformer
{
    internal static class CrateFlotation
    {
        public static float EffectiveWeight(float dryWeight, float submergedFraction, float fullSubmersionWeightMultiplier)
            => dryWeight * Mathf.Lerp(1f, Mathf.Clamp01(fullSubmersionWeightMultiplier), Mathf.Clamp01(submergedFraction));

        public static float IntegrateVertical(float velocity, float force, float mass, float drag,
            float stiffness, float dampingRatio, float maximumSpeed, float dt)
        {
            mass = Mathf.Max(0.01f, mass);
            float damping = Mathf.Max(drag, 2f * dampingRatio * Mathf.Sqrt(Mathf.Max(0f, stiffness) / mass));
            float next = (velocity + force / mass * dt) * Mathf.Exp(-damping * dt);
            return Mathf.Clamp(next, -maximumSpeed, maximumSpeed);
        }
    }
}
