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
        EditorGUILayout.HelpBox("1 unit = 32 pixels. Speeds in units/s; accelerations in units/s².", MessageType.Info);

        Section("Interaction Areas");
        Field("interactions.crateGrabReach", "Maximum lateral distance between the mouse collider and a crate for pushing or pulling. Shown as cyan bands beside the crate in gizmos.");
        Field("interactions.ropeGrabRadius", "Maximum hand-to-rope attachment distance. Shown as the cyan outline along the rope in gizmos.");
        EditorGUILayout.HelpBox("Bite: select Player → Player Rope Bite → Reach.\nCollectibles: select the object → Collectible → Pickup Area, then edit that collider's Size/Radius and Offset.\nShow Gizmos on each object only controls area visibility.", MessageType.Info);

        Section("Player — Movement");
        Field("player.speed");
        Field("player.groundAcceleration");
        Field("player.airAcceleration");
        Field("player.maxStepHeight", "Automatically steps over small ground height differences. 0 disables stepping; 0.15 units equals 4.8 pixels.");
        Field("player.coyoteTime");
        Field("player.jumpBuffer");
        Field("player.launchControlTime");
        Field("player.launchControlMultiplier");
        Section("Player — Footsteps");
        Field("player.runFootstepsPerSecond", "0 disables running footsteps.");
        Field("player.walkFootstepsPerSecond", "Reserved for future walking support. 0 disables footsteps.");
        Section("Player — Slopes and Sliding");
        Field("player.slopeSlideAngle", "The mouse remains stable below this threshold. The effective value cannot exceed the climb limit.");
        Field("player.maxSlopeAngle", "Climbing is blocked beyond this angle. Between the two thresholds, movement can counteract sliding.");
        Field("player.slideOnSteepSlopes");
        Field("player.slopeSlideAcceleration", "Applied along the surface in proportion to its slope.");
        Field("player.slopeSlideMaxSpeed");
        Section("Ledge Climb");
        Field("ledgeClimb.enabled");
        Field("ledgeClimb.maxHeight", "Maximum height difference between the feet and the ledge. While grounded, directional input climbs obstacles above standard step height.");
        Field("ledgeClimb.forwardReach");
        Field("ledgeClimb.hangHeightFraction", "Fraction of collider height measured from the feet: 0.85 places the grip near the top of the body.");
        Field("ledgeClimb.grabVerticalReach", "Maximum distance between the ledge and grip height during a jump.");
        Field("ledgeClimb.climbSpeed");
        Field("ledgeClimb.retryDelay");

        Section("Water — Horizontal Movement");
        Field("water.speed", "Maximum left/right speed in water.");
        Field("water.bottomWalkSpeedMultiplier", "Relative to normal player speed: 0.5 = half speed. Used while submerged and standing on a walkable surface.");
        Field("water.acceleration", "How quickly the requested speed is reached.");
        Field("water.strokeAcceleration", "Lateral response after a swim stroke, including just outside the water.");
        Field("water.strokeControlTime");
        Field("water.braking", "How quickly movement stops after releasing directional input.");
        Section("Water — Vertical Movement");
        Field("water.gravity");
        Field("water.maxSinkSpeed");
        Field("water.strokeSpeed");
        Field("water.verticalDrag");

        Section("Gravity — Player and Jump");
        Field("gravity.playerGravity");
        Field("gravity.fallGravityMultiplier");
        Field("gravity.playerTerminalSpeed");
        Field("gravity.jumpHeight", "Full jump height above the takeoff point. Releasing Jump early reduces the height.");
        Field("gravity.jumpReleaseMultiplier");
        Section("Crates and Ropes");
        Field("gravity.crateGravity");
        Field("gravity.crateTerminalSpeed");
        Field("gravity.ropeGravity");
        Field("gravity.ropeJumpUpSpeed");
        Field("gravity.ropeJumpSwingBoost");
        Field("interactions.crateMoveSpeed");
        Field("interactions.ropeClimbSpeed");
        Field("interactions.ropeSwingAcceleration");
        Field("interactions.ropeRegrabDelay");
        Section("Ladder");
        Field("ladder.climbSpeed");
        Field("ladder.jumpHorizontalSpeed");
        Field("ladder.jumpUpSpeed");
        Field("ladder.grabReach");
        Field("ladder.regrabDelay");
        Section("Digging — Shared Bite Input");
        Field("digging.enabled");
        Field("digging.speed");
        Field("digging.radius", "The effective radius cannot be smaller than the radius needed to contain the mouse collider.");
        Field("digging.reach");
        Field("digging.excavatedAlpha", "0 = transparent; 254 = almost opaque. Does not change collisions: the tunnel stays open.");
        Field("digging.excavatedTint", "The color alpha channel is ignored; adjust Excavated Alpha instead.");
        serializedObject.ApplyModifiedProperties();
    }

    private static void Section(string label)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private void Field(string path, string tooltip = "")
    {
        SerializedProperty property = serializedObject.FindProperty(path);
        if (property != null) EditorGUILayout.PropertyField(property, new GUIContent(ObjectNames.NicifyVariableName(property.name), string.IsNullOrEmpty(tooltip) ? path : path + "\n" + tooltip));
        else EditorGUILayout.HelpBox($"Missing parameter: {path}", MessageType.Error);
    }
}
