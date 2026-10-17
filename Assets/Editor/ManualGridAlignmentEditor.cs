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
        EditorGUILayout.HelpBox("Posiziona stanze o Grid sotto questo contenitore, poi allineale. Le stanze vengono spostate intere; i tile non vengono modificati.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || EditorUtility.IsPersistent(target)))
            if (GUILayout.Button("Allinea griglie figlie")) Align((ManualGridAlignment)target);
    }

    private static void Align(ManualGridAlignment container)
    {
        Dictionary<Transform, Vector3> displacements = new Dictionary<Transform, Vector3>();
        foreach (Grid grid in container.GetComponentsInChildren<Grid>(true))
        {
            if (grid == container.ReferenceGrid || grid.transform == container.transform) continue;
            if (!container.TryGetDisplacement(grid, out Vector3 displacement, out string error))
            {
                Debug.LogWarning(error + " Nessun oggetto spostato.", grid);
                return;
            }
            RoomDefinition room = grid.GetComponentInParent<RoomDefinition>();
            Transform mover = room != null && room.transform != container.transform
                && room.transform.IsChildOf(container.transform) ? room.transform : grid.transform;
            if (displacements.TryGetValue(mover, out Vector3 existing)
                && (existing - displacement).sqrMagnitude > 0.000001f)
            {
                Debug.LogWarning(mover.name + ": le griglie interne richiedono spostamenti diversi. Nessun oggetto spostato.", mover);
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
        Undo.SetCurrentGroupName("Allinea griglie figlie");
        int moved = 0;
        foreach (Placement placement in placements)
        {
            if ((placement.Transform.position - placement.Position).sqrMagnitude <= 0.00000001f) continue;
            Undo.RecordObject(placement.Transform, "Allinea alla griglia");
            placement.Transform.position = placement.Position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(placement.Transform);
            moved++;
        }
        Undo.CollapseUndoOperations(group);
        Physics2D.SyncTransforms();
        Debug.Log("Allineamento completato: " + moved + " stanze/griglie spostate.", container);
    }

    private static int Depth(Transform transform)
    {
        int depth = 0;
        for (Transform parent = transform.parent; parent != null; parent = parent.parent) depth++;
        return depth;
    }

    [MenuItem("Tools/Mouse Platformer/Stanze/Crea contenitore griglia manuale")]
    private static void CreateContainer()
    {
        GameObject container = new GameObject("Manual Room Grid", typeof(Grid), typeof(ManualGridAlignment));
        Undo.RegisterCreatedObjectUndo(container, "Crea contenitore griglia manuale");
        Selection.activeGameObject = container;
    }
}
