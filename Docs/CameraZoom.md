# Camera zoom

LoopScene's camera has Pixel Perfect Zoom linked to Pixel Perfect Viewport.

- Wheel up / R1 (Xbox RB): one level closer.
- Wheel down / L1 (Xbox LB): one level farther away.
- Three levels: Zoom Out, Normal and Zoom In. Starts at Normal and stops at either end.
- Controls are suspended during pauses, photos and test endings.

Normal preserves the original framing and reference resolution (640×360, 32 PPU). Other levels use the integer scale immediately below or above the normal window scale. At 1920×1080 these are 2×, 3× and 4×. Zoom is not interpolated. PPU, Point filtering and pixel grid snapping remain unchanged.

Pixel Perfect Viewport retains the original resolution for UI, bands and frame, whose dimensions do not change when zooming. Only scene rendering resolution and camera cropping change. Window resizing recalculates levels; disabling the viewport restores the original camera values. Other scripts must not modify the Pixel Perfect Camera resolution while this system is active.

If Normal is already 1×, Zoom Out is blocked because a smaller scale cannot preserve every pixel. Below 640×360 the existing Normal behavior fits the image to the window.

Check in Unity: wheel/shoulder controls, UI dimensions, zoom limits, resolution changes, pause/photos, vertical camera movement over 50% of the new view, and restoration of saved camera settings after Play Mode.

## Vertical look and height lock

Hold Up/Down while the mouse stands still on a surface to move the camera by up to 50% of its height. Adjust Vertical Look Fraction and Vertical Look Smooth Time on Platform Camera Controller.

C or right-stick press locks/unlocks the current camera height. Horizontal following continues. While locked, Up/Down and player vertical movement do not change the height. Unlocking smoothly restores following. Recovery/respawn through SnapToTarget clears the lock.

CAMERA HEIGHT LOCKED and the current device's unlock command appear at the top right. Lock input is suspended during pauses and test endings. Shake remains active while height is locked.
## Camera control modes

Camera control is explicit; Up/Down no longer shifts the camera during normal player movement. The camera command is C on keyboard and the current right-shoulder binding on gamepad.

| Current mode | Camera command | Jump | Direction input |
| --- | --- | --- | --- |
| Follow | Enter Inspect if the player is still and supported/hanging | Normal gameplay jump | Move the mouse |
| Inspect | Return to Follow, clear the manual offset | Freeze the selected framing (Fixed) | Pan the camera |
| Fixed | Enable mouse movement, retaining the framing offset (FollowOffset) | Resume Inspect | Flash the centered unlock reminder |
| FollowOffset | Re-enter Inspect when the mouse is still | Normal gameplay jump | Move the mouse |

Inspection requires no movement input and either grounded rest, ledge hanging, or a stopped rope/ladder climb. It is unavailable while falling, sliding, climbing up a ledge, digging or using another interaction that locks gameplay input. PlayerMovement is suspended during Inspect and Fixed, and gameplay input is blocked with a separate camera-owned lock; wheel interactions keep their own lock. Time and the rest of the scene continue to run. Jump used for the camera is cleared from the gameplay jump queue.

Pan limits are half the current camera width/height (Horizontal Look Fraction and Vertical Look Fraction, both 0.5); Camera Move Speed is 6 world units per second. Smoothing uses Vertical Look Smooth Time for both manual axes. Position rendering stays on the pixel grid; zoom changes are reflected in the inspection bounds. FollowOffset continues normal player following with the selected two-axis offset. C from Inspect returns smoothly to normal framing. Recovery/respawn SnapToTarget clears camera mode and offset.

Trying to move while Fixed shows a blinking centered message: Jump unlocks camera control; C re-enables the mouse. The message follows the active device prompts and disappears after stopping the attempt or switching mode. The top-right status describes the current mode. Pause suspends camera controls, and the camera input lock does not change wheel ownership or other gameplay locks.

Manual checks: enter Inspect while stationary on the floor and hanging; reject entry while falling or moving; move the camera in all four directions to the limits; Jump to Fixed; direction input shows the blinking reminder; Jump resumes inspection; C from Fixed restores player control with offset retained; re-enter Inspect then C returns to standard framing; pause and respawn while inspecting.
