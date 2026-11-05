using System;
using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(WaterBasin))]
public sealed class WaterBasinEditor : Editor
{
    private readonly List<BoxBoundsHandle> sectionHandles = new List<BoxBoundsHandle>();
    private bool editSections;
    private bool previousToolsHidden;

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        SetEditSections(false);
        WaterSimulationPreview.Stop(target);
    }

    private void OnSelectionChanged()
    {
        if (target == null || !Selection.Contains(((WaterBasin)target).gameObject)) SetEditSections(false);
    }

    private void OnPlayModeChanged(PlayModeStateChange state) => SetEditSections(false);

    private void SetEditSections(bool enabled)
    {
        if (editSections == enabled) return;
        if (enabled)
        {
            previousToolsHidden = Tools.hidden;
            Tools.hidden = true;
            WaterSimulationPreview.Stop(target);
        }
        else Tools.hidden = previousToolsHidden;
        editSections = enabled;
        SceneView.RepaintAll();
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        WaterBasin basin = (WaterBasin)target;
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        DrawPropertiesExcluding(serializedObject, "showWaterPreview", "waterPreview");
        SerializedProperty initial = serializedObject.FindProperty("initialSurfaceHeight");
        Rect bounds = basin.GeometryBounds;
        float depth = Mathf.Clamp(basin.transform.position.y + initial.floatValue - bounds.yMin, 0f, bounds.height);
        EditorGUI.BeginChangeCheck();
        float nextDepth = EditorGUILayout.Slider(new GUIContent("Initial Water Depth", "Height above the basin floor in world units. 0 = empty; maximum = full."), depth, 0f, bounds.height);
        if (EditorGUI.EndChangeCheck()) initial.floatValue = bounds.yMin + nextDepth - basin.transform.position.y;
        EditorGUILayout.LabelField("Maximum Water Depth", bounds.height.ToString("0.###") + " units");
        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        if (!Application.isPlaying && changed)
        {
            WaterGeometryBakeTools.PreviewBasin((WaterBasin)target);
            WaterSimulationPreview.Stop(target);
        }
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode
            || !Selection.Contains(basin.gameObject) || serializedObject.FindProperty("sections").arraySize == 0))
            if (GUILayout.Button(editSections ? "Finish Editing Sections" : "Edit Sections")) SetEditSections(!editSections);
        if (editSections)
            EditorGUILayout.HelpBox("Drag section edges to resize, or the center square to move. Changes support Undo and update water previews. Deselecting the basin ends editing. A new bake replaces manual section geometry; links are not recalculated here.", MessageType.Info);
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showWaterPreview"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("waterPreview"));
        if (serializedObject.ApplyModifiedProperties()) SceneView.RepaintAll();
        WaterSimulationPreview.DrawInspector(target, new WaterBasin[] { (WaterBasin)target });
        if (GUILayout.Button(Application.isPlaying ? "Reset Water" : "Apply Water Preview"))
        {
            if (Application.isPlaying) WaterGeometryBakeTools.PreviewBasin(basin);
            else ApplyWaterPreview(basin);
        }
    }

    private void ApplyWaterPreview(WaterBasin basin)
    {
        serializedObject.Update();
        Rect bounds = basin.GeometryBounds;
        float surface = Mathf.Lerp(bounds.yMin, bounds.yMax, serializedObject.FindProperty("waterPreview").floatValue / 100f);
        if (WaterSimulationPreview.TryGetSurface(basin, out float simulatedSurface)) surface = simulatedSurface;
        WaterSimulationPreview.Stop(target);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply water preview as initial level");
        float depth = Mathf.Clamp(surface - bounds.yMin, 0f, bounds.height);
        serializedObject.FindProperty("initialSurfaceHeight").floatValue = bounds.yMin + depth - basin.transform.position.y;
        serializedObject.FindProperty("waterPreview").floatValue = bounds.height > 0f ? depth / bounds.height * 100f : 0f;
        serializedObject.ApplyModifiedProperties();
        WaterGeometryBakeTools.PreviewBasin(basin);
        PrefabUtility.RecordPrefabInstancePropertyModifications(basin);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(basin.gameObject.scene);
        Undo.CollapseUndoOperations(group);
    }

    private void OnSceneGUI()
    {
        if (Application.isPlaying) return;
        WaterBasin basin = (WaterBasin)target;
        if (editSections)
        {
            DrawSectionHandles(basin);
            return;
        }
        serializedObject.Update();
        SerializedProperty initial = serializedObject.FindProperty("initialSurfaceHeight");
        Rect bounds = basin.GeometryBounds;
        Vector3 point = new Vector3(bounds.center.x, bounds.yMin + basin.InitialWaterDepth, basin.transform.position.z);
        Handles.color = Color.cyan;
        Handles.Label(point + Vector3.right * 0.15f, "Initial water depth: " + basin.InitialWaterDepth.ToString("0.###") + " / " + bounds.height.ToString("0.###"));
        EditorGUI.BeginChangeCheck();
        Vector3 next = Handles.Slider(point, Vector3.up, HandleUtility.GetHandleSize(point) * 0.12f,
            Handles.ConeHandleCap, 1f / 32f);
        if (!EditorGUI.EndChangeCheck()) return;
        initial.floatValue = Mathf.Clamp(next.y, bounds.yMin, bounds.yMax) - basin.transform.position.y;
        serializedObject.ApplyModifiedProperties();
        WaterSimulationPreview.Stop(target);
        WaterGeometryBakeTools.PreviewBasin(basin);
    }

    private void DrawSectionHandles(WaterBasin basin)
    {
        serializedObject.Update();
        SerializedProperty sections = serializedObject.FindProperty("sections");
        while (sectionHandles.Count < sections.arraySize)
            sectionHandles.Add(new BoxBoundsHandle { axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y });
        using (new Handles.DrawingScope(Matrix4x4.Translate(basin.transform.position)))
        {
            for (int index = 0; index < sections.arraySize; index++)
            {
                SerializedProperty boundsProperty = sections.GetArrayElementAtIndex(index).FindPropertyRelative("bounds");
                Rect bounds = boundsProperty.rectValue;
                BoxBoundsHandle handle = sectionHandles[index];
                Color color = Color.HSVToRGB(Mathf.Repeat(index * 0.618034f, 0.8f), 0.55f, 1f);
                handle.handleColor = color;
                handle.wireframeColor = color;
                handle.center = new Vector3(bounds.center.x, bounds.center.y, 0f);
                handle.size = new Vector3(bounds.width, bounds.height, 0f);
                EditorGUI.BeginChangeCheck();
                handle.DrawHandle();
                Vector3 center = handle.center;
                Handles.color = color;
                float size = HandleUtility.GetHandleSize(center) * 0.045f;
                center = Handles.FreeMoveHandle(center, size, Vector3.zero, Handles.RectangleHandleCap);
                Handles.Label(new Vector3(bounds.xMin, bounds.yMax, 0f), "Section " + index);
                if (!EditorGUI.EndChangeCheck()) continue;
                Vector2 dimensions = new Vector2(Mathf.Max(0.01f, handle.size.x), Mathf.Max(0.01f, handle.size.y));
                boundsProperty.rectValue = new Rect((Vector2)center - dimensions * 0.5f, dimensions);
                serializedObject.ApplyModifiedProperties();
                WaterSimulationPreview.Stop(target);
                WaterGeometryBakeTools.PreviewBasin(basin);
                PrefabUtility.RecordPrefabInstancePropertyModifications(basin);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(basin.gameObject.scene);
                serializedObject.Update();
            }
        }
    }

}


// Keeps all preview state in the editor; no scene volumes, colliders or initial levels are mutated.
internal sealed class WaterSimulationPreview
{
    private sealed class Link
    {
        public WaterBasin first;
        public WaterBasin second;
        public float sill;
        public float rate;
        public bool open;
        public string name;
        public string closedReason;
    }

    private static WaterSimulationPreview active;
    private readonly UnityEngine.Object owner;
    private readonly Dictionary<WaterBasin, WaterBasinModel> models = new Dictionary<WaterBasin, WaterBasinModel>();
    private readonly Dictionary<WaterBasin, double> initialVolumes = new Dictionary<WaterBasin, double>();
    private readonly List<Link> links = new List<Link>();
    private readonly Dictionary<Renderer, bool> hiddenRenderers = new Dictionary<Renderer, bool>();
    private float requestedTime;
    private float simulatedTime;
    private bool disposed;

    private WaterSimulationPreview(UnityEngine.Object owner, IReadOnlyList<WaterBasin> seeds)
    {
        this.owner = owner;
        Component component = (Component)owner;
        List<Link> candidates = new List<Link>();
        foreach (GameObject root in component.gameObject.scene.GetRootGameObjects())
            foreach (WaterConnection connection in root.GetComponentsInChildren<WaterConnection>())
            {
                if (!connection.isActiveAndEnabled) continue;
                SerializedObject data = new SerializedObject(connection);
                WaterBasin first = data.FindProperty("first").objectReferenceValue as WaterBasin;
                WaterBasin second = data.FindProperty("second").objectReferenceValue as WaterBasin;
                if (first == null || second == null || first == second || !first.isActiveAndEnabled || !second.isActiveAndEnabled) continue;
                Door door = data.FindProperty("blockingDoor").objectReferenceValue as Door;
                bool doorOpen = door == null || new SerializedObject(door).FindProperty("startsOpen").boolValue;
                PipeValve valve = data.FindProperty("blockingValve").objectReferenceValue as PipeValve;
                bool valveOpen = valve == null || valve.InitiallyOpen;
                bool linkOpen = valve != null && valve.Connection == connection ? valveOpen : data.FindProperty("startsOpen").boolValue;
                candidates.Add(new Link { first = first, second = second, sill = connection.SillHeight,
                    rate = data.FindProperty("flowRate").floatValue, open = linkOpen && doorOpen && valveOpen,
                    name = connection.name, closedReason = !doorOpen ? "Door starts closed" : !valveOpen ? "Pipe valve starts closed" : "Link starts closed" });
            }
        HashSet<WaterBasin> connected = new HashSet<WaterBasin>();
        Queue<WaterBasin> queue = new Queue<WaterBasin>();
        foreach (WaterBasin basin in seeds)
            if (basin != null && basin.isActiveAndEnabled && connected.Add(basin)) queue.Enqueue(basin);
        while (queue.Count > 0)
        {
            WaterBasin basin = queue.Dequeue();
            foreach (Link link in candidates)
            {
                WaterBasin next = link.first == basin ? link.second : link.second == basin ? link.first : null;
                if (next != null && connected.Add(next)) queue.Enqueue(next);
            }
        }
        foreach (WaterBasin basin in connected)
        {
            Rect bounds = basin.GeometryBounds;
            if (bounds.width <= 0f || bounds.height <= 0f) continue;
            WaterBasinModel model = basin.CreatePreviewModel();
            models.Add(basin, model);
            initialVolumes.Add(basin, model.Volume);
            basin.IsSimulationPreviewActive = true;
            foreach (WaterVolume water in basin.GetComponentsInChildren<WaterVolume>(true))
                foreach (Renderer visual in water.GetComponents<Renderer>())
                {
                    if (hiddenRenderers.ContainsKey(visual)) continue;
                    hiddenRenderers.Add(visual, visual.forceRenderingOff);
                    visual.forceRenderingOff = true;
                }
        }
        foreach (Link link in candidates)
            if (models.ContainsKey(link.first) && models.ContainsKey(link.second)) links.Add(link);
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Undo.undoRedoPerformed += Dispose;
    }

    public static void DrawInspector(UnityEngine.Object owner, IReadOnlyList<WaterBasin> seeds)
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            bool enabled = active != null && active.owner == owner;
            bool next = EditorGUILayout.Toggle("Show Simulation Preview", enabled);
            if (next != enabled)
            {
                active?.Dispose();
                if (next) active = new WaterSimulationPreview(owner, seeds);
                SceneView.RepaintAll();
            }
            if (!next) return;
            float time = EditorGUILayout.FloatField(new GUIContent("Simulation Time (s)", "Elapsed time from initial levels, without a 100 second limit."), active.requestedTime);
            time = float.IsNaN(time) || float.IsInfinity(time) ? active.requestedTime : Mathf.Max(0f, time);
            if (time < active.simulatedTime)
            {
                foreach (KeyValuePair<WaterBasin, WaterBasinModel> pair in active.models) pair.Value.SetVolume(active.initialVolumes[pair.Key]);
                active.simulatedTime = 0f;
            }
            active.requestedTime = time;
            EditorGUILayout.LabelField("Preview calculated to", active.simulatedTime.ToString("0.00") + " s");
            int openCount = active.links.FindAll(link => link.open && link.rate > 0f).Count;
            EditorGUILayout.LabelField("Connected basins / open links", active.models.Count + " / " + openCount + " (" + active.links.Count + " links total)");
            foreach (Link link in active.links)
                EditorGUILayout.LabelField(link.name, active.LinkStatus(link));
            if (active.links.Count == 0) EditorGUILayout.HelpBox("No valid active links connect these basins. Water cannot move to another basin.", MessageType.Warning);
            foreach (KeyValuePair<WaterBasin, WaterBasinModel> pair in active.models)
                EditorGUILayout.LabelField(pair.Key.name + " volume", active.initialVolumes[pair.Key].ToString("0.###") + " → " + pair.Value.Volume.ToString("0.###"));
            EditorGUILayout.HelpBox("Starts from Initial Water Depth, not the geometric Water Preview slider. Geometric overlays and initial water sprites are temporarily hidden on simulated basins. Link/door initial states are respected. Re-enable after editing links or initial settings.", MessageType.Info);
        }
    }

    public static void Stop(UnityEngine.Object owner)
    {
        if (active != null && active.owner == owner) active.Dispose();
    }

    public static bool TryGetSurface(WaterBasin basin, out float surface)
    {
        surface = 0f;
        if (active == null || !active.models.TryGetValue(basin, out WaterBasinModel model)) return false;
        surface = model.Surface;
        return true;
    }

    private void OnPlayModeChanged(PlayModeStateChange state) => Dispose();

    private string LinkStatus(Link link)
    {
        if (!link.open) return link.closedReason;
        if (link.rate <= 0f) return "Flow rate is zero";
        WaterBasinModel first = models[link.first];
        WaterBasinModel second = models[link.second];
        WaterBasinModel source = first.Surface >= second.Surface ? first : second;
        WaterBasinModel destination = source == first ? second : first;
        if (source.Volume <= 0d) return "No water";
        if (source.Surface <= link.sill + 0.0001f) return "Water below passage sill";
        if (destination.Capacity - destination.Volume <= 0.000001d) return "Receiving basin full";
        if (source.Surface - destination.Surface <= 0.0001f) return "Levels balanced";
        return "Flowing";
    }

    private void Dispose()
    {
        if (disposed) return;
        disposed = true;
        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Undo.undoRedoPerformed -= Dispose;
        foreach (WaterBasin basin in models.Keys)
            if (basin != null) basin.IsSimulationPreviewActive = false;
        foreach (KeyValuePair<Renderer, bool> pair in hiddenRenderers)
            if (pair.Key != null) pair.Key.forceRenderingOff = pair.Value;
        if (active == this) active = null;
        SceneView.RepaintAll();
    }

    private void Update()
    {
        if (owner == null || EditorApplication.isPlayingOrWillChangePlaymode) { Dispose(); return; }
        if (simulatedTime >= requestedTime) return;
        double deadline = EditorApplication.timeSinceStartup + 0.008d;
        do
        {
            float step = Mathf.Min(Mathf.Max(0.001f, Time.fixedDeltaTime), requestedTime - simulatedTime);
            foreach (Link link in links)
            {
                if (!link.open || link.rate <= 0f) continue;
                WaterBasinModel first = models[link.first];
                WaterBasinModel second = models[link.second];
                WaterBasinModel source = first.Surface >= second.Surface ? first : second;
                WaterBasinModel destination = source == first ? second : first;
                double transfer = WaterBasinModel.CalculateTransfer(source, destination, link.sill, link.rate, step);
                source.SetVolume(source.Volume - transfer);
                destination.SetVolume(destination.Volume + transfer);
            }
            simulatedTime = Mathf.Min(requestedTime, simulatedTime + step);
        } while (simulatedTime < requestedTime && EditorApplication.timeSinceStartup < deadline);
        SceneView.RepaintAll();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawBasin(WaterBasin basin, GizmoType gizmoType)
    {
        if (active == null || !basin.isActiveAndEnabled || !active.models.TryGetValue(basin, out WaterBasinModel model)) return;
        basin.DrawVolumeGizmos(new Color(0.1f, 0.85f, 1f, 0.65f), model.Surface);
    }
}
