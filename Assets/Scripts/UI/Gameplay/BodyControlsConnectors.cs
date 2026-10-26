using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [UxmlElement]
    public sealed partial class BodyControlsConnectors : VisualElement
    {
        private static readonly CustomStyleProperty<Color> LineColor = new CustomStyleProperty<Color>("--body-connector-color");
        private static readonly CustomStyleProperty<float> LineWidth = new CustomStyleProperty<float>("--body-connector-width");
        private static readonly CustomStyleProperty<Color> AnchorColor = new CustomStyleProperty<Color>("--body-anchor-color");
        private readonly VisualElement[] callouts = new VisualElement[6];
        private readonly VisualElement[] anchors = new VisualElement[6];
        private readonly List<VisualElement> watchedElements = new List<VisualElement>();
        private readonly List<Vector2> hiddenIntervals = new List<Vector2>(6);
        private Color lineColor = Color.white;
        private Color anchorColor = new Color32(64, 224, 240, 255);
        private float lineWidth = 1f;

        public BodyControlsConnectors()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        }

        private void OnAttach(AttachToPanelEvent attachEvent)
        {
            if (parent == null) return;
            Watch(parent);
            VisualElement image = parent.Q("body-image");
            if (image != null) Watch(image);
            ResolveTargets();
            MarkDirtyRepaint();
        }

        private void ResolveTargets()
        {
            if (parent == null) return;
            for (int index = 0; index < callouts.Length; index++)
            {
                string calloutName = index == 5 ? "body-camera-callout1"
                    : index == 4 ? "body-camera-callout" : "body-callout-" + index;
                string anchorName = index >= 4 ? "body-camera-anchor" : "body-anchor-" + index;
                if (callouts[index] == null) callouts[index] = parent.Q(calloutName);
                if (anchors[index] == null) anchors[index] = parent.Q(anchorName);
                if (callouts[index] != null) Watch(callouts[index]);
                if (anchors[index] != null) Watch(anchors[index]);
            }
        }

        private void Watch(VisualElement element)
        {
            if (watchedElements.Contains(element)) return;
            watchedElements.Add(element);
            element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnDetach(DetachFromPanelEvent detachEvent)
        {
            foreach (VisualElement element in watchedElements) element.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            watchedElements.Clear();
            for (int index = 0; index < callouts.Length; index++)
            {
                callouts[index] = null;
                anchors[index] = null;
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent geometryEvent) => MarkDirtyRepaint();

        private void OnStyleResolved(CustomStyleResolvedEvent styleEvent)
        {
            if (customStyle.TryGetValue(LineColor, out Color color)) lineColor = color;
            if (customStyle.TryGetValue(LineWidth, out float width)) lineWidth = Mathf.Max(0f, width);
            if (customStyle.TryGetValue(AnchorColor, out Color pointColor)) anchorColor = pointColor;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            // UI Builder can attach this element before its sibling labels are cloned.
            ResolveTargets();
            Painter2D painter = context.painter2D;
            painter.lineWidth = lineWidth;
            painter.strokeColor = painter.fillColor = lineColor;
            for (int index = 0; index < callouts.Length; index++)
            {
                if (callouts[index] == null || anchors[index] == null) continue;
                Vector2 start = callouts[index].ChangeCoordinatesTo(this, callouts[index].contentRect.center);
                Vector2 end = anchors[index].ChangeCoordinatesTo(this, Vector2.zero);
                DrawVisibleLine(painter, start, end);
            }

            painter.strokeColor = new Color32(9, 28, 40, 255);
            painter.fillColor = anchorColor;
            painter.lineWidth = 1f;
            for (int index = 0; index < anchors.Length; index++)
            {
                if (callouts[index] == null || anchors[index] == null) continue;
                if (index == 5 && anchors[index] == anchors[4]) continue;
                Vector2 point = anchors[index].ChangeCoordinatesTo(this, Vector2.zero);
                painter.BeginPath();
                painter.Arc(point, 2.5f, 0f, 360f);
                painter.Fill();
                painter.Stroke();
            }
        }

        private void DrawVisibleLine(Painter2D painter, Vector2 start, Vector2 end)
        {
            hiddenIntervals.Clear();
            foreach (VisualElement callout in callouts)
            {
                if (callout == null || callout.resolvedStyle.display == DisplayStyle.None) continue;
                Vector2 firstCorner = callout.ChangeCoordinatesTo(this, Vector2.zero);
                Vector2 oppositeCorner = callout.ChangeCoordinatesTo(this, callout.layout.size);
                Vector2 minimum = Vector2.Min(firstCorner, oppositeCorner);
                Vector2 maximum = Vector2.Max(firstCorner, oppositeCorner);
                Rect bounds = Rect.MinMaxRect(minimum.x - 1f, minimum.y - 1f, maximum.x + 1f, maximum.y + 1f);
                if (TryGetHiddenInterval(start, end, bounds, out Vector2 interval)) hiddenIntervals.Add(interval);
            }

            // Keycap textures are transparent: omit the line beneath every callout.
            hiddenIntervals.Sort(CompareIntervals);
            float visibleStart = 0f;
            foreach (Vector2 interval in hiddenIntervals)
            {
                DrawLineSection(painter, start, end, visibleStart, interval.x);
                visibleStart = Mathf.Max(visibleStart, interval.y);
            }
            DrawLineSection(painter, start, end, visibleStart, 1f);
        }

        private static int CompareIntervals(Vector2 first, Vector2 second) => first.x.CompareTo(second.x);

        private static void DrawLineSection(Painter2D painter, Vector2 start, Vector2 end, float from, float to)
        {
            if (to <= from) return;
            painter.BeginPath();
            painter.MoveTo(Vector2.Lerp(start, end, from));
            painter.LineTo(Vector2.Lerp(start, end, to));
            painter.Stroke();
        }

        private static bool TryGetHiddenInterval(Vector2 start, Vector2 end, Rect bounds, out Vector2 interval)
        {
            float enter = 0f;
            float exit = 1f;
            Vector2 direction = end - start;
            bool intersects = ClipAxis(start.x, direction.x, bounds.xMin, bounds.xMax, ref enter, ref exit)
                && ClipAxis(start.y, direction.y, bounds.yMin, bounds.yMax, ref enter, ref exit);
            interval = new Vector2(enter, exit);
            return intersects;
        }

        private static bool ClipAxis(float origin, float direction, float minimum, float maximum, ref float enter, ref float exit)
        {
            if (Mathf.Abs(direction) < 0.0001f) return origin >= minimum && origin <= maximum;
            float first = (minimum - origin) / direction;
            float second = (maximum - origin) / direction;
            enter = Mathf.Max(enter, Mathf.Min(first, second));
            exit = Mathf.Min(exit, Mathf.Max(first, second));
            return enter <= exit;
        }
    }
}
