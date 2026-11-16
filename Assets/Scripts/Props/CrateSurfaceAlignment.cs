using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class CrateSurfaceAlignment : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField, Tooltip("Tip around the edge of the supported footprint on dry land.")]
        private bool useSupportPivot;
        [SerializeField, Min(1f), Tooltip("Tipping speed in degrees per second.")]
        private float tippingSpeed = 160f;
        [SerializeField, Min(0f), Tooltip("Vertical gap below the crate that still counts as support while tipping.")]
        private float supportContactTolerance = 0.03125f;

        private Quaternion restRotation;
        private float angularSpeed;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        public bool UseSupportPivot => isActiveAndEnabled && useSupportPivot;

        private void Awake() => restRotation = transform.localRotation;

        internal void TipOnSupport(PropsMotion motion, float dt)
        {
            if (body == null) return;
            BoxCollider2D shape = body.Shape;
            Vector2 halfSize = shape.size * 0.5f;
            RaycastHit2D left = default;
            RaycastHit2D right = default;
            ContactFilter2D filter = body.SolidFilter;

            SampleFace(shape, Vector2.down, halfSize, filter, ref left, ref right);
            SampleFace(shape, Vector2.left, halfSize, filter, ref left, ref right);
            SampleFace(shape, Vector2.right, halfSize, filter, ref left, ref right);
            SampleFace(shape, Vector2.up, halfSize, filter, ref left, ref right);
            if (left.collider == null)
            {
                if (angularSpeed != 0f)
                    RotateAround(shape, shape.bounds.center, null, angularSpeed * dt);
                return;
            }

            float centerOfMassX = motion.CenterOfMassX;
            float margin = body.Skin * 2f;
            if (centerOfMassX >= left.point.x - margin && centerOfMassX <= right.point.x + margin)
            {
                angularSpeed = 0f;
                return;
            }
            bool tipRight = centerOfMassX > right.point.x;
            RaycastHit2D support = tipRight ? right : left;
            angularSpeed = tippingSpeed * (tipRight ? -1f : 1f);
            RotateAround(shape, support.point, support.collider, angularSpeed * dt);
        }

        private void SampleFace(BoxCollider2D shape, Vector2 face, Vector2 halfSize, ContactFilter2D filter,
            ref RaycastHit2D left, ref RaycastHit2D right)
        {
            if (shape.transform.TransformDirection(face).y >= -0.01f) return;

            Vector2 midpoint = shape.offset + Vector2.Scale(face, halfSize);
            Vector2 halfEdge = Vector2.Scale(new Vector2(-face.y, face.x), halfSize);
            Vector2 start = shape.transform.TransformPoint(midpoint - halfEdge);
            Vector2 end = shape.transform.TransformPoint(midpoint + halfEdge);
            const int intervals = 8;
            Vector2 previousPoint = start;
            RaycastHit2D previousHit = FindSupport(shape, start, filter);
            RecordSupport(previousHit, ref left, ref right);
            for (int i = 1; i <= intervals; i++)
            {
                Vector2 point = Vector2.Lerp(start, end, (float)i / intervals);
                RaycastHit2D hit = FindSupport(shape, point, filter);
                bool hasSupport = hit.collider != null;
                bool previousHasSupport = previousHit.collider != null;
                if (hasSupport != previousHasSupport)
                {
                    Vector2 supported = hasSupport ? point : previousPoint;
                    Vector2 unsupported = hasSupport ? previousPoint : point;
                    RaycastHit2D edge = hasSupport ? hit : previousHit;
                    // Refine the pivot where the face goes past the edge of a ledge.
                    for (int step = 0; step < 5; step++)
                    {
                        Vector2 midpointSample = (supported + unsupported) * 0.5f;
                        RaycastHit2D candidate = FindSupport(shape, midpointSample, filter);
                        if (candidate.collider == null) unsupported = midpointSample;
                        else
                        {
                            supported = midpointSample;
                            edge = candidate;
                        }
                    }
                    RecordSupport(edge, ref left, ref right);
                }
                RecordSupport(hit, ref left, ref right);
                previousPoint = point;
                previousHit = hit;
            }

            // A narrow ledge can fall between two of the evenly spaced samples.
            float horizontalSpan = end.x - start.x;
            if (!body.HasGroundContact || Mathf.Abs(horizontalSpan) <= body.Skin) return;
            float fraction = (body.GroundPoint.x - start.x) / horizontalSpan;
            if (fraction > 0f && fraction < 1f)
                RecordSupport(FindSupport(shape, Vector2.Lerp(start, end, fraction), filter), ref left, ref right);
        }

        private RaycastHit2D FindSupport(BoxCollider2D shape, Vector2 point, ContactFilter2D filter)
        {
            float aboveFace = body.Skin * 16f;
            Vector2 origin = point + Vector2.up * aboveFace;
            float belowFace = Mathf.Max(body.Skin * 4f, supportContactTolerance);
            Physics2D.Raycast(origin, Vector2.down, filter, hits, aboveFace + belowFace);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == shape || hit.collider.transform.IsChildOf(transform)
                    || hit.distance <= 0f || hit.normal.y <= 0.01f) continue;
                return hit;
            }
            return default;
        }

        private static void RecordSupport(RaycastHit2D hit, ref RaycastHit2D left, ref RaycastHit2D right)
        {
            if (hit.collider == null) return;
            if (left.collider == null || hit.point.x < left.point.x) left = hit;
            if (right.collider == null || hit.point.x > right.point.x) right = hit;
        }

        private void RotateAround(BoxCollider2D shape, Vector2 pivot, Collider2D support, float angle)
        {
            Vector3 pivotWorld = new Vector3(pivot.x, pivot.y, transform.position.z);
            Vector3 center = shape.transform.TransformPoint(shape.offset);
            Vector3 radius = transform.position - pivotWorld;
            Quaternion startRotation = transform.rotation;
            Vector3 position = transform.position;
            Quaternion rotation = startRotation;
            float lift = 0f;
            // Test the arc in small increments so the far edge cannot skip through another collider.
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(angle)));
            for (int i = 1; i <= steps; i++)
            {
                Quaternion turn = Quaternion.AngleAxis(angle * i / steps, Vector3.forward);
                Quaternion nextRotation = turn * startRotation;
                Vector3 nextCenter = pivotWorld + turn * (center - pivotWorld);
                if (!TryClearSupport(shape, nextCenter, nextRotation, support, ref lift))
                {
                    angularSpeed = 0f;
                    break;
                }
                position = pivotWorld + turn * radius + Vector3.up * lift;
                rotation = nextRotation;
            }

            if (rotation == startRotation) return;
            transform.SetPositionAndRotation(position, rotation);
            body.RefreshGround();
        }

        private bool TryClearSupport(BoxCollider2D shape, Vector2 center, Quaternion rotation, Collider2D support, ref float lift)
        {
            Collider2D obstacle = FindObstacle(shape, center + Vector2.up * lift, rotation);
            if (obstacle == null) return true;
            if (obstacle != support) return false;

            // A corner touching several tile shapes can start slightly inside its support.
            float maxLift = body.Skin * 16f;
            if (FindObstacle(shape, center + Vector2.up * maxLift, rotation) != null) return false;
            float blocked = lift;
            float clear = maxLift;
            for (int i = 0; i < 6; i++)
            {
                float offset = (blocked + clear) * 0.5f;
                if (FindObstacle(shape, center + Vector2.up * offset, rotation) == null) clear = offset;
                else blocked = offset;
            }
            lift = clear;
            return true;
        }

        internal void AlignToGround()
        {
            if (body == null || !isActiveAndEnabled) return;
            angularSpeed = 0f;
            bool grounded = GroundSurfaceAlignment.TryGetSurface(body, out Vector2 normal, out Vector2 point);
            float angle = grounded ? Vector2.SignedAngle(Vector2.up, normal) : 0f;
            Quaternion neutral = transform.parent != null ? transform.parent.rotation * restRotation : restRotation;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward) * neutral;
            if (Quaternion.Angle(transform.rotation, rotation) < 0.1f) return;

            BoxCollider2D shape = body.Shape;
            Vector3 scale = transform.lossyScale;
            Vector2 size = Vector2.Scale(shape.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Vector3 offset = rotation * Vector3.Scale(shape.offset, scale);
            Vector3 center = shape.transform.TransformPoint(shape.offset);
            if (grounded)
            {
                Vector2 right = rotation * Vector3.right;
                Vector2 up = rotation * Vector3.up;
                float extent = (Mathf.Abs(Vector2.Dot(normal, right)) * size.x
                    + Mathf.Abs(Vector2.Dot(normal, up)) * size.y) * 0.5f;
                // Keep the box at the same X when the new lower face meets the ground.
                float height = (extent + body.Skin - Vector2.Dot((Vector2)center - point, normal)) / normal.y;
                if (Mathf.Abs(height) > size.y * 0.5f) return;
                center.y += height;
            }

            if (FindObstacle(shape, center, rotation) != null) return;
            transform.SetPositionAndRotation(center - offset, rotation);
            body.RefreshGround(grounded);
        }

        private Collider2D FindObstacle(BoxCollider2D shape, Vector2 center, Quaternion rotation)
        {
            Vector3 scale = shape.transform.lossyScale;
            Vector2 size = Vector2.Scale(shape.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            float inset = Mathf.Min(body.Skin, Mathf.Min(size.x, size.y) * 0.25f);
            Physics2D.OverlapBox(center, size - Vector2.one * (inset * 2f),
                rotation.eulerAngles.z, body.MotionFilter, overlaps);
            foreach (Collider2D obstacle in overlaps)
            {
                if (obstacle == shape || obstacle.transform.IsChildOf(transform) || !body.IsMotionObstacle(obstacle)) continue;
                return obstacle;
            }
            return null;
        }
    }
}
