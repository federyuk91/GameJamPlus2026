# Player action groups

| Group | Keyboard | Gamepad | Actions |
| --- | --- | --- | --- |
| Mouth | F | North (Xbox Y / PlayStation Triangle) | Eat cheese and green cheese; hold to bite rope or dig |
| Front paws | E | West (Xbox X / PlayStation Square) | Collect photos/items, press buttons, use wheel, confirm ledge climb, hold to push/pull |
| Hind paws | Space | South (Xbox A / PlayStation Cross) | Jump and swim strokes |

Movement and climbing continue to use directional input. UI dialogue advance
retains its separately configured F/East bindings. Camera and menu bindings are
unchanged. HUD hints, keyboard/Xbox icon references and pause controls match the
groups; other gamepad families retain their existing text fallback.

The collector finds nearest food and nearest hand collectible independently.
Food takes priority over rope biting/digging on the mouth input. Cutting takes
priority over digging. The nearest hand collectible, button or wheel wins the
front-paw press; while such a press is available, prop holding is suppressed.
Mouth and front-paw consumption are independent; dialogue/UI consumes both so
advancing a communication cannot also eat or activate an object.

A rope can be bitten from the ground, while attached to a rope or underwater.
A free airborne player cannot target/cut it, so the cut hint is absent during
ordinary jumps and falls. Existing held-rope priority, reach and visibility
checks still apply.

Gameplay checks to review in Unity: ordinary air jump beside a rope versus held
rope biting; cheese beside photo/button; prop grip with E/East; ledge confirmation;
photo and wheel exit input release; keyboard/gamepad prompt switching.

The tail is reserved for a future action; it currently has no gameplay binding.
