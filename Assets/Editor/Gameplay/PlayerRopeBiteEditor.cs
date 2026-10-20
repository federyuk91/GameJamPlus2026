using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerRopeBite)), CanEditMultipleObjects]
public sealed class PlayerRopeBiteEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        EditorGUILayout.LabelField("Bite Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("reach"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("holdDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mouthOffset"));
        EditorGUILayout.HelpBox("Reach measures mouth-to-rope distance while the mouse is unattached. While hanging, the held rope is used directly without the Reach limit; obstacles can still prevent biting.", MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Biting automatically selects the nearest cuttable rope within Reach of the mouth, in front of or aligned with the mouse collider center. The mouth may already be past the rope. A clear path and Can Be Cut are still required. While hanging, the held rope takes priority.", MessageType.Info);
        DrawPropertiesExcluding(serializedObject, "m_Script", "reach", "holdDuration", "mouthOffset");
        serializedObject.ApplyModifiedProperties();
    }
}
