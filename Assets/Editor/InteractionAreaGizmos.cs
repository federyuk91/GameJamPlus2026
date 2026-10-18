using MousePlatformer;
using UnityEditor;
using UnityEngine;

public static class InteractionAreaGizmos
{
    private const GizmoType Visibility = GizmoType.Selected | GizmoType.NonSelected;
    private static readonly Color PickupColor = new Color(0.4f, 1f, 0.45f);
    private static readonly Color GrabColor = new Color(0.2f, 0.85f, 1f);
    private static readonly Color BiteColor = new Color(1f, 0.8f, 0.2f);

    [DrawGizmo(Visibility)]
    private static void DrawCollectible(Collectible collectible, GizmoType gizmoType)
    {
        if (!collectible.ShowGizmos) return;
        Collider2D area = collectible.PickupArea;
        if (area == null) area = collectible.GetComponent<Collider2D>();
        if (area == null) return;
        using (new Handles.DrawingScope(collectible.CanBeCollected ? PickupColor : Color.gray, area.transform.localToWorldMatrix))
        {
            DrawCollider(area);
        }
    }

    [DrawGizmo(Visibility)]
    private static void DrawCrate(PushableCrate crate, GizmoType gizmoType)
    {
        if (!crate.ShowGizmos || crate.Body == null) return;
        PlayerCrateInteraction player = FindPlayer<PlayerCrateInteraction>(crate);
        float reach = player != null ? player.GrabReach : PhysicsSettings.DefaultInteractions.crateGrabReach;
        BoxCollider2D box = crate.Body.Shape;
        // These bands are tested against the player's collider, rather than its pivot.
        Vector3 center = box.transform.TransformPoint(box.offset);
        Vector3 scale = box.transform.lossyScale;
        Vector2 size = Vector2.Scale(box.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
        bool available = crate.isActiveAndEnabled && (!Application.isPlaying || crate.Body.Grounded);
        using (new Handles.DrawingScope(available ? GrabColor : Color.gray))
        {
            Handles.DrawWireCube(center + Vector3.left * ((size.x + reach) * 0.5f), new Vector3(reach, size.y, 0f));
            Handles.DrawWireCube(center + Vector3.right * ((size.x + reach) * 0.5f), new Vector3(reach, size.y, 0f));
        }
    }

    [DrawGizmo(Visibility)]
    private static void DrawRope(RopeSimulation rope, GizmoType gizmoType)
    {
        if (!rope.ShowGizmos) return;
        PlayerRopeInteraction player = FindPlayer<PlayerRopeInteraction>(rope);
        PlayerRopeBite bite = player != null ? player.GetComponent<PlayerRopeBite>() : FindPlayer<PlayerRopeBite>(rope);
        float grabRadius = player != null ? player.GrabRadius : PhysicsSettings.DefaultInteractions.ropeGrabRadius;
        float biteRadius = bite != null ? bite.Reach : 0.4f;
        SerializedObject serializedRope = new SerializedObject(rope);
        float segmentLength = serializedRope.FindProperty("segmentLength").floatValue;
        for (int index = 0; index < rope.PieceCount; index++)
        {
            Vector3 a;
            Vector3 b;
            bool canGrab = true;
            if (Application.isPlaying && rope.Chain != null)
            {
                if (!rope.Chain.IsSegmentIntact(index)) continue;
                a = rope.Chain[index];
                b = rope.Chain[index + 1];
                a.z = b.z = rope.transform.position.z;
                canGrab = index < rope.Chain.ConnectedSegmentCount;
            }
            else
            {
                Transform piece = rope.Piece(index);
                if (piece == null) continue;
                Vector3 half = piece.up * (segmentLength * Mathf.Abs(rope.transform.lossyScale.y) * 0.5f);
                a = piece.position + half;
                b = piece.position - half;
            }
            if (canGrab) DrawCapsule(a, b, grabRadius, rope.isActiveAndEnabled ? GrabColor : Color.gray);
            if (rope.CanBeCut) DrawCapsule(a, b, biteRadius, rope.isActiveAndEnabled ? BiteColor : Color.gray);
        }
    }

    private static T FindPlayer<T>(Component target) where T : Component
    {
        // Editor-only lookup also allows prefab inspection without storing scene references in assets.
        T[] players = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (T player in players)
        {
            if (player.gameObject.scene == target.gameObject.scene) return player;
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        return prefab != null ? prefab.GetComponent<T>() : null;
    }

    private static void DrawCapsule(Vector3 a, Vector3 b, float radius, Color color)
    {
        using (new Handles.DrawingScope(color))
        {
            Vector3 direction = (b - a).normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                Handles.DrawWireDisc(a, Vector3.forward, radius);
                return;
            }
            Vector3 side = new Vector3(-direction.y, direction.x, 0f) * radius;
            Handles.DrawLine(a + side, b + side);
            Handles.DrawLine(a - side, b - side);
            Handles.DrawWireArc(a, Vector3.forward, side.normalized, 180f, radius);
            Handles.DrawWireArc(b, Vector3.forward, -side.normalized, 180f, radius);
        }
    }

    private static void DrawCollider(Collider2D area)
    {
        if (area is BoxCollider2D box)
        {
            Handles.DrawWireCube(box.offset, box.size);
        }
        else if (area is CircleCollider2D circle)
        {
            Handles.DrawWireDisc(circle.offset, Vector3.forward, circle.radius);
        }
        else if (area is CapsuleCollider2D capsule)
        {
            bool vertical = capsule.direction == CapsuleDirection2D.Vertical;
            float radius = (vertical ? capsule.size.x : capsule.size.y) * 0.5f;
            float halfLength = Mathf.Max(0f, (vertical ? capsule.size.y : capsule.size.x) * 0.5f - radius);
            Vector2 axis = vertical ? Vector2.up : Vector2.right;
            DrawCapsule(capsule.offset - axis * halfLength, capsule.offset + axis * halfLength, radius, Handles.color);
        }
        else if (area is PolygonCollider2D polygon)
        {
            for (int path = 0; path < polygon.pathCount; path++)
            {
                Vector2[] points = polygon.GetPath(path);
                for (int index = 0; index < points.Length; index++)
                    Handles.DrawLine(points[index] + polygon.offset, points[(index + 1) % points.Length] + polygon.offset);
            }
        }
        else
        {
            using (new Handles.DrawingScope(Handles.color, Matrix4x4.identity))
                Handles.DrawWireCube(area.bounds.center, area.bounds.size);
        }
    }
}
