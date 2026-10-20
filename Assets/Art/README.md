# Art organization

## Pixel Art

Gameplay images are in `Assets/Art/Pixel Art`. The importer applies Point filtering, 32 pixels per unit, no mipmaps and no power-of-two resizing.

| Folder | Contents |
| --- | --- |
| `Pixel Art/Characters/Mouse` | `Mouse_Idle_Sheet.png` and `Mouse_Run_Sheet.png`, preserving slices and animation references. |
| `Pixel Art/Props` | `Crate.png`, `PressureButton_Sheet.png` and `WinchDrum_Sheet.png`. |
| `Pixel Art/Prototypes` | `Mouse_Prototype.png`, the previous placeholder player sprite. |

Animation sheets end in `_Sheet`; frame names include `Mouse_Run_0`, `PressureButton_0` and `WinchDrum_0`. Square images up to 64×64 use Sprite Mode Single; others use Multiple. Edit sheet slices in Sprite Editor.

## Palette and UI

- `Palettes/GamePalette.png`: shared palette used as a menu reference.
- `UI/Branding/GameJamPlus_Icon.png`: application icon assigned in Player Settings.
- `UI/MainMenu`: backgrounds, branding, icons and menu illustrations organized by function.

Palette and UI artwork retain their import settings outside Pixel Art. Animations are grouped by subject in `Assets/Animations`; see `Docs/ProjectStructure.md` for script and prefab locations. Scenes and settings retain their paths.
