# Tilemaps and Tile Palette

LoopScene contains Gameplay Tilemaps with a rectangular 1×1-unit grid. Art/Tiles sheets are sliced into 32×32 cells at 32 PPU with Point filtering and no mipmaps.

Window → 2D → Tile Palette contains TilemapFloor, TilemapTube and TilePipes. The 39 Tile assets are in Assets/Tilemaps/Tiles and preserve sheet layout. Tools → Mouse Platformer → Tilemaps can open Tile Palette or prepare missing assets.

Choose a palette and Active Target:

- Terrain - Solid: floors, walls, platforms and level profiles.
- Pipes - Solid: pipes blocking movement and sight.
- Background - Decorative: no colliders, order -20.
- Foreground - Decorative: in front of the mouse, no colliders, order 5.

All palettes work on every Tilemap. Sprite-type Tile colliders follow sprite Physics Shapes rather than filling cells. Edit Sprite Editor → Custom Physics Shape or choose Grid on a Tile asset for a full square. Decorative Tilemaps have no TilemapCollider2D and remain non-solid even if their Tile assets define shapes.

Solid maps use Static Rigidbody2D, TilemapCollider2D and CompositeCollider2D Polygons with a small merge tolerance. Default is included in player/crate collisions and piranha Occluder Layers. Movement, stepping, climbing, support and visibility use these colliders. Use the same Tilemap for continuous surfaces to merge seams.

The scene grid starts empty, retaining existing geometry for reference. Painting LoopScene's instance saves scene overrides; the prefab remains reusable.

## Future diggable terrain and water

When suitable tiles exist, use dedicated maps:

- Diggable terrain: painted cells define initial soil; partition into fine excavation masks, shaders and updatable colliders. Tunnels retain the current radius, unrestricted by one-unit cells.
- Water: contiguous cells form merged trigger volumes exposed to swimming/flotation and consistent piranha detection; visuals may use a water material.

These maps were not created in this setup; existing scene terrain/water remain active. Check painted solid surfaces, reachable ledges, piranha sight occlusion and passable decoration in Unity.

## Updating sheets without mixing tiles

Unity slice coordinates start at the image bottom. Adding a row below existing upper artwork requires shifting old Sprite Editor rectangles upward. Preserve names and especially Sprite ID/internalID used by Tile assets/rooms; do not reslice away old sprites. Adding cells on the right avoids this vertical shift.

TilemapTube repair: 16 original sprites now use y=32 in the updated 544×64 image. Six lower-row slices are TilemapTube_New_0..5. Their Tile assets are in TilemapTube and the added palette row. Two manually created Tile assets retain their GUIDs.
