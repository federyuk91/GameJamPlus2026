using System;
using System.Collections.Generic;
using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaterBasin))]
public sealed class WaterBasinEditor : Editor
{
    private void OnDisable() => WaterSimulationPreview.Stop(target);

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        DrawPropertiesExcluding(serializedObject, "showWaterPreview", "waterPreview");
        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        if (!Application.isPlaying && changed)
        {
            WaterGeometryBakeTools.PreviewBasin((WaterBasin)target);
            WaterSimulationPreview.Stop(target);
        }
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showWaterPreview"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("waterPreview"));
        if (serializedObject.ApplyModifiedProperties()) SceneView.RepaintAll();
        WaterSimulationPreview.DrawInspector(target, new WaterBasin[] { (WaterBasin)target });
        if (GUILayout.Button(Application.isPlaying ? "Reset Water" : "Apply Initial Water Preview"))
            WaterGeometryBakeTools.PreviewBasin((WaterBasin)target);
    }

    private void OnSceneGUI()
    {
        if (Application.isPlaying) return;
        WaterBasin basin = (WaterBasin)target;
        serializedObject.Update();
        SerializedProperty initial = serializedObject.FindProperty("initialSurfaceHeight");
        Vector3 point = basin.transform.position + Vector3.up * initial.floatValue;
        Handles.color = Color.cyan;
        Handles.Label(point + Vector3.right * 0.15f, "Initial water surface");
        EditorGUI.BeginChangeCheck();
        Vector3 next = Handles.Slider(point, Vector3.up, HandleUtility.GetHandleSize(point) * 0.12f,
            Handles.ConeHandleCap, 1f / 32f);
        if (!EditorGUI.EndChangeCheck()) return;
        initial.floatValue = next.y - basin.transform.position.y;
        serializedObject.ApplyModifiedProperties();
        WaterGeometryBakeTools.PreviewBasin(basin);
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
            EditorGUILayout.HelpBox("Starts from Initial Surface Height, not the geometric Water Preview slider. Geometric overlays and initial water sprites are temporarily hidden on simulated basins. Link/door initial states are respected. Re-enable after editing links or initial settings.", MessageType.Info);
        }
    }

    public static void Stop(UnityEngine.Object owner)
    {
        if (active != null && active.owner == owner) active.Dispose();
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
