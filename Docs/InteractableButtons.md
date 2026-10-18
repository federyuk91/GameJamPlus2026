# Interactable Button

Drag Assets/Prefabs/Interactable Button.prefab into the scene. The pivot is at the button's base. Three PressureButton_Sheet frames keep the base aligned while pressing.

The mouse must overlap Interaction Area and have a clear line to the button. Press F or gamepad West (Xbox X / PlayStation Square). Walking over it does not press it automatically.

Add UnityEvent calls to Interactable Button → On Pressed. The event fires once per valid press. Animation Step Duration controls the brief animation. Single Use prevents further presses and leaves the button down; otherwise it can be reused after the animation.

Box Collider 2D → Size/Offset controls proximity, shown in cyan when Show Gizmos is enabled. It is a trigger and does not block movement. No Rigidbody2D, tags or manual button lists are needed.

PlayerButtonInteraction is assigned in the Player prefab and LoopScene's GameplayInteractionAvailability. When a collectible and button share the input, the nearest wins; collectibles retain priority at equal distances. The HUD displays Press button and the correct input. Existing crate, bite and dig priorities remain in effect.

Check On Pressed, obstructed access, nearby buttons, adjacent collectibles, single use and reuse in Play Mode.
