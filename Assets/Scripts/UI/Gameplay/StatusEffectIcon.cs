using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [UxmlElement]
    public sealed partial class StatusEffectIcon : VisualElement
    {
        public enum EffectKind { Poison, Electricity, Heart }

        private static readonly string[] PoisonPixels =
        {
            "    ####    ", "   ######   ", "  ########  ", "  ## ## ##  ",
            "  ## ## ##  ", "   ######   ", "    #  #    ", "    ####    ",
            " #        # ", "  ##    ##  ", "    ####    ", "  ##    ##  "
        };
        private static readonly string[] ElectricityPixels =
        {
            "       ##   ", "      ###   ", "     ###    ", "    ###     ",
            "   ###      ", "  #######   ", "   ######   ", "      ###   ",
            "     ###    ", "    ###     ", "   ##       ", "  #         "
        };
        private static readonly string[] HeartPixels =
        {
            "            ", "  ###  ###  ", " ########## ", " ########## ",
            " ########## ", "  ########  ", "   ######   ", "    ####    ",
            "     ##     ", "            ", "            ", "            "
        };
        private EffectKind effect;

        [UxmlAttribute]
        public EffectKind Effect
        {
            get => effect;
            set { effect = value; MarkDirtyRepaint(); }
        }

        public StatusEffectIcon()
        {
            pickingMode = PickingMode.Ignore;
            style.width = style.height = 12f;
            style.flexShrink = 0f;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            float scale = Mathf.Floor(Mathf.Min(contentRect.width, contentRect.height) / 12f);
            if (scale < 1f) return;
            Vector2 origin = new Vector2(Mathf.Floor((contentRect.width - 12f * scale) * 0.5f),
                Mathf.Floor((contentRect.height - 12f * scale) * 0.5f));
            string[] pixels = effect == EffectKind.Heart ? HeartPixels
                : effect == EffectKind.Poison ? PoisonPixels : ElectricityPixels;
            Painter2D painter = context.painter2D;
            painter.fillColor = effect == EffectKind.Heart ? new Color(1f, 0.3f, 0.35f)
                : effect == EffectKind.Poison
                ? new Color(64f / 255f, 201f / 255f, 119f / 255f)
                : new Color(153f / 255f, 221f / 255f, 1f);
            for (int row = 0; row < pixels.Length; row++)
            {
                for (int column = 0; column < pixels[row].Length; column++)
                {
                    if (pixels[row][column] != '#') continue;
                    Vector2 point = origin + new Vector2(column, row) * scale;
                    painter.BeginPath();
                    painter.MoveTo(point);
                    painter.LineTo(point + Vector2.right * scale);
                    painter.LineTo(point + Vector2.one * scale);
                    painter.LineTo(point + Vector2.up * scale);
                    painter.ClosePath();
                    painter.Fill();
                }
            }
        }
    }
}
