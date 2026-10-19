# Current gameplay controls

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Move | WASD / arrows | Left stick / D-pad |
| Jump / swim stroke | Space | South (Xbox A / PlayStation Cross) |
| Jump off rope toward chosen side | Space + A/D or Left/Right | South + horizontal left stick / D-pad |
| Attach to rope | W / Up | Left stick up / D-pad up |
| Climb rope / ladder | W/S / Up/Down | Left stick / D-pad vertical |
| Push / pull crate | Hold E + direction | Hold East + direction |
| Eat cheese / collect photo / press button / enter or exit wheel | F | East (Xbox B / PlayStation Circle) |
| Cut rope | Hold F | Hold East |
| Dig | Hold F + direction | Hold East + direction |
| Climb ledge | W / Up, or Space toward ledge | Up, or South toward ledge |
| Jump vertically from a blocked ledge | Space + W / Up | South + Up |
| Drop from ledge | S / Down | Down |
| Jump off ledge | Space away from ledge | South (away or no horizontal direction) |
| Toggle camera height lock | C | Right shoulder (RB / R1) |
| Pause | Esc | Start / Options |

Menus retain Space / South to confirm and Esc / East to cancel. Scientist messages (reveal/next/close), photo close and stage Continue use F / East. Scrolling text, controls and the notebook uses Page Up/Down, mouse wheel or the right stick vertically. The shared press is consumed before returning to gameplay so closing a message cannot also collect, cut or dig.

Main menu and pause navigation use WASD/arrows or left stick/D-pad. A/D or Left/Right adjust the selected volume slider. The main menu assigns explicit navigation, confirm and back actions to its InputSystemUIInputModule while enabled and restores the previous actions on disable. Its device-aware footer includes WASD. On the pause main page, Esc, East and Start/Options resume; Esc/East return from subpages to the pause main page, while Start/Options resumes directly.

On a shared interaction key, the nearest collectible/button/wheel is selected first; while one is available it suppresses cutting/digging and, on gamepad, crate movement. Without such a target, crate movement takes priority over cutting/digging on East. Cutting takes priority over digging. Keyboard E remains independent of F. Only the selected action is executed and represented by its contextual hint. When mounted, the wheel exit is the only player action; camera height toggling is also suppressed.

The controls page follows device names and scrolls with the right stick, Page Up/Down or mouse wheel so the expanded instructions fit at 640 × 360. Direction hints use W / D-pad-up sprites from the existing white icon set, tinted green.

InputPromptIcons assigns each action explicitly per device: Interact, Collect, CutOrDig and Advance use F/East; MoveCrate uses E/East; Up uses W/D-pad up. There is no hidden Collect/MoveCrate remapping. The PlayStation Up reference uses a full D-pad slice added to the original texture metadata, preserving its existing four partial sprites and their IDs. LoopScene's HUD references the same InputPromptIcons asset.

This replaces the previous E / West rope grab, West collection, F / West ledge climb and right-stick-click camera lock. Ledge climb no longer triggers by merely releasing and reapplying the horizontal direction. Review shared East actions, rope entry and ledge jump directions in Unity; no compilation or Play Mode tests were run.
