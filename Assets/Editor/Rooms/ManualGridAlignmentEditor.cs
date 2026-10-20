using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ManualGridAlignment))]
public sealed class ManualGridAlignmentEditor : Editor
{
    private readonly struct Placement
    {
        public Transform Transform { get; }
        public Vector3 Position { get; }
        public Placement(Transform transform, Vector3 position)
        {
            Transform = transform;
            Position = position;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Place rooms or Grids under this container, then align them. Entire rooms are moved; tiles are not modified.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || EditorUtility.IsPersistent(target)))
            if (GUILayout.Button("Align child grids")) Align((ManualGridAlignment)target);
    }

    private static void Align(ManualGridAlignment container)
    {
        Dictionary<Transform, Vector3> displacements = new Dictionary<Transform, Vector3>();
        foreach (Grid grid in container.GetComponentsInChildren<Grid>(true))
        {
            if (grid == container.ReferenceGrid || grid.transform == container.transform) continue;
            if (!container.TryGetDisplacement(grid, out Vector3 displacement, out string error))
            {
                Debug.LogWarning(error + " No objects moved.", grid);
                return;
            }
            RoomDefinition room = grid.GetComponentInParent<RoomDefinition>();
            Transform mover = room != null && room.transform != container.transform
                && room.transform.IsChildOf(container.transform) ? room.transform : grid.transform;
            if (displacements.TryGetValue(mover, out Vector3 existing)
                && (existing - displacement).sqrMagnitude > 0.000001f)
            {
                Debug.LogWarning(mover.name + ": internal grids require different offsets. No objects moved.", mover);
                return;
            }
            displacements[mover] = displacement;
        }
        List<Placement> placements = new List<Placement>();
        foreach (KeyValuePair<Transform, Vector3> entry in displacements)
            placements.Add(new Placement(entry.Key, entry.Key.position + entry.Value));
        // Apply absolute positions parent-first so nested grids do not receive a double shift.
        placements.Sort((Placement a, Placement b) => Depth(a.Transform).CompareTo(Depth(b.Transform)));
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Align child grids");
        int moved = 0;
        foreach (Placement placement in placements)
        {
            if ((placement.Transform.position - placement.Position).sqrMagnitude <= 0.00000001f) continue;
            Undo.RecordObject(placement.Transform, "Align to grid");
            placement.Transform.position = placement.Position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(placement.Transform);
            moved++;
        }
        Undo.CollapseUndoOperations(group);
        Physics2D.SyncTransforms();
        Debug.Log("Alignment complete: " + moved + " rooms/grids moved.", container);
    }

    private static int Depth(Transform transform)
    {
        int depth = 0;
        for (Transform parent = transform.parent; parent != null; parent = parent.parent) depth++;
        return depth;
    }

    [MenuItem("Tools/Mouse Platformer/Rooms/Create Manual Grid Container")]
    private static void CreateContainer()
    {
        GameObject container = new GameObject("Manual Room Grid", typeof(Grid), typeof(ManualGridAlignment));
        Undo.RegisterCreatedObjectUndo(container, "Create manual grid container");
        Selection.activeGameObject = container;
    }
}
