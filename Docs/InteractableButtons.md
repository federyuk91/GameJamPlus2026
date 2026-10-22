# Interactable buttons

Prefabs are in Assets/Prefabs/Buttons. All buttons support both input and weight;
there is no activation-mode selection. Interactable Button and its colored versions
retain their existing references. Pressure Button is also ready for the same use.

The mouse can press with the existing interaction input while overlapping the
cyan proximity trigger with a clear line of sight. It can also stand on the solid
Button Surface, just like a crate or another active SimulatedBody2D. Side proximity
and pickup triggers do not count as weight. A crate resting on it at startup
presses it normally. No Rigidbody2D or tags are required.

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

Advance Spawn Index off repeats the supplied index. When enabled, that index
starts a sequence advancing after each successful spawn, stopping after the last
entry. A different starting index resets the sequence. Invalid indices or empty
entries warn without advancing. ActivateButton and LockButton preserve the cursor.

For externally rearmed spawns, start locked and enable Advance Spawn Index. On
Pressed calls Spawn(0) and LockButton; another button calls ActivateButton before
each use. Single Use may replace the self-lock action.

Existing scene prefab instances inherit the solid surface. No gameplay scenes
were edited or reloaded. Check mouse descent, crate removal, door events,
Single Use and reactivation in Play Mode.
