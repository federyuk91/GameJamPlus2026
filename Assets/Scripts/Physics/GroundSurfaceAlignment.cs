using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    internal static class GroundSurfaceAlignment
    {
        private static readonly List<RaycastHit2D> surfaceHits = new List<RaycastHit2D>(8);

        public static void Align(SimulatedBody2D body, Transform visual, Vector3 localBasePoint)
        {
            if (!TryGetSurface(body, out Vector2 normal, out Vector2 point)) return;

            float angle = Vector2.SignedAngle(Vector2.up, normal);
            visual.rotation = Quaternion.AngleAxis(angle, Vector3.forward) * visual.rotation;
            Vector2 basePoint = visual.TransformPoint(localBasePoint);
            float gap = Vector2.Dot(basePoint - point, normal);
            visual.position += (Vector3)(normal * (body.Skin - gap));
        }

        public static bool TryGetSurface(SimulatedBody2D body, out Vector2 normal, out Vector2 point)
        {
            normal = body.GroundNormal;
            point = body.GroundPoint;
            if (!body.HasGroundContact) return false;
            if (Mathf.Abs(normal.x) < 0.001f && body.GroundCollider is CompositeCollider2D terrain)
                FindSurfaceBelowBody(body, terrain, ref normal, ref point);
            return true;
        }

        private static void FindSurfaceBelowBody(SimulatedBody2D body, CompositeCollider2D terrain,
            ref Vector2 normal, ref Vector2 point)
        {
            // A box cast can report up at a ramp corner. Sample below the box to include the collision clearance.
            Bounds bounds = body.Shape.bounds;
            float inset = Mathf.Min(body.Skin, bounds.extents.x);
            float highest = bounds.min.y - body.Skin * 4f;
            float originY = bounds.max.y + body.Skin;
            ContactFilter2D filter = body.SolidFilter;
            for (int sample = -1; sample <= 1; sample++)
            {
                float x = bounds.center.x + sample * (bounds.extents.x - inset);
                Physics2D.Raycast(new Vector2(x, originY), Vector2.down, filter, surfaceHits, originY - highest);
                foreach (RaycastHit2D hit in surfaceHits)
                {
                    if (hit.collider != terrain || hit.distance <= 0f || hit.normal.y <= 0.01f
                        || hit.point.y <= highest) continue;
                    highest = hit.point.y;
                    normal = hit.normal;
                    point = hit.point;
                }
            }
        }
    }
}
