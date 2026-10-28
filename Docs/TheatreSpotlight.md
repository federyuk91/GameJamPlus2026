# Theatre Spotlight

Drag `Assets/Prefabs/Props/Theatre Spotlight.prefab` into a gameplay scene and place its root above the area to illuminate.

- `Tracking Area`: move this child and edit its BoxCollider2D size to choose where the player activates tracking. This trigger does not block movement and stays fixed while the lamp rotates.
- `Theatre Spotlight`: configure Rotation Speed and Fade Duration. Target Collider can reference the player explicitly; an empty reference uses GameManager.PlayerCollider.
- `Spot Light 2D`: configure cone Inner/Outer Angle, Inner/Outer Radius, Color, Intensity and Volumetric intensity. Uses a URP Point Light narrowed into a spotlight, with the project's Additive blend style and both existing sorting layers.
- `Aiming Pivot`: rest rotation points down. The lamp follows the player's collider center while it overlaps the area, fades on, and returns to rest while fading off after the player leaves. Tracking pauses with gameplay.
- Select the root for the cyan activation-area gizmo and gold beam outline. Keep the root scale at one; use collider size and light radii for dimensions.

The sprite `Assets/Art/Pixel Art/Props/Faretto.png` is an exact 23×20 pixel extraction of the visible layer in `Assets/Art/Pixel Art/Faretto.aseprite`, imported at 32 pixels per unit with Point filtering, no mipmaps and no compression. The original Aseprite file is preserved.

C# compilation verified after adding manual controls. No Play Mode or automated tests were run. Check lamp orientation, smooth tracking, light visibility and area boundaries in Unity.

## Manual controls

The Theatre Spotlight component exposes public parameterless UnityEvent methods:

- TurnOnLight, TurnOffLight, ToggleLight: force light output on/off, preserving Fade Duration.
- TurnOnFollow, TurnOffFollow, ToggleFollow: enable player tracking anywhere or freeze the current orientation. These do not change light output.
- UseAutomaticLight, UseAutomaticFollow: restore area-based behavior independently.

Light Mode and Follow Mode start at Automatic, preserving existing prefab behavior: the light fades on and follows while the player is inside Tracking Area, then fades off and rotates back to its rest pose outside. Manual On ignores the tracking area; following still requires an available player collider. Toggle uses the requested state rather than the fading intensity, so repeated toggles reverse correctly during a fade.

Wire an event to the root Theatre Spotlight GameObject and select the required method under TheatreSpotlight. Review event wiring and fades in Unity Play Mode.
