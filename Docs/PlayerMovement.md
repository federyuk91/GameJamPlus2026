# Player platformer

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

`Assets/Editor/PlatformerValidation.cs` exposes `PlatformerValidation.Run` for Unity batch execution. It checks components, absence of Rigidbody2D, references, sprite/camera setup and floor/wall/ceiling sweeps. Run only in batch: it exits the Editor and does not save the temporary scene changes.

Manually check analog movement, early jump release, edge jumping and buffered jumps just before landing.

## Falling and camera

Downward gravity is 66 units/s² (30 × 2.2), capped at 26 units/s. Upward gravity remains 30. `fallGravityMultiplier` and `terminalSpeed` were originally assigned on PlayerMovement; they now belong to settings.

`PlatformCameraController` on Main Camera follows the Player Transform in LateUpdate. Inspector controls include dead zone, axis damping, horizontal look-ahead and downward fall look-ahead. Falling uses faster vertical tracking. Final camera position snaps to 1/32 unit while damping state retains full precision. Z stays constant. `SnapToTarget()` realigns after teleports/respawns. The initial implementation imposed no room bounds.

## Parameter units

Physical distances use Unity units (1 unit = 32 pixels), speeds use units/s, acceleration/gravity use units/s², times use seconds and multipliers are dimensionless. For example, speed 5 means 160 pixels/s. Acceleration × deltaTime changes velocity; velocity × deltaTime changes position. PPU affects rendering, not physical units.

## Crates: pushing and pulling

The movable crate in LoopScene uses `SimulatedBody2D` and `PushableCrate`, without Rigidbody2D. PushableCrate applies gravity before the player tick and retains no horizontal inertia. `PlayerCrateInteraction` pushes on contact and pulls with E / gamepad west (Xbox X / PlayStation Square) plus direction, from either side. Initially both bodies had to be grounded; jumping or releasing the button ended the grip.

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

PlayerMovement, PlayerSwimming, PlayerRopeInteraction, RopeSimulation and the two scene PushableCrates share the asset. Values are read each tick and can be adjusted in Play Mode. Asset edits may persist after leaving Play Mode. Former component copies were removed.

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
