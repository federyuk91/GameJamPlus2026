using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MagnifyingLens2D)), CanEditMultipleObjects]
public sealed class MagnifyingLens2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "maskSource", "effectMask", "effectMaskSprite", "maskChannel", "maskStrength", "greenDominance");
        SerializedProperty source = serializedObject.FindProperty("maskSource");
        EditorGUILayout.PropertyField(source);
        if (source.hasMultipleDifferentValues)
            EditorGUILayout.HelpBox("Select the same Mask Source to edit its mask fields together.", MessageType.Info);
        else
        {
            MagnifyingLens2D.EffectMaskSource mode = (MagnifyingLens2D.EffectMaskSource)source.enumValueIndex;
            switch (mode)
            {
                case MagnifyingLens2D.EffectMaskSource.GreenChroma:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("greenDominance"));
                    break;
                case MagnifyingLens2D.EffectMaskSource.SeparateTexture:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("effectMask"));
                    break;
                case MagnifyingLens2D.EffectMaskSource.SeparateSprite:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("effectMaskSprite"));
                    break;
                case MagnifyingLens2D.EffectMaskSource.SpriteSecondaryTexture:
                    EditorGUILayout.HelpBox("Assign a Secondary Texture named _LensMask in the main sprite's Sprite Editor. No separate mask field is required here.", MessageType.Info);
                    break;
            }
            if (mode != MagnifyingLens2D.EffectMaskSource.GreenChroma)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maskChannel"));
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maskStrength"));
        if (!serializedObject.ApplyModifiedProperties()) return;
        foreach (Object item in targets) ((MagnifyingLens2D)item).ApplySettings();
        SceneView.RepaintAll();
    }
}
