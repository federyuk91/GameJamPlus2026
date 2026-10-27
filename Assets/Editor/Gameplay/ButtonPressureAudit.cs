using System.Collections.Generic;
using System.IO;
using System.Text;
using MousePlatformer;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ButtonPressureAudit
{
    private static double nextUpdate;

    static ButtonPressureAudit() => EditorApplication.update += Update;

    private static void Update()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextUpdate) return;
        nextUpdate = EditorApplication.timeSinceStartup + 1d;
        Capture();
    }

    [MenuItem("Tools/Gameplay/Audit Button Pressure")]
    public static void Capture()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        SimulatedBody2D[] bodies = Object.FindObjectsByType<SimulatedBody2D>(FindObjectsSortMode.None);
        ButtonPressurePlate[] plates = Object.FindObjectsByType<ButtonPressurePlate>(FindObjectsSortMode.None);
        StringBuilder report = new StringBuilder();
        report.AppendLine($"UTC: {System.DateTime.UtcNow:O}; Playing: {EditorApplication.isPlaying}");
        report.AppendLine($"Scene bodies: {bodies.Length}; Registered bodies: {SimulatedBody2D.ActiveBodies.Count}");
        Physics2D.SyncTransforms();
        foreach (SimulatedBody2D body in bodies)
        {
            if (!body.TryGetComponent(out PlayerMovement movement)) continue;
            Collider2D shape = body.Shape;
            if (shape == null) continue;
            bool registered = false;
            foreach (SimulatedBody2D active in SimulatedBody2D.ActiveBodies) registered |= active == body;
            float threshold = body.Settings != null ? body.Settings.buttonWeightActivation : PhysicsSettings.DefaultButtonWeightActivation;
            report.AppendLine($"Player {body.name}: scene={body.gameObject.scene.path}, weight={body.Weight}, threshold={threshold}, CanPress={body.CanPressButtons}, skin={body.Skin}, registered={registered}, enabled={body.isActiveAndEnabled}, shapeEnabled={shape.enabled}");
            if (body.TryGetComponent(out PlayerLedgeClimb climb))
            {
                SerializedObject climbData = new SerializedObject(climb);
                report.AppendLine($"Climb: state={climb.State}, position={body.transform.position}, bounds={shape.bounds}, lastAttempt={climbData.FindProperty("lastAttempt").stringValue}, blocker={climbData.FindProperty("blockingCollider").objectReferenceValue}");
            }
            foreach (ButtonPressurePlate plate in plates)
            {
                Collider2D surface = plate.SurfaceCollider;
                if (surface == null || Vector2.Distance(shape.bounds.center, surface.bounds.center) > 2f) continue;
                SerializedObject serialized = new SerializedObject(plate);
                float tolerance = serialized.FindProperty("contactTolerance").floatValue;
                float minimumNormal = Mathf.Max(0.01f, Mathf.Cos(body.MaxSlopeAngle * Mathf.Deg2Rad));
                float reach = (body.Skin + tolerance) / minimumNormal;
                Collider2D support = body.Probe(Vector2.down, reach, out float distance, out Vector2 normal, out Vector2 point);
                ColliderDistance2D contact = surface.Distance(shape);
                plate.TryGetComponent(out InteractableButton button);
                report.AppendLine($"Button {plate.name}: active={plate.isActiveAndEnabled}, locked={button != null && button.IsLocked}, HasWeight={plate.HasWeight}, surfaceEnabled={surface.enabled}, trigger={surface.isTrigger}, separation={contact.distance}, pointA={contact.pointA}, bodyBounds={shape.bounds}, surfaceBounds={surface.bounds}");
                report.AppendLine($"  Probe: support={(support != null ? support.name : "none")}, matchesButton={support == surface}, distance={distance}, normal={normal}, point={point}, effectiveGap={distance * normal.y - body.Skin}, tolerance={tolerance}");
            }
        }
        Directory.CreateDirectory("Library/CodexGameplayCheck");
        File.WriteAllText("Library/CodexGameplayCheck/ButtonPressureAudit.txt", report.ToString());
    }
}
