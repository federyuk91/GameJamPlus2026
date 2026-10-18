# Pixel-art display — 640 × 360

## Gameplay

LoopScene's Main Camera uses URP Pixel Perfect Camera:

- Assets PPU: 32, matching sprites and tiles.
- Reference Resolution: 640×360, 16:9.
- Grid Snapping: Upscale Render Texture for base-resolution world rendering.
- Filter Mode: Point, without pixel interpolation.
- Crop Frame: Windowbox, integer enlargement without distortion/cropping.

Full camera view is 20×11.25 units, with 20×8.125 visible after LoopScene's HUD bands. Following retains full-precision internal smoothing and rounds only the final position to the pixel grid.

| Display / Game View | Visible gameplay | Black bands |
| --- | --- | --- |
| 640×360 | 640×260, 1× | top 20 px, bottom 80 px |
| 1280×720 | 1280×520, 2× | top 40, bottom 160 |
| 1920×1080 | 1920×780, 3× | top 60, bottom 240 |
| 1366×768 | 1280×520, 2× | top 64, bottom 184, sides 43 |
| 2560×1440 | 2560×1040, 4× | top 80, bottom 320 |
| 1024×768 | 640×260, 1× | top 224, bottom 284, sides 192 |

The complete 640×360 base includes 20 black top rows, 260 visible rows and 80 bottom HUD rows. All 100 reserved rows count toward integer scaling. Different screen sizes/aspects may add external bands. PixelPerfectViewport draws the non-interactive black mask after camera rendering.

Bands intentionally retain uniform pixels and framing across aspect ratios; filling non-multiple dimensions would require interpolation or unequal pixel sizes.

PixelPerfectViewport is Inspector-assigned to the camera. Below either base dimension it switches to Stretch Fill while preserving 16:9 to show the full scene. Detail necessarily decreases below 640×360. Windowbox returns when the screen is large enough. Size polling is required because Unity exposes no universal window-resize event.

## Rendering and import

URP uses Render Scale 1, no MSAA and Nearest-Neighbor upscaling. Both cameras disable HDR, MSAA, post-processing and dynamic resolution. URP HDR and Adaptive Performance are disabled. 2D light textures use scale 1. Every quality level disables anti-aliasing and global texture reduction.

Assets/Art/Pixel Art and Assets/Art/Tiles textures use Point, 32 PPU, no mipmaps, no NPOT resizing and no lossy compression. Maximum import size must fit original sheets. The postprocessor applies these rules to new imports while preserving existing sprite modes/manual slices; enabled platform overrides become uncompressed.

The initial desktop window remains 640×360, resizable, with native-display fullscreen resolution. Do not call Screen.SetResolution(640, 320) to force world resolution; the camera render texture already handles this.

## Menu

UI Toolkit renders as a screen-resolution overlay rather than through the low-resolution gameplay texture, keeping fonts sharp on large monitors.

PanelSettings uses Scale With Screen Size, 640×360 and Expand. The centered reference layout is 640 units wide; a black container reserves 20 above/below, leaving 320 at base resolution. Expand guarantees at least base logical space across aspect ratios. UI scaling may be fractional for legibility, independently of gameplay integer scaling.

Main buttons are 32 units high with 15-point text; secondary text 12, title 24, laboratory label 11. Dialogs, audio sliders and toggles are compact. Oversized inline UXML styles were removed so they do not override USS. Custom titles/text are retained.

## Manual Unity review

ExecuteAlways draws PixelPerfectViewport bands in Game View outside Play Mode, including resize repaints; Scene View displays the working world without the camera mask. No automated tests or Play Mode were run for this change.

1. At fixed 640×360 and 1× preview, inspect menu, Options, Credits and quit confirmation.
2. Try 1280×720, 1920×1080, 1366×768 and 1024×768: constant framing, bands, uniform pixels and legible text.
3. Move mouse/camera; inspect sprites, tilemaps and moving objects.
4. Navigate Options by controller, change all three volumes and return.
5. Resize a windowed build, switch fullscreen and shrink below base resolution.

Game View preview resizing may itself blur the Editor display; judge at 1× or integer preview scales and in builds. Future HDR/bloom effects require deliberate HDR-setting changes.

References: bundled URP 17.3 PixelPerfectCamera.cs / PixelPerfectCameraInternal.cs; [Unity PanelSettings source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/Core/GameObjects/PanelSettings.cs).
