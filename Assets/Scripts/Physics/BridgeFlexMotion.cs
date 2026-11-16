using UnityEngine;

namespace MousePlatformer
{
    public sealed class BridgeFlexMotion
    {
        private readonly float[] coordinates;
        private readonly float[] targets;
        private readonly float[] offsets;
        private readonly float[] velocities;
        public float Offset(int index) => offsets[index];

        public BridgeFlexMotion(float[] nodeCoordinates)
        {
            coordinates = nodeCoordinates;
            targets = new float[coordinates.Length];
            offsets = new float[coordinates.Length];
            velocities = new float[coordinates.Length];
        }

        public void BeginStep(float restSag)
        {
            for (int index = 0; index < targets.Length; index++)
            {
                float position = Normalized(coordinates[index]);
                targets[index] = -Mathf.Max(0f, restSag) * 4f * position * (1f - position);
            }
        }

        public void AddLoad(float coordinate, float kilograms, float sagPerKilogram)
        {
            float loadPosition = Normalized(coordinate);
            float sag = Mathf.Max(0f, kilograms) * Mathf.Max(0f, sagPerKilogram);
            // A fixed-end tension profile spreads a point load across the entire span.
            for (int index = 1; index < targets.Length - 1; index++)
            {
                float position = Normalized(coordinates[index]);
                targets[index] -= sag * 4f * Mathf.Min(position, loadPosition)
                    * (1f - Mathf.Max(position, loadPosition));
            }
        }

        public void Step(float dt, float frequency, float dampingRatio, float maximumSag, float maximumSpeed)
        {
            if (dt <= 0f) return;
            frequency = Mathf.Clamp(frequency, 0.1f, 12f);
            float omega = 2f * Mathf.PI * frequency;
            float damping = 2f * Mathf.Clamp(dampingRatio, 0.05f, 2f) * omega;
            maximumSag = Mathf.Max(0f, maximumSag);
            maximumSpeed = Mathf.Max(0f, maximumSpeed);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt * frequency * 32f));
            float step = dt / steps;
            for (int substep = 0; substep < steps; substep++)
            {
                for (int index = 1; index < offsets.Length - 1; index++)
                {
                    float target = Mathf.Clamp(targets[index], -maximumSag, 0f);
                    float acceleration = omega * omega * (target - offsets[index]) - damping * velocities[index];
                    velocities[index] = Mathf.Clamp(velocities[index] + acceleration * step, -maximumSpeed, maximumSpeed);
                    float next = offsets[index] + velocities[index] * step;
                    offsets[index] = Mathf.Clamp(next, -maximumSag, maximumSag * 0.15f);
                    if (next != offsets[index]) velocities[index] = 0f;
                }
            }
        }

        private float Normalized(float coordinate)
        {
            return Mathf.InverseLerp(coordinates[0], coordinates[coordinates.Length - 1], coordinate);
        }
    }
}
