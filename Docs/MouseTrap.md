# Mouse Trap

Drag Assets/Prefabs/Props/Mouse Trap.prefab into the scene. Box Collider 2D is a trigger on the base; Size and Offset control the lethal area. No Rigidbody2D or tags are needed. Player Collider is resolved through GameManager when not directly assigned.

Contact reserves the test ending and pauses gameplay. MouseTrap plays all four MouseTrap_Sheet frames in real time, holds the last for Closed Hold Duration, then signals completion to GameManager. Only then does the white flash begin, followed by comment, result and normal universe progression. No estimated delay replaces actual animation completion.

- Frame Duration: initially 0.08 seconds per frame.
- Closed Hold Duration: initially 0.25 seconds on the closed trap.
- Show Gizmos: red lethal-area outline in the Editor.
- Dialogue Message Player → Messages: “That should not have been there! Clean room seven.”
- Test End Event → Result Messages: test results; Subject Died is enabled.

During DeathAnimation, GameManager prevents other lethal events from replacing the current one. Disabling the trap during animation still releases the transition to the flash. Recovery Zone excludes traps from recovery destinations.

Check grounded contact, all four frames before the flash, overlapping hazards and level restart in Play Mode.

## Custom prop physics

Mouse Trap now uses the same composition as cheese collectibles: SimulatedBody2D + GroundContact2D + PropsMotion, with Follow Support enabled. Gravity Scale 1, Max Speed 8, Air Drag 0, Water Drag 5, Ground Friction 1, Skin 0.002 and Weight 0.01 match Cheese. There is no Rigidbody2D, Pushable or buoyancy component.

A separate trigger BoxCollider2D is assigned as the body's Shape: width 0.9375, height 0.125, offset (0, 0.0625), placing its bottom at the trap's visual base. The existing lethal Contact Area remains unchanged and explicitly assigned. Movement ignores player colliders, so the trap settles on solid ground and moving props without becoming a platform or being blocked by the player. Its closing animation, audio and test-ending events remain unchanged. Existing prefab instances inherit the physics components.

Manual Unity checks: spawn above ground, settle on a moving crate, trigger the trap before and after settling. No compilation or Play Mode was run.
