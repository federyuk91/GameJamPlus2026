using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RopeSwingConstraint))]
public sealed class RopeSwingConstraintDrawer : PropertyDrawer
{
    private const float AngleLimit = 175f;
    private const float OuterMinDistance = 0.01f;
    private const float TrackHeight = 24f;
    private const float HandleSize = 8f;

    private static readonly GUIContent[] AngleLabels =
    {
        new GUIContent("Min outer", "Hard stop at the negative angle."),
        new GUIContent("Min inner", "Start of braking towards the negative angle."),
        new GUIContent("Max inner", "Start of braking towards the positive angle."),
        new GUIContent("Max outer", "Hard stop at the positive angle.")
    };

    private static readonly Color InnerColor = new Color(0.2f, 0.8f, 0.95f);
    private static readonly Color OuterColor = new Color(1f, 0.48f, 0.26f);

    private sealed class HandleDrag
    {
        public float Offset { get; set; }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float gap = EditorGUIUtility.standardVerticalSpacing;
        return property.isExpanded ? 3f * line + TrackHeight + 3f * gap : line;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float gap = EditorGUIUtility.standardVerticalSpacing;
        using (EditorGUI.PropertyScope scope = new EditorGUI.PropertyScope(position, label, property))
        {
            Rect header = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, scope.content, true);
            if (!property.isExpanded) return;

            SerializedProperty[] angles =
            {
                property.FindPropertyRelative("minOuterAngle"), property.FindPropertyRelative("minInnerAngle"),
                property.FindPropertyRelative("maxInnerAngle"), property.FindPropertyRelative("maxOuterAngle")
            };
            Rect content = EditorGUI.IndentedRect(new Rect(position.x, header.yMax + gap, position.width, TrackHeight));
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                DrawSlider(content, angles);
                DrawAngleFields(content, angles, line, gap);
            }
        }
    }

    private static void DrawSlider(Rect rect, SerializedProperty[] angles)
    {
        Rect track = new Rect(rect.x + HandleSize, rect.center.y - 2f, Mathf.Max(1f, rect.width - 2f * HandleSize), 4f);
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(track, new Color(0.5f, 0.5f, 0.5f, 0.45f));
            DrawBand(track, angles[0].floatValue, angles[1].floatValue, OuterColor);
            DrawBand(track, angles[1].floatValue, angles[2].floatValue, InnerColor);
            DrawBand(track, angles[2].floatValue, angles[3].floatValue, OuterColor);
            EditorGUI.DrawRect(new Rect(track.center.x - 0.5f, track.y - 3f, 1f, 10f), EditorStyles.label.normal.textColor);
        }

        for (int i = 0; i < angles.Length; i++) DrawHandle(track, angles, i);
    }

    private static void DrawBand(Rect track, float start, float end, Color color)
    {
        float left = AngleToX(track, start);
        float right = AngleToX(track, end);
        color.a = 0.55f;
        if (right > left) EditorGUI.DrawRect(new Rect(left, track.y, right - left, track.height), color);
    }

    private static void DrawHandle(Rect track, SerializedProperty[] angles, int index)
    {
        SerializedProperty angle = angles[index];
        bool outer = index == 0 || index == 3;
        float x = AngleToX(track, angle.floatValue);
        // Le corsie distinguono inner e outer, la tacca centrale separa min e max a zero.
        float y = outer ? track.y - HandleSize : track.yMax;
        Rect handle = new Rect(x - HandleSize * 0.5f, y, HandleSize, HandleSize);
        Rect hitArea = new Rect(handle.x - 3f, handle.y - 1f, handle.width + 6f, handle.height + 2f);
        if (index < 2)
        {
            handle.xMax = Mathf.Min(handle.xMax, track.center.x);
            hitArea.xMax = Mathf.Min(hitArea.xMax, track.center.x);
        }
        else
        {
            handle.xMin = Mathf.Max(handle.xMin, track.center.x);
            hitArea.xMin = Mathf.Max(hitArea.xMin, track.center.x);
        }
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event input = Event.current;
        bool enabled = GUI.enabled;

        if (enabled) EditorGUIUtility.AddCursorRect(hitArea, MouseCursor.ResizeHorizontal);
        GUI.Label(hitArea, new GUIContent(string.Empty, $"{AngleLabels[index].text}: {angle.floatValue:0.##}°"));
        switch (input.GetTypeForControl(id))
        {
            case EventType.Repaint:
                Color color = outer ? OuterColor : InnerColor;
                if (GUIUtility.hotControl == id || (enabled && hitArea.Contains(input.mousePosition)))
                    color = Color.Lerp(color, Color.white, 0.4f);
                if (!enabled) color.a = 0.5f;
                EditorGUI.DrawRect(handle, color);
                break;
            case EventType.MouseDown when enabled && input.button == 0 && hitArea.Contains(input.mousePosition):
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = id;
                HandleDrag drag = (HandleDrag)GUIUtility.GetStateObject(typeof(HandleDrag), id);
                drag.Offset = input.mousePosition.x - x;
                input.Use();
                break;
            case EventType.MouseDrag when GUIUtility.hotControl == id:
                HandleDrag activeDrag = (HandleDrag)GUIUtility.GetStateObject(typeof(HandleDrag), id);
                float fraction = Mathf.InverseLerp(track.xMin, track.xMax, input.mousePosition.x - activeDrag.Offset);
                float value = Mathf.Lerp(-AngleLimit, AngleLimit, fraction);
                SetAngle(angles, index, Mathf.Round(value * 10f) * 0.1f);
                input.Use();
                break;
            case EventType.MouseUp when GUIUtility.hotControl == id && input.button == 0:
                GUIUtility.hotControl = 0;
                input.Use();
                break;
        }
    }

    private static void DrawAngleFields(Rect trackRow, SerializedProperty[] angles, float line, float gap)
    {
        const float spacing = 4f;
        float width = Mathf.Max(1f, (trackRow.width - 3f * spacing) / angles.Length);
        for (int i = 0; i < angles.Length; i++)
        {
            Rect title = new Rect(trackRow.x + i * (width + spacing), trackRow.yMax + gap, width, line);
            Rect field = new Rect(title.x, title.yMax + gap, width, line);
            using (new EditorGUI.PropertyScope(field, AngleLabels[i], angles[i]))
            {
                EditorGUI.LabelField(title, AngleLabels[i], EditorStyles.miniLabel);
                EditorGUI.BeginChangeCheck();
                float value = EditorGUI.DelayedFloatField(field, angles[i].floatValue);
                if (EditorGUI.EndChangeCheck()) SetAngle(angles, i, value);
            }
        }
    }

    private static float AngleToX(Rect track, float angle) =>
        Mathf.Lerp(track.xMin, track.xMax, Mathf.InverseLerp(-AngleLimit, AngleLimit, angle));

    private static void SetAngle(SerializedProperty[] angles, int index, float value)
    {
        angles[index].floatValue = index switch
        {
            0 => Mathf.Clamp(value, -AngleLimit, Mathf.Min(angles[1].floatValue, -OuterMinDistance)),
            1 => Mathf.Clamp(value, angles[0].floatValue, 0f),
            2 => Mathf.Clamp(value, 0f, angles[3].floatValue),
            _ => Mathf.Clamp(value, Mathf.Max(angles[2].floatValue, OuterMinDistance), AngleLimit)
        };
    }
}
