using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class LadderSettings
    {
        [Min(0f)] public float jumpHorizontalSpeed = 6f;
        [Min(0f)] public float jumpUpSpeed = 9f;
        [Min(0f)] public float regrabDelay = 0.3f;

        public float Climb(float localHeight, float vertical, float worldUnitsPerLocalUnit, float dt,
            float bottom, float top, float climbSpeed)
            => Mathf.Clamp(localHeight + Mathf.Clamp(vertical, -1f, 1f) * Mathf.Max(0f, climbSpeed) * dt
                / Mathf.Max(0.0001f, worldUnitsPerLocalUnit), bottom, top);

        public Vector2 JumpVelocity(Vector2 carrierVelocity, float horizontal, float facing)
        {
            float direction = Mathf.Abs(horizontal) > 0.1f ? Mathf.Sign(horizontal) : Mathf.Sign(facing);
            return carrierVelocity + new Vector2(direction * jumpHorizontalSpeed, jumpUpSpeed);
        }
    }
}
