using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(RoomDefinition))]
public sealed class RoomDefinitionEditor : Editor
{
    private readonly BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RoomDefinition room = (RoomDefinition)target;
        EditorGUILayout.HelpBox("Paint Tilemaps inside the prefab. Exits snap to cell edges/centers. Include the entire room in its footprint.", MessageType.Info);
        if (!room.Validate(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        if (GUILayout.Button("Align exits to grid"))
        {
            for (int i = 0; i < room.PortCount; i++)
            {
                RoomPort port = room.Port(i);
                if (port == null) continue;
                Undo.RecordObject(port.transform, "Align exit to grid");
                port.transform.position = room.SnapPortPosition(port.Position, port.Direction);
                PrefabUtility.RecordPrefabInstancePropertyModifications(port.transform);
            }
        }
        EditorGUILayout.LabelField("Add exit", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (RoomDirection direction in System.Enum.GetValues(typeof(RoomDirection)))
            {
                bool exists = false;
                for (int i = 0; i < room.PortCount; i++)
                    if (room.Port(i) != null && room.Port(i).Direction == direction) exists = true;
                using (new EditorGUI.DisabledScope(exists))
                    if (GUILayout.Button(direction.ToString())) AddPort(room, direction);
            }
        }
    }

    private static void AddPort(RoomDefinition room, RoomDirection direction)
    {
        GameObject socket = new GameObject("Exit " + direction);
        Undo.RegisterCreatedObjectUndo(socket, "Add room exit");
        socket.transform.SetParent(room.transform, false);
        Rect rect = room.Footprint;
        Vector2 axis = RoomPort.Axis(direction);
        socket.transform.localPosition = rect.center + Vector2.Scale(axis, rect.size * 0.5f);
        RoomPort port = Undo.AddComponent<RoomPort>(socket);
        SerializedObject portData = new SerializedObject(port);
        portData.FindProperty("direction").enumValueIndex = (int)direction;
        portData.ApplyModifiedProperties();
        socket.transform.position = room.SnapPortPosition(socket.transform.position, direction);
        SerializedObject data = new SerializedObject(room);
        SerializedProperty ports = data.FindProperty("ports");
        ports.arraySize++;
        ports.GetArrayElementAtIndex(ports.arraySize - 1).objectReferenceValue = port;
        data.ApplyModifiedProperties();
        Selection.activeGameObject = socket;
    }

    private void OnSceneGUI()
    {
        RoomDefinition room = (RoomDefinition)target;
        Rect rect = room.Footprint;
        using (new Handles.DrawingScope(Color.cyan, room.transform.localToWorldMatrix))
        {
            boundsHandle.center = rect.center;
            boundsHandle.size = new Vector3(rect.width, rect.height, 0f);
            EditorGUI.BeginChangeCheck();
            boundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                SerializedProperty footprint = serializedObject.FindProperty("footprint");
                footprint.rectValue = new Rect((Vector2)boundsHandle.center - (Vector2)boundsHandle.size * 0.5f,
                    new Vector2(Mathf.Max(0.01f, boundsHandle.size.x), Mathf.Max(0.01f, boundsHandle.size.y)));
                serializedObject.ApplyModifiedProperties();
            }
        }
        for (int i = 0; i < room.PortCount; i++)
            if (room.Port(i) != null) Handles.Label(room.Port(i).Position, room.Port(i).Direction.ToString());
    }
}

[CustomEditor(typeof(RoomLayoutGenerator))]
public sealed class RoomLayoutGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RoomLayoutGenerator generator = (RoomLayoutGenerator)target;
        EditorGUILayout.HelpBox("The total includes the starting room and terminals. Rooms align to a shared Grid; exits snap to cell edges and centers.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Generate rooms")) Generate(generator);
            using (new EditorGUI.DisabledScope(generator.GeneratedRoot == null))
                if (GUILayout.Button("Remove generated rooms")) Clear(generator);
        }
        if (!string.IsNullOrEmpty(generator.LastReport)) EditorGUILayout.HelpBox(generator.LastReport, MessageType.Info);
    }

    private static void Generate(RoomLayoutGenerator generator)
    {
        if (EditorUtility.IsPersistent(generator))
        {
            Debug.LogWarning("Drag the generator into a scene before generating.", generator);
            return;
        }
        if (Quaternion.Angle(generator.transform.rotation, Quaternion.identity) > 0.01f
            || (generator.transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
        {
            Debug.LogError("The generator must have zero rotation and a world scale of 1.", generator);
            return;
        }
        RoomLayoutPlanner planner = new RoomLayoutPlanner();
        if (!planner.TryPlan(generator, out List<RoomPlacement> plan, out string report))
        {
            Undo.RecordObject(generator, "Update generation report");
            generator.SetResult(generator.GeneratedRoot, report);
            EditorUtility.SetDirty(generator);
            Debug.LogWarning(report, generator);
            return;
        }
        foreach (RoomPlacement placement in plan)
        {
            if (!PrefabUtility.IsPartOfPrefabAsset(placement.Prefab) || placement.Prefab.transform.parent != null)
            {
                Debug.LogError("Assign RoomDefinition on the root of saved prefabs, rather than scene objects.", generator);
                return;
            }
        }
        if (generator.GeneratedRoot != null && generator.GeneratedRoot.parent != generator.transform)
        {
            Debug.LogError("The previous result was moved outside the generator. Restore it before regenerating.", generator);
            return;
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate procedural rooms");
        Undo.RecordObject(generator, "Update generator");
        if (generator.GeneratedRoot != null) Undo.DestroyObjectImmediate(generator.GeneratedRoot.gameObject);
        GameObject root = new GameObject("Generated Rooms");
        Undo.RegisterCreatedObjectUndo(root, "Create room container");
        root.transform.SetParent(generator.transform, false);
        Vector2 cellSize = generator.CellSize;
        Vector3 origin = generator.transform.position;
        origin.x = Mathf.Round(origin.x / cellSize.x) * cellSize.x;
        origin.y = Mathf.Round(origin.y / cellSize.y) * cellSize.y;
        root.transform.position = origin;
        Grid commonGrid = Undo.AddComponent<Grid>(root);
        commonGrid.cellSize = new Vector3(cellSize.x, cellSize.y, 1f);
        foreach (RoomPlacement placement in plan)
        {
            GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(placement.Prefab.gameObject, generator.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(room, "Create room");
            room.transform.SetParent(root.transform, false);
            room.transform.localPosition = placement.Position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(room.transform);
            RoomDefinition definition = room.GetComponent<RoomDefinition>();
            for (int i = 0; i < definition.PortCount; i++)
            {
                RoomPort port = definition.Port(i);
                port.transform.position = root.transform.TransformPoint(placement.Position + placement.Prefab.PortOffset(i));
                PrefabUtility.RecordPrefabInstancePropertyModifications(port.transform);
            }
            // Keep authoring grids in the source prefabs; generated instances share this one.
            foreach (Grid localGrid in room.GetComponentsInChildren<Grid>(true))
                Undo.DestroyObjectImmediate(localGrid);
            SerializedObject definitionData = new SerializedObject(definition);
            definitionData.FindProperty("layoutGrid").objectReferenceValue = commonGrid;
            definitionData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(definition);
            foreach (Tilemap tilemap in room.GetComponentsInChildren<Tilemap>(true)) tilemap.RefreshAllTiles();
        }
        generator.SetResult(root.transform, report);
        EditorUtility.SetDirty(generator);
        PrefabUtility.RecordPrefabInstancePropertyModifications(generator);
        Undo.CollapseUndoOperations(group);
        Debug.Log(report, generator);
    }

    private static void Clear(RoomLayoutGenerator generator)
    {
        if (generator.GeneratedRoot == null || generator.GeneratedRoot.parent != generator.transform) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Remove generated rooms");
        Undo.RecordObject(generator, "Update generator");
        Undo.DestroyObjectImmediate(generator.GeneratedRoot.gameObject);
        generator.SetResult(null, "Generated rooms removed.");
        EditorUtility.SetDirty(generator);
        PrefabUtility.RecordPrefabInstancePropertyModifications(generator);
        Undo.CollapseUndoOperations(group);
    }
}
