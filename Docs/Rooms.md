# Prefab rooms and procedural assembly

## Drawing a room

1. Duplicate Assets/Prefabs/Rooms/Room Template.prefab and rename it.
2. Open Prefab Mode. Gameplay Tilemaps contains configured solid/decorative maps for existing Tile Palettes.
3. RoomDefinition.Footprint must contain the complete room. Edit it in Inspector or with cyan Scene View handles. Keep root rotation zero and scale 1.
4. Up/Down/Left/Right buttons create assigned Exit children. Position exits at pipe connection points. Green arrows show direction without creating art/colliders. Align exits to grid also snaps inside prefabs.
5. Save. Allow at most one exit per direction; remove both child and Ports reference to delete an exit.

Compatible connections are Up–Down and Left–Right with coincident positions. Rooms are not rotated/mirrored to fit. Pipe dimensions must match; direction checks compatibility, not passage width. A terminal must have exactly one exit, occupied by its connection. Draw closed remaining walls and colliders; the generator does not create caps.

## Generating layouts

Place Assets/Prefabs/Rooms/Room Layout Generator.prefab in a scene. Assign Start Room, intermediate Rooms and Terminal Rooms. Room Count includes start and terminals. Seed reproduces choices with the same catalog/configuration.

Generate rooms creates Generated Rooms with source prefab links. Generation is Editor-button-only, not automatic on game startup. Search aligns opposing ports, uses each exit once and rejects positive-area footprint intersections; rectangle edges may touch. Connections form a tree without cycles. A solution must have the exact requested count and no unused exits. Search tries alternatives up to Search Limit; failure preserves the old result and reports the problem.

Regeneration replaces only this generator's container. Generation/removal support Undo. If the container was moved outside the generator, restore it before regenerating. Require zero world rotation and world scale 1. Snap the result to world-grid cells and save the scene to retain it.

## Included examples

Example Up Left is the agreed two-exit starting example. Example Left Right / Up Down are catalog corridors. Terminal Up/Down/Left/Right close corresponding directions. These editable 8×6 examples contain painted walls/openings. The generator is assigned to them with Room Count 10 and Seed 12345; the empty template is not in the catalog.

Check an example's footprint/ports, generate in free space, inspect seams/colliders/passages, and change seed/count. Keep Player, camera and global systems outside room prefabs.

## Shared generated Grid

Generated Rooms contains one Grid, retained during Play Mode. Drawing prefabs retain local Grids; only generated instances lose them and use the shared Grid. Generator Cell Size must match prefab cells (1×1 here). Require rectangular XY grids, no Cell Gap, rotation or scale. Tilemap origins must lie on whole local cells; off-grid transforms are reported.

Exit snapping uses the cell edge along travel direction and cell center transversely: Up/Down have centered X and edge Y; Left/Right have edge X and centered Y. Gizmos show snapped points and a yellow line where authored points differ. Generation updates instances without altering source prefabs.

The planner aligns Grid origins while compensating room-root/Grid local offsets. Rounding only the root is insufficient. The initial result snaps to the world grid even for fractional generator positions.

Regenerate after this change. Continue editing source prefabs in Prefab Mode. Snapping aligns cells; designers must still provide matching pipe widths/profiles and footprints containing real geometry.

## Manual assembly with snapping

LoopScene contains empty Manual Room Grid with a 1×1 reference Grid and ManualGridAlignment. Its prefab is in Assets/Prefabs/Rooms. Create others through Tools → Mouse Platformer → Rooms → Create manual grid container.

Parent room roots or Grids under it, place them approximately and select the container. Align child grids snaps origins to reference-grid intersections. Keep the container at zero for world-grid alignment or assign another Reference Grid.

Whole RoomDefinition roots move, compensating internal Grid offsets, including nested/inactive children. Alignment only translates: cell dimensions, rotations and Cell Gap must match. If a room's grids require different offsets, cancel before mutating objects. Edit Mode supports Undo and prefab overrides.

Manual snapping retains separate room Grids without merging them or selecting pipe connections. Place rooms near the desired connection before snapping.
