using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaterConnection))]
public sealed class WaterConnectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        WaterConnection connection = (WaterConnection)target;
        EditorGUILayout.LabelField("World Sill Height", connection.SillHeight.ToString("0.000"));
        EditorGUILayout.HelpBox("Yellow marker: passage sill. Cyan double arrow: connection orientation; flow can reverse. Basin yellow lines show the actual water surface, not pipes or collision shapes.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        WaterConnection connection = (WaterConnection)target;
        Vector3 sill = new Vector3(connection.transform.position.x, connection.SillHeight, connection.transform.position.z);
        Handles.Label(sill + Vector3.up * 0.2f, "Water passage · sill " + connection.SillHeight.ToString("0.00"));
    }
}
