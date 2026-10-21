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
