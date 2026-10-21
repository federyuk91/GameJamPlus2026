# Magnifying lens

BG (1) in Welcome uses MagnifyingLens2D with MagnifyingLens2D.shader. Its bright green pixels are a chroma mask: the shader replaces them with a point-sampled, enlarged background. Sprite artwork and its alpha are preserved. Lens Center UV defaults to the centroid of the painted green region in dsa1.png; Magnification defaults to 2. Green Dominance adjusts color-key sensitivity. Sprite tint applies to the frame, not the magnified background brightness.

Use Tools > Gameplay > Configure Magnifying Lens with Welcome open to wire references, create the material, enable the Renderer2D Camera Sorting Layer Texture without downsampling, add the Lens Background sorting layer and extend existing scene lighting to that layer. Configuration uses Undo and marks the open scene dirty without saving or reopening it. Save the scene normally afterwards.

The component includes SpriteRenderers, TilemapRenderers and root SortingGroups. Before the assigned camera or Scene view renders, it moves elements visually behind the lens into the capture sorting layer, preserving relative layer/order/depth priorities. It restores all original layer IDs and orders at the end of that camera render. Higher sorting layer/order draws in front and remains unaffected. Camera depth breaks ties. No positions, colliders or input are changed. Runtime-spawned renderers are discovered every 0.25 seconds; cached background classification follows moving objects every rendered frame.

This setup supports one lens plane per camera. Overlapping lenses with different planes need separate captures. Existing lights are configured for the capture layer by the setup command; new lights must also include Lens Background. Camera Sorting Layer Texture adds a full-resolution color copy to the 2D renderer. No gameplay tests are automated; check green replacement, foreground occlusion, camera pan/zoom and a moved background sprite in Unity.

## Scientist parallax

Busto and Lente are linked by ScientistParallax on their common parent. The bust follows camera translation with Parallax Factor (0.8 horizontal, 0.85 vertical). Lens center remains on the interpolated eye position: horizontal camera travel moves it from First Eye UV to Second Eye UV over Camera Travel For Sweep (6 world units). Movement reverses when the camera returns; Reverse Sweep swaps the travel direction. Initial Sweep Position chooses the starting eye. Sweep Smooth Time softens changes. Both sprite transforms retain their scale and Z position, and no colliders or camera settings change. The first LateUpdate captures the camera reference after camera startup positioning; disabling restores the original sprite positions.

Tools > Gameplay > Configure Scientist Parallax wires the loaded Welcome scene using Undo and marks it dirty without saving. Save normally after reviewing Inspector settings. Eye UVs use the current Busto.png artwork and can be adjusted if it changes. No Play Mode checks were run.
