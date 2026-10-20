using MousePlatformer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhysicsSettings)), CanEditMultipleObjects]
public sealed class PhysicsSettingsEditor : Editor
{
    [MenuItem("Tools/Mouse Platformer/Physics Settings")]
    private static void SelectSettings()
    {
        PhysicsSettings settings = AssetDatabase.LoadAssetAtPath<PhysicsSettings>("Assets/Settings/PhysicsSettings.asset");
        if (settings == null)
        {
            Debug.LogError("PhysicsSettings.asset not found in Assets/Settings.");
            return;
        }
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.LabelField("1 unit = 32 pixels | Speed: units/s | Acceleration: units/s²", EditorStyles.wordWrappedMiniLabel);

        Group("Player");
        Section("Shared Movement Settings", "PlayerMovement, PlayerLedgeClimb");
        Field("player.maxStepHeight", "Automatically steps over small ground height differences. 0 disables stepping; 0.15 units equals 4.8 pixels.");
        Field("player.coyoteTime");
        Field("player.jumpBuffer");
        Field("player.launchControlTime");
        Field("player.launchControlMultiplier");

        Section("Footsteps", "PlayerFootsteps");
        Field("player.runFootstepsPerSecond", "0 disables running footsteps.");
        Field("player.walkFootstepsPerSecond", "Reserved for future walking support. 0 disables footsteps.");

        Section("Ledge Climb", "PlayerLedgeClimb");
        Field("ledgeClimb.enabled");
        Field("ledgeClimb.maxHeight", "Maximum height difference between the feet and the ledge. While grounded, directional input climbs obstacles above standard step height.");
        Field("ledgeClimb.hangHeightFraction", "Fraction of collider height measured from the feet: 0.85 places the grip near the top of the body.");
        Field("ledgeClimb.retryDelay");

        Section("Ladder", "PlayerLadderInteraction");
        Field("ladder.jumpHorizontalSpeed");
        Field("ladder.jumpUpSpeed");
        Field("ladder.regrabDelay");

        Section("Rope Movement", "PlayerRopeInteraction");
        Field("interactions.ropeJumpUpSpeed");
        Field("interactions.ropeJumpSwingBoost");
        Field("interactions.ropeClimbSpeed");
        Field("interactions.ropeSwingAcceleration");
        Field("interactions.ropeRegrabDelay");

        Section("Push / Pull", "PlayerCrateInteraction → Pushable, PropsMotion");
        Field("interactions.crateMoveSpeed");

        Section("Water — Horizontal Movement", "PlayerSwimming, PlayerMovement (bottom walking)");
        Field("water.bottomWalkSpeedMultiplier", "Relative to normal player speed: 0.5 = half speed. Used while submerged and standing on a walkable surface.");
        Field("water.acceleration", "How quickly the requested speed is reached.");
        Field("water.strokeAcceleration", "Lateral response after a swim stroke, including just outside the water.");
        Field("water.strokeControlTime");
        Field("water.braking", "How quickly movement stops after releasing directional input.");

        Section("Water — Vertical Movement", "PlayerSwimming");
        Field("water.gravityMultiplier");
        Field("water.maxSinkSpeed");
        Field("water.strokeSpeed");

        Section("Digging — Shared Bite Input", "PlayerDigging → DiggableTerrain");
        Field("digging.enabled");
        Field("digging.radius", "The effective radius cannot be smaller than the radius needed to contain the mouse collider.");
        Field("digging.reach");
        EditorGUILayout.HelpBox("Local settings: movement and hand reach → PlayerMovement; speed multipliers → PlayerSwimming / PlayerDigging; bite reach → PlayerRopeBite.", MessageType.None);

        Group("Environment & Props");
        Section("Immersed Weight", "PropsMotion → Pushable, PropsBuoyancy");
        Field("fullSubmersionWeightMultiplier");

        Section("Excavated Terrain Appearance", "DiggableTerrain");
        Field("digging.excavatedAlpha", "0 = transparent; 254 = almost opaque. Does not change collisions: the tunnel stays open.");
        Field("digging.excavatedTint", "The color alpha channel is ignored; adjust Excavated Alpha instead.");

        Group("Physics");
        Section("World Gravity", "SimulatedBody2D → PlayerMovement, PlayerLedgeClimb, PlayerSwimming, PropsMotion; RopeSimulation");
        Field("gravityAcceleration");

        Section("Props Simulation", "PropsMotion (individual bodies and coordinated groups)");
        Field("simulationSubsteps");

        Section("Rope Simulation", "RopeSimulation");
        Field("ropeSimulationSubsteps");
        Field("ropeConstraintIterations");
        EditorGUILayout.HelpBox("Per body: Gravity Scale, Max Speed, Max Slope Angle, Ground Friction and drag. PlayerSwimming also uses the body's Water Drag.", MessageType.None);
        serializedObject.ApplyModifiedProperties();
    }

    private static void Group(string label)
    {
        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private static void Section(string label, string components)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Components: " + components, EditorStyles.wordWrappedMiniLabel);
    }

    private void Field(string path, string tooltip = "")
    {
        SerializedProperty property = serializedObject.FindProperty(path);
        if (property == null)
        {
            EditorGUILayout.HelpBox($"Missing parameter: {path}", MessageType.Error);
            return;
        }
        string description = string.IsNullOrEmpty(tooltip) ? property.tooltip : tooltip;
        string help = string.IsNullOrEmpty(description) ? path : path + "\n" + description;
        EditorGUILayout.PropertyField(property, new GUIContent(ObjectNames.NicifyVariableName(property.name), help));
    }
}
