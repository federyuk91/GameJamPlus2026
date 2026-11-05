using System;
using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaterGeometryBaker))]
public sealed class WaterGeometryBakerEditor : Editor
{
    private void OnEnable()
    {
        if (Application.isPlaying) return;
        serializedObject.Update();
        SerializedProperty prefab = serializedObject.FindProperty("waterVolumePrefab");
        SerializedProperty material = serializedObject.FindProperty("waterMaterial");
        if (prefab.objectReferenceValue == null || AssetDatabase.GetAssetPath(prefab.objectReferenceValue) == "Assets/Prefabs/Environment/Water Volume.prefab") prefab.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Water Section.prefab");
        if (material.objectReferenceValue == null) material.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlatformWater.mat");
        serializedObject.ApplyModifiedProperties();
        WaterGeometryBakeTools.RenameConnections((WaterGeometryBaker)target);
    }

    private void OnDisable() => WaterSimulationPreview.Stop(target);

    public override void OnInspectorGUI()
    {
        WaterGeometryBaker baker = (WaterGeometryBaker)target;
        float previousOffset = baker.VisualOverlapOffset;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) WaterSimulationPreview.Stop(target);
        if (!Mathf.Approximately(previousOffset, baker.VisualOverlapOffset)) WaterGeometryBakeTools.RefreshWaterPreviews(baker);
        WaterSimulationPreview.DrawInspector(target, baker.Basins);
        EditorGUILayout.HelpBox("Assign solid Tilemaps or select them in the Hierarchy and use Tools > Mouse Platformer > Water > Create Baker from Selected Tilemaps. Decorative Tilemaps do not block water.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("Bake Water Geometry"))
            {
                try { WaterGeometryBakeTools.Bake(baker); }
                catch (OperationCanceledException exception) { Debug.Log(exception.Message, baker); }
                catch (Exception exception) { Debug.LogException(exception, baker); }
            }
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || baker.Basins.Count == 0))
            if (GUILayout.Button("Center Basin Pivots")) WaterGeometryBakeTools.CenterBasinPivots(baker);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || baker.Basins.Count == 0))
            if (GUILayout.Button("Refresh Water Preview")) WaterGeometryBakeTools.RefreshWaterPreviews(baker);
        if (baker.HasBake) EditorGUILayout.LabelField("Baked world area", baker.BakedBounds.ToString());
    }

    private void OnSceneGUI()
    {
        WaterGeometryBaker baker = (WaterGeometryBaker)target;
        UnityEngine.Rendering.CompareFunction previousDepth = Handles.zTest;
        Color previousColor = Handles.color;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        Handles.color = Color.cyan;
        try
        {
            if (baker.UseCustomBounds)
            {
                Handles.color = new Color(1f, 0.65f, 0.15f, 1f);
                Handles.Label(new Vector3(baker.CustomBounds.xMin, baker.CustomBounds.yMax, 0f), "Custom Bounds · next bake");
            }
            Handles.color = Color.cyan;
            foreach (WaterBasin basin in baker.Basins)
            {
                if (basin == null || !basin.gameObject.activeInHierarchy) continue;
                Vector2 center = basin.GeometryBounds.center;
                Vector3 point = new Vector3(center.x, center.y, basin.transform.position.z);
                float size = HandleUtility.GetHandleSize(point) * 0.07f;
                Handles.Label(point + Vector3.right * size * 1.5f, basin.name);
                if (!Handles.Button(point, Quaternion.identity, size, size * 1.5f, Handles.DotHandleCap)) continue;
                Selection.activeGameObject = basin.gameObject;
                EditorGUIUtility.PingObject(basin.gameObject);
                break;
            }
        }
        finally
        {
            Handles.zTest = previousDepth;
            Handles.color = previousColor;
        }
    }

}
