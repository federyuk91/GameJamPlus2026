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
        EditorGUILayout.HelpBox("Reach misura la distanza dalla bocca alla corda quando il topo è libero. Da appeso viene usata direttamente la corda impugnata, senza il limite di Reach; eventuali ostacoli possono comunque impedire il morso.", MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Il morso seleziona automaticamente la corda tagliabile più vicina entro Reach dalla bocca, davanti al centro del collider del topo o allineata ad esso. La bocca può aver già oltrepassato la corda. Restano necessari l'assenza di ostacoli e Can Be Cut. Da appeso ha priorità la corda impugnata.", MessageType.Info);
        DrawPropertiesExcluding(serializedObject, "m_Script", "reach", "holdDuration", "mouthOffset");
        serializedObject.ApplyModifiedProperties();
    }
}
