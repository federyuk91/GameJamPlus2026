# Camera controls and pixel-perfect zoom

| Action | Keyboard / mouse | Controller |
| --- | --- | --- |
| Zoom in | Wheel up | L1 / LB |
| Zoom out | Wheel down | L2 / LT |
| Inspect and pan | Hold C while stationary, then WASD / arrows | Hold R1 / RB while stationary, then left stick / D-pad |
| Pin the framing while inspecting | V | R2 / RT |
| Recenter a pinned camera immediately | V | R2 / RT |

Holding the inspect command blocks player movement and gameplay actions, while time and the rest of the scene continue. Entry requires the existing stationary supported/hanging condition; movement, falling, sliding and active gameplay locks prevent entry. Releasing the command always returns the controls to the player.

Without pinning, release restores ordinary following with a smooth return. Pinning immediately freezes the chosen framing; directions remain assigned to the camera until the hold is released. After release the camera stays fixed in world space while the player can move freely. Hold the inspect command again while stationary to adjust the pinned framing; release retains the new position. The pin persists until the lock/recenter command or respawn. Recenter is immediate and also clears the pin; when used during inspection, inspection continues until its hold command is released.

Pause, external input locks and test endings stop inspection and release its player-input ownership. A pinned framing remains pinned. Jump is no longer a camera command. Camera controls reserve both shoulders, both triggers, C and V from new body-action bindings.

The top-right HUD shows hold/lock/recenter commands for the active device. The controls page in the pause menu describes the updated commands. Legacy CameraMode.FollowOffset and ToggleHeightLock remain for API compatibility; the interactive flow uses Follow, Inspect and Fixed.

## Zoom and framing

Pixel Perfect Zoom uses Pixel Perfect Viewport. Three levels remain: Zoom Out, Normal and Zoom In. Starts at Normal and stops at either end. Wheel input is preserved. Zoom is suspended during pause and test endings.

Normal preserves 640x360 and 32 PPU. Other levels use the integer pixel scale immediately below or above the normal window scale. At 1920x1080 these are 2x, 3x and 4x. UI, black bands and HUD framing retain their original reference resolution. Zoom Out is unavailable when the normal pixel scale is already 1x. Zoom has no interpolation, preserving the pixel grid.

Pan limits remain half the current view width/height, using Horizontal Look Fraction and Vertical Look Fraction. Camera Move Speed and Vertical Look Smooth Time control inspection. Starting a new inspection rebases its range around the current framing. SnapToTarget clears the pin and manual offset.

Manual checks in Unity: inspect from stationary positions; reject inspection while moving; release without pinning; pin, release and move the mouse; readjust an existing pin; recenter with and without R1 held; pause during inspection; respawn; L1/L2 zoom and mouse wheel. No Play Mode, compilation or automated tests were run.
