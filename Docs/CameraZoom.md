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
