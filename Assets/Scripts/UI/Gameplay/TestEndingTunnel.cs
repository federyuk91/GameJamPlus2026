using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class TestEndingTunnel : VisualElement
    {
        private const int RadialSteps = 64;
        private const int AngularSteps = 96;
        private float progress;

        public TestEndingTunnel()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = style.right = style.top = style.bottom = 0f;
            style.overflow = Overflow.Hidden;
            generateVisualContent += Draw;
        }

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            style.opacity = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.08f));
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (contentRect.width <= 0f || contentRect.height <= 0f) return;
            Vector2 center = contentRect.center;
            float outerRadius = center.magnitude * 1.03f;
            MeshWriteData mesh = context.Allocate((RadialSteps + 1) * (AngularSteps + 1),
                RadialSteps * AngularSteps * 6);
            float opening = Mathf.Lerp(0.012f, 1.15f, Mathf.Pow(progress, 3f));
            float whiteout = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.88f, 1f, progress));

            for (int ring = 0; ring <= RadialSteps; ring++)
            {
                float radius = Mathf.Pow((float)ring / RadialSteps, 1.8f);
                float depth = Mathf.Log(Mathf.Max(radius, 0.001f) / 0.012f);
                float bands = 0.5f + 0.5f * Mathf.Cos(depth * 4f - progress * progress * 32f);
                float halo = 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(opening * 0.7f, opening * 2.4f, radius));
                float core = 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(opening * 0.35f, opening, radius));

                for (int segment = 0; segment <= AngularSteps; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / AngularSteps;
                    float shafts = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(angle * 13f), 6f);
                    float wall = 0.018f + bands * 0.035f + shafts * 0.018f;
                    Color color = new Color(wall * 0.7f, wall * 0.85f, wall);
                    color = Color.Lerp(color, new Color(1f, 0.94f, 0.8f), halo * 0.8f);
                    color = Color.Lerp(color, Color.white, core);
                    color = Color.Lerp(color, Color.white, whiteout);
                    mesh.SetNextVertex(new Vertex
                    {
                        position = new Vector3(center.x + Mathf.Cos(angle) * radius * outerRadius,
                            center.y + Mathf.Sin(angle) * radius * outerRadius, Vertex.nearZ),
                        tint = color,
                        uv = Vector2.zero
                    });
                }
            }

            for (int ring = 0; ring < RadialSteps; ring++)
            {
                for (int segment = 0; segment < AngularSteps; segment++)
                {
                    ushort inner = (ushort)(ring * (AngularSteps + 1) + segment);
                    ushort outer = (ushort)(inner + AngularSteps + 1);
                    // UI Toolkit expects clockwise triangles in its downward Y coordinate system.
                    mesh.SetNextIndex(inner);
                    mesh.SetNextIndex(outer);
                    mesh.SetNextIndex((ushort)(outer + 1));
                    mesh.SetNextIndex(inner);
                    mesh.SetNextIndex((ushort)(outer + 1));
                    mesh.SetNextIndex((ushort)(inner + 1));
                }
            }
        }
    }
}
