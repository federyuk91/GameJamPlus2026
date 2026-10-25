# Stage conclusions

StageEndManager sits on the same GameObject as GameManager and DialogueManager in Assets/Prefabs/GameLoop/Universe Rig.prefab. The universe scenes inherit the catalog. Standalone LoopScene, Levels/0 and Levels/1_WelcomeToUniverse have the same component next to their own GameManager.

## Responsibilities

- StageEndDefinition is a serializable data class containing deceased/survived status, DialogueCase, transition selection and optional duration override, notebook observations and alternative result lines.
- StageEndManager owns the eleven configurations and the conclusion sequence: claim one request, record results, pause, optionally wait for a local animation, play the transition, show the scientist communication and notebook. It delegates communication selection to DialogueManager and progression to GameManager.
- GameManager owns spawn, campaign progression and scene loading. LoopState remains its public gameplay status, updated internally by the conclusion coordinator.
- Damage, contact areas, physical properties, sound and local animations remain on the player, traps and props. Components only serialize a StageEndCase instead of a local TestEndEvent reference. None disables an optional conclusion.

## Cases

Starvation, Drowning, Poisoning, Crushing, ElectricShock, MouseTrap, Fall, Goal, CheeseGoal, PhotoGoal and Piranha. Existing dialogue selections, report alternatives, observations and durations were migrated without changing death or survival classifications. All crushing props share Crushing. Goal retains the exit image and Sense of direction observation.

The existing Player poison outcome was unassigned and fell back to starvation. It remains poisonEnding=None, preserving that behavior; the separate Poisoning definition is available for explicit assignment.

## Calls and animation ownership

Call StageEndManager.Request(case, source) for an immediate conclusion. It returns false when the stage is not playing or another ending already owns the sequence.

For animated deaths call TryBeginAnimated(case, source, out StageEndRequest). Keep the accepted request locally and call request.Complete() after the animation. Each request contains its manager and a unique identifier. Duplicate, stale or unrelated completions are ignored. MouseTrap and FallingCrushHazard complete their accepted request on interruption/despawn as well, avoiding a permanently paused stage.

The coordinator restores its owned Time.timeScale when loading, disabled or cancelled. Existing unscaled animation and transition timing is preserved.

## Migration and retirement

The Player and hazard/goal prefabs no longer contain TestEndEvent components. No UnityEvent relay was needed: the existing endings were called by gameplay components, not serialized EndTest UnityEvents. Collectible editor setup/audit tools were updated to enum fields.

TestEndEvent.cs and its .meta were removed with the user's explicit approval after migration. Operational code and assets no longer reference the retired class. Recovery snapshots in Assets/_Recovery are archival legacy data and have not been migrated.

No compilation, Play Mode or automated tests were run. In Unity verify ordinary and animated deaths, interrupted/despawned traps, goal success, quantity goals, notebook observations, and progression to the next stage.
