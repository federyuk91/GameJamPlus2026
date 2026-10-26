using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimulatedBody2D)), CanEditMultipleObjects]
public sealed class SimulatedBody2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty localOverride = serializedObject.FindProperty("overrideMaxSpeed");
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            bool speedValue = property.propertyPath == "maxSpeed";
            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"
                || (speedValue && (!localOverride.boolValue || localOverride.hasMultipleDifferentValues))))
            {
                if (speedValue) EditorGUILayout.PropertyField(property, new GUIContent("Override Max Speed Value", property.tooltip));
                else EditorGUILayout.PropertyField(property, true);
            }
            if (property.propertyPath == "weight")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    bool canPress = ((SimulatedBody2D)target).CanPressButtons;
                    bool mixed = false;
                    foreach (Object selected in targets)
                        mixed |= ((SimulatedBody2D)selected).CanPressButtons != canPress;
                    EditorGUI.showMixedValue = mixed;
                    EditorGUILayout.Toggle("Can Press Buttons", canPress);
                    EditorGUI.showMixedValue = false;
                }
            }
        }
        serializedObject.ApplyModifiedProperties();
        if (targets.Length == 1)
        {
            SimulatedBody2D body = (SimulatedBody2D)target;
            EditorGUILayout.LabelField("Effective Max Speed", body.MaxSpeed.ToString("0.###") + " units/s");
        }
    }
}
