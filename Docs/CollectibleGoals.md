# Collectible quantities and persistence IDs

The cheese goal lives under Player > Cheese Collection Goal, with a CollectibleTestGoal wired to the player's CollectibleCollector and a non-lethal TestEndEvent. Required Count is 10. It listens to quantity changes and finishes in LateUpdate, after ordinary pickup events, to avoid an ordinary cheese dialogue replacing the ending.

Photo Collectible > CollectiblePhoto checks Photos To End Test (3) after closing the photograph. It reads the quantity directly from GameSession. Collection IDs only track which placed photos remain collected between universes; they never determine whether the threshold is reached. Missing Curiosity Ending now emits an explicit error instead of silently doing nothing.

Both components expose Use Session Total. On is the configured default and counts the same persistent session total shown by the HUD. Off is available for goals that intentionally count only the current level/subject. The current level quantity resets at GameSession.Advance; the HUD total survives between universes of the same run. Photo and cheese counts are separate; green cheese does not count as ordinary cheese.

Tools > Gameplay > Repair Collectible Goal Prefabs wires the cheese goal and verifies the Photo type and Play event in the photo prefab. Tools > Gameplay > Repair Collectible IDs In Scenes repairs blank or duplicate IDs in placed remains/photo components, verifies photo callbacks and keeps snapshots in Recovery. It excludes recovered scene copies. Scenes already open remain dirty without automatic saving; other scenes are backed up before saving. The same ID check runs automatically when a scene is saved, so duplicate/newly placed collectibles receive their own persistent IDs. Source prefab IDs may remain blank: the ID belongs to each placed instance.

Tools > Gameplay > Audit Collectible Scene Setup writes Temp/CollectibleSceneAudit.txt for loaded scenes, listing types, IDs, pickup callbacks, photo thresholds and goal references. Repair results are in Temp/CollectibleIdRepair-report.txt and Temp/CollectibleGoalSetup-report.txt. These are configuration checks, not automated gameplay tests.
