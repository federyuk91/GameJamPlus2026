using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(RoomDefinition))]
public sealed class RoomDefinitionEditor : Editor
{
    private readonly BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RoomDefinition room = (RoomDefinition)target;
        EditorGUILayout.HelpBox("Disegna le Tilemap nel prefab. Sposta le uscite al centro dei punti di aggancio e includi tutta la stanza nell'ingombro.", MessageType.Info);
        if (!room.Validate(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        EditorGUILayout.LabelField("Aggiungi uscita", EditorStyles.boldLabel);
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
        Undo.RegisterCreatedObjectUndo(socket, "Aggiungi uscita stanza");
        socket.transform.SetParent(room.transform, false);
        Rect rect = room.Footprint;
        Vector2 axis = RoomPort.Axis(direction);
        socket.transform.localPosition = rect.center + Vector2.Scale(axis, rect.size * 0.5f);
        RoomPort port = Undo.AddComponent<RoomPort>(socket);
        SerializedObject portData = new SerializedObject(port);
        portData.FindProperty("direction").enumValueIndex = (int)direction;
        portData.ApplyModifiedProperties();
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
        EditorGUILayout.HelpBox("Il totale include partenza e terminali. Ogni uscita deve essere collegata; le stanze mantengono il proprio orientamento.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Genera stanze")) Generate(generator);
            using (new EditorGUI.DisabledScope(generator.GeneratedRoot == null))
                if (GUILayout.Button("Rimuovi stanze generate")) Clear(generator);
        }
        if (!string.IsNullOrEmpty(generator.LastReport)) EditorGUILayout.HelpBox(generator.LastReport, MessageType.Info);
    }

    private static void Generate(RoomLayoutGenerator generator)
    {
        if (EditorUtility.IsPersistent(generator))
        {
            Debug.LogWarning("Trascina il generatore in una scena prima di generare.", generator);
            return;
        }
        if (Quaternion.Angle(generator.transform.rotation, Quaternion.identity) > 0.01f
            || (generator.transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
        {
            Debug.LogError("Il generatore deve avere rotazione zero e scala mondo 1.", generator);
            return;
        }
        RoomLayoutPlanner planner = new RoomLayoutPlanner();
        if (!planner.TryPlan(generator, out List<RoomPlacement> plan, out string report))
        {
            Undo.RecordObject(generator, "Aggiorna rapporto generazione");
            generator.SetResult(generator.GeneratedRoot, report);
            EditorUtility.SetDirty(generator);
            Debug.LogWarning(report, generator);
            return;
        }
        foreach (RoomPlacement placement in plan)
        {
            if (!PrefabUtility.IsPartOfPrefabAsset(placement.Prefab) || placement.Prefab.transform.parent != null)
            {
                Debug.LogError("Assegnare RoomDefinition sulla radice di prefab salvati, non oggetti della scena.", generator);
                return;
            }
        }
        if (generator.GeneratedRoot != null && generator.GeneratedRoot.parent != generator.transform)
        {
            Debug.LogError("Il risultato precedente e stato spostato fuori dal generatore. Ripristinalo prima di rigenerare.", generator);
            return;
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Genera stanze procedurali");
        Undo.RecordObject(generator, "Aggiorna generatore");
        if (generator.GeneratedRoot != null) Undo.DestroyObjectImmediate(generator.GeneratedRoot.gameObject);
        GameObject root = new GameObject("Generated Rooms");
        Undo.RegisterCreatedObjectUndo(root, "Crea contenitore stanze");
        root.transform.SetParent(generator.transform, false);
        foreach (RoomPlacement placement in plan)
        {
            GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(placement.Prefab.gameObject, generator.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(room, "Crea stanza");
            room.transform.SetParent(root.transform, false);
            room.transform.localPosition = placement.Position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(room.transform);
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
        Undo.SetCurrentGroupName("Rimuovi stanze generate");
        Undo.RecordObject(generator, "Aggiorna generatore");
        Undo.DestroyObjectImmediate(generator.GeneratedRoot.gameObject);
        generator.SetResult(null, "Stanze generate rimosse.");
        EditorUtility.SetDirty(generator);
        PrefabUtility.RecordPrefabInstancePropertyModifications(generator);
        Undo.CollapseUndoOperations(group);
    }
}
