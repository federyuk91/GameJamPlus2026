# Checkpoints

Prefab: Assets/Prefabs/GameLoop/Checkpoint.prefab. No scene was modified.

Place instances along the route and resize their trigger BoxCollider2D.
Entering records the destination; it does not complete the stage. On Activated
is an optional UnityEvent. Leaving and entering again can reactivate a checkpoint.
The most recently entered checkpoint wins.

Target Scene dropdown:
- This: reload the checkpoint's own scene and spawn at the green Scene handle.
  Respawn Offset is relative to the checkpoint; the handle marks the player pivot,
  not its feet. Show Gizmos toggles the cyan activation area and green destination.
- Enabled build scenes: labels show the actual enabled-scene build index and name.
  The destination scene uses its Game Manager's assigned active Spawn Points at
  random. No local respawn handle appears for these choices.

Scenes must be saved and enabled in Build Profiles. The Inspector offers Add This
Scene To Build for a local checkpoint in a scene that is not yet enabled. Scene
paths are stored instead of build indices, so reordering the build list does not
change a checkpoint destination. Renaming/moving a scene requires selecting it
again; disabled/missing destinations show a warning.

After the normal flash, scientist comment and Stage complete screen, Continue uses
the recorded destination. A local checkpoint persists over reloads of its scene;
entering another gameplay scene clears the old checkpoint and uses normal initial
spawning. Starting a new game also clears it. Collected counters and Universe
advance as before. Universe zero still takes precedence and loads EndGame.

The destination scene must have a configured Game Manager, Player and Gameplay HUD.
Assign its Spawn Points for cross-level entry. No Rigidbody2D is required: contact
uses the same transform-body overlap polling as other gameplay zones.
