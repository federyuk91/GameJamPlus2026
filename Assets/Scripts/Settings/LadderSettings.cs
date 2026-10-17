using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class LadderSettings
    {
        [InspectorName("Climb Speed (units/s)"), Min(0f)] public float climbSpeed = 3f;
        [InspectorName("Side Jump Speed (units/s)"), Min(0f)] public float jumpHorizontalSpeed = 6f;
        [InspectorName("Jump Up Speed (units/s)"), Min(0f)] public float jumpUpSpeed = 9f;
        [InspectorName("Grab Reach (units)"), Min(0f)] public float grabReach = 0.65f;
        [InspectorName("Regrab Delay (s)"), Min(0f)] public float regrabDelay = 0.3f;

        public float Climb(float localHeight, float vertical, float worldUnitsPerLocalUnit, float dt,
            float bottom, float top)
            => Mathf.Clamp(localHeight + Mathf.Clamp(vertical, -1f, 1f) * climbSpeed * dt
                / Mathf.Max(0.0001f, worldUnitsPerLocalUnit), bottom, top);

        public Vector2 JumpVelocity(Vector2 carrierVelocity, float horizontal, float facing)
        {
            float direction = Mathf.Abs(horizontal) > 0.1f ? Mathf.Sign(horizontal) : Mathf.Sign(facing);
            return carrierVelocity + new Vector2(direction * jumpHorizontalSpeed, jumpUpSpeed);
        }
    }
}
