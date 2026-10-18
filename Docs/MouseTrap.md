# Mouse Trap

Drag Assets/Prefabs/Mouse Trap.prefab into the scene. Box Collider 2D is a trigger on the base; Size and Offset control the lethal area. No Rigidbody2D or tags are needed. Player Collider is resolved through GameManager when not directly assigned.

Contact reserves the test ending and pauses gameplay. MouseTrap plays all four MouseTrap_Sheet frames in real time, holds the last for Closed Hold Duration, then signals completion to GameManager. Only then does the white flash begin, followed by comment, result and normal universe progression. No estimated delay replaces actual animation completion.

- Frame Duration: initially 0.08 seconds per frame.
- Closed Hold Duration: initially 0.25 seconds on the closed trap.
- Show Gizmos: red lethal-area outline in the Editor.
- Dialogue Message Player → Messages: “That should not have been there! Clean room seven.”
- Test End Event → Result Messages: test results; Subject Died is enabled.

During DeathAnimation, GameManager prevents other lethal events from replacing the current one. Disabling the trap during animation still releases the transition to the flash. Recovery Zone excludes traps from recovery destinations.

Check grounded contact, all four frames before the flash, overlapping hazards and level restart in Play Mode.
