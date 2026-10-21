# Interactable Button

Drag Assets/Prefabs/Interactables/Buttons/Interactable Button.prefab into the scene. The pivot is at the button's base. Three PressureButton_Sheet frames keep the base aligned while pressing.

The mouse must overlap Interaction Area and have a clear line to the button. Press F or gamepad West (Xbox X / PlayStation Square). Walking over it does not press it automatically.

Add UnityEvent calls to Interactable Button → On Pressed. The event fires once per valid press. Animation Step Duration controls the brief animation. Single Use prevents further presses and leaves the button down; otherwise it can be reused after the animation.

Box Collider 2D → Size/Offset controls proximity, shown in cyan when Show Gizmos is enabled. It is a trigger and does not block movement. No Rigidbody2D, tags or manual button lists are needed.

PlayerButtonInteraction is assigned in the Player prefab and LoopScene's GameplayInteractionAvailability. When a collectible and button share the input, the nearest wins; collectibles retain priority at equal distances. The HUD displays Press button and the correct input. Existing crate, bite and dig priorities remain in effect.

Check On Pressed, obstructed access, nearby buttons, adjacent collectibles, single use and reuse in Play Mode.

## Spawn action

All button prefabs expose Spawn Prefabs, Spawn Offset and Advance Spawn Index. Populate the list, then add the button component to its own On Pressed event and choose InteractableButton.Spawn(int), supplying a zero-based starting index. Existing events are preserved; spawning only occurs when this method is invoked.

With Advance Spawn Index disabled, each call repeats the supplied element. With it enabled, repeated calls with the same starting index advance after each successful spawn and stop at the end of the list. A different starting index begins a new sequence. Empty entries or invalid indices do not instantiate or advance; a warning identifies the button. Each button instance keeps its own runtime cursor. The object uses its prefab rotation and spawns without a parent.

Select a button and enable Show Gizmos. A green marker and Spawn Point position handle locate the spawn point; dragging the handle updates the local Spawn Offset with normal editor undo/prefab override support. The position follows the button transform. No compilation or Play Mode was run; manually check repeated and sequential spawning in Unity.


## Initially locked buttons

Enable Starts Locked on an instance or prefab to spawn it pressed and unavailable. From any UnityEvent, assign that button and select InteractableButton.ActivateButton(). Activation releases and rearms it without invoking On Pressed. Single Use permits one press per activation, then locks the button again. Reactivation cancels any unfinished press animation so it cannot overwrite the released appearance. Disabling/re-enabling preserves the runtime lock state. Existing buttons remain unlocked by default.

For alternating buttons, enable Single Use on both, leave the first unlocked and enable Starts Locked on the second. Wire each On Pressed event to the other button's ActivateButton(). Each press locks its own button and rearms the other.

LockButton() is also available to UnityEvents independently of Single Use. For a controlled spawn sequence, enable Starts Locked and Advance Spawn Index, disable Single Use, and add Spawn(0) followed by LockButton() on the same button to On Pressed. Another button calls ActivateButton() to allow the next spawn. Activation preserves the spawn cursor; after the last list entry no further objects spawn. LockButton() immediately shows the pressed sprite and cancels any unfinished press animation.
