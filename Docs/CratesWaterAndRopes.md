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
- CrateBuoyancy configures full-submersion lift and drag per crate. Submersion uses collider bounds and active water volumes. Effective weight decreases continuously with immersion. With lift 100, crate gravity 66 and dry weight 1, a single crate balances near 49.6% submersion. Vertical Damping Ratio defaults to 1 (critical damping near the surface).
- PushableCrate enables grabbing with E / gamepad East and exposes individual Weight plus cumulative StackWeight. It no longer handles falling.
- CrateRopeAttachment references a rope and local attachment point. Its distance constraint does not rotate the crate and allows slack. Hanging crates attach at the top; seabed-tethered crates at the bottom.

Attachment disables PushableCrate and cutting reenables it. Detachment preserves velocity. Effective stack weight affects player pushing and flotation load; buoyancy strength remains a separate setting. Existing crates outside LoopScene need CrateMotion and physics settings, plus CrateBuoyancy to float. Adding PushableCrate to a new crate automatically adds movement.

## Stack weight and mouse contact

Player push/pull speed uses the target crate's effective weight plus every supported crate above it, recursively, counting each crate once. Each crate contributes Weight * (1 - 0.5 * submergedFraction): 100% dry, 75% at half immersion, 50% fully submerged. Three dry unit-weight crates give effective weights 3 / 2 / 1 from bottom / middle / top; three fully immersed ones give 1.5 / 1 / 0.5. A fully submerged base with two dry crates above gives 2.5. Side-by-side crates and the support below the target are not added. Disabled PushableCrate components on active upper crates still contribute their load. The total is recalculated from current contacts and immersion.

At effective total weight 5 or higher the player cannot push or pull, including by swimming contact. The ground interaction hint is unavailable and a held crate is released if its stack becomes too heavy. Below the limit, base push speed is divided by StackWeight. Passive gravity, buoyancy and support carrying are not disabled by this player strength limit.

Ground push/pull and CrateMotion (including water drift and swimming pushes) carry crates resting on top by the support's actual horizontal displacement, including stacks with multiple levels. Carrying does not add velocity; player displacement already accounts for cumulative stack weight. Each carried crate retains its own solid collisions: if an upper crate meets a wall, it stops while its support can slide underneath. Upper crates require an enabled PushableCrate for horizontal carrying; tethered upper crates remain excluded. A tethered lower support can still carry free crates through its CrateMotion movement.

## Coordinated flotation

Water stacks now use a single vertical simulation, computed once per physics tick before individual crate movement, independently of component execution order. Contact grouping is rebuilt each tick; a horizontal slide that removes support takes effect in the next tick. The solver sums each member's buoyancy and effective gravity once, divides by total dry mass, averages drag, and critically damps near the waterline. Members share vertical velocity instead of repeatedly applying upper loads to each level. Dry stacks retain ordinary free-fall handling.

CrateMotion -> Maximum Water Vertical Speed defaults to 2 units/s, both rising and falling. The lowest member limit applies to a coordinated stack, including dry upper crates. Swimming pushes and rope corrections share the per-crate vertical travel budget for that physics tick. CrateBuoyancy -> Vertical Damping Ratio defaults to 1; larger values slow settling further. Vertical movement is swept against external solids; group members are ignored during clearance checks, then moved in leading order. Rope constraints and passenger clearance can limit the group's movement. Immersion no longer halves weight instantaneously.

Underwater contact pushes crates without E, including tethered crates; rope constraints and effective stack weight limit movement. Slow downward swimmer motion on a crate does not become a downward force that cancels flotation. PlayerPlatformRide carries a standing mouse and transfers support velocity on jumping. Rising crates can lift submerged mice. Overhead clearance limits carrying; crushing damage is not implemented.

Review a free floating crate, three-crate stacks entering/exiting water, mixed weights, effective total 4.5 versus 5, side walls, low ceilings, rope-tethered supports, swimming pushes and a mouse riding the top crate in Unity. No compilation or Play Mode tests were run. See [CrateFlotation.md](CrateFlotation.md) for numerical details.

## Biting

Hold F / gamepad East to bite a rope in front of the mouth. PlayerRopeBite exposes Hold Duration (initially 0.6 seconds), Reach and Mouth Offset. Original Inspector Ropes lists were superseded by the enabled-rope registry; see RopeInteraction.md.

Releasing input or losing the target cancels biting. Repeated cuts require a new press. Solids obstruct biting; there is no dedicated bite animation yet. Disable RopeSimulation.Can Be Cut for steel cable; climbing availability is independent.

Cutting removes one link, releases the crate and keeps remaining segments visible. The anchored section stays attached and separated pieces fall. A disconnected grip originally detached the player; current reattachment behavior is detailed in RopeInteraction.md. Segments remain simulated points without solid colliders. Seabed ropes reverse climb coordinates so vertical input stays consistent in world space.

Check surface equilibrium, contact pushing before/after cuts, riding rising crates, jumping from hanging crates, wall/ceiling constraints, interrupted biting and uncuttable ropes. Submersion assumes horizontal water surfaces using volume bounds; rotated/irregular water requires different sampling.

## Editor rope length

Rope Simulation.Length is a piece count (minimum 1); editing it adds/removes pieces. Segment Length controls each piece's world length. Rope.prefab uses one-unit pieces, so Length 5 means five units. Crate prefabs retain 0.25-unit segments and original total lengths.

The anchor stays fixed. Pieces align without gaps in the initial direction, upward for seabed ropes. Rebuild and align pieces restores alignment after manual edits. Edit geometry outside Play Mode; simulation, climbing, bites and crate constraints then use the new count. Ropes do not use one large BoxCollider; interaction follows simulated segments.
