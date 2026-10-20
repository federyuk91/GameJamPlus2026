# Contextual hints and active input device

Current bindings and shared East selection are documented in [CurrentControls.md](CurrentControls.md); they supersede the historical bindings below. Gameplay interactions and scientist messages/photos/stage results use East on controllers. W / Up climbs ledges and attaches to ropes; camera lock uses RB / R1. SYSTEM hints put the large green-tinted command sprite in the avatar terminal and usage instructions in the text terminal.

LoopScene assigns GameplayInteractionAvailability to PlayerInputReader, CollectibleCollector, PlayerRopeBite and PlayerDigging through Inspector. Availability is evaluated after interaction ticks and emits Changed only on changes. Gameplay HUD displays hints in the bottom-right dialogue region without focus/click capture. Scientist dialogue takes priority throughout opening/display/closing; hints return afterward.

## Original bindings

| Action | Keyboard | Xbox / generic controller |
| --- | --- | --- |
| Reachable unlocked grounded crate | Hold E + direction · push / pull | Hold X + direction · push / pull |
| Cheese in pickup area | F · Eat cheese | X · Eat cheese |
| Green cheese in pickup area | F · Eat green cheese | X · Eat green cheese |
| Reachable cuttable rope | Hold F · Cut rope | Hold B · Cut rope |
| Reachable diggable terrain | Hold F + direction · Dig | Hold B + direction · Dig |

F collects; E remains crate-specific. X denotes controller West, not a new keyboard binding. Later shared-input updates are documented below.

Multiple hints may coexist, such as Eat cheese and Cut rope. Cutting and digging share input; cutting takes precedence and they are not advertised simultaneously on the same button. Availability reuses real collector targets, mouth reach/direction/intact segments/held ropes/sightlines and digging HasEarth/interaction constraints. Dig also requires directional input. Queries neither consume inputs nor execute actions. Pauses/test-ending screens hide gameplay hints.

InputDevicePrompts listens to Input System events without scene-object scans. Keyboard, mouse clicks/wheel select desktop prompts; sufficiently active gamepad buttons/sticks select controller prompts. Plain mouse motion/noisy controls are ignored. Disconnecting the active controller restores keyboard prompts. Layout-derived names show Square/Circle/Triangle on PlayStation rather than X/B/Y. Devices must be recognized as Gamepad.

White Kenney sprites in Assets/UI Toolkit/MainMenu/InputKenny are assigned through Assets/UI Toolkit/Gameplay/InputPromptIcons.asset to GameplayHudController. The catalog includes keyboard E/F/Enter, Xbox X/B/Y, PlayStation Square/Circle/Triangle and Switch Y/A/X. Hints use separate icon/text rows. Scientist/test-result buttons also show current icons. Source images stay white; USS applies --scientist-green = rgb(92, 255, 123) using -unity-background-image-tint-color. Missing sprites fall back to textual input names. The catalog uses white variants without color suffixes.

Dialogue buttons display Reveal, Next or Close. Device switching updates open dialogue without restarting typing or timeout. Narrative messages come from assigned dialogue data.

## Manual review

No automated tests or Play Mode were run. Check real proximity and obstructed ropes, overlapping collectible/rope hints, digging direction/cut priority, device switching in open dialogue, drift/mouse-motion rejection, disconnection and pause/resume.

Crate hints reuse PlayerCrateInteraction checks and require grounded, dry, non-sliding, non-climbing players. Tethered crates with disabled Pushable show no hint. The panel retains the dialogue's bottom anchoring across resolutions.

Communications later changed from Enter to F, including photos/results. UI consumes the press so it cannot simultaneously collect or start bite/dig; release F before a new interaction. Controller communications/photos/results later changed from North to West, matching collectibles (Xbox X, PlayStation Square, Switch Y). Icons and input consumption reflect that update.

## SYSTEM communications and shared selection

Hints now occupy the text terminal under SYSTEM using existing input icons. They persist while available, without timeout/Close. The separate avatar terminal is empty for system hints. Scientist communications retain precedence, arrays, typing, timeout and Reveal/Next/Close; valid hints return afterward.

GameplayInteractionAvailability.Current exposes selected actions for actual shared bindings rather than every competing action. Selection is also applied to PlayerInputReader and action components:

- Keyboard: F collects the selected target, excluding bite/dig on that F while a collectible is selected. E crate movement remains independent.
- Controller: West collects or presses buttons. East held with direction pushes/pulls crates. A reachable crate has priority over cut/dig on East. Rope grabbing remains on West.
- Cutting has priority over digging on their shared input.
- Nearest collectibles win. The press validates/collects the exact InteractionTarget used for the hint; an unreachable target is not unexpectedly replaced during the press.

Prompt device changes update selection. Without active GameplayInteractionAvailability, PlayerInputReader enables all actions for compatibility with other scenes. Manually check collectible/photo overlap, collectible/rope with F, collectible/crate on controller, simultaneous separate bindings, device changes and hints returning after dialogue.

### Priority without buffering

Communications reserve the terminal immediately on request, before coroutine/animation startup. Until CRT closing finishes, availability updates discard/clear SYSTEM hints instead of building hidden rows or queued tooltips. Completion recalculates overlaps/current actions without collecting during reading; only still-valid hints appear. Additional guarding handles callbacks starting another communication during recalculation.

Manually check same-frame communication/proximity, leaving an area during dialogue, staying nearby until completion and approaching another interactable before completion. No tests or Play Mode were run.

## Ledge climb confirmation

After grabbing a ledge from a jump, holding the approach direction no longer starts climbing automatically. Press F / gamepad West, or release the direction toward the ledge and press it again. Down still drops the mouse; Jump still launches away from the ledge. Ground-level obstacle climbs remain automatic when moving toward the obstacle.

While hanging from a ledge, ClimbLedge owns the interaction input and its SYSTEM hint. Collectibles, buttons and cutting/digging cannot use the same press. Interaction presses are queued for the physics step and consumed on climb confirmation.

While hanging, Jump + direction toward the ledge also confirms Climb Up, without requiring a direction release. Jump + the opposite direction still launches away; Jump with no horizontal direction keeps the same detach behavior. F / West and releasing/repressing the direction remain available.

Ledge jump update: Jump without a horizontal direction launches upward. Jump away from the ledge adds backward launch speed; Jump toward the ledge confirms Climb Up. Existing upward movement commands and interaction confirmation are preserved.
