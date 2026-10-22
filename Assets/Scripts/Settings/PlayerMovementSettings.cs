using System;
using UnityEngine;

namespace MousePlatformer
{
    [Serializable]
    public sealed class PlayerMovementSettings
    {
        public const float DefaultHandGrabReach = 0.6f;
        public const float DefaultHorizontalHandInteraction = 0.15f;
        [Tooltip("Horizontal movement speed for this animal in world units/s. Also used when jumping away from a ledge; underwater walking applies its existing multiplier.")]
        [Min(0f)] public float moveSpeed = 5f;
        [Tooltip("Multiplies this animal's Move Speed for both ladder movement and ledge climbing. 0.6 uses 60% of normal movement speed; 0 stops climbing. The body's Max Speed still limits motion.")]
        [Min(0f)] public float climbSpeedMultiplier = 0.6f;
        public float ClimbSpeed => Mathf.Max(0f, moveSpeed) * Mathf.Max(0f, climbSpeedMultiplier);
        [Tooltip("How quickly this animal reaches the requested ground speed, in world units/s². Also controls braking against downhill sliding.")]
        [Min(0f)] public float groundAcceleration = 45f;
        [Tooltip("How quickly this animal changes horizontal speed in the air, in world units/s². Launch Control Multiplier still modifies control after a rope, ladder or ledge release.")]
        [Min(0f)] public float airAcceleration = 28f;
        [Tooltip("Height of this animal's full held jump in world units. Launch velocity is calculated using the current player gravity; also used for ledge jumps.")]
        [Min(0f)] public float jumpHeight = 2f;
        [Tooltip("Multiplier applied once to upward velocity when Jump is released early. 0 cuts the ascent immediately; 1 leaves the upward velocity unchanged.")]
        [Range(0f, 1f)] public float jumpReleaseMultiplier = 0.45f;
        [Tooltip("Multiplier of player gravity while this animal's vertical velocity is zero or downward, outside swimming and ground-following movement. 1 keeps the normal gravity; larger values accelerate falling. Does not change upward jump gravity or the body's Max Speed.")]
        [Min(1f)] public float fallGravityMultiplier = 2.2f;
        [Tooltip("Maximum effective stack weight in kilograms this animal can push or pull, including by swimming into submerged props. A load equal to or above this value is blocked; 0 disables pushing.")]
        [Min(0f)] public float maximumPushWeight = 5f;
        [Tooltip("Shared rope and ladder grip offset from this animal's transform, in world axes and world units. Preserves the existing grip calculation for both animations.")]
        public Vector2 handOffset = new Vector2(0f, 0.2f);
        [Tooltip("Maximum world-space distance from Hand Offset to a rope or ladder axis when attaching. Shared by both interactions for this animal; ladder attachment also requires overlap with its trigger.")]
        [Min(0f)] public float handGrabReach = DefaultHandGrabReach;
        [Tooltip("Shared horizontal interaction reach in world units beyond the animal collider's side, used for pushing/pulling props and reaching a ledge. Vertical ledge reach and rope/ladder hand grab reach are configured separately.")]
        [Min(0f)] public float horizontalHandInteraction = DefaultHorizontalHandInteraction;
        [Tooltip("Maximum vertical distance in world units between this animal's ledge grip height and the edge while grabbing in the air. Grip height is determined by Hang Height Fraction; this does not change the maximum climbable obstacle height.")]
        [Min(0f)] public float verticalHandInteraction = 0.15f;
    }

    [Serializable]
    public sealed class PlayerInteractionSettings
    {
        [Tooltip("Upward velocity added when jumping from a rope with horizontal directional input, in world units/s. A neutral release does not receive this boost.")]
        [Min(0f)] public float ropeJumpUpSpeed = 7f;
        [Tooltip("Extra horizontal speed when jumping from a rope with horizontal input, in world units/s. Input chooses the launch side. A neutral release does not receive this boost.")]
        [Min(0f)] public float ropeJumpSwingBoost = 4f;
        [Tooltip("Fraction of current rope velocity retained when pressing Jump without horizontal input. 0 drops vertically from rest; 1 retains full momentum. No upward or directional boost is added.")]
        [Range(0f, 1f)] public float ropeReleaseMomentumMultiplier = 0.35f;
        [Min(0.01f)] public float crateMoveSpeed = 2.5f;
        [Min(0f)] public float ropeClimbSpeed = 2f;
        [Tooltip("Maximum horizontal pumping acceleration on a rope. Suspended ropes accept input with the swing and reduce outward drive as momentum fades, allowing gravity to reverse the pendulum instead of holding it on one side.")]
        [Min(0f)] public float ropeSwingAcceleration = 70f;
        [Min(0f)] public float ropeRegrabDelay = 0.3f;
    }
}
