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

Kenney input prompts use `Assets/Resources/InputPrompts.asset` through InputPromptCatalog and InputPromptElement. The active device and current bindings determine each glyph; missing artwork uses a text fallback. InputPrompts.uss defines shared glyph presentation. The obsolete InputPromptIcons asset is no longer used.

Dialogue buttons display Reveal, Next or Close. Device switching updates open dialogue without restarting typing or timeout. Narrative messages come from assigned dialogue data.

## Manual review

No automated tests or Play Mode were run. Check real proximity and obstructed ropes, overlapping collectible/rope hints, digging direction/cut priority, device switching in open dialogue, drift/mouse-motion rejection, disconnection and pause/resume.

Crate hints reuse PlayerCrateInteraction checks and require grounded, dry, non-sliding, non-climbing players. Tethered crates with disabled Pushable show no hint. The panel retains the dialogue's bottom anchoring across resolutions.

Communications later changed from Enter to F, including photos/results. UI consumes the press so it cannot simultaneously collect or start bite/dig; release F before a new interaction. Controller communications/photos/results later changed from North to West, matching collectibles (Xbox X, PlayStation Square, Switch Y). Icons and input consumption reflect that update.

## Separate contextual hints

`interaction-hints` is an independent HUD panel under `interaction-hints-anchor`, centered at the bottom with an 8-pixel margin. Its minimum width is 110 logical pixels and it expands for longer hints up to 46% of the frame width, then wraps. Input combinations and descriptions occupy separate columns. No duplicate command icons are rendered in the scientist portrait area.

Hints are rebuilt from current availability, without a queue. Ordinary scientist communications do not suppress them. Pause, photos and stage-ending screens still hide them. Availability callbacks are rechecked before rendering in case they opened a blocking overlay.

GameplayInteractionAvailability remains responsible for selecting valid targets and resolving shared-input conflicts; this presentation change does not change interaction rules. Actual bindings are resolved through PlayerControlBindings and InputDevicePrompts rather than the historical hardcoded keys listed in earlier revisions.

## Ledge climb confirmation

After grabbing a ledge from a jump, holding the approach direction no longer starts climbing automatically. Press F / gamepad West, or release the direction toward the ledge and press it again. Down still drops the mouse; Jump still launches away from the ledge. Ground-level obstacle climbs remain automatic when moving toward the obstacle.

While hanging from a ledge, ClimbLedge owns the interaction input and its SYSTEM hint. Collectibles, buttons and cutting/digging cannot use the same press. Interaction presses are queued for the physics step and consumed on climb confirmation.

While hanging, Jump + direction toward the ledge also confirms Climb Up, without requiring a direction release. Jump + the opposite direction still launches away; Jump with no horizontal direction keeps the same detach behavior. F / West and releasing/repressing the direction remain available.

Ledge jump update: Jump without a horizontal direction launches upward. Jump away from the ledge adds backward launch speed; Jump toward the ledge confirms Climb Up. Existing upward movement commands and interaction confirmation are preserved.
