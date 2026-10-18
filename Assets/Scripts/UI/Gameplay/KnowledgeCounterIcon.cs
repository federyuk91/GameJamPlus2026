using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class KnowledgeCounterIcon : VisualElement
    {
        public KnowledgeCounterIcon()
        {
            pickingMode = PickingMode.Ignore;
            tooltip = "Ricerca / Conoscenza";
            style.position = Position.Absolute;
            style.left = style.right = style.top = style.bottom = 0f;
            generateVisualContent += DrawBulb;
        }

        private void DrawBulb(MeshGenerationContext context)
        {
            float scale = Mathf.Min(contentRect.width, contentRect.height) / 16f;
            if (scale <= 0f) return;
            Vector2 offset = new Vector2((contentRect.width - 16f * scale) * 0.5f, (contentRect.height - 16f * scale) * 0.5f);
            Painter2D painter = context.painter2D;
            painter.fillColor = new Color(1f, 0.85f, 0.3f);
            painter.BeginPath();
            painter.MoveTo(offset + new Vector2(5f, 1f) * scale);
            painter.LineTo(offset + new Vector2(11f, 1f) * scale);
            painter.LineTo(offset + new Vector2(13f, 3f) * scale);
            painter.LineTo(offset + new Vector2(13f, 7f) * scale);
            painter.LineTo(offset + new Vector2(10f, 10f) * scale);
            painter.LineTo(offset + new Vector2(10f, 11f) * scale);
            painter.LineTo(offset + new Vector2(6f, 11f) * scale);
            painter.LineTo(offset + new Vector2(6f, 10f) * scale);
            painter.LineTo(offset + new Vector2(3f, 7f) * scale);
            painter.LineTo(offset + new Vector2(3f, 3f) * scale);
            painter.ClosePath();
            painter.Fill();
            painter.strokeColor = new Color(0.85f, 0.9f, 0.95f);
            painter.lineWidth = Mathf.Max(1f, scale);
            painter.BeginPath();
            painter.MoveTo(offset + new Vector2(6f, 13f) * scale);
            painter.LineTo(offset + new Vector2(10f, 13f) * scale);
            painter.MoveTo(offset + new Vector2(7f, 15f) * scale);
            painter.LineTo(offset + new Vector2(9f, 15f) * scale);
            painter.Stroke();
        }
    }
}
