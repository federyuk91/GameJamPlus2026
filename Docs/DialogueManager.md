# Central scientist dialogue

Open Assets/Prefabs/GameLoop/Universe Rig.prefab and select the GameObject containing GameManager. DialogueManager is on that same object, with a serialized HUD reference. GameManager references it explicitly.

DialogueMessage is a plain serializable class, not a MonoBehaviour or ScriptableObject. Each case contains a portrait, alternative messages (one random non-empty line per occurrence), and duration. {universe} is replaced at playback. An empty portrait uses the HUD's default scientist.

## Cases

Default Ending, Welcome, Starvation, Drowning, Low Oxygen, Cheese Collected, Poisoned, Cheese Goal, Photo Goal, Piranha, Fall Death, Goal, Electric Shock and Mouse Trap.

All original alternatives were preserved; the extra Cheese Collected alternative from the prefab is now also available to the old placed cheeses. Existing seven-second durations and explicit portraits are preserved. Cases that were already unused by gameplay remain available without introducing new triggers.

## Calls

- StageEndDefinition selects a DialogueCase. StageEndManager asks DialogueManager to choose the final communication and coordinates the ending sequence while preserving HUD priority rules.
- DialogueTrigger is only a lightweight local UnityEvent relay with a case and Play(). It contains no text, portrait or timing. It resolves the current GameManager's DialogueManager so standalone prefabs need no reference to a scene object.
- Welcome text, portrait and duration moved from GameplayHudController into DialogueManager. The HUD retains its Show Welcome On Start policy and continues to render typing, scrolling, CRT animations, voice and input.
- The text reported in the stage notebook is StageEndDefinition.resultMessages; it is distinct from the scientist's dialogue.

## Migration

DialogueMessagePlayer was retired. Its GUID is retained by DialogueTrigger for retained UnityEvent targets. Unneeded dialogue components and their overrides were removed; UnityEvents now target DialogueTrigger.Play. The collectible setup editor tool now assigns the CheeseGoal case and does not recreate local message data.

Universe scenes inherit the catalog from Universe Rig. LoopScene and the two older level scenes have a DialogueManager beside their existing standalone GameManager for compatibility. Recovery snapshots under Assets/_Recovery are archival and were not migrated.

This changes the script API: use DialogueManager.Play(DialogueCase), or DialogueTrigger.Play through UnityEvents. Existing public HUD presentation methods remain available.

No compilation, Play Mode or automated tests were run. In Unity check welcome, normal cheese, low-oxygen warning, starvation/drowning endings, goal, traps and the transition to the notebook.
