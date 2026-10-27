# Interactable buttons

Prefabs are in Assets/Prefabs/Buttons. All buttons support both input and weight;
there is no activation-mode selection. Interactable Button and its colored versions
retain their existing references. Pressure Button is also ready for the same use.

The mouse can press with the existing interaction input while overlapping the
cyan proximity trigger with a clear line of sight. It can also stand on the solid
Button Surface, just like a crate or another active SimulatedBody2D. Side proximity
and pickup triggers do not count as weight. A crate resting on it at startup
presses it normally. No Rigidbody2D or tags are required.

SimulatedBody2D / Can Press Buttons controls whether a supported body supplies
pressure, independently of its collision or physical mass. Empty cheese plates
disable it when consumed. They can still move with the button surface but do not
hold it down. Reusable buttons release when the last pressing body is removed
or loses this flag. Single Use retains its explicit lock until ActivateButton.

## Events and animation

On Pressed is the single shared UnityEvent for input and weight. Previous button
event connections remain in that serialized field. It now fires once when the
animation reaches the bottom, including input presses (previously immediate).
On Release fires once after the reverse animation reaches the fully raised frame.
For a door, connect opening to On Pressed and closing to On Release.

Animation Step Duration controls each sprite step. ButtonPressurePlate fits the
solid collider to each frame while keeping the base aligned. Bodies directly on
top move with its height change, using their own obstacle casts. Contact Tolerance
sets the small accepted gap between their feet and the surface. Show Gizmos draws
the proximity trigger in cyan and the solid surface in yellow.

Without Single Use, an input press auto-releases unless a weight is still present.
A weight keeps the button down until the last body leaves. Returning weight during
the release reverses movement without firing On Release or repeating On Pressed.
A brief weight removed before full depression does not activate a reusable button.

## Locking and reactivation

Single Use remains pressed and locked after use, including when weight leaves.
ActivateButton releases and rearms it for another use; that release emits On Release
if the button had emitted On Pressed. If weight remains, it presses again.
Starts Locked initializes down without firing events. LockButton immediately locks
it down; locking an idle button does not invoke actions. A lock during pressing
completes that press. Disabling/re-enabling preserves locks and the spawn cursor.

For alternating buttons, enable Single Use on both, start the second locked and
wire each On Pressed to the other button's ActivateButton.

## Spawn actions

Spawn Prefabs supplies the zero-based list for Spawn(int index). Add Spawn(0), for
example, to On Pressed. Spawn Offset and the green Spawn Point handle position the
spawn relative to the button. Each object uses its prefab rotation and no parent.

Spawn Position Mode selects positions independently of the prefab index:

- Fixed (default): always uses Spawn Offset, preserving existing buttons.
- Sequential: uses the assigned Spawn Points transforms in Inspector order.
- Shuffle: uses each assigned point once in random order. Between repeated
  cycles, avoids immediately reusing the previous point when at least two exist.

Create Empty objects at the desired positions and assign them to Spawn Points.
Green gizmos and numbered Scene handles show and edit their world positions.
Null references and duplicate references to the same Transform are ignored.
Without any valid points, all modes use the original fixed Spawn Offset.
Position markers supply only position; spawned objects retain prefab rotation.

Repeat Spawn Points on repeats position cycles. Off stops spawning once all
valid points have been used. It does not lock the button or suppress other
On Pressed actions. With only one valid point, repeating naturally reuses it.
Position progress advances only after a successful spawn. ActivateButton,
LockButton, disabling/re-enabling and changing the requested prefab index do
not reset it. Changing the point list or selection mode restarts its cycle.
ResetSpawnPositions is available to UnityEvents to restart positions explicitly;
it does not reset the prefab sequence. Reloading the scene resets both sequences.

SetSpawnPositionFrom(Transform) is also callable from UnityEvents: assign an
Empty to update the fixed spawn offset using its current world position.
This samples the position at the time of the call and does not follow the marker.
It affects Fixed mode (and the fallback when no points are assigned).

Advance Spawn Index off repeats the supplied index. When enabled, that index
starts a sequence advancing after each successful spawn, stopping after the last
entry. A different starting index resets the sequence. Invalid indices or empty
entries warn without advancing. ActivateButton and LockButton preserve the cursor.

For externally rearmed spawns, start locked and enable Advance Spawn Index. On
Pressed calls Spawn(0) and LockButton; another button calls ActivateButton before
each use. Single Use may replace the self-lock action.

For the same crate at three different points, leave Advance Spawn Index off,
choose Sequential, assign three Spawn Points and call Spawn(0). Disable Repeat
Spawn Points to stop after the third crate, or leave it enabled to repeat.

Existing scene prefab instances inherit the solid surface. No gameplay scenes
were edited or reloaded. Check mouse descent, crate removal, door events,
Single Use and reactivation in Play Mode.


## Sprite Physics Shape collision

ButtonPressurePlate now uses a PolygonCollider2D surface matching every physics path of the currently displayed sprite, including intermediate press/release frames. Sprite-local vertices are converted through renderer and collider transforms, with Flip X/Y support. With no usable sprite physics paths, a sprite-bounds rectangle is used as a fallback. Weight detection checks the actual polygon distance/contact point instead of only the overall bounds top, so lower portions of stepped shapes can support bodies.

All six button prefabs use polygon surfaces while retaining their original surface component IDs and interaction triggers. The authored initial polygon preserves the previous resting rectangle; Initialize/SetSprite installs the sprite's physics paths at startup. Older standalone scene copies with a box surface are converted locally on first MatchVisual (old box disabled, polygon used). On a frame change, existing passengers follow the change in top height using the same obstacle-constrained carry as before. Gizmos draw the polygon paths. Button events, Single Use and locking behavior are unchanged.

Runtime and Editor compilation verified. Check pressed/halfway/released collision, weight release and rider movement in Unity using the authored sprite Physics Shapes.


## Pushing crates over small button edges

Pushable > Max Push Step Height defaults to 0.0625 world units (2 artwork pixels at 32 PPU). It enables small up/across/down steps only during player push/pull and only while the crate is grounded. Zero disables it. The player/crate shared-displacement clearance query includes the same step route so it does not reject the edge before the actual move can step over it. Ceiling, wall, landing slope and movement budget checks remain active. Ordinary gravity/prop movement and swimming pushes keep their existing no-step behavior. This does not change the player's own stepping height or enlarge the button collider. Stacked objects can still block upward clearance.

Runtime and Editor compilation checked. In Unity, verify pushing/pulling over low button rims, rejection of taller edges, and blocked clearance below a ceiling.


## Skin-aware pressure contacts

ButtonPressurePlate checks registered active SimulatedBody2D shapes with the same downward Probe used by movement. The nearest supporting collider must be the button surface and its normal must be walkable. The residual contact gap is Probe distance * normal.y - body.Skin, compared against Contact Tolerance. Probe already compensates for the inset cast box, so the skin is not counted twice. The bounds check uses XY Rects only, ignoring renderer Z depth as Physics2D does. The bounds check and probe reach include Skin + Contact Tolerance, converted to vertical distance for the maximum walkable slope. Bodies register on enable and unregister on disable; no runtime scene search is needed. Weight activation still uses Weight > PhysicsSettings.buttonWeightActivation.
