# Communicating water — Welcome prototype

The Welcome setup uses two communicating basins and the existing raised passage. This is a platform-oriented approximation, not a fluid solver.

## Scene setup

`Tools > Mouse Platformer > Water > Configure Welcome Communicating Basins` configures and saves `Assets/Scenes/Levels/1_WelcomeToUniverse.unity`, preserving its loaded edits. It uses Undo, does not reload an open scene, and leaves the separate pool alone. A repeated run preserves the existing setup and checks its ceiling bounds, including grates that let isolated upward probes pass through.

The setup probes actual terrain collider walls and the passage. Empty space inside the two room bounds and passage is sampled at 1/32 world unit (one game-art pixel), then compacted into non-overlapping rectangles. This accounts for static border profiles and excludes solid terrain. It is pixel-accurate to this sampling resolution, not an exact continuous polygon integration. Changes to terrain after baking require rebaking; runtime moving props do not displace water in this version.

The hierarchy root is `Communicating Water - Welcome`:

- Left Basin: the left room plus the raised passage.
- Right Basin: the adjacent room.
- Connection - Raised Passage: the sill is the lowest traversable passage height.

An existing fill confined to the left room retains its surface height. An old single volume spanning both rooms is converted to a demonstration with the left room nearly full and the right room empty. The hand introduction holds the hydraulic simulation until gameplay starts.

## Controls

- Water Basin > Initial Surface Height: starting height relative to the basin root. At runtime this becomes a conserved quantity of water. In Scene View the cyan vertical handle adjusts it; Apply Initial Water Preview refreshes the editor fill.
- Water Connection > Flow Rate: transfer speed in water-area units per second at one unit of head difference. Open, Close and Toggle can be assigned to UnityEvents. A Blocking Door reference optionally gates flow with Door.IsOpen.
- Water Basin > AddWater(float), RemoveWater(float), ResetWater(): event-callable operations. Amount is 2D water area, assuming uniform depth. Capacity clamps prevent overfilling; overflow routing is not implemented.
- `Assets/Materials/PlatformWater.mat`: Wave Amplitude, Wave Speed, Pixels Per Unit, Depth Range, Surface Color, Tint.

Keep basin roots unrotated and unscaled. Move the whole setup together if needed. During play, level calculations use the baked geometry in world space.

## Rendering and simulation

The shader supplies a thin pixel-aligned surface, mild waves, depth shading and flow streaks/ripples driven by transfer direction and strength. CPU simulation owns actual water quantity, level and trigger geometry. Rendered level follows the simulation smoothly; small surface waves are cosmetic.

At rest, level is found from the actual area of the baked sections below the surface. During transfer the same volume-height relationship is used continuously; no global bounding-box area is substituted. Transfer cannot overshoot equilibrium, create water, drain below the passage sill, or exceed the receiver's capacity. Air is assumed vented.

WaterVolume.SubmergedFraction measures the union of water sections, so objects crossing section boundaries retain correct buoyancy and overlapping legacy water volumes do not double-count immersion. Dynamic level changes update the existing swimming and buoyancy queries.

C# compilation is checked separately. Visual appearance and gameplay feel should be checked in Unity.


## Baking selected Tilemaps

Add WaterGeometryBaker and drag solid Tilemaps into Source Tilemaps, or select Tilemaps/their Grid in the Hierarchy and choose Tools > Mouse Platformer > Water > Create Baker from Selected Tilemaps. The inspector supplies the water prefab/material defaults. Click Bake Water Geometry outside Play Mode, then save the scene.

Only assigned, enabled, non-trigger Tilemap collider shapes are sampled. Decorations and other scene colliders are not obstacles. Bounds are computed from the selected maps; Use Custom Bounds can restrict the area. Pixels Per Unit defaults to 32. The packed collision mask is serialized in the scene and reused at runtime; there are no runtime Tilemap searches.

Automatic room detection erodes empty space by Room Separation Radius to distinguish chambers from narrow corridors, grows those chambers back into free space, recovers enclosed unassigned vertical/angled pipes between the chambers, and creates connections at their lowest shared openings. Unassigned space connected to the bake boundary is treated as exterior and excluded. Passage Reach limits horizontal growth beyond room cores. This is a platform-oriented heuristic: very small rooms, wide corridors, grates and open layouts may require adjusting these values or Custom Bounds. A zero-core bake still supplies collision geometry for Water Units, but does not create basins.

Rebakes reuse matching basin objects and their initial levels, preserving references from UnityEvents. Water section geometry is rebuilt. Newly detected dry basins start empty; legacy static water overlapping newly generated basins is adopted as initial water and its old GameObject is disabled with Undo. Obsolete generated basins are disabled rather than destroyed. Existing manually wired connections retain their event references and settings; after moving an opening, inspect its sill height.

The Welcome integration assigns its solid Tilemaps and existing communicating basins, and initially bakes only the collision mask so the authored setup is preserved. The same inspector button can subsequently regenerate basin geometry from the selected Tilemaps.

## One-unit water prefab

Assets/Prefabs/Environment/Water Unit.prefab starts as a 1 × 1 world-unit square containing one 2D area unit of water. Place it above baked geometry or instantiate it via the existing button spawner. Its Geometry reference is optional: otherwise it resolves a baker in its own scene once, using the baked registry.

It falls under Fall Acceleration up to Maximum Fall Speed, collides with the baked static geometry, spreads on a resting surface up to Maximum Spread Width, and seeks a nearby lower outlet. On contact with a basin floor or surface it transfers its quantity into the basin and disables itself when empty. Basin level and shader ripples update while preserving transferred quantity. This is a lightweight parcel approximation; it does not simulate particles, splashes separating into droplets, or collisions with moving crates. Moving a tile/door after baking does not alter the stored collision mask: rebake static changes; use WaterConnection's blocking-door control for hydraulic gating.

Surface Flow Speed controls spreading and travel towards ledge outlets. Width and Water Amount let you author other starting quantities. Keep the prefab scale at 1 and edit those fields instead. A unit that cannot fit into a full basin retains its remaining water; global overflow routing is not implemented.


## Reading the gizmos

- Water Basin: gizmos remain visible without selecting the individual basin. A dim cyan box shows its overall area and a small cyan marker remains visible even when dry. Yellow segments show its actual water surface where it intersects the sections. Show Section Bounds optionally reveals the detailed baked rectangles.
- Water Connection: gizmos remain visible without selecting the individual connection. A small yellow bar/circle marks its sill; a short cyan double arrow shows the connection orientation and bidirectional capability. There are no lines to basin transform origins. Select the connection to see the sill label and World Sill Height in the Inspector.
- Orange outlines are Unity's selected collider/sprite physics-shape display; they are not hydraulic links.

The shader receives a two-axis flow direction. Vertical transfer animates vertical streaks, while surface ripples still spread horizontally. A bake made before enclosed-passage recovery must be regenerated to add the missing pipe sections and connections. Generated connections are repositioned on rebake, and obsolete generated links are disabled; manual links retain their authored settings.

Show Gizmos on each basin/connection controls these indicators. Unity Scene View Gizmos must also be enabled. The overall basin box is a visual envelope; Show Section Bounds reveals the actual baked subregions.


## Selecting basins and centered pivots

Selecting a Water Geometry Baker displays a cyan clickable handle and name at the geometry center of each active referenced basin. Click one to select and ping that basin in the Hierarchy.

Bake Water Geometry centers each rebuilt basin's Transform pivot on the bounding area of its baked sections. Section coordinates and the initial level are compensated; existing children retain their world positions. Basin object references remain stable. Center Basin Pivots applies the same correction to existing basins without rebaking geometry, and supports Undo. Save the scene afterwards.


## Internal section prefab and empty sections

Visual Overlap Offset on the baker expands water rendering on every side by the specified world-unit distance (for example 0.03125 is one pixel at 32 PPU). It defaults to zero. Changing it updates referenced basins and their water previews without rebaking; new bakes also copy the offset into the basins. Geometric and simulation gizmos display the same expansion. Section capacity, initial volume, link positions and immersion colliders keep their logical dimensions; only the sprite quad and displayed surface extend into the walls. Dry sections stay hidden. Small offsets are intended: internal section edges also overlap, which can make translucent seams darker.

During baking, samples are eligible for basin geometry only if their cell contains a tile in at least one selected source Tilemap. HasTile determines presence: transparent tiles count, empty cells do not. Physics shapes still determine which parts of painted cells are solid. Empty cells are excluded from room detection and passage growth without being added as solid obstacles to the runtime collision mask. Rebake to apply this rule to existing geometry.

The baker's Show Volume checkbox overlays the full baked capacity with translucent, distinct basin colors, including dry sections. It draws the actual section union rather than the overall bounds and does not enable water renderers or colliders. Scene View Gizmos must be enabled. The overlay stays visible when selecting other objects until the flag is cleared.

Enable Show Water Preview and use Water Preview (0–100) to preview a common horizontal water level across all active referenced basins. Zero is the lowest floor, 100 the highest ceiling; higher rooms start filling only when the preview height reaches them. The cyan translucent gizmos follow the actual baked sections. This is a geometric Scene View preview, independent of connections and flow simulation, and changes neither initial water levels nor section renderers/colliders. Disable Show Water Preview to hide it.

Each basin also has Show Water Preview and Water Preview (0–100), spanning only that basin's floor and ceiling.

On either the baker or a basin, enable Show Simulation Preview and enter Simulation Time (s) in a numeric field, in seconds from zero with no 100-second cap. Zero uses Initial Surface Height on every basin. The editor follows the connected network and simulates transfers on private model copies using the same transfer calculation and fixed timestep as gameplay. Link flow rates, sill heights, Starts Open and the blocking door's initial Starts Open are respected. Runtime events, moving doors, added Water Units and player interactions are not simulated. At equilibrium the levels naturally stop changing; the slider measures elapsed time, not percentage of equilibrium.

The simulation runs incrementally to keep the editor responsive; Preview calculated to reports the time actually displayed while catching up. Scrubbing backwards restarts from the initial volumes. Preview state is temporary and cleared on deselection, Undo or entering Play Mode. Re-enable it after editing other objects' settings to capture their changes. While simulating, geometric overlays on participating basins are suppressed and their WaterVolume renderers are temporarily hidden using forceRenderingOff. Their previous rendering flags are restored when preview ends. No serialized renderer settings, gameplay volumes, colliders or initial levels are changed. The inspector reports connected basin/open link counts, each link's current flow/block reason, and initial/current volumes. These diagnostics distinguish empty initial basins, closed doors/links, zero portata, water below the sill, full targets and balanced levels.

Connection objects are named Link B1-B2 using their basin numbers (or list order for manually named basins). Existing links are renamed with Undo when inspecting their baker or after baking; references and events are preserved. Save the scene to keep these names.

Generated sections now use `Assets/Prefabs/Environment/Water Section.prefab`, with neutral position/scale, explicit visual/shape references, and renderer/trigger disabled by default. The legacy `Water Volume.prefab` remains a separate placeable asset. Default baker references pointing to the legacy prefab are upgraded to the section prefab on inspection or bake; custom prefab assignments are preserved.

WaterVolume remains the swimming/buoyancy adapter on a section. Its Transform describes the full baked section capacity plus any visual overlap, while its trigger size/offset describes only the logical filled portion. The shader clips that capacity to the current water surface. Dry sections are also positioned correctly and retain no old prefab rectangle. Their sprite and collider remain disabled.

Do not enable section sprites/colliders to fill a basin: change Initial Surface Height or add Water Units. Refresh Water Preview on the baker corrects all existing section transforms and previews without rebaking or replacing object references. Apply Initial Water Preview does the same for one basin. New bake geometry uses the dedicated section prefab.


## Initial level and applying a preview

The basin Inspector displays Initial Water Depth in world units measured from GeometryBounds.yMin: zero is empty, Maximum Water Depth is full. The Scene View handle uses the same range and clamps to floor/ceiling. Existing serialized pivot-relative initialSurfaceHeight remains as an internal compatibility field; no scene migration, rebake or level shift is required. Pivot centering keeps its existing coordinate compensation.

Apply Water Preview now writes the local Water Preview percentage into Initial Water Depth and refreshes the actual section visuals/colliders, with Undo. If simulation preview is active, the displayed simulated level is applied to this basin instead and the temporary simulation is stopped. This applies the time actually calculated, not an unfinished requested time. The percentage is synchronized to the applied depth. In Play Mode the button remains Reset Water and restores the configured initial level.

The former Apply Initial Water Preview button only refreshed visuals from the already configured initial level; it did not apply the percentage slider. Refresh Water Preview on the baker still refreshes existing initial levels without changing them.


## Editing individual sections

Select a basin and click Edit Sections. Each logical section receives a labeled XY bounds handle: drag its four sides to resize, or the center square to move it. The regular transform manipulator is temporarily hidden to avoid input conflicts. Finish Editing Sections exits the mode; deselection (including with a locked Inspector), Inspector disposal, or changing Play Mode automatically exits and restores the previous tool visibility.

Edits are serialized in the basin's sections array with Undo support, a minimum size of 0.01 world units, and immediate section renderer/collider/initial-water refresh. They affect real basin capacity, not just the visual overlap. Keep sections non-overlapping to avoid counting water capacity twice. Link sill positions/topology and the baker collision sampling mask are not regenerated by these handles. A subsequent Bake Water Geometry replaces manually edited section rectangles.


## Overlapping section rendering

PlatformWater.shader uses stencil bit 6 (mask 64) to shade visible water once per camera pixel. A successful water fragment marks the bit; later sections skip that pixel instead of accumulating alpha. Surface and transparent-texture clipping happens before stencil replacement, so dry or fully transparent pixels do not hide other water sections. The existing Renderer2D depth/stencil buffer is enabled. All sections sharing this shader share the same coverage mask; first visible section in renderer order supplies tint and flow in an overlap. This changes rendering only, not section bounds, overlap offset, simulation or swimming colliders. Reserve stencil bit 6 for water when adding other custom stencil effects. Review Game and Scene views after shader import, including the lens capture and animated surfaces.
