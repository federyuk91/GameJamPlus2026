# Crates, water and ropes

## LoopScene examples

Crate Water Rope Examples contains three cases using the existing Water volume:

| Object | Initial position | Behavior |
| --- | --- | --- |
| 01 Free Floating Crate | (15.5, 5.235) | Starts submerged and rises to surface equilibrium. |
| 02 Horizontal Swinging Crate | (-3.78, -0.871) | Suspended about three units left of the initial Player position; swings with collider/art kept horizontal. |
| 03 Seabed Tethered Crate | (17.5, -11.695) | Tethered to the existing basin floor; buoyancy tensions the rope upward, and cutting releases the crate to rise. |

Both example ropes are cuttable. Player references also include the original scene rope.

## Crate components

- CrateMotion integrates velocity, gravity, collisions and constraints, including while tethered. All LoopScene crates, including the original two, use it.
- CrateBuoyancy configures full-submersion lift and drag per crate. Submersion uses collider bounds and active water volumes. Lift must exceed gravity to float: initial values 100 and 66 yield roughly 66% equilibrium submersion.
- PushableCrate enables grabbing with E / gamepad West and exposes Weight as a push/pull speed divisor. It no longer handles falling.
- CrateRopeAttachment references a rope and local attachment point. Its distance constraint does not rotate the crate and allows slack. Hanging crates attach at the top; seabed-tethered crates at the bottom.

Attachment disables PushableCrate and cutting reenables it. Detachment preserves velocity. Push/pull weight is independent of buoyancy. Existing crates outside LoopScene need CrateMotion and physics settings, plus CrateBuoyancy to float. Adding PushableCrate to a new crate automatically adds movement.

## Mouse contact

Underwater contact pushes crates without E, including tethered crates; rope constraints and Weight still limit movement. Slow downward swimmer motion on a crate does not become a downward force that cancels flotation.

PlayerPlatformRide carries a standing mouse and transfers support velocity on jumping. A rising crate can lift a submerged mouse. Overhead obstacles limit carrying to prevent passage through ceilings; crushing damage is not implemented.

## Biting

Hold F / gamepad East to bite a rope in front of the mouth. PlayerRopeBite exposes Hold Duration (initially 0.6 seconds), Reach and Mouth Offset. Original Inspector Ropes lists were superseded by the enabled-rope registry; see RopeInteraction.md.

Releasing input or losing the target cancels biting. Repeated cuts require a new press. Solids obstruct biting; there is no dedicated bite animation yet. Disable RopeSimulation.Can Be Cut for steel cable; climbing availability is independent.

Cutting removes one link, releases the crate and keeps remaining segments visible. The anchored section stays attached and separated pieces fall. A disconnected grip originally detached the player; current reattachment behavior is detailed in RopeInteraction.md. Segments remain simulated points without solid colliders. Seabed ropes reverse climb coordinates so vertical input stays consistent in world space.

Check surface equilibrium, contact pushing before/after cuts, riding rising crates, jumping from hanging crates, wall/ceiling constraints, interrupted biting and uncuttable ropes. Submersion assumes horizontal water surfaces using volume bounds; rotated/irregular water requires different sampling.

## Editor rope length

Rope Simulation.Length is a piece count (minimum 1); editing it adds/removes pieces. Segment Length controls each piece's world length. Rope.prefab uses one-unit pieces, so Length 5 means five units. Crate prefabs retain 0.25-unit segments and original total lengths.

The anchor stays fixed. Pieces align without gaps in the initial direction, upward for seabed ropes. Rebuild and align pieces restores alignment after manual edits. Edit geometry outside Play Mode; simulation, climbing, bites and crate constraints then use the new count. Ropes do not use one large BoxCollider; interaction follows simulated segments.
