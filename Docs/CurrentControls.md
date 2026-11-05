# Current gameplay controls

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Move | WASD / arrows | Left stick / D-pad |
| Jump / swim stroke | Space | South (Xbox A / PlayStation Cross) |
| Jump off rope toward chosen side | Space + A/D or Left/Right | South + horizontal left stick / D-pad |
| Attach to rope | W / Up | Left stick up / D-pad up |
| Climb rope / ladder | W/S / Up/Down | Left stick / D-pad vertical |
| Push / pull crate | Hold E + direction | Hold East + direction |
| Eat cheese | F | North |
| Collect photo / press button / enter or exit wheel / Snake Box | E | East (Xbox B / PlayStation Circle) |
| Cut rope | Hold F | Hold North |
| Dig | Hold E + direction | Hold East + direction |
| Sprint | Hold Left Shift | Hold West |
| Horizontal water impulse | Press Left Shift | Press West |
| Climb ledge | W / Up, or Space toward ledge | Up, or South toward ledge |
| Jump vertically from a blocked ledge | Space + W / Up | South + Up |
| Drop from ledge | S / Down | Down |
| Jump off ledge | Space away from ledge | South (away or no horizontal direction) |
| Toggle camera height lock | C | Right shoulder (RB / R1) |
| Pause | Esc | Start / Options |

Menus retain Space / South to confirm and Esc / East to cancel. Scientist messages (reveal/next/close), photo close and stage Continue use F / East. Scrolling text, controls and the notebook uses Page Up/Down, mouse wheel or the right stick vertically. The shared press is consumed before returning to gameplay so closing a message cannot also collect, cut or dig.

Main menu and pause navigation use WASD/arrows or left stick/D-pad. A/D or Left/Right adjust the selected volume slider. The main menu assigns explicit navigation, confirm and back actions to its InputSystemUIInputModule while enabled and restores the previous actions on disable. Its device-aware footer includes WASD. On the pause main page, Esc, East and Start/Options resume; Esc/East return from subpages to the pause main page, while Start/Options resumes directly.

Front Paws selects the nearest collectible, button, wheel or Snake Box before pushing or digging. An active dig keeps the hand action until released. Mouth (eating or rope cutting) is independent. Tail is held for land sprint and tapped for a horizontal water impulse. Body-action bindings can be reassigned in the controls menu. While inside a Snake Box, hands or jump request exit; wheel exit uses hands. Camera height control is suppressed in both. Figures in the table are the defaults.

The controls page follows device names and scrolls with the right stick, Page Up/Down or mouse wheel so the expanded instructions fit at 640 × 360. Direction hints use W / D-pad-up sprites from the existing white icon set, tinted green.

Current input glyphs come from `Assets/Resources/InputPrompts.asset` through InputPromptCatalog. InputPromptElement resolves the active device and configured binding; the old InputPromptIcons ScriptableObject and partial-sprite mappings have been removed. See ControlsUI.md for current body-action bindings and editable control views.

This replaces the previous E / West rope grab, West collection, F / West ledge climb and right-stick-click camera lock. Ledge climb no longer triggers by merely releasing and reapplying the horizontal direction. Review shared East actions, rope entry and ledge jump directions in Unity; no compilation or Play Mode tests were run.
