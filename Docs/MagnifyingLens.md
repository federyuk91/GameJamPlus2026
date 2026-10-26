# Magnifying lens

BG (1) in Welcome uses MagnifyingLens2D with MagnifyingLens2D.shader. Its bright green pixels are a chroma mask: the shader replaces them with a point-sampled, enlarged background. Sprite artwork and its alpha are preserved. Lens Center UV defaults to the centroid of the painted green region in dsa1.png; Magnification defaults to 2. Green Dominance adjusts color-key sensitivity. Sprite tint applies to the frame, not the magnified background brightness.

Use Tools > Gameplay > Configure Magnifying Lens with Welcome open to wire references, create the material, enable the Renderer2D Camera Sorting Layer Texture without downsampling, add the Lens Background sorting layer and extend existing scene lighting to that layer. Configuration uses Undo and marks the open scene dirty without saving or reopening it. Save the scene normally afterwards.

The component includes SpriteRenderers, TilemapRenderers and root SortingGroups. Before the assigned camera or Scene view renders, it moves elements visually behind the lens into the capture sorting layer, preserving relative layer/order/depth priorities. It restores all original layer IDs and orders at the end of that camera render. Higher sorting layer/order draws in front and remains unaffected. Camera depth breaks ties. No positions, colliders or input are changed. Runtime-spawned renderers are discovered every 0.25 seconds; cached background classification follows moving objects every rendered frame.

This setup supports one lens plane per camera. Overlapping lenses with different planes need separate captures. Existing lights are configured for the capture layer by the setup command; new lights must also include Lens Background. Camera Sorting Layer Texture adds a full-resolution color copy to the 2D renderer. No gameplay tests are automated; check green replacement, foreground occlusion, camera pan/zoom and a moved background sprite in Unity.

## Scientist parallax

ScientistParallax keeps the bust's Parallax Factor and the lens sweep controls.
Lens Left Limit and Lens Right Limit are lens-center coordinates in the bust's
local space. The cyan and yellow Scene handles move them with Undo; Show Gizmos
also displays both endpoints without selecting the object. Lens Vertical Offset
adds world-space vertical displacement relative to the bust to both limits.
Camera Travel For Sweep controls the speed between limits, Initial Sweep Position
the starting position, and Reverse Sweep its direction. Old eye UV limits migrate
to equivalent local coordinates without changing the saved camera travel.

The four empty groups are 01 Far Background, 02 Distant Background,
03 Middle Background and 04 Near Background. Place decorative objects under the
appropriate group. Their configurable factors default to 0.95, 0.9, 0.85, 0.8:
1 follows the camera completely (distant); 0 stays in world space (near).
Groups are siblings. Their SortingGroups use the lens sorting layer, with orders
immediately below the lens, far to near; children keep their internal ordering.
The lens is the frontmost plane and keeps its separate, bounded sweep over the
bust. Busto and Lente retain their existing parents and own motion settings.

Use Create Background Groups in the component Inspector, or Tools > Gameplay >
Configure Scientist Parallax with Welcome open. Setup is undoable, does not move
existing scenery into groups, preserves assigned roots and marks the loaded
scene dirty without saving. Save normally after reviewing the result. Disable
restores original positions. No Play Mode checks were run.


## Sprite color/alpha plus an independent effect mask

The existing Green Chroma mode remains the default for existing MagnifyingLens2D components and keeps the previous green-replacement composition. The independent Mask Source choices keep the original sprite RGBA and composite it above the magnified/distorted captured background. Neither requires painting a key color into the sprite.

Quick setup: open the SpriteRenderer component context menu and choose Configure Masked Lens. It adds/configures MagnifyingLens2D, assigns Assets/Materials/MaskedSpriteLens.mat, selects Sprite Secondary Texture mode and centers the lens at (0.5, 0.5). No existing scenes/prefabs are converted automatically.

For Sprite Secondary Texture, open the sprite in Sprite Editor > Secondary Textures and assign a mask texture with the exact name `_LensMask`. Use the same source dimensions/layout as the sprite sheet; Unity's sprite/atlas UVs also sample the secondary mask. Choose Mask Channel (Red/Green/Blue/Alpha) on MagnifyingLens2D. Red is the default: a grayscale mask works directly and leaves the original sprite alpha available for glass, color artwork and antialiased edges.

Alternatively choose Separate Texture and assign Effect Mask. This texture maps across the selected sprite's full original rectangle using local coordinates, independent of primary sprite atlas placement. For animated sprites this mode uses one mask stretched per frame; use Secondary Textures for matching per-frame masks on a sheet.

White/1 applies the lens, black/0 leaves the background unchanged, intermediate mask values blend the effect. Mask Strength scales the coverage. Missing mask textures disable the effect, including when the chosen channel is Alpha. For numeric grayscale masks, import with sRGB disabled; use Clamp wrapping, and Point filtering for pixel-art masks or Bilinear for soft edges. Keep the sprite alpha source set to its input alpha.

Use Mesh Type = Full Rect on sprites that need the lens inside transparent regions or outside the opaque artwork. A shader can only affect pixels covered by the sprite's geometry. It cannot extend outside that rectangle. An opaque sprite remains opaque above the effect; use transparent/semitransparent artwork where the background should show through. SpriteRenderer tint and alpha also affect the composite, so renderer alpha fades the lens together with the sprite.

Magnification remains on MagnifyingLens2D; Lens Distortion is adjusted on the material. Lens Center UV and Lens Radius UV position/shape the existing distortion. The mask controls coverage separately. The same per-lens sorting-aware capture camera is reused: each active lens still renders its own background capture, rather than using a new render feature or global opaque texture.

Runtime and Editor C# compilation verified with current project sources. Shader import and visual behavior need checking in Unity; no shader compilation or Play Mode validation was run in the terminal.


### Selecting an individual sliced mask sprite

Effect Mask accepts a Texture2D, so Unity does not allow a subasset such as Background Elements_0 there. Select Mask Source = Separate Sprite and assign the sliced asset to Effect Mask Sprite instead. It samples only that sprite's region of its sheet. Local-to-atlas UV mapping is recovered from the mask sprite mesh and cached, including rotated atlas mappings; validation or changing the sprite/texture invalidates the cache. The selected mask rectangle is stretched across the displayed sprite rectangle.

For mask sprites use Full Rect geometry and disable tight packing in any atlas containing them, so the full rectangular mask area remains allocated. Select Mask Channel = Alpha to use the mask sprite's silhouette/transparency, or Red/Green/Blue for a painted mask channel. The source sprite import and sheet are not modified by this feature. Runtime/Editor C# compilation verified; shader visual validation remains in Unity.


### Foreground opacity and conditional mask fields

The main sprite's imported alpha determines foreground opacity. There is no Sprite Opacity multiplier on the component or shader. SpriteRenderer tint/alpha retains its standard whole-renderer behavior. Glass/liquid areas intended to show the background must have the desired alpha authored in the main PNG; opaque pixels remain opaque.

The component Inspector shows only fields relevant to Mask Source: Green Chroma shows Green Dominance; Separate Texture shows Effect Mask (Texture2D); Separate Sprite shows Effect Mask Sprite; Sprite Secondary Texture shows instructions for assigning `_LensMask` in Sprite Editor without either separate-mask field. Mask Channel appears for independent masks, while Mask Strength remains available for all modes. Hidden assignments are retained for switching modes, but only the selected mode is sampled.

Universo_10_Composed's Green Conical Flask Lens still uses Separate Texture, GreenFlask_mask.png and the Red channel. The temporary spriteOpacity scene override has been removed. The mask import uses linear grayscale data, and its transparent exterior suppresses RGB-mask sampling outside the silhouette. Both PNG files are unchanged. Runtime/editor C# compilation checked; shader rendering remains to be checked in Unity.
