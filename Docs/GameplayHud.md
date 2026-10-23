# Gameplay HUD

## Views and layout

`Assets/Prefabs/UI/Gameplay HUD.prefab` uses `GameplayHud.uxml`, `GameplayHud.uss` and `GameplayHudPanelSettings.asset` in `Assets/UI Toolkit/Gameplay`.

Edit layout, colors, fonts and imagery in UI Builder outside Play Mode. Keep bound element names intact. The current HUD contains independent panels:

- `timer-anchor`: elapsed time, top left.
- `resource-counters`: Cheese, GreenCheese, Knowledge and Photo, top right.
- `resource-dashboard`: health, satiety, conditional oxygen and active status icons, bottom right.
- `scientist-screen`: integrated portrait and communication text, bottom left inside the bottom band.
- `interaction-hints-anchor` / `interaction-hints`: centered contextual tooltips, 8 logical pixels from the bottom, minimum width 110 with content-driven expansion.

The old separate avatar monitor, SYSTEM key display and plate-progress counter sequence have been removed. Tooltips coexist with ordinary scientist communications and have no transcript, timeout, portrait or CRT animation. Pause, photos and stage-ending overlays hide contextual hints.

## Responsibilities

`GameplayHudController` binds gameplay events, device prompts, counters, timer, overlays and pause controls. It clones PanelSettings and matches PixelPerfectViewport's integer scale and frame. It updates viewport geometry and timer anchoring; it does not reposition communications or resource panels.

`ScientistCommentView` manages message sequences, typing, timeout, speech portrait frames and CRT transitions. It animates a separate clipping window around `scientist-display`, preserving authored text and portrait geometry. `ScientistEndingLayout` temporarily enlarges the terminal for stage-ending communications and restores the original layout afterward.

`SatietyHud` binds health, satiety, oxygen and statuses. Oxygen stays visible underwater and until fully recovered. The status row exists only while poisoned or electrically charged. A resource at one remaining point pulses gently. Screen flashes and camera-shake behavior are unchanged.

## Counters

Indices: 0 Cheese, 1 GreenCheese, 2 Knowledge, 3 Photo. Counts follow GameSession totals across universes of the same game; New Game resets them. Zero counts remain visible. A count increase briefly highlights its slot without changing the source image or its normal layout size.

`Counter Icons` overrides an authored UXML sprite. Empty slots retain the authored sprite, then use `Default Counter Icon` as a fallback. Knowledge uses the procedural bulb when no override is assigned. Icon dimensions belong to USS, not sprite source pixels.

`SetCounter(int index, int value)` updates a displayed count; pickup events resynchronize it with session progress. The obsolete `Cheese Counter Frames` field and prefab data were removed; the cheese icon is assigned in UXML.

## Communications

`ShowScientistComment(string, Sprite, float)` and the string-array overload replace the current communication. `HideScientistComment()` interrupts it. Welcome text, portrait, duration and startup visibility remain Inspector settings.

Typing speed comes from ScientistSpeechSettings, with a 30-character/second fallback. Message timeout includes typing and uses unscaled time; duration is clamped to 1–30 seconds. Reveal / Next / Close use the active input device. Character events drive scientist voice and typing audio. Manual scrolling suspends automatic following for the current message.

During ordinary gameplay, available interactions take priority over advancing
communication when they share the pressed control. Availability is refreshed at
input time and respects keyboard/gamepad rebindings. For example, F eats nearby
cheese or gnaws a rope instead of advancing the message. A control assigned only
to advancing can still advance it. Photo overlays, stage-ending communications
and results retain their modal input behavior.

`ResetTimer()`, `SetTimerRunning(bool)` and `ElapsedSeconds` control elapsed game time independently of communications.

## Shared controls and icons

See `ControlsUI.md` for independently editable control pages and rebinding. `Assets/Resources/InputPrompts.asset` and `InputPromptCatalog` supply the Kenney glyphs used by InputPromptElement. The previous InputPromptIcons ScriptableObject and unreferenced asset have been removed; its source file retains the shared enums.

## Verification

C# compilation and static UXML/reference/style checks cover the cleanup. In Unity, review communication transitions, tooltip coexistence, resource/status visibility, pickups and pause at the intended display resolutions.

## Stage-ending communication layout

The enlarged terminal uses `.scientist-ending` rules in GameplayHud.uss. Text occupies a wide central region with 40/24 logical pixel side margins within the display viewport. The small portrait remains at the bottom left. The full-width transcript remains scrollable for long messages.

The Next button moves into the authored `scientist-ending-footer`, outside and below the monitor, aligned with its right edge. The footer is layered above the white stage-ending cover, while retaining the original button's input handlers and icon. ScientistEndingLayout restores the original parent, index and inline layout when the enlarged communication closes.

Edit `.scientist-ending .scientist-content`, `.scientist-ending .scientist-transcript` and `.scientist-ending-footer` for this presentation. Compact gameplay communication geometry remains independent.

## Subject observations

The result notebook now contains seven observations:

- Enjoys the food: eat normal or green cheese.
- Gets regular exercise: enter a wheel.
- Loves the pool: remain in water continuously for 20 seconds. PlayerSwimming's Pool Observation Duration is editable; leaving water or disabling the player resets the unfinished interval. Pause does not consume time.
- Extended stay: finish a stage after more than 60 seconds on its timer.
- Little mechanic: trigger a spring mouse trap, whether or not the hit is lethal. Replaces the former gardening row.
- Very curious: collect three photos during the same subject's surviving sequence of stages.
- Electrician: receive at least one successful electrical shock.

GameSession preserves acquired observations and photo progress when a stage ends with Subject Died disabled. On death, the current Results screen still shows that subject's observations; advancing clears those observations and photo progress for the next subject. New Game also clears them. Per-stage collectible counts and the session-wide HUD totals retain their existing reset rules. The seven rows and captions belong to GameplayHud.uxml; TestResultView binds their flags.

## Photo trial-ending cadence

CollectiblePhoto ends a nonlethal trial only on a positive multiple of Photos To End Test. The default interval of 3 uses the session-wide HUD quantity: 3, 6, 9, and so on. Totals 4 and 5 no longer trigger an ending. Each photo continuation is handled only once. This cadence is independent of the Very curious observation, which is earned after three photos for the current subject and retained until that subject dies.
