using UnityEngine;

namespace MousePlatformer
{
    public interface IRopeGripConstraint
    {
        Vector2 LimitGrip(Vector2 proposed);
        Vector2 LimitSegmentPoint(Vector2 start, Vector2 proposed);
    }

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

        public int RemoveShortFragments(int minimumPieces)
        {
            int minimum = Mathf.Max(1, minimumPieces);
            int start = 0;
            int remaining = 0;
            for (int index = 0; index <= intact.Length; index++)
            {
                if (index < intact.Length && intact[index]) continue;
                int count = index - start;
                if (count < minimum)
                {
                    for (int piece = start; piece < index; piece++) intact[piece] = false;
                }
                else remaining += count;
                start = index + 1;
            }
            return remaining;
        }

        public void InitializeEndpoint(Vector2 anchor, Vector2 endpoint)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = previous[i] = Vector2.Lerp(anchor, endpoint, (float)i / SegmentCount);
                velocities[i] = Vector2.zero;
            }
        }

        public void ReleaseEndpoint(float fixedDeltaTime, int substeps)
        {
            previous[SegmentCount] = points[SegmentCount] - velocities[SegmentCount] * (fixedDeltaTime / Mathf.Max(1, substeps));
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
            float gripDistance, float driveAcceleration, int iterations, int substeps,
            RopeSwingConstraint swingLimits = null, Vector2? endpoint = null, bool solveTautSpan = false,
            IRopeGripConstraint gripConstraint = null)
        {
            if (dt <= 0f) return;
            substeps = Mathf.Max(1, substeps);
            iterations = Mathf.Max(1, iterations);
            if (solveTautSpan && endpoint.HasValue && ConnectedSegmentCount == SegmentCount
                && Vector2.Distance(anchor, endpoint.Value) >= Length - 0.001f)
            {
                StepTautSpan(anchor, endpoint.Value, dt, substeps);
                return;
            }
            if (endpoint.HasValue || ConnectedSegmentCount != SegmentCount) swingLimits = null;
            swingLimits?.Sanitize();
            for (int i = 0; i < points.Length; i++) stepStart[i] = points[i];
            int driven = Mathf.Clamp(Mathf.RoundToInt(gripDistance / SegmentLength), 1, SegmentCount);
            float h = dt / Mathf.Max(1, substeps);
            Vector2 anchorVelocity = (anchor - stepStart[0]) / dt;
            for (int step = 0; step < substeps; step++)
            {
                float startAngle = RopeSwingConstraint.Angle(points[SegmentCount] - anchor);
                float filteredDrive = driveAcceleration;
                if (!endpoint.HasValue && driven <= ConnectedSegmentCount)
                    filteredDrive = FilterSwingDrive(anchor, anchorVelocity, driven, h, gravity, filteredDrive);
                if (swingLimits != null) filteredDrive = swingLimits.FilterDrive(startAngle, filteredDrive);
                for (int i = 1; i < points.Length; i++)
                {
                    Vector2 position = points[i];
                    Vector2 acceleration = Vector2.down * gravity;
                    if (i == driven) acceleration.x += filteredDrive;
                    points[i] += (points[i] - previous[i]) * Mathf.Exp(-damping * h) + acceleration * (h * h);
                    previous[i] = position;
                }
                SolveConstraints(anchor, endpoint, driven, gripDistance, iterations, gripConstraint);
                points[0] = previous[0] = anchor;
                if (endpoint.HasValue) points[SegmentCount] = previous[SegmentCount] = endpoint.Value;
                // Stop for the rest of this tick so zero velocity is observable before reversal.
                float returnAcceleration = gravity / Length * Mathf.Rad2Deg * Mathf.Abs(Mathf.Sin(startAngle * Mathf.Deg2Rad));
                float angleBeforeLimits = RopeSwingConstraint.Angle(points[SegmentCount] - anchor);
                bool stopped = swingLimits != null && swingLimits.Apply(points, previous, anchor, startAngle, h, returnAcceleration);
                if (gripConstraint != null && Mathf.Abs(Mathf.DeltaAngle(angleBeforeLimits,
                    RopeSwingConstraint.Angle(points[SegmentCount] - anchor))) > 0.0001f)
                    SolveConstraints(anchor, endpoint, driven, gripDistance, iterations, gripConstraint);
                if (stopped) break;
            }
            for (int i = 0; i < points.Length; i++)
                velocities[i] = swingLimits != null ? (points[i] - previous[i]) / h : (points[i] - stepStart[i]) / dt;
        }

        public Vector2 Point(float distance) => Sample(points, distance);

        private void SolveConstraints(Vector2 anchor, Vector2? endpoint, int driven, float gripDistance,
            int iterations, IRopeGripConstraint gripConstraint)
        {
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
                if (gripConstraint != null && gripDistance >= 0f)
                    ResolveGrip(gripDistance, endpoint.HasValue, gripConstraint);
            }
        }

        private void ResolveGrip(float distance, bool hasEndpoint, IRopeGripConstraint constraint)
        {
            float coordinate = Mathf.Clamp(distance / SegmentLength, 0f, SegmentCount);
            int first = Mathf.Clamp(Mathf.CeilToInt(coordinate) - 1, 0, SegmentCount - 1);
            int second = first + 1;
            if (!intact[first]) return;
            ConstrainNode(first, hasEndpoint, constraint);
            ConstrainNode(second, hasEndpoint, constraint);
            float fraction = coordinate - first;
            Vector2 grip = Vector2.Lerp(points[first], points[second], fraction);
            Vector2 correction = constraint.LimitGrip(grip) - grip;
            float a = first == 0 ? 0f : 1f - fraction;
            float b = hasEndpoint && second == SegmentCount ? 0f : fraction;
            float denominator = a * a + b * b;
            if (denominator <= 0.00001f || correction.sqrMagnitude <= 0.0000000001f) return;
            points[first] += correction * (a / denominator);
            points[second] += correction * (b / denominator);
            // Il contatto assorbe la velocità dei nodi della presa senza creare un rimbalzo Verlet.
            if (a > 0f) previous[first] = points[first];
            if (b > 0f) previous[second] = points[second];
        }

        private void ConstrainNode(int index, bool hasEndpoint, IRopeGripConstraint constraint)
        {
            if (index == 0 || (hasEndpoint && index == SegmentCount)) return;
            Vector2 limited = constraint.LimitSegmentPoint(stepStart[index], points[index]);
            if ((limited - points[index]).sqrMagnitude <= 0.0000000001f) return;
            points[index] = previous[index] = limited;
        }

        private void StepTautSpan(Vector2 anchor, Vector2 endpoint, float dt, int substeps)
        {
            Vector2 anchorVelocity = (anchor - points[0]) / dt;
            Vector2 endpointVelocity = (endpoint - points[SegmentCount]) / dt;
            float h = dt / substeps;
            // A piena tensione la soluzione è rettilinea. La storia Verlet segue il moto degli estremi.
            for (int i = 0; i < points.Length; i++)
            {
                float fraction = (float)i / SegmentCount;
                points[i] = Vector2.Lerp(anchor, endpoint, fraction);
                velocities[i] = Vector2.Lerp(anchorVelocity, endpointVelocity, fraction);
                previous[i] = points[i] - velocities[i] * h;
            }
        }

        private float FilterSwingDrive(Vector2 anchor, Vector2 anchorVelocity, int driven, float dt,
            float gravity, float drive)
        {
            Vector2 radial = points[driven] - anchor;
            float radius = radial.magnitude;
            if (radial.y >= 0f || radius <= 0.00001f || drive == 0f) return drive;

            Vector2 tangent = new Vector2(-radial.y, radial.x) / radius;
            Vector2 relativeVelocity = (points[driven] - previous[driven]) / dt - anchorVelocity;
            float swingSpeed = Vector2.Dot(relativeVelocity, tangent);
            if (swingSpeed * drive * tangent.x < 0f) return 0f;
            if (radial.x * drive <= 0f) return drive;

            // Keep full pumping strength except close to a turning point or stationary deflection.
            const float turningSpeedFraction = 0.1f;
            float turningSpeed = Mathf.Sqrt(Mathf.Max(0f, gravity) * radius) * turningSpeedFraction;
            return drive * Mathf.Clamp01(Mathf.Abs(swingSpeed) / Mathf.Max(0.00001f, turningSpeed));
        }

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

        public void AddVelocity(float distance, Vector2 velocity, float fixedDeltaTime, int substeps)
        {
            // Verlet history must use the same substep duration as the rope solver.
            int index = Mathf.Clamp(Mathf.RoundToInt(distance / SegmentLength), 1, SegmentCount);
            previous[index] -= Vector2.ClampMagnitude(velocity, 20f) * (fixedDeltaTime / Mathf.Max(1, substeps));
        }
    }
}
