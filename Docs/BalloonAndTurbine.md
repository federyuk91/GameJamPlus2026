# Balloon and turbine

## Ready-made prefabs

- `Assets/Prefabs/Props/Balloons/Balloon.prefab`: animated balloon with an eight-piece cuttable hanging rope (2 units long). Uses the existing crate collision/movement system, so it can be pushed and collides with terrain without Rigidbody2D. Drag the whole prefab into the level, above the ground with space for its rope.
- `Assets/Prefabs/Environment/Turbine.prefab`: seven-frame VentolaAreazione sprite animation, outlet facing local +X and a wind trigger extending 6 units forward, 2 units high. Rotate the whole prefab around Z to change wind direction. The supplied Aseprite sheet is exported unchanged to VentolaAreazione_Sheet.png, sliced into seven centered 32x32 sprites at 32 PPU with Point filtering and no compression/mipmaps. Turbine_Run loops at 10 FPS. There is no separate Rotor object or Transform rotation script.

The scene has not been modified and Play Mode/compilation have not been run.

## Balloon physics

### Cargo and the 0.4-0.6 neutral band

`BalloonLift.Lift Capacity` defaults to 0.5 and Hover Weight Tolerance to 0.1 (a neutral band from 0.4 through 0.6). `Assets/Prefabs/Props/Balloons/Balloon With Crate.prefab` contains a balloon, a two-unit cuttable rope, and a weight-1 crate already linked at its top. Place the rig with the crate on terrain, then change the child crate's SimulatedBody2D.Weight:

- Weight 1: remains grounded; its effective weight for pushing is 0.5.
- Weight 0.4 through 0.6: neutral vertical balance once the rope is taut.
- Weight below 0.4: rises; lighter loads have a higher ascent speed. Above 0.6, the load is too heavy to lift in still air.

In still air with no other load and default settings, maximum ascent is `SimulatedBody2D.Max Speed * (0.4 - payload weight) / 0.4` for weights below 0.4. The dead band is applied once to the whole load, and force ramps smoothly beyond its boundaries. Drag smooths acceleration. The neutral tolerance affects physical balance; the effective weight discount for pushing remains exactly 0.5. Upper stacked crates and passengers add load. Water and vertical turbine wind still contribute forces and can change the balance. Inertial mass remains positive; effective push weight is clamped to zero.

The balloon and its taut payload join the existing coordinated vertical simulation, so the balloon cannot independently fly away from a heavy grounded load. When the rope is slack the balloon first rises until it is taut; it does not remotely lift the crate. Cutting the link immediately removes the 0.5 discount and separates their simulation on the next physics tick.

For manual setup, assign Balloon Rope's `RopeSimulation.Balloon Anchor` to BalloonLift. Add RopeAttachment to the crate, assign that rope, its local Pushable and PropsMotion, and use a top attachment offset. Assign the attachment to PropsMotion.Tether and Pushable.Rope Attachment. Keep the crate outside the moving balloon's hierarchy. The free Balloon prefab already has Balloon Anchor and Lift Capacity assigned. This supports one payload attachment per crate with the current rope setup.

### Ground or moving-object tether

`Assets/Prefabs/Props/Balloons/Balloon Ground Tether.prefab` is a ready-made floor-tethered setup: Ground Anchor at the prefab origin, a two-unit rope extending upward, and Balloon above it. Place Ground Anchor on the floor. Balloon uses RopeAttachment with a bottom attachment offset of -0.375 and starts constrained to the rope length. Wind or a rider can deflect it, but it cannot escape until the rope is cut.

For a moving anchor, unpack the scene instance if Unity requires it for hierarchy edits, then parent **only Ground Anchor** to the desired object. Leave Balloon outside that moving object's hierarchy. The serialized rope reference remains connected; moving the anchor changes the constraint without directly parenting/teleporting the balloon. The target can be a platform, crate or any other Transform. Anchor motion is kinematic; the balloon does not apply reaction forces to the anchor object.

On a cut, floating loads now preserve the balloon-side fragment in the same way as underwater crates. The balloon rises freely; only a rider connected to its remaining fragment influences it. BalloonLift's Attachment slot identifies the endpoint load and uses LoadPull rather than the ground anchor's pull. The free Balloon prefab keeps its ordinary downward hanging rope.

To assemble manually, add RopeAttachment to Balloon, assign its Rope, Pushable and Motion references, set Attachment Offset to the balloon knot, assign it to PropsMotion.Tether and BalloonLift.Attachment, and keep the anchor separate from Balloon's hierarchy. Disable/remove the free hanging rope if reusing it as a single floor tether. Change Length/Segment Length on Ground Anchor's RopeSimulation to rebuild its geometry.

`BalloonLift` supplies upward acceleration, air resistance and limited horizontal/vertical velocity independently of water. Default lift is 78 versus Crate Gravity 66: the balloon rises until blocked by a ceiling. SimulatedBody2D.Air Drag damps motion; its single Max Speed (1.5 on balloon prefabs) caps total velocity and the motion travel budget in every medium. Without BalloonLift, PropsMotion retains its ordinary crate behaviour.

The Rope reference points to the child Balloon Rope. The anchor follows the balloon, while the rope chain remains simulated in world space. SimulatedBody2D supplies the mouse's mass for the vertical load balance. `RopeSimulation.AnchorPull` supplies weight-scaled pendulum tension for horizontal steering; Swing Influence controls that steering. This is a gameplay approximation of pendulum coupling, not a Rigidbody joint simulation. It needs tuning in Unity for the intended responsiveness. See BodyWeight.md for standing/hanging loads and the 30 g default.

Grabbing, climbing, jumping and biting use the existing rope controls. A cut retains the normal connected fragments and removes fragments shorter than two pieces. Only a rider still connected to the balloon's anchor transmits force. Detached pieces no longer steer or weigh down the balloon. The child rope is already attached; a separate floor tether can optionally use the existing RopeAttachment setup.

Free balloons remain outside water stacks. A balloon with a taut payload joins that payload's coordinated vertical group; water and cargo lift are combined there. Ordinary crate stacks retain the existing water simulation.

## Animation and art

The original `Assets/Art/Pixel Art/Props/Balloon_Sheet.aseprite` contains a 160x32 horizontal sheet. Its artwork was exported unchanged to `Assets/Art/Pixel Art/Props/Balloon_Sheet.png`, sliced into five 32x32 sprites at 32 PPU with a shared centered pivot, Point filtering and no mipmaps/compression. `Assets/Animations/Balloon/Balloon_Float.anim` loops at 8 FPS through the Balloon Animator controller. The collider stays fixed as the sprite animates.

## Wind

Select Turbine to see the wind bounds and direction gizmo. Edit the child Wind Area's BoxCollider2D Size and Offset to resize the airflow; it is a trigger and does not obstruct movement. Keep the area extending along local +X. Acceleration controls force, Far End Strength controls attenuation along its length. Overlapping turbines add their acceleration.

With Scene view Gizmos enabled and the turbine selected, green cells show unobstructed wind and red cells show obstacle shadows. Sample rays turn red after their first obstruction; a small circle marks the blocking surface. The preview uses the same outlet visibility query, layers and trigger/self exclusions as gameplay. Show Wind Gizmos toggles it; Gizmo Columns/Rows control preview resolution. This is a sampled preview, so small obstacles may require higher resolution. A stopped/disabled turbine shows only a gray outline. The overlay is editor-only and does not change wind behaviour.

Blocked By Walls casts towards the target against Blocking Layers (Default and Diggable by default), ignoring the turbine itself and triggers. A solid obstacle interrupts the wind. Wind affects balloons and ordinary PropsMotion bodies, including crates suspended from ceiling ropes. Crates divide the wind force by their Weight; heavier crates respond less. Existing collisions, tether constraints and damping remain active. Vertical wind also contributes to coordinated water-stack forces. Balloons keep their existing response and receive the wind only once. The mouse is not directly affected.

Running enables/disables wind and pauses/resumes the sprite Animator at its current frame. Bind `TurbineWind.SetRunning(bool)` or `ToggleRunning()` to a button event to control the turbine. Disabling the component also removes its wind.

## In-editor checks

Verify balloon rise/ceiling collisions, mouse attachment and steering in both directions, jumping off, cuts above/below the grip and release of disconnected fragments. Check that a running turbine moves the balloon within its area, a wall blocks the wind, rotation changes direction, and stopping the turbine removes airflow. Tune lift, Swing Influence, air resistance and wind acceleration to the desired gameplay pace.
