# Player prefab

`Assets/Prefabs/Characters/Player.prefab` is the common player used by gameplay scenes, directly or nested in `Universe Rig`.

## Audio hierarchy

- Player / Audio / Footsteps
- Player / Audio / Voices
- Player / Audio / Bubbles
- Player / Audio / Swimming

The four AudioSources retain their original component IDs, settings and serialized references. Presentation behaviours stay on the Player root and reference their own source explicitly. PlayerFootsteps requires PlayerMovement only; no root AudioSource is required or searched for.

## Scene override audit

Gameplay tuning now inherits the Player prefab: maxSpeed 26, skin 0.005, weight 0.03 and rope-bite mouthOffset (0.2, -0.1).

Removed maxSpeed 30 from LoopScene; skin 0.01, weight 0.05 and maxSpeed 25 from Levels/0 and Levels/1_WelcomeToUniverse; the mouthOffset.x override and RopeBiteArea transform offsets from Levels/0. Placement and scene terrain references remain local. The nested Player in Universe Rig has placement/name overrides only; no further overrides of its exposed component aliases were found in universe scenes.

BalloonAscent remains a cinematic exception: satiety and oxygen are disabled and initialRope references the scene balloon rope. Recovery snapshots in Assets/_Recovery are not migrated. Audio hierarchy changes are inherited by all prefab instances, including the cinematic and recovery snapshots.

Scientist communication data is now centralized on DialogueManager in Universe Rig; see DialogueManager.md. StageEndManager in Universe Rig now owns the ending definitions; the Player only selects StageEndCase values and keeps a lightweight DialogueTrigger for the low-oxygen warning. See StageEndManager.md.

No compilation, Play Mode or automated tests were run. Check footsteps, voices, bubbles and swimming audio in Unity, including simultaneous playback and independent stopping.
