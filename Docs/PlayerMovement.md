# Player platformer

## Local movement settings: first five parameters

Select Player (or its prefab), then Player Movement > Movement Settings. PlayerMovementSettings is now a serialized object owned by each animal, containing the first five migrated parameters plus Maximum Push Weight, all with explanatory tooltips:

| Parameter | Player prefab value | Previous global field |
| --- | --- | --- |
| Move Speed | 5 | player.speed |
| Ground Acceleration | 45 | player.groundAcceleration |
| Air Acceleration | 28 | player.airAcceleration |
| Jump Height | 2.0166667 | gravity.jumpHeight |
| Jump Release Multiplier | 0.45 | gravity.jumpReleaseMultiplier |
| Maximum Push Weight | 5 kg (exclusive) | Pushable.PlayerPushWeightLimit constant |

Those five fields were removed from PhysicsSettings.asset and its Inspector. Remaining global player parameters are temporarily grouped in SharedPlayerMovementSettings at the existing player path, retaining their values. Footstep cadence, jump buffering, gravity and other categories remain global; slope settings now belong to the body or are calculated (see GroundPhysics.md).

PlayerLedgeClimb references the same PlayerMovement instance for ledge jump height and horizontal launch speed. The LoopScene player inherits its local settings from Player.prefab. New components created without a saved configuration use the same historical fallback defaults (including jump height 2).

No Play Mode or compilation checks were run. In Unity, compare walking/air control, full and released jumps, and ledge jumps. Changing these local fields on a second animal should affect that animal independently of the shared PhysicsSettings asset.

Airborne ledge attachment checks the reachable side/top and collision-free hanging alignment independently of the space above the ledge. Landing space and the swept up-then-across path are checked when attempting to climb, and rechecked on each new attempt so a moving obstacle can clear the route. A blocked climb keeps the mouse hanging. Jump + Up launches vertically when climbing is blocked; with a clear route it climbs normally. Jump away/no direction still launches away, Down drops, and ground-level automatic climbs still require full clearance before starting. Upward launches remain collision-constrained and cannot pass through ceilings. Review low-ceiling hangs, blocked/clear landing space and moving obstacles in Unity; no Play Mode tests were run.

Ledge attachment and automatic obstacle climbs are blocked while PlayerSwimming detects the player underwater. The check refreshes after movement so entering water during the same physics tick cannot start a ledge hang. An active ledge interaction is cancelled if the player becomes submerged. Existing water detection uses the player collider's centre; ledges become available again when that point leaves the water volume.

## Ledge grabbing and climbing

`PlayerLedgeClimb` distinguishes None, Hanging and Climbing:

- On the ground, steps up to `player.maxStepHeight` (0.15 units) use the step system. Taller obstacles up to `ledgeClimb.maxHeight` (0.75 units) start a climb when the mouse walks toward them.
- In the air, a ledge near grip height can be grabbed. The mouse hangs until input is held or pressed toward the obstacle; releasing direction after jumping is unnecessary.
- While hanging, Jump launches away from the ledge. Down drops; holding Down prevents immediate reattachment.

Ground and hanging climbs share Animator state `ClimbUp`. `LedgeState` is 0 for normal movement, 1 while hanging and 2 while climbing. Until dedicated clips are available, `Hanging` displays a stationary Idle frame and `ClimbUp` uses Idle as a placeholder. Assign the climbing clip to the `ClimbUp` state's Motion in `Assets/Animations/Mouse/Mouse.controller`; match climb speed to clip duration.

PhysicsSettings' player ledge section exposes maximum height, probe distance, collider-relative grip height (0.85) and vertical tolerance (0.15 units). Grip height follows BoxCollider2D resizing. The path checks ceilings and body clearance, and the grip follows moving support colliders.

Manually check LoopScene's `Climbable` object from both sides, grabbing with/without held direction, Jump/Down release and moving crates. The historical sections below describe earlier controller versions. Also see `Docs/CratesWaterAndRopes.md` for current crate, water and rope behavior.

Main scene: `Assets/Scenes/LoopScene.unity`, enabled in the build.

## Composition

- `PlayerInputReader`: Input System analog stick / D-pad, A/D or arrows. Jump uses the gamepad south button (Xbox A / PlayStation Cross) or Space. Presses remain queued until the physics tick.
- `SimulatedBody2D`: X/Y BoxCasts against `solidLayers` (originally Default), excluding triggers and the player's collider, with a 0.005-unit skin. Moves the Transform without Rigidbody2D. The initial implementation targeted static floors/walls and did not implement moving platforms, one-way surfaces, pushes or slope climbing.
- `PlayerMovement`: speed, ground/air acceleration, gravity, variable jump release, coyote time and jump buffer, simulated in FixedUpdate. Parameters originally lived on the component; later sections describe their migration to settings.
- `MouseFacing`: flips the SpriteRenderer without flipping the collider or Transform.

## Scale

32 pixels = 1 unit. The mouse sprite has a 32×32 canvas, PPU 32, point filtering and no mipmaps/compression. Its original 24×22-pixel collider avoids ears and tail blocking movement. The original Pixel Perfect Camera used 640×320, a 20×10-unit reference area, integer enlargement, Point filtering and aspect-preserving bars. Draw assets at 32 PPU with dimensions in multiples of 1/32 unit. The current 640×360 display configuration is documented in `Docs/PixelArtDisplay.md`.

The original StartRoom geometry was retained, including its partial right wall and an exit leading to a fall. Checkpoints/respawn were not part of that initial implementation; see `Docs/RecoveryZones.md` for later additions.

## Validation

`Assets/Editor/Validation/PlatformerValidation.cs` exposes `PlatformerValidation.Run` for Unity batch execution. It checks components, absence of Rigidbody2D, references, sprite/camera setup and floor/wall/ceiling sweeps. Run only in batch: it exits the Editor and does not save the temporary scene changes.

Manually check analog movement, early jump release, edge jumping and buffered jumps just before landing.

## Falling and camera

Downward gravity is 66 units/s² (30 × 2.2), capped at 26 units/s. Upward gravity remains 30. `fallGravityMultiplier` belongs to the animal's local PlayerMovementSettings; the speed ceiling belongs to SimulatedBody2D.Max Speed.

`PlatformCameraController` on Main Camera follows the Player Transform in LateUpdate. Inspector controls include dead zone, axis damping, horizontal look-ahead and downward fall look-ahead. Falling uses faster vertical tracking. Final camera position snaps to 1/32 unit while damping state retains full precision. Z stays constant. `SnapToTarget()` realigns after teleports/respawns. The initial implementation imposed no room bounds.

## Parameter units

Physical distances use Unity units (1 unit = 32 pixels), speeds use units/s, acceleration/gravity use units/s², times use seconds and multipliers are dimensionless. For example, speed 5 means 160 pixels/s. Acceleration × deltaTime changes velocity; velocity × deltaTime changes position. PPU affects rendering, not physical units.

## Crates: pushing and pulling

The movable crate in LoopScene uses `SimulatedBody2D` and `Pushable`, without Rigidbody2D. Pushable applies gravity before the player tick and retains no horizontal inertia. `PlayerCrateInteraction` pushes on contact and pulls with E / gamepad west (Xbox X / PlayStation Square) plus direction, from either side. Initially both bodies had to be grounded; jumping or releasing the button ended the grip.

Original interaction speed is 2.5 units/s (80 pixels/s), reach 0.12 units. Queries constrain both crate and player against obstacles. Planned horizontal dragging ignores only the held crate collider; it remains solid vertically so the mouse can jump onto it. Chain pushing, rotation and carrying the player on moving crates were outside the initial implementation. Consult `Docs/CratesWaterAndRopes.md` for later changes.

`PlatformerValidation.Run` includes a temporary scene for pushing, pulling, release, collider separation and walls on either side. It does not save that scene.

## Ropes: climbing, swinging and jumping

LoopScene's Rope originally contained five Rope Piece objects joined through simulated distance constraints without Rigidbody2D or Unity Joint2D. The Rope Transform is the top anchor; `pieces` is ordered top to bottom. Each segment is 0.5 units (16 pixels), totaling 2.5 units. Initially overlapping pieces were arranged into a chain in the saved scene.

- Attach with E / gamepad west, or Up/W near the rope. Holding the button is unnecessary after attachment.
- Climb with Up/Down, W/S, vertical stick or D-pad, at 2 units/s, clamped to the rope endpoints.
- Swing with Left/Right, A/D, horizontal stick or D-pad. Rope acceleration is 70 units/s²; alternate input with the swing.
- Release with Jump. Horizontal input (A/D, Left/Right, left stick or D-pad) selects the launch side, even opposite the current swing. Horizontal speed keeps the inherited magnitude with a minimum of Rope Jump Swing Boost (default 4 units/s), redirected toward the chosen side; vertical velocity inherits the rope's motion plus Rope Jump Up Speed (default 7 units/s). With no horizontal input, the launch follows the measured swing direction; at turning points it retains the last measured direction. A stationary rope with no direction input adds only the upward boost. Air control remains reduced for 0.25 s after release. Review jumps toward both sides, against the swing and without directional input in Unity; no automated tests or Play Mode were run for this change.

`RopeChain` uses Verlet integration, four substeps and 24 constraint iterations. `RopeSimulation` connects the solver to scene pieces. `PlayerRopeInteraction` handles attachment, climbing and release; PlayerMovement coordinates each tick and releases crates when attaching to a rope. Reach, speed, acceleration, damping and timing are adjustable with explicit units. Reattachment is blocked for 0.3 s after release.

Player sweeps still collide with walls, floors and crates; blocked rope-following releases the grip. Segments are visual, without world collision or obstacle wrapping. The initial solver assumes the anchor is not moved or resized during simulation.

Historical `RopeValidation.RunChainChecks` numerical checks passed at 30/50/100 Hz using Unity assemblies outside the Editor: anchor stability, lengths, swing, units/s velocity and grip limits. Input/collider interactions still require Play Mode checks.

## Enforced swing limits and inner/outer gizmos

`RopeSimulation > Swing Limits` sets actual limits in degrees from downward vertical (0). Min is left, Max right. Scene values: Min Inner -45, Min Outer -60, Max Inner 45, Max Outer 60. Each side is independent; inner stays between center and its outer limit.

Movement is unrestricted in the center. Between inner and outer, a stopping-distance angular speed cap progressively slows outward movement. Outward input also fades; inward input remains available. At outer, all segments stop for the rest of the tick, with zero velocity and no instant bounce. Gravity starts the return next tick. Corrections rotate the chain around its anchor, preserving segment lengths. The measured angle runs from anchor to rope tip, not between individual segments. Jump velocity uses the solver's post-braking state.

With Rope selected and Gizmos enabled, cyan rays/arcs show inner limits, red shows outer limits, translucent orange sectors show deceleration bands, and a yellow Play Mode ray shows the current angle. Gizmos read simulation limits directly; the former preview angles were removed.

Historical numerical checks passed at 30/50/100 Hz for braking, asymmetric bounds, zero node velocity at outer, reversal under held outward input, segment lengths and solver regressions. C# compilation also included UNITY_EDITOR gizmos.

## Swimming

LoopScene's Water has `WaterVolume` and a BoxCollider2D trigger matching its sprite/scale. The player has `PlayerSwimming`; `SwimMotion` holds numerical logic. No Rigidbody2D is added. Each tick queries the mouse collider center, without depending on trigger callbacks. Overlapping volumes work without entry/exit counters. Add WaterVolume and size its BoxCollider2D to create more water.

Original swimming values were speed 3 units/s, acceleration 10 units/s², braking 6 units/s², sinking gravity 6 units/s², max sinking speed 2.2 units/s and vertical drag 0.8/s. Each new Jump press renews an upward 4.8-units/s stroke. Holding Jump does not repeat it; releasing does not shorten it. Repeated presses allow ascent without unbounded impulse accumulation. Later settings migration updated these values as listed below.

Leaving water with the collider center restores normal movement while preserving stroke momentum. Swimming retains solid collisions, resets ground coyote time/jump buffer and originally released crates. An attached rope takes priority and can lift the mouse out. The initial swim implementation did not add crate buoyancy; see `Docs/CratesWaterAndRopes.md` for later behavior.

Historical C# compilation and swimming numerical checks passed at 30/50/100 Hz. PlatformerValidation also includes Editor checks for Rigidbody-free volume queries, disabled/overlapping volumes and surface/side exits. Full input and collider behavior requires Play Mode verification.

## Shared gravity and water settings

Select `Assets/Settings/PhysicsSettings.asset`. Gravity contains player gravity/jump, crate gravity/fall limits, rope gravity and rope launch speed. Water contains horizontal speed, acceleration/braking, sinking gravity, maximum sink speed, stroke speed and vertical drag.

PlayerMovement, PlayerSwimming, PlayerRopeInteraction, RopeSimulation and the two scene Pushables share the asset. Values are read each tick and can be adjusted in Play Mode. Asset edits may persist after leaving Play Mode. Former component copies were removed.

Migration transferred SwimMotion values: speed 5, acceleration 10, braking 6, gravity 7, maxSinkSpeed 3.5, strokeSpeed 4.8 and verticalDrag 1.5, replacing the scene's older inline values. Other physics settings retained scene values. Code defaults initialize new assets and provide fallback when no asset is assigned; all six affected LoopScene components had references assigned.

Create profiles with Create > Mouse Platformer > Physics Settings and assign each component's Physics Settings field. Scale remains 32 pixels/unit; speeds use units/s, acceleration uses units/s², drag uses 1/s and multipliers are dimensionless.

## Ladder: Stair

Stair contains five StairPiece rungs and two child rails. Its Ladder trigger is 1×5 units. `PlayerLadderInteraction`, coordinated by PlayerMovement, attaches/climbs with Up/W/vertical input and descends with Down/S. Without vertical input it holds position without gravity. Jump plus Left/Right chooses the launch side; without direction it uses the last facing side.

Grip position is stored in ladder-local space. Moving Stair or its parent carries the player at that point; support velocity is added to jumps. PlayerMovement ticks after default-order scripts, allowing FixedUpdate ladder movers to update first. Do not reparent the player or add Rigidbody2D. SimulatedBody2D sweeps remain active; blocked following releases the grip. Descending past the bottom detaches. At the top, climbing stops 0.6 world units below the former feet-clearance position, clamped to the ladder bottom for very short ladders. The same limit applies on attachment. The player stays attached, including while holding Up or releasing vertical input. Down resumes descent; Jump releases the ladder.

PhysicsSettings > Ladder contains climb speed 3 units/s, horizontal jump 6 units/s, upward jump 9 units/s, reach 0.65 units and reattachment delay 0.3 s. An already-held rope takes priority. Ladders can be used from water and release crate grips.

Historical compilation and numerical checks passed at 30/50/100 Hz, including scaled ladders and inherited support velocity. PlatformerValidation includes a moving-ladder check for climbing, descending, local-coordinate following, launch and cooldown; Unity object checks require Editor/Play Mode execution.

## Complete player movement settings

PhysicsSettings also contains Player (horizontal speed, ground acceleration/braking, air acceleration, coyote time, jump buffer, reduced-control duration/multiplier after rope/ladder launch) and Interactions (crate push/pull speed/reach, rope climb/swing acceleration, reach and reattachment delay). Former component copies were removed and scene values migrated. Gravity, Water and Ladder retain their own sections.

The custom Inspector shows player movement first. In the default Inspector, expand Player. All values are read during simulation; no script edits are needed.

## Jump Height

PhysicsSettings > Gravity > Jump Height replaces ground Jump Speed. It specifies maximum ascent above the launch position for a full held jump without overhead obstacles. Two units equals 64 pixels. Initial speed is `sqrt(2 * gravity * height)`. Ascent uses analytical displacement and ends exactly at the apex with zero velocity, avoiding Euler error or missing the apex between ticks. Descent retains increased gravity and terminal speed.

Early release still produces a short jump; Jump Height describes the full jump. Collisions can stop ascent early. Keep gravity constant during a jump to obtain the configured height. Player gravity must be positive (minimum 0.001). Previous speed 11 with gravity 30 migrated to approximately 2.016667 units; new assets default to 2 units. Swimming and rope/ladder launches keep their own parameters.

Historical JumpValidation covered five heights, three gravities and 10/30/50/100 Hz ticks: apex error below 0.0001 units, zero apex velocity, subsequent descent, early release and zero height. Numerical checks and compilation passed.

## Automatic ledge climb

PlayerLedgeClimb reads PhysicsSettings > Ledge Climb. When pushing toward a wall, it probes downward just beyond the corner for the contacted collider's upper surface. Height above the feet must be within Max Height (default 0.75 units / 24 pixels). Triggers and excessively tall walls are excluded.

Before starting, it checks overhead clearance, the upper horizontal corridor and destination body space. It sweeps upward then across each tick without teleporting. New obstacles interrupt movement. The destination is support-local to follow small support movements. A final ground probe returns control to normal movement. The maneuver temporarily locks ordinary movement and adds no extra jump.

Settings include enabled, maximum height, reach beyond the edge (0.12 units), speed (5 units/s) and retry delay (0.15 s). Active rope/ladder attachments take priority. Water ledges can be climbed; crate grips are released.

Compilation and scene references were checked during implementation. Tools > Mouse Platformer > Validate Ledge Climb runs real-collider checks from both sides at 30/50/100 Hz, covering low ceilings, tall walls, triggers and non-overlapping landing. These Unity checks were not run at that stage; verify behavior in Play Mode.

## Horizontal control after a swim stroke

After Jump in water, PlayerSwimming keeps a 0.35 s faster-control window: acceleration 45 units/s² instead of normal swimming acceleration 10. Maximum speed is unchanged; the stroke preserves lateral velocity. Just above the surface, swimming velocity/response remain until the window expires, avoiding an immediate switch to ground movement parameters. Rope/ladder attachment and ledge climbing cancel the window.

PhysicsSettings > Water exposes `strokeAcceleration` (acceleration after Jump) and `strokeControlTime` (control duration after Jump). Timing state belongs to each player, not the shared asset. Vertical stroke speed and sinking remain separate from horizontal control.

Historical numerical checks passed at 30/50/100 Hz for retained velocity, acceleration from rest, surface control and direction reversal. Compilation passed; full input/collider checks remain manual.

## Digging and biting with F / East

F / gamepad east is shared by rope cutting and digging. A cuttable rope within mouth reach takes priority and requires a 0.6-second hold. Steel ropes cannot be cut. Otherwise, holding F plus direction digs reachable soil. A reachable cuttable rope interrupts digging. Once biting starts, the same press cannot switch to digging; release after cutting.

Digging uses both movement axes, including diagonals. Releasing direction stops the mouse; releasing F restores normal movement/gravity. Digging cannot start while attached to ropes, ladders or ledges. Crates retain E / west; ladder, climb and jump controls remain unchanged.

PhysicsSettings > Digging exposes enabled, speed, radius, reach, excavated alpha (0–255) and tint. Effective radius automatically expands to contain the player collider. Alpha 254 reveals only 1/255 of the background; lower it for visibility without closing tunnels.

`DiggableTerrain` on LoopScene's Diggable ground stores removed cells in a separate runtime mask. A URP 2D unlit shader displays the selected tint above the player (order 10), preserving the original texture. The original BoxCollider2D trigger defines the area and is replaced at runtime by merged rectangular solid colliders. Player and crate collision masks include Diggable.

Terrain resolution is 16 cells/unit (two art pixels), up to 512 cells per axis. Only changed cells update masks/colliders. Tunnels last for the session and reset on scene reload; no disk save is implemented. For new areas, assign terrain references/material and add them to PlayerDigging's Terrains list.

PlayerRopeBite diagnostics show Last Attempt, Bite Progress and Blocking Collider. Selected-player yellow gizmos show the mouth position, radius and direction used to search for ropes.

While hanging, biting first selects the held rope even behind the mouth or outside the normal frontal radius. The cut segment is at the grip toward the anchor. CanBeCut, obstacle checks between mouth/grip and the hold duration still apply. Originally normal rope control released the mouse if the grip lost its anchor connection after cutting; see `Docs/RopeInteraction.md` for current behavior. Unheld ropes retain frontal search and reach limits.

## Local push strength and shared simulation rules

Maximum Push Weight belongs to PlayerMovement.MovementSettings on each animal. PlayerCrateInteraction reads that same instance for ground push/pull, contextual availability and swimming pushes. Effective stack weight equal to or above the limit blocks the interaction; 0 disables pushing. The Player prefab preserves the previous 5 kg threshold.

PropsMotion.PushBySwimming previously used Pushable.PlayerPushWeightLimit as an extra guard before moving an immersed prop. It now receives the acting animal's maximumPushWeight as an argument from PlayerCrateInteraction, retaining that guard without storing animal strength on the prop. This threshold does not control passive flotation, wind, balloon lift or passenger carrying. The method's signature now requires the limit before dt.

PhysicsSettings.asset > Shared Simulation Rules exposes Simulation Substeps (4), Rope Simulation Substeps (4), Rope Constraint Iterations (24) and Full Submersion Weight Multiplier (0.5). Both individual props and coordinated vertical groups use Simulation Substeps. Rope grabs/releases use the configured rope substep duration for Verlet history. The immersion multiplier is shared by push load, vertical gravity and waterline stiffness; inertial mass remains unchanged. Pushable references its local PropsMotion to use the same physics asset.

The existing editor rope validation callers were updated for the explicit solver arguments; no validation, compilation or Play Mode was run. Review ground/swimming push thresholds, water equilibrium and rope grab/release in Unity.

Hand Offset is now shared by rope and ladder in the local Movement Settings, preserving (0, 0.2). Their animation clips remain distinct. All player physics components now read the shared PhysicsSettings asset reference from SimulatedBody2D; assign it there instead of on each interaction component.

The global player terminal speed was removed. PlayerMovement now integrates falling using SimulatedBody2D.Max Speed and caps its velocity vector through that body. Player.prefab retains Max Speed 26 units/s. Body move requests are also capped; walking, swimming and climb speed settings remain requested control speeds below the common ceiling. Review diagonal movement and jump trajectories when lowering Max Speed.

Fall Gravity Multiplier was migrated from global gravity settings to Player Movement > Movement Settings on each animal. Player.prefab preserves 2.2, with an explanatory tooltip. The upward player gravity and body Max Speed are unchanged. Coyote Time, Jump Buffer, Launch Control Time and Launch Control Multiplier remain global by design. No compilation or Play Mode was run; review falling acceleration in Unity.

## Shared slope solver

GroundContact2D detects ground for both animal and prop bodies. Max Slope Angle belongs to SimulatedBody2D (Player: 45 degrees). The old global slide enable/angle/acceleration/speed fields were removed. GroundMotion calculates passive velocity from gravity force, surface normal, body mass and Ground Friction; total velocity remains limited by Body.Max Speed. Player friction is 1 (tan(45 degrees)), aligning gravity-only static holding with the intended 45-degree slope angle. Walking control remains separate and can brake uphill slide. Details and manual review points are in GroundPhysics.md.

## Unified world gravity

PlayerMovement, ledge jumping and ground sliding now use SimulatedBody2D.GravityAcceleration (world gravity times local Gravity Scale). The Player prefab uses scale 1 with world gravity 30; its local Fall Gravity Multiplier remains 2.2. Swimming applies water.gravityMultiplier (7/30) to body gravity, preserving downward acceleration 7. Rope launch speeds are shared in interactions rather than the removed GravitySettings.

## Shared hand grab reach

Player Movement > Movement Settings > Hand Grab Reach is the single rope/ladder attachment distance, set to 0.6 world units on Player.prefab. Both interactions read the same local instance and share Hand Offset. Ladder attachment still requires overlap with the ladder trigger; rope availability and regrab conditions remain. The old global ropeGrabRadius and ladder.grabReach were removed, and rope gizmos use the local shared reach. Bite reach, crate reach and ledge distances remain independent. No compilation or Play Mode was run.

Horizontal Hand Interaction is the shared local horizontal reach for push/pull and ledge grabbing, set to 0.15 world units on Player.prefab and measured beyond the collider side. It replaces global interactions.crateGrabReach and ledgeClimb.forwardReach. Contextual crate availability, reach checks and gizmos read the same setting. Hand Grab Reach remains 0.6 for ropes/ladders; vertical ledge reach remains independent. No compilation or Play Mode was run.

Vertical Hand Interaction (0.15 on Player.prefab) is the local airborne ledge-grab tolerance, replacing global ledgeClimb.grabVerticalReach. It measures vertical separation from the grip height defined by Hang Height Fraction and is distinct from the maximum climbable height. No compilation or Play Mode was run.

### Horizontal swimming speed

PlayerSwimming stores a local Swim Speed Multiplier (default 1), with a reference to the same animal's PlayerMovement. Horizontal swimming speed is calculated from Move Speed × Swim Speed Multiplier, including surface control after a stroke. With Move Speed 5 and multiplier 1 the current speed remains 5. SwimMotion no longer stores an independent absolute speed; underwater bottom walking keeps its separate existing multiplier. SimulatedBody2D.MaxSpeed remains the physical ceiling.

### Digging movement speed

PlayerDigging stores a local Dig Speed Multiplier (default 0.3), with a reference to the same animal's PlayerMovement. Digging speed is Move Speed × Dig Speed Multiplier; Move Speed 5 therefore preserves the previous 1.5 units/s. DiggingSettings no longer contains an absolute speed. Terrain removal follows the requested displacement, and SimulatedBody2D.MaxSpeed still limits body motion.

### Shared body water damping

PlayerSwimming passes SimulatedBody2D.WaterDrag to SwimMotion for vertical velocity damping. The Player prefab uses 1.5 to preserve the previous swimming response. Props retain their existing body values and immersion-based application; horizontal swimming retains its acceleration/braking controls. The global SwimMotion.verticalDrag field has been removed.

### Shared ladder and ledge climbing speed

PlayerMovement > Movement Settings > Climb Speed Multiplier controls both ladder ascent/descent and the up/across ledge climb. Both read the calculated ClimbSpeed = Move Speed × Climb Speed Multiplier. Default 0.6 with Move Speed 5 gives 3 units/s; this preserves ladder speed and changes ledge climbing from 5 to 3. The two old global climbSpeed fields are removed. Rope climbing and release jump velocities are unchanged.

### Rope and ladder release stability

Rope and ladder grip alignment is positional correction, not inherited launch velocity. Rope carrier velocity is measured across fixed ticks at the same distance along the rope, before applying climbing input; attaching or regripping resets that sample. Ladder release inherits only movement of the same local grip point. A blocked alignment releases with carrier velocity, excluding the snap and requested climb movement. Release velocities are limited by the animal body's existing Max Speed.

PlayerMovement begins one movement budget on SimulatedBody2D per fixed tick. Platform carry, rope/ladder alignment, free movement, ground snapping and step corrections consume that same travel budget. Multiple Move calls can no longer each spend a full Max Speed × fixedDeltaTime. Every movement still uses the existing collision casts. Other physics drivers retain their existing per-call limits until they explicitly start a shared movement step.

Manual Unity checks: repeat jump + right, up + jump + right around ropes and ladders, including thin walls and ceilings; also check opposite-direction jumps, rope cuts, moving ladders, platform carry and ordinary slope stepping. No compilation, automated checks or Play Mode were run for this change. The original wall-crossing report remains to be reproduced in Unity.

### Grab while jumping and staged alignment

Up may attach to a reachable rope or ladder even on the same tick as a Jump press. A new attachment consumes that press; only a subsequent press may release it. Holding Jump after a normal jump does not block Up. Existing regrab cooldowns still apply.

Attachment candidates are checked against the final clamped grip point and a collision sweep. Alignment may take multiple fixed ticks when limited by the shared movement budget; incomplete alignment alone no longer detaches the animal. Climbing waits until aligned, and rope swing input waits too. A real obstacle can still reject or interrupt the grip. Inherited carrier velocity is restricted by the reachable collision displacement. Releasing a ledge or ladder prevents another traversal mode from attaching in that same tick.

These changes address the grab regression and additional release/reattach paths. The remaining intermittent dash/wall crossing still requires manual reproduction in Unity; no automated checks, compilation or Play Mode were run.

### Suspended rope swing input

For a free rope section hanging below its anchor, horizontal input pumps the pendulum instead of applying a constant sustaining force. The rope solver filters input each substep using tangential velocity relative to the anchor. Input opposing the current swing contributes no force. Outward input fades continuously to zero as tangential speed approaches zero; gravity can therefore reverse the swing even while the same direction is held. Inward input can start motion from a tilted rest position, and a vertical resting rope can still receive an initial push.

The existing Rope Swing Acceleration remains the maximum input strength; no values were changed. Existing angle limits remain in effect. Sections connected to a load at both ends and upward-running floor-anchored sections retain their existing drive logic. Release direction controls remain unchanged. Manual Unity check: hold one horizontal direction near both extremes, then alternate directions with the swing; also check balloon-anchored ropes. No compilation or Play Mode was run.

### Swing strength and release boost

Outward pumping now reaches full configured acceleration once tangential speed reaches 10% of sqrt(gravity × grip radius). It fades only near a turning point or stationary deflection, instead of attenuating the whole outward swing. Opposing input still contributes no sustaining force, and existing angular limits still apply.

A directional rope jump now adds Rope Jump Swing Boost to the magnitude of inherited horizontal speed before choosing the requested side. It no longer treats the boost as a minimum: inherited 6 plus boost 4 gives horizontal 10 before the shared body speed limit. Upward impulse and MaxSpeed remain unchanged. Manual Unity checks: early directional release while accelerating, release near either turning point, opposite-direction release and holding one direction through a complete swing. No compilation or Play Mode was run.

### Ledge climb completion handoff

Successful climb completion now reports released=true, allowing PlayerMovement to resume normal movement within the same fixed tick. Horizontal travel during the final climb step is retained; upward climb velocity is not carried into a false airborne launch. A grounded completion does not start reduced post-launch air control and preserves a Jump press received on that completion tick. Vertical and horizontal climb phases may advance within the same tick when vertical alignment is complete, respecting the existing shared movement budget and collision casts. No compilation or Play Mode was run; manually check continued movement and jumping as the climb finishes.


### Ledge climb landing clearance

The up/across traversal uses swept movement with ground following disabled until the final downward settle. Ordinary body movement could snap the horizontal phase down to the support, causing the next tick to raise the body again and delaying completion until the two-second timeout. Collision sweeps and the shared movement budget remain active. Last Attempt now distinguishes successful completion from a traversal timeout. No Play Mode checks were run; check continuous movement over a low ledge and climbing from a hanging position.
