using UnityEngine;

namespace MousePlatformer
{
    // Position based distance joints. No scene objects or Rigidbody dependencies.
    public sealed class RopeChain
    {
        private readonly Vector2[] points;
        private readonly Vector2[] previous;
        private readonly Vector2[] stepStart;
        private readonly Vector2[] velocities;
        private readonly bool[] intact;
        public int SegmentCount => points.Length - 1;
        public float SegmentLength { get; }
        public float Length => SegmentCount * SegmentLength;
        public Vector2 this[int index] => points[index];
        public int ConnectedSegmentCount
        {
            get
            {
                int count = 0;
                while (count < intact.Length && intact[count]) count++;
                return count;
            }
        }
        public float ConnectedLength => ConnectedSegmentCount * SegmentLength;
        public int EndpointConnectedSegmentCount
        {
            get
            {
                int count = 0;
                while (count < intact.Length && intact[intact.Length - 1 - count]) count++;
                return count;
            }
        }
        public bool IsSegmentIntact(int index) => index >= 0 && index < intact.Length && intact[index];

        public bool CutSegment(int index)
        {
            if (!IsSegmentIntact(index)) return false;
            intact[index] = false;
            return true;
        }

        public void InitializeEndpoint(Vector2 anchor, Vector2 endpoint)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = previous[i] = Vector2.Lerp(anchor, endpoint, (float)i / SegmentCount);
                velocities[i] = Vector2.zero;
            }
        }

        public void ReleaseEndpoint(float fixedDeltaTime)
        {
            previous[SegmentCount] = points[SegmentCount] - velocities[SegmentCount] * (fixedDeltaTime / 4f);
        }

        public RopeChain(Vector2 anchor, int segments, float segmentLength)
        {
            SegmentLength = Mathf.Max(0.01f, segmentLength);
            segments = Mathf.Max(1, segments);
            points = new Vector2[segments + 1];
            previous = new Vector2[segments + 1];
            stepStart = new Vector2[segments + 1];
            velocities = new Vector2[segments + 1];
            intact = new bool[segments];
            for (int i = 0; i < segments; i++) intact[i] = true;
            for (int i = 0; i < points.Length; i++)
                points[i] = previous[i] = anchor + Vector2.down * (i * SegmentLength);
        }

        public void Step(Vector2 anchor, float dt, float gravity, float damping,
            float gripDistance, float driveAcceleration, int iterations = 24, int substeps = 4,
            RopeSwingConstraint swingLimits = null, Vector2? endpoint = null)
        {
            if (dt <= 0f) return;
            if (endpoint.HasValue || ConnectedSegmentCount != SegmentCount) swingLimits = null;
            swingLimits?.Sanitize();
            for (int i = 0; i < points.Length; i++) stepStart[i] = points[i];
            int driven = Mathf.Clamp(Mathf.RoundToInt(gripDistance / SegmentLength), 1, SegmentCount);
            float h = dt / Mathf.Max(1, substeps);
            for (int step = 0; step < substeps; step++)
            {
                float startAngle = RopeSwingConstraint.Angle(points[SegmentCount] - anchor);
                float filteredDrive = swingLimits != null ? swingLimits.FilterDrive(startAngle, driveAcceleration) : driveAcceleration;
                for (int i = 1; i < points.Length; i++)
                {
                    Vector2 position = points[i];
                    Vector2 acceleration = Vector2.down * gravity;
                    if (i == driven) acceleration.x += filteredDrive;
                    points[i] += (points[i] - previous[i]) * Mathf.Exp(-damping * h) + acceleration * (h * h);
                    previous[i] = position;
                }
                for (int pass = 0; pass < iterations; pass++)
                {
                    points[0] = anchor;
                    if (endpoint.HasValue) points[SegmentCount] = endpoint.Value;
                    for (int i = 0; i < SegmentCount; i++)
                    {
                        if (!intact[i]) continue;
                        Vector2 delta = points[i + 1] - points[i];
                        float length = delta.magnitude;
                        if (length < 0.00001f) continue;
                        Vector2 correction = delta * ((length - SegmentLength) / length);
                        float a = i == 0 ? 0f : i == driven && gripDistance >= 0f ? 0.25f : 1f;
                        float b = endpoint.HasValue && i + 1 == SegmentCount ? 0f
                            : i + 1 == driven && gripDistance >= 0f ? 0.25f : 1f;
                        if (a + b > 0f)
                        {
                            points[i] += correction * (a / (a + b));
                            points[i + 1] -= correction * (b / (a + b));
                        }
                    }
                }
                points[0] = previous[0] = anchor;
                if (endpoint.HasValue) points[SegmentCount] = previous[SegmentCount] = endpoint.Value;
                // Stop for the rest of this tick so zero velocity is observable before reversal.
                float returnAcceleration = gravity / Length * Mathf.Rad2Deg * Mathf.Abs(Mathf.Sin(startAngle * Mathf.Deg2Rad));
                if (swingLimits != null && swingLimits.Apply(points, previous, anchor, startAngle, h, returnAcceleration)) break;
            }
            for (int i = 0; i < points.Length; i++)
                velocities[i] = swingLimits != null ? (points[i] - previous[i]) / h : (points[i] - stepStart[i]) / dt;
        }

        public Vector2 Point(float distance) => Sample(points, distance);
        public Vector2 Velocity(float distance) => Sample(velocities, distance);
        private Vector2 Sample(Vector2[] values, float distance)
        {
            float coordinate = Mathf.Clamp(distance / SegmentLength, 0f, SegmentCount);
            int index = Mathf.Min(Mathf.FloorToInt(coordinate), SegmentCount - 1);
            return Vector2.Lerp(values[index], values[index + 1], coordinate - index);
        }

        public Vector2 Tangent(float distance, float minimumDistance = 0f, float maximumDistance = float.PositiveInfinity)
        {
            int first = Mathf.Clamp(Mathf.FloorToInt(minimumDistance / SegmentLength), 0, SegmentCount - 1);
            int last = float.IsPositiveInfinity(maximumDistance) ? SegmentCount - 1
                : Mathf.Clamp(Mathf.CeilToInt(maximumDistance / SegmentLength) - 1, first, SegmentCount - 1);
            int index = Mathf.Clamp(Mathf.FloorToInt(distance / SegmentLength), first, last);
            return (points[index + 1] - points[index]).normalized;
        }

        public float ClosestDistance(Vector2 position, out float separation, bool anchoredOnly = false)
        {
            return ClosestDistance(position, out separation, 0, anchoredOnly ? ConnectedSegmentCount : SegmentCount);
        }

        public float ClosestDistance(Vector2 position, out float separation, int firstSegment, int endSegment)
        {
            float nearest = float.PositiveInfinity;
            float along = 0f;
            for (int i = firstSegment; i < endSegment; i++)
            {
                if (!intact[i]) continue;
                Vector2 delta = points[i + 1] - points[i];
                float t = Mathf.Clamp01(Vector2.Dot(position - points[i], delta) / Mathf.Max(delta.sqrMagnitude, 0.000001f));
                float squared = (position - (points[i] + delta * t)).sqrMagnitude;
                if (squared < nearest) { nearest = squared; along = (i + t) * SegmentLength; }
            }
            separation = Mathf.Sqrt(nearest);
            return along;
        }

        public void AddVelocity(float distance, Vector2 velocity, float fixedDeltaTime)
        {
            // Verlet history uses the substep duration (four substeps per fixed tick).
            int index = Mathf.Clamp(Mathf.RoundToInt(distance / SegmentLength), 1, SegmentCount);
            previous[index] -= Vector2.ClampMagnitude(velocity, 20f) * (fixedDeltaTime / 4f);
        }
    }
}
