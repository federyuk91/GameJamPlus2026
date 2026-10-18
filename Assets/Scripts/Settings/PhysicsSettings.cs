using System;
using UnityEngine;

namespace MousePlatformer
{
    [CreateAssetMenu(fileName = "PhysicsSettings", menuName = "Mouse Platformer/Physics Settings")]
    public sealed class PhysicsSettings : ScriptableObject
    {
        [Header("Player movement")]
        public PlayerMovementSettings player = new PlayerMovementSettings();
        [Header("Player interactions — crates and ropes")]
        public PlayerInteractionSettings interactions = new PlayerInteractionSettings();
        [Header("Gravity and jumping — 1 unit = 32 pixels")]
        public GravitySettings gravity = new GravitySettings();
        [Header("Water / swimming")]
        public SwimMotion water = new SwimMotion();
        [Header("Ladder climbing and jumping")]
        public LadderSettings ladder = new LadderSettings();
        [Header("Automatic ledge climb")]
        public LedgeClimbSettings ledgeClimb = new LedgeClimbSettings();

        [Header("Digging")]
        public DiggingSettings digging = new DiggingSettings();
        public static DiggingSettings DefaultDigging { get; } = new DiggingSettings();

        // Components created at runtime can work before an asset is assigned.
        public static GravitySettings DefaultGravity { get; } = new GravitySettings();
        public static SwimMotion DefaultWater { get; } = new SwimMotion();
        public static LadderSettings DefaultLadder { get; } = new LadderSettings();
        public static PlayerMovementSettings DefaultPlayer { get; } = new PlayerMovementSettings();
        public static PlayerInteractionSettings DefaultInteractions { get; } = new PlayerInteractionSettings();
        public static LedgeClimbSettings DefaultLedgeClimb { get; } = new LedgeClimbSettings();
    }

    [Serializable]
    public sealed class GravitySettings
    {
        [Min(0.001f)] public float playerGravity = 30f;
        [Min(1f)] public float fallGravityMultiplier = 2.2f;
        [Min(0f)] public float playerTerminalSpeed = 26f;
        [Tooltip("Height above takeoff for a full held jump, without overhead obstacles.")]
        [Min(0f)] public float jumpHeight = 2f;
        [Range(0f, 1f)] public float jumpReleaseMultiplier = 0.45f;
        [Min(0f)] public float crateGravity = 66f;
        [Min(0f)] public float crateTerminalSpeed = 26f;
        [Min(0f)] public float ropeGravity = 30f;
        [Min(0f)] public float ropeJumpUpSpeed = 7f;
        [Min(0f)] public float ropeJumpSwingBoost = 4f;
    }
}
