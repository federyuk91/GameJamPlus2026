using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class RopeSwingConstraint
    {
        [InspectorName("Min Inner Angle (deg)"), Range(-175f, 0f)] public float minInnerAngle = -45f;
        [InspectorName("Min Outer Angle (deg)"), Range(-175f, 0f)] public float minOuterAngle = -60f;
        [InspectorName("Max Inner Angle (deg)"), Range(0f, 175f)] public float maxInnerAngle = 45f;
        [InspectorName("Max Outer Angle (deg)"), Range(0f, 175f)] public float maxOuterAngle = 60f;
        [NonSerialized] private float brakingDirection;
        [NonSerialized] private float brakingAcceleration;
        [NonSerialized] private float reversingDirection;
        [NonSerialized] private float returnSpeed;

        public void Sanitize()
        {
            minOuterAngle = Mathf.Clamp(minOuterAngle, -175f, -0.01f);
            maxOuterAngle = Mathf.Clamp(maxOuterAngle, 0.01f, 175f);
            minInnerAngle = Mathf.Clamp(minInnerAngle, minOuterAngle, 0f);
            maxInnerAngle = Mathf.Clamp(maxInnerAngle, 0f, maxOuterAngle);
        }

        public static float Angle(Vector2 direction) => Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg;

        public float FilterDrive(float angle, float drive)
        {
            if (angle * drive <= 0f) return drive; // Inward input stays responsive.
            float inner = angle < 0f ? -minInnerAngle : maxInnerAngle;
            float outer = angle < 0f ? -minOuterAngle : maxOuterAngle;
            float remaining = Mathf.Clamp01((outer - Mathf.Abs(angle)) / Mathf.Max(0.0001f, outer - inner));
            return drive * remaining;
        }

        // Rotate the whole chain to preserve every distance joint. Returns true at a hard stop.
        public bool Apply(Vector2[] points, Vector2[] previous, Vector2 anchor, float startAngle, float dt,
            float returnAcceleration = 0f)
        {
            float proposed = Angle(points[points.Length - 1] - anchor);
            float target = Mathf.Clamp(proposed, minOuterAngle, maxOuterAngle);
            bool stop = proposed <= minOuterAngle || proposed >= maxOuterAngle;
            float direction = Mathf.Sign(proposed);
            float delta = Mathf.DeltaAngle(startAngle, proposed) * direction;
            float inner = direction < 0f ? -minInnerAngle : maxInnerAngle;
            float outer = direction < 0f ? -minOuterAngle : maxOuterAngle;
            float start = startAngle * direction;

            if (reversingDirection != 0f && returnAcceleration > 0f)
            {
                float boundary = reversingDirection < 0f ? -minOuterAngle : maxOuterAngle;
                if (startAngle * reversingDirection >= boundary - 0.5f)
                {
                    // Flexible links can initially bend outward under gravity even at rest.
                    // Apply the pendulum's inward gravity torque while leaving the stop.
                    returnSpeed += returnAcceleration * dt;
                    float inward = startAngle - reversingDirection * returnSpeed * dt;
                    target = reversingDirection > 0f ? Mathf.Min(target, inward) : Mathf.Max(target, inward);
                    stop = false;
                    brakingDirection = 0f;
                }
                else reversingDirection = 0f;
            }

            if (reversingDirection == 0f && delta > 0f && proposed * direction > inner && outer > inner + 0.0001f)
            {
                float freeTravel = Mathf.Max(0f, inner - start);
                float remaining = Mathf.Max(0f, outer - Mathf.Max(start, inner));
                float rawSpeed = delta / dt;
                if (brakingDirection != direction)
                {
                    brakingDirection = direction;
                    // v² = 2*a*distance: choose a stopping distance ending exactly at outer.
                    brakingAcceleration = rawSpeed * rawSpeed / (2f * Mathf.Max(remaining, 0.0001f));
                }
                float brakingTime = Mathf.Max(0f, dt - freeTravel / rawSpeed);
                float speedCap = Mathf.Sqrt(2f * brakingAcceleration * remaining);
                if (brakingTime * brakingAcceleration >= speedCap || remaining < 0.00001f)
                {
                    target = direction * outer;
                    stop = true;
                }
                else
                {
                    float travel = freeTravel + speedCap * brakingTime
                        - 0.5f * brakingAcceleration * brakingTime * brakingTime;
                    target = startAngle + direction * Mathf.Min(delta, Mathf.Max(0f, travel));
                    stop = false;
                }
            }
            else brakingDirection = 0f;

            float radians = Mathf.DeltaAngle(proposed, target) * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 relative = points[i] - anchor;
                points[i] = anchor + new Vector2(relative.x * cos - relative.y * sin,
                    relative.x * sin + relative.y * cos);
                if (stop) previous[i] = points[i];
            }
            if (stop)
            {
                brakingDirection = 0f;
                reversingDirection = Mathf.Sign(target);
                returnSpeed = 0f;
            }
            return stop;
        }
    }
}
