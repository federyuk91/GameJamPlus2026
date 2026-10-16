using UnityEngine;

namespace MousePlatformer
{
    public static class JumpPhysics
    {
        public static float LaunchSpeed(float height, float gravity)
            => Mathf.Sqrt(2f * Mathf.Max(0f, height) * Mathf.Max(0.001f, gravity));

        public static float Integrate(ref float velocity, float gravity, float terminalSpeed, float dt)
        {
            if (dt <= 0f) return 0f;
            gravity = Mathf.Max(0.001f, gravity);
            if (velocity > 0f)
            {
                float timeToApex = velocity / gravity;
                float riseTime = Mathf.Min(dt, timeToApex);
                float rise = velocity * riseTime - 0.5f * gravity * riseTime * riseTime;
                // Expose the apex for one tick, rather than stepping past it into descent.
                velocity = timeToApex <= dt ? 0f : velocity - gravity * dt;
                return rise;
            }
            velocity = Mathf.Max(velocity - gravity * dt, -terminalSpeed);
            return velocity * dt;
        }
    }
}
