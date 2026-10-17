using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(RopeSimulation))]
    public sealed class RopeSwingGizmo : MonoBehaviour
    {
        [SerializeField] private RopeSimulation rope;
        [SerializeField] private Color innerColor = new Color(0.2f, 0.9f, 1f);
        [SerializeField] private Color outerColor = new Color(1f, 0.35f, 0.2f);
        [SerializeField] private Color brakingColor = new Color(1f, 0.65f, 0.1f, 0.15f);

        private void OnDrawGizmosSelected()
        {
            if (rope == null) rope = GetComponent<RopeSimulation>();
            if (rope == null || rope.Length <= 0f || rope.SwingLimits == null) return;
            RopeSwingConstraint limits = rope.SwingLimits;
            Vector3 anchor = rope.transform.position;
            Color saved = Gizmos.color;
            DrawBand(anchor, limits.minOuterAngle, limits.minInnerAngle);
            DrawBand(anchor, limits.maxInnerAngle, limits.maxOuterAngle);
            DrawArc(anchor, limits.minInnerAngle, limits.maxInnerAngle, innerColor);
            DrawArc(anchor, limits.minOuterAngle, limits.maxOuterAngle, outerColor);
            DrawRay(anchor, limits.minInnerAngle, innerColor, "Inner min");
            DrawRay(anchor, limits.maxInnerAngle, innerColor, "Inner max");
            DrawRay(anchor, limits.minOuterAngle, outerColor, "Outer min");
            DrawRay(anchor, limits.maxOuterAngle, outerColor, "Outer max");
            Gizmos.color = innerColor;
            Gizmos.DrawWireSphere(anchor, 0.06f);
            if (Application.isPlaying && rope.Chain != null)
            {
                float angle = RopeSwingConstraint.Angle(rope.Chain[rope.Chain.SegmentCount] - rope.Chain[0]);
                DrawRay(anchor, angle, Color.yellow, "Current");
            }
            Gizmos.color = saved;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(anchor + Vector3.up * 0.2f,
                "Swing limits: inner → braking → outer (stop)");
#endif
        }

        private Vector3 Direction(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), -Mathf.Cos(radians), 0f);
        }

        private void DrawBand(Vector3 anchor, float min, float max)
        {
#if UNITY_EDITOR
            Color saved = UnityEditor.Handles.color;
            UnityEditor.Handles.color = brakingColor;
            UnityEditor.Handles.DrawSolidArc(anchor, Vector3.forward, Direction(min), max - min, rope.Length);
            UnityEditor.Handles.color = saved;
#endif
        }

        private void DrawArc(Vector3 anchor, float min, float max, Color color)
        {
            Gizmos.color = color;
            Vector3 previous = anchor + Direction(min) * rope.Length;
            int steps = Mathf.Max(1, Mathf.CeilToInt((max - min) / 3f));
            for (int i = 1; i <= steps; i++)
            {
                Vector3 next = anchor + Direction(Mathf.Lerp(min, max, (float)i / steps)) * rope.Length;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        private void DrawRay(Vector3 anchor, float angle, Color color, string label)
        {
            Gizmos.color = color;
            Vector3 end = anchor + Direction(angle) * rope.Length;
            Gizmos.DrawLine(anchor, end);
            Gizmos.DrawWireSphere(end, 0.04f);
#if UNITY_EDITOR
            var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
            style.normal.textColor = color;
            UnityEditor.Handles.Label(end + Direction(angle) * 0.15f, $"{label}: {angle:F1}°", style);
#endif
        }
    }
}
