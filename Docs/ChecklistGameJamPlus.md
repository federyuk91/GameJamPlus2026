# Project checklist against “Game Jam Plus Themes”

Analysis dated October 17, 2026, of the working directory at that time, including uncommitted changes. No automated tests, Play Mode checks or builds were run. This is a historical audit: its open tasks do not describe the project's current implementation status.

## Comparison scope

Updated after reading the [shared conversation](https://chatgpt.com/share/6ad3e70c-8f04-83eb-9dba-ebaa0723904e), including the opening discussion and assistant responses. Generated images and the sketch require a separate visual comparison; this audit considers text and project files.

The user's decisions were: a mouse in a maze with buttons; a replacement subject after each death; death presented as completion; different experiments between trials; a final revelation of deaths; welcoming pixel-art utopia inspired by BioShock and Experiment 25; SNES-style jazz/blues with dystopian melancholy; and five specific outcomes. Unconfirmed assistant suggestions remain optional.

Unchecked items mean unfinished work or explicit manual verification. Additional polish items come from the project and are not requirements reconstructed from the conversation.

## Foundation already present in the files

- [x] Universe-10 → Universe-1 → EndGame loop, resetting the session for New Game.
- [x] End-of-test sequence: pause, white flash, scientist comment, timed result and progression.
- [x] Configurable `TestEndEvent` comments, result messages and subject-death flag.
- [x] Report showing completed tests, subjects used, deceased, recovered and completion percentage.
- [x] Three endings selected for 0, 1–9 and at least 10 deaths.
- [x] Piranha patrol, vision cone, underwater pursuit and return to patrol.
- [x] Movement, swimming, ladders, digging, crates and ropes.
- [x] Green cheese ends the test; the photo ends it after confirmation.
- [x] Gameplay music assigned in LoopScene, with volume controls.
- [x] MainMenu, LoopScene and EndGame included in the build scene list.
- [x] Camera configured at 640×360 and 32 PPU, consistent with the mouse proportions.
- [x] Report outcome based on completed tests: it can show 100% success even when all subjects died, matching the intended twist.

“Present” means found in code and/or serialized data, not verified in gameplay.

## Priority 0 — Complete the core concept

- [ ] **World buttons:** implement mouse-operated buttons/pressure plates with assignable state and events. `PressureButton_Sheet.png` exists, but no gameplay button components were found; UI buttons do not fulfill the jam modifier.
- [ ] **Decision consequences:** connect buttons to doors, routes or hazards and implement at least one complete puzzle with understandable alternatives. Doors, reversals, bait and sacrifices were examples, not a requirement to implement all of them.
- [ ] **Different experiments:** vary layouts, button configurations or trial conditions between universes. The current Game Manager reloads LoopScene and changes its number; room generation runs only in the Editor. Each trial must be a different experiment, but procedural generation is optional.
- [ ] **Consistent deception:** ensure deaths and survival receive the same positive treatment without revealing deaths through text or effects. The shared UI flow exists; the cage survival route is missing.
- [ ] **Subject replacement:** ensure the next trial feels like progression and the final report explains the cost in subjects. Reloading already recreates the player and counts subjects; assess the narrative with someone unfamiliar with the twist.
- [ ] **Blind playtest:** let someone play without explaining the concept and observe what they interpret as success and when they understand the report.

## Priority 1 — Complete the five discussed outcomes

- [x] **Piranha:** lethal contact uses `LethalContact`, connected to `TestEndEvent` in LoopScene. This connection was found during the follow-up audit.
- [x] Piranha result uses a variant of the proposed sentence: “The subject has socialized with the fish with remarkable enthusiasm.” `Subject Died` is enabled, with appropriate scientist comments.
- [ ] **Electricity:** implement a lethal electrical zone/trap and connect it to test completion. `ElettricTrap.png` and four ElectricArc sounds exist, but no gameplay logic was found.
- [ ] Configure the electricity result: “The subject has discovered electricity”, with `Subject Died` enabled.
- [ ] **Spring trap:** implement activation, snapping and test completion. `MouseTrap_Sheet.png` exists, but no trap component was found.
- [ ] Configure the trap result: “The subject shows an interest in mechanics”, with `Subject Died` enabled.
- [ ] **Drowning:** define and detect drowning, then end the test. Swimming and water volumes do not currently handle immersion deaths.
- [ ] Configure the drowning result: “The subject has proven to be extremely thirsty”, with `Subject Died` enabled.
- [ ] **Return to cage:** create a maze exit entering the cage and connect completion to the Game Manager. No configured subject-recovery event was found.
- [ ] Configure the cage result: “The subject has returned to the nest”, with `Subject Died` disabled to count a recovery.
- [ ] Place all five outcomes within the playable route, each with an appropriate scientist comment.

The UI already has the “TEST COMPLETE!” heading. Put these sentences in `Result Messages` without repeating the heading. The piranha variant exists; the other four sentences were not found in scripts, prefabs or scenes during this audit.

Technical constraint: `SimulatedBody2D` moves the player's Transform through physics queries without Rigidbody2D. Hazard detection must support this system; do not assume adding `OnTriggerEnter2D` is sufficient.

## Priority 2 — Requested music and atmosphere

- [ ] **Listening review:** check whether `Assets/Sounds/Music/Music_Gameplay.wav` matches blues/jazz influences, SNES sound and melancholy with an experimental/dystopian undertone. File analysis confirms its presence and assignment, not its musical effect.
- [ ] Assign `Assets/Sounds/Music/Music_Ending.wav` to EndGame. The file exists, but its GUID was not referenced elsewhere in the project.
- [ ] **In-game listening:** check looping, balance against footsteps/effects and transitions between tests. `BackgroundMusic` is recreated with LoopScene, restarting the track on each load.
- [ ] **Narrative review:** assess music, dialogue and results together; bureaucratic humor should support the unsettling undertone.
- [ ] **Visual review:** compare the level with the chosen welcoming utopia, mouse-centered pixel art and BioShock/Experiment 25 references. Having tiles and sprites does not establish the intended effect.

The assistant's musical response suggests reassuring harmonies with melancholy resolutions and subtle tension. BPM, mood percentages and instruments are creative suggestions, not mandatory criteria. Strudel is a prototyping tool, not a Unity dependency.

## Priority 3 — Additional project polish

These items go beyond the conversation's explicit requirements.

- [ ] Replace the no-deaths and no-survivors ending placeholders. EndGame still uses `Ending_NoDeaths_Placeholder.png` and `Ending_AllSubjectsLost_Placeholder.png`; the intermediate ending uses `Assets/Art/EndGame1.png`.
- [ ] Assign the scientist portrait to welcome/dialogue views. Welcome and Cheese/CheeseGreen use the cheese sprite; the photo prefab has no avatar.
- [ ] Decide whether the photo should count as a death. `Photo Collectible` currently has `Subject Died = true` and ends the test after Next. Disable the flag if it should recover the subject.
- [ ] **Level review:** finalize routes, hazards, collectibles and interaction readability. Implemented systems do not prove that the level is complete or balanced.
- [ ] **Menu artwork review:** identify assets still needing replacement. Several names include Placeholder, but some have local changes; names alone do not prove they remain temporary.
- [ ] Unify button, hint and text language; Italian and English labels currently coexist.

## Optional ideas requiring a scope decision

These are not mandatory missing features. Checkboxes record a scope decision, including a decision to exclude an idea.

- [ ] Decide whether to use persistent bodies, clones or action echoes. The user expressed interest in copies; the assistant suggested narrative replacement and physical persistence. Previous subjects currently do not remain in the level.
- [ ] Decide how to express Together Is Better: collective subject data or mechanical cooperation between attempts. The latter needs additional systems; the former needs clear narrative communication.
- [ ] Decide whether to add progressive clues: subject serials, notes, observers or previous-test marks. Universe numbers already exist, but do not automatically identify subjects.
- [ ] Decide whether to add a shared completion fanfare. The conversation suggests it; gameplay music currently continues through results.
- [ ] Decide whether to add average time and button counts. The session records subject/test counts; the timer is shown per trial and not aggregated in the report.

Other assistant-proposed deaths (laser, acid, press, freezing, etc.), the illustrative reference to 12 trials and suggested names are not additional requirements. The project's ten trials and three endings can remain.

## Manual Unity and build checks

- [ ] Try each of the five outcomes: exactly one completion, correct comments/results and correct death/recovery counts.
- [ ] Try buttons and puzzles across trials: correct effects, no unintended blocked routes and variations actually loaded.
- [ ] Complete ten tests from Universe-10 through EndGame, without skips or double progression; New Game resets data.
- [ ] Check all three endings with zero, some and ten deaths, comparing the report and selected image.
- [ ] Check green cheese and photo, including pause, confirmation and subject counts.
- [ ] Try keyboard, mouse and gamepad in dialogue, results, menus and EndGame, including repeated input during transitions.
- [ ] Check HUD, long text and images at 640×360, 1280×720, 1920×1080 and 1366×768.
- [ ] Play the complete jam build: scenes, controls, audio and return to menu.

## Main references

- `Assets/Scripts/GameLoop/GameManager.cs`, `GameSession.cs`, `TestEndEvent.cs`.
- `Assets/Scripts/GameLoop/LethalContact.cs` for the piranha's lethal contact.
- `Assets/Scripts/Enemies/PiranhaChase.cs`, `PiranhaPatrol.cs`.
- `Assets/Scripts/Player/PlayerSwimming.cs`, `SimulatedBody2D.cs`.
- `Assets/Prefabs/CheeseGreen.prefab`, `Assets/Prefabs/Photo Collectible.prefab`.
- `Assets/Scenes/LoopScene.unity`, `Assets/Scenes/EndGame.unity`.
- `Docs/GameLoop.md`, `Docs/EndGame.md`, `Docs/Rooms.md`.
