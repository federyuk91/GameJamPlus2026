using UnityEngine;

namespace MousePlatformer
{
    [CreateAssetMenu(fileName = "PhysicsSettings", menuName = "Mouse Platformer/Physics Settings")]
    public sealed class PhysicsSettings : ScriptableObject
    {
        [Tooltip("Positive world gravity acceleration in world units/s². Bodies and ropes apply their own Gravity Scale; swimmer response applies its separate multiplier.")]
        [Min(0.001f)] public float gravityAcceleration = DefaultGravityAcceleration;
        public static float DefaultGravityAcceleration => 30f;
        [Tooltip("Shared maximum total speed in world units/s for all SimulatedBody2D components, unless a body explicitly enables its local override.")]
        [Min(0.01f)] public float maxSpeed = DefaultMaxSpeed;
        public static float DefaultMaxSpeed => 26f;
        [Tooltip("A body can activate pressure buttons only when its dry weight is strictly greater than this threshold.")]
        [Min(0f)] public float buttonWeightActivation = DefaultButtonWeightActivation;
        public static float DefaultButtonWeightActivation => 0.5f;
        [Header("Shared simulation rules")]
        [Tooltip("Substeps per fixed tick for props and their coordinated vertical groups. Higher values improve precision at a higher CPU cost.")]
        [Min(1)] public int simulationSubsteps = DefaultSimulationSubsteps;
        [Tooltip("Substeps per fixed tick for rope simulation, including the velocity history used when grabbing or releasing a rope.")]
        [Min(1)] public int ropeSimulationSubsteps = DefaultRopeSimulationSubsteps;
        [Tooltip("Distance constraint passes per rope substep. Higher values reduce stretching at a higher CPU cost.")]
        [Min(1)] public int ropeConstraintIterations = DefaultRopeConstraintIterations;
        [Tooltip("Fraction of dry prop weight retained at full immersion. Interpolated progressively with immersion; affects pushing and gravitational load, not inertial mass. 0.5 retains half the weight.")]
        [Range(0f, 1f)] public float fullSubmersionWeightMultiplier = DefaultFullSubmersionWeightMultiplier;

        public static int DefaultSimulationSubsteps => 4;
        public static int DefaultRopeSimulationSubsteps => 4;
        public static int DefaultRopeConstraintIterations => 24;
        public static float DefaultFullSubmersionWeightMultiplier => 0.5f;

        [Header("Player movement")]
        public SharedPlayerMovementSettings player = new SharedPlayerMovementSettings();
        [Header("Player interactions — crates and ropes")]
        public PlayerInteractionSettings interactions = new PlayerInteractionSettings();
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
        public static SwimMotion DefaultWater { get; } = new SwimMotion();
        public static LadderSettings DefaultLadder { get; } = new LadderSettings();
        public static SharedPlayerMovementSettings DefaultPlayer { get; } = new SharedPlayerMovementSettings();
        public static PlayerInteractionSettings DefaultInteractions { get; } = new PlayerInteractionSettings();
        public static LedgeClimbSettings DefaultLedgeClimb { get; } = new LedgeClimbSettings();
    }

}
