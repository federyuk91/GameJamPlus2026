using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(ParallaxConfigurator))]
public sealed class ParallaxConfiguratorEditor : Editor
{
    private const string UndoLabel = "Configure parallax depth planes";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("deskSurface"));
        SerializedProperty depths = serializedObject.FindProperty("depths");
        for (int index = 0; index < depths.arraySize; index++)
        {
            SerializedProperty plane = depths.GetArrayElementAtIndex(index);
            SerializedProperty category = plane.FindPropertyRelative("depth");
            string title = $"{index + 1:00} - {category.enumDisplayNames[category.enumValueIndex]}";
            EditorGUILayout.PropertyField(plane, new GUIContent(title), true);
        }
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.IsPersistent(target)))
        {
            if (GUILayout.Button("Configure All Depths")) Configure((ParallaxConfigurator)target);
        }
        EditorGUILayout.HelpBox("Applies world Z and sorting to all children, including inactive objects. Preserves X, scale, relative drawing order and parallax settings. Planes with Rest On Desk enabled are aligned vertically to Desk Surface. Undo restores the changes.", MessageType.Info);
    }

    public static bool Configure(ParallaxConfigurator configurator)
    {
        if (!Validate(configurator)) return false;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);
        int configured = 0;
        try
        {
            foreach (ParallaxConfigurator.DepthPlane plane in configurator.Depths)
            {
                if (plane.Parent == null) continue;
                ConfigurePlane(plane);
                configured++;
            }
            ConfigureDeskDecorations(configurator);
        }
        catch (System.Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception, configurator);
            return false;
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
        if (configurator.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(configurator.gameObject.scene);
        Debug.Log($"Configured {configured} parallax depth planes. Changes can be undone together.", configurator);
        return true;
    }

    private static bool Validate(ParallaxConfigurator configurator)
    {
        if (configurator.Depths == null) return false;
        List<Transform> parents = new List<Transform>();
        foreach (ParallaxConfigurator.DepthPlane plane in configurator.Depths)
        {
            if (plane == null)
            {
                Debug.LogError("Remove empty depth entries before configuring parallax.", configurator);
                return false;
            }
            if (plane.Parent == null) continue;
            if (IsDeskDecorationPlane(plane) && configurator.DeskSurface == null)
            {
                Debug.LogError("Assign Desk Surface before configuring decorations resting on the desk. No changes applied.", configurator);
                return false;
            }
            Transform root = plane.Parent.transform;
            if (IsDeskDecorationPlane(plane) && configurator.DeskSurface.transform.IsChildOf(root))
            {
                Debug.LogError("Desk Surface must be outside the decoration parents it supports. No changes applied.", configurator);
                return false;
            }
            if (plane.Parent.scene != configurator.gameObject.scene || plane.Parent.GetComponentInChildren<Camera>(true) != null)
            {
                Debug.LogError($"{plane.Parent.name}: use a parent in this scene that does not contain a camera.", configurator);
                return false;
            }
            foreach (Transform previous in parents)
            {
                if (root == previous || root.IsChildOf(previous) || previous.IsChildOf(root))
                {
                    Debug.LogError("Depth parents must be distinct and must not contain each other.", configurator);
                    return false;
                }
            }
            bool layerExists = false;
            foreach (SortingLayer layer in SortingLayer.layers)
                if (layer.name == plane.SortingLayer) layerExists = true;
            if (!layerExists)
            {
                Debug.LogError($"Sorting Layer '{plane.SortingLayer}' does not exist. No changes applied.", configurator);
                return false;
            }
            parents.Add(root);
        }
        return true;
    }

    private static void ConfigurePlane(ParallaxConfigurator.DepthPlane plane)
    {
        Transform root = plane.Parent.transform;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        SortingGroup[] groups = root.GetComponentsInChildren<SortingGroup>(true);
        SortingGroup rootGroup = root.GetComponent<SortingGroup>();
        List<long> priorities = new List<long>();
        foreach (Renderer renderer in renderers) AddPriority(priorities, renderer.sortingLayerID, renderer.sortingOrder);
        foreach (SortingGroup group in groups)
            if (group != rootGroup) AddPriority(priorities, group.sortingLayerID, group.sortingOrder);
        priorities.Sort();

        foreach (Transform child in transforms) Undo.RecordObject(child, UndoLabel);
        Vector3 position = root.position;
        position.z = plane.WorldZ;
        root.position = position;
        foreach (Transform child in transforms)
        {
            if (child != root)
            {
                Vector3 localPosition = child.localPosition;
                localPosition.z = 0f;
                child.localPosition = localPosition;
            }
            RecordChange(child);
        }

        int layer = SortingLayer.NameToID(plane.SortingLayer);
        foreach (Renderer renderer in renderers)
        {
            int order = RemapOrder(priorities, renderer.sortingLayerID, renderer.sortingOrder, plane);
            Undo.RecordObject(renderer, UndoLabel);
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
            RecordChange(renderer);
        }
        foreach (SortingGroup group in groups)
        {
            if (group == rootGroup) continue;
            int order = RemapOrder(priorities, group.sortingLayerID, group.sortingOrder, plane);
            Undo.RecordObject(group, UndoLabel);
            group.sortAtRoot = false;
            group.sortingLayerID = layer;
            group.sortingOrder = order;
            RecordChange(group);
        }
        if (plane.UseSortingGroup)
        {
            if (rootGroup == null) rootGroup = Undo.AddComponent<SortingGroup>(plane.Parent);
            Undo.RecordObject(rootGroup, UndoLabel);
            rootGroup.enabled = true;
            rootGroup.sortAtRoot = false;
            rootGroup.sortingLayerID = layer;
            rootGroup.sortingOrder = plane.SortingOrder;
            RecordChange(rootGroup);
        }
        else if (rootGroup != null) Undo.DestroyObjectImmediate(rootGroup);
    }

    private static long Priority(int layer, int order)
    {
        return SortingLayer.GetLayerValueFromID(layer) * 65536L + order + 32768L;
    }

    private static bool IsDeskDecorationPlane(ParallaxConfigurator.DepthPlane plane)
    {
        return plane.RestOnDesk;
    }

    private static void ConfigureDeskDecorations(ParallaxConfigurator configurator)
    {
        foreach (ParallaxConfigurator.DepthPlane plane in configurator.Depths)
        {
            if (plane.Parent == null || !IsDeskDecorationPlane(plane)) continue;
            ConfigureDeskProp(plane.Parent.transform, configurator.DeskSurface);
        }
    }

    private static void ConfigureDeskProp(Transform prop, SpriteRenderer surface)
    {
        DeskSurfaceAnchor anchor = prop.GetComponent<DeskSurfaceAnchor>();
        SpriteRenderer ownRenderer = prop.GetComponent<SpriteRenderer>();
        if (anchor == null && ownRenderer == null)
        {
            foreach (Transform child in prop) ConfigureDeskProp(child, surface);
            return;
        }
        if (anchor == null) anchor = Undo.AddComponent<DeskSurfaceAnchor>(prop.gameObject);
        Undo.RecordObject(anchor, UndoLabel);
        Undo.RecordObject(prop, UndoLabel);
        anchor.Configure(surface, prop.GetComponentsInChildren<SpriteRenderer>(true));
        anchor.AlignToSurface();
        RecordChange(anchor);
        RecordChange(prop);
    }

    private static void AddPriority(List<long> priorities, int layer, int order)
    {
        long priority = Priority(layer, order);
        if (!priorities.Contains(priority)) priorities.Add(priority);
    }

    private static int RemapOrder(List<long> priorities, int layer, int order, ParallaxConfigurator.DepthPlane plane)
    {
        int rank = priorities.IndexOf(Priority(layer, order));
        int result = plane.UseSortingGroup ? rank : plane.SortingOrder - priorities.Count + 1 + rank;
        return Mathf.Clamp(result, short.MinValue, short.MaxValue);
    }

    private static void RecordChange(Object changed)
    {
        EditorUtility.SetDirty(changed);
        if (PrefabUtility.IsPartOfPrefabInstance(changed)) PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
    }
}
