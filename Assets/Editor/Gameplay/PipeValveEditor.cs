using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(PipeValve))]
public sealed class PipeValveEditor : Editor
{
    private sealed class Candidate
    {
        public Tilemap map;
        public Vector3Int cell;
        public string label;
    }

    private readonly List<Candidate> candidates = new List<Candidate>();
    private Vector3 previousPosition;
    private WaterConnection previousConnection;
    private string nearestConnectionMessage;
    private double nextConnectionRefresh;

    private void OnEnable()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        serializedObject.Update();
        SerializedProperty frames = serializedObject.FindProperty("closingFrames");
        if (frames.arraySize != 5) frames.arraySize = 5;
        int[] indices = { 241, 240, 226, 225, 224 };
        for (int index = 0; index < indices.Length; index++)
            if (frames.GetArrayElementAtIndex(index).objectReferenceValue == null)
                frames.GetArrayElementAtIndex(index).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/Tiles/All/Full TileSet_" + indices[index] + ".asset");
        serializedObject.ApplyModifiedProperties();
        PipeValve valve = (PipeValve)target;
        previousPosition = valve.transform.position;
        SynchronizeLinkBinding(valve);
        RefreshCandidates();
        if (valve.AutoWaterConnection) GetNearestWaterConnection(valve);
        EditorApplication.update += TrackMovement;
    }

    private void OnDisable() => EditorApplication.update -= TrackMovement;

    private void TrackMovement()
    {
        if (target == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        PipeValve valve = (PipeValve)target;
        if (previousPosition != valve.transform.position)
        {
            previousPosition = valve.transform.position;
            if (valve.Mode == PipeValve.BindingMode.Nearest) BindNearest();
            Repaint();
            SceneView.RepaintAll();
        }
        if (!valve.AutoWaterConnection || EditorApplication.timeSinceStartup < nextConnectionRefresh) return;
        nextConnectionRefresh = EditorApplication.timeSinceStartup + 0.5d;
        GetNearestWaterConnection(valve);
    }

    public override void OnInspectorGUI()
    {
        PipeValve valve = (PipeValve)target;
        EditorGUI.BeginChangeCheck();
        DrawValveProperties();
        if (EditorGUI.EndChangeCheck() && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SynchronizeLinkBinding(valve);
            RefreshCandidates();
            if (valve.AutoWaterConnection) GetNearestWaterConnection(valve);
        }
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Refresh Valve Tiles")) RefreshCandidates();
            if (valve.Mode == PipeValve.BindingMode.ManualSelection)
            {
                List<string> labels = new List<string> { "Select a valve tile…" };
                int selected = 0;
                for (int index = 0; index < candidates.Count; index++)
                {
                    Candidate candidate = candidates[index];
                    labels.Add(candidate.label);
                    if (valve.HasBinding && valve.TargetTilemap == candidate.map && valve.TargetCell == candidate.cell) selected = index + 1;
                }
                int next = EditorGUILayout.Popup("Valve Tile", selected, labels.ToArray());
                if (next != selected && next > 0) Bind(candidates[next - 1]);
            }
        }
        if (!valve.HasBinding)
            EditorGUILayout.HelpBox("No valve bound. Paint a supported tile, then Refresh Valve Tiles. Nearest mode searches within Maximum Search Distance. Search Tilemap is an optional filter.", MessageType.Warning);
        else
        {
            EditorGUILayout.LabelField("Bound Tilemap", valve.TargetTilemap.name);
            EditorGUILayout.LabelField("Bound Cell", valve.TargetCell.ToString());
            if (valve.FrameIndex(valve.TargetTilemap.GetTile(valve.TargetCell)) < 0)
                EditorGUILayout.HelpBox("The bound cell is no longer a valve tile. Refresh the list or select another cell.", MessageType.Warning);
            if (GUILayout.Button("Frame Bound Tile"))
                SceneView.lastActiveSceneView?.Frame(new Bounds(valve.TargetTilemap.GetCellCenterWorld(valve.TargetCell), Vector3.one * 2f), false);
            foreach (GameObject root in valve.gameObject.scene.GetRootGameObjects())
                foreach (PipeValve other in root.GetComponentsInChildren<PipeValve>(true))
                    if (other != valve && other.HasBinding && other.TargetTilemap == valve.TargetTilemap && other.TargetCell == valve.TargetCell)
                        EditorGUILayout.HelpBox("This cell is also controlled by " + other.name + ". Bind only one Pipe Valve per cell.", MessageType.Warning);
        }
        if (valve.Connection != null)
        {
            SerializedObject link = new SerializedObject(valve.Connection);
            if (link.FindProperty("blockingValve").objectReferenceValue != valve)
                EditorGUILayout.HelpBox("This WaterConnection already has another blocking valve. Use a dedicated link or reassign its Blocking Valve.", MessageType.Warning);
        }
        EditorGUILayout.HelpBox("Use Open / Close / Toggle in button UnityEvents. Bake water with the passage open. Tile animation runs only in Play Mode; it preserves painted rotation and tint.", MessageType.Info);
        if (!Application.isPlaying) return;
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open")) valve.Open();
            if (GUILayout.Button("Close")) valve.Close();
            if (GUILayout.Button("Toggle")) valve.Toggle();
        }
    }

    private void DrawValveProperties()
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        SerializedProperty automatic = serializedObject.FindProperty("autoWaterConnection");
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.propertyPath == "autoWaterConnection") continue;
            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"
                || (property.propertyPath == "waterConnection" && automatic.boolValue)))
                EditorGUILayout.PropertyField(property, true);
            if (property.propertyPath != "waterConnection") continue;
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                EditorGUILayout.PropertyField(automatic, new GUIContent("Auto Water Connection"));
            if (automatic.boolValue && !string.IsNullOrEmpty(nearestConnectionMessage))
                EditorGUILayout.HelpBox(nearestConnectionMessage, MessageType.Info);
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void GetNearestWaterConnection(PipeValve valve)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !valve.gameObject.scene.IsValid()) return;
        Vector2 origin = valve.HasBinding ? (Vector2)valve.TargetTilemap.GetCellCenterWorld(valve.TargetCell) : (Vector2)valve.transform.position;
        WaterConnection nearest = null;
        float distance = float.PositiveInfinity;
        foreach (GameObject root in valve.gameObject.scene.GetRootGameObjects())
            foreach (WaterConnection connection in root.GetComponentsInChildren<WaterConnection>())
            {
                if (!connection.isActiveAndEnabled) continue;
                Vector2 passage = new Vector2(connection.transform.position.x, connection.SillHeight);
                float current = (passage - origin).sqrMagnitude;
                if (current >= distance) continue;
                nearest = connection;
                distance = current;
            }
        if (nearest == null)
        {
            nearestConnectionMessage = "No active WaterConnection found in this scene. Current assignment preserved.";
            return;
        }
        SerializedObject link = new SerializedObject(nearest);
        UnityEngine.Object blocker = link.FindProperty("blockingValve").objectReferenceValue;
        if (blocker != null && blocker != valve)
        {
            nearestConnectionMessage = nearest.name + " is the nearest link but is already assigned to " + blocker.name + ". Current assignment preserved.";
            return;
        }
        nearestConnectionMessage = "Assigned " + nearest.name + " (" + Mathf.Sqrt(distance).ToString("0.###") + " world units).";
        if (valve.Connection == nearest) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Assign nearest water connection");
        previousConnection = valve.Connection;
        serializedObject.Update();
        serializedObject.FindProperty("waterConnection").objectReferenceValue = nearest;
        serializedObject.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(valve);
        SynchronizeLinkBinding(valve);
        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll();
        Repaint();
    }

    private void RefreshCandidates()
    {
        candidates.Clear();
        PipeValve valve = (PipeValve)target;
        if (!valve.gameObject.scene.IsValid()) return;
        foreach (GameObject root in valve.gameObject.scene.GetRootGameObjects())
            foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (valve.SearchTilemap != null && map != valve.SearchTilemap) continue;
                foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                {
                    TileBase tile = map.GetTile(cell);
                    int frame = valve.FrameIndex(tile);
                    if (frame < 0) continue;
                    candidates.Add(new Candidate { map = map, cell = cell,
                        label = "(" + cell.x + "," + cell.y + ") - " + MapLabel(map.transform) });
                }
            }
        if (valve.Mode == PipeValve.BindingMode.Nearest) BindNearest();
        Repaint();
    }

    private static string MapLabel(Transform item)
    {
        return item.parent != null ? item.name + "\\" + item.parent.name : item.name;
    }

    private void BindNearest()
    {
        PipeValve valve = (PipeValve)target;
        Candidate nearest = null;
        float distance = valve.MaximumSearchDistance * valve.MaximumSearchDistance;
        foreach (Candidate candidate in candidates)
        {
            if (candidate.map == null || !candidate.map.gameObject.activeInHierarchy) continue;
            float current = (candidate.map.GetCellCenterWorld(candidate.cell) - valve.transform.position).sqrMagnitude;
            if (current > distance) continue;
            distance = current;
            nearest = candidate;
        }
        Bind(nearest);
    }

    private void Bind(Candidate candidate)
    {
        serializedObject.Update();
        serializedObject.FindProperty("targetTilemap").objectReferenceValue = candidate?.map;
        serializedObject.FindProperty("targetCell").vector3IntValue = candidate != null ? candidate.cell : Vector3Int.zero;
        serializedObject.FindProperty("hasBinding").boolValue = candidate != null;
        if (!serializedObject.ApplyModifiedProperties()) return;
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        PipeValve valve = (PipeValve)target;
        if (valve.AutoWaterConnection) GetNearestWaterConnection(valve);
        SceneView.RepaintAll();
    }

    private void SynchronizeLinkBinding(PipeValve valve)
    {
        if (previousConnection == valve.Connection) return;
        if (previousConnection != null)
        {
            SerializedObject previous = new SerializedObject(previousConnection);
            SerializedProperty blocker = previous.FindProperty("blockingValve");
            if (blocker.objectReferenceValue == valve) { blocker.objectReferenceValue = null; previous.ApplyModifiedProperties(); }
        }
        previousConnection = valve.Connection;
        if (valve.Connection == null) return;
        SerializedObject link = new SerializedObject(valve.Connection);
        SerializedProperty blockingValve = link.FindProperty("blockingValve");
        if (blockingValve.objectReferenceValue != null && blockingValve.objectReferenceValue != valve) return;
        blockingValve.objectReferenceValue = valve;
        link.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(valve.Connection);
    }

    private void OnSceneGUI()
    {
        PipeValve valve = (PipeValve)target;
        if (!valve.HasBinding) return;
        Vector3 point = valve.TargetTilemap.GetCellCenterWorld(valve.TargetCell);
        Handles.Label(point + Vector3.up * 0.25f, "Pipe Valve " + valve.TargetCell);
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
    private static void DrawValveIcon(PipeValve valve, GizmoType gizmoType)
    {
        if (!valve.gameObject.activeInHierarchy) return;
        Gizmos.DrawIcon(valve.transform.position, "PipeValve.png", false);
    }
}
