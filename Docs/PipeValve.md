# Tilemap pipe valves

Use `Assets/Prefabs/Environment/Pipe Valve.prefab`, or add PipeValve to an empty scene object. The custom inspector supplies the five existing Full TileSet tiles in closing order 241, 240, 226, 225, 224. They already use sprite collider shapes. Animation runs only in Play Mode and preserves the cell matrix, tint and tile flags. TilemapCollider changes are processed immediately; manual composites on its attached rigidbody are regenerated.

## Binding in the Editor

Nearest is the default. Select the valve and position it near a painted valve tile; the inspector finds the nearest compatible cell within Maximum Search Distance (default 3 world units). Search Tilemap optionally limits the search; otherwise all Tilemaps in the same scene are scanned. Moving the selected valve updates its binding from the cached candidate list. Refresh Valve Tiles rescans after painting or deleting tiles.

Manual Selection lists compatible cells as `(x,y) - Tilemap\Parent`, without the full hierarchy path or animation state. Choose the desired cell and use Frame Bound Tile to locate it. A pickable valve-image gizmo marks every active controller, including when unselected. Click it in Scene View to select the controller (Scene View Gizmos must be enabled). It uses Assets/Gizmos/PipeValve.png, copied from the supplied valve artwork. Gizmo Size on each PipeValve controls its screen size: 1 = 32 GUI pixels, 2 = 64 (the default), 3 = 96. The editor draws a clickable Scene View overlay with constant screen size, independent of zoom and the global 3D icon size. Visibility and picking respect both the Scene View master Gizmos switch and the native PipeValve entry in its Gizmos menu (registered by OnDrawGizmosSelected). Size remains controlled by Gizmo Size in the component inspector. The icon starts 12 screen pixels below the pivot so it does not cover the central transform handle. Once a valve is selected, its icon remains visible but no longer captures mouse input, leaving manipulation handles usable. The selected scene gizmo connects the controller to its bound cell. The inspector warns if another controller owns the same cell. A cell containing none of the configured animation Tile assets is rejected at runtime.

Bound scene controllers are named automatically as `Pipe Valve (x,y) Bz-Bw`, using the cell coordinates and assigned link basin identifiers. Without a link, only `Pipe Valve (x,y)` is used. Names refresh when binding/link references change and on editor hierarchy refresh; prefab assets keep their generic name. Renaming supports Undo and marks the scene dirty.

The bound Tilemap and coordinates are serialized; there is no runtime nearest search. Save the scene after binding. A prefab instance must be bound in its destination scene; the prefab itself has no scene reference.

## Animation and events

Initial State defaults to Painted Tile. Open and Closed force the respective endpoint at startup. An intermediate painted frame starts there and finishes closing unless Open is requested. Seconds Per Frame defaults to 0.1 (four transitions take 0.4 s).

Connect button events to PipeValve.Open(), Close() or Toggle(). Opposite commands reverse from the current displayed frame. Repeated identical commands do not restart animation. On Opened and On Closed fire on animated arrival at their endpoints, not at startup. There are Play Mode inspector buttons for manual checking.

## Water

Auto Water Connection is enabled by default, including on the prefab. While enabled the Water Connection reference is read-only; the editor automatically assigns the nearest active link in the same scene, measuring 2D distance from the bound tile center (or controller position if unbound) to each passage sill. It updates after binding changes and periodically while the valve inspector is open, including when links move or are regenerated. Only reference changes create Undo operations. Disable the flag to choose another Water Connection manually; that assignment is preserved while automatic mode is off. If no link exists or the nearest link belongs to another valve, the current assignment is preserved and the reason is displayed. Runtime uses the saved reference and performs no proximity search. The inspector assigns that link's Blocking Valve when free, and removes its own old reference when reassigned. A link already claimed by another valve is warned about and not overwritten.

The valve closes flow at the start of closing and enables flow only after the final opening frame. Disabling an initialized controller closes its linked flow. A separate blocking Door still gates flow as well. Water simulation preview respects the valve's configured initial state; it does not simulate button-driven animation.

Bake water geometry with the pipe tile painted open, so the passage exists in the baked geometry. Use Initial State = Closed to close it at runtime without changing the open bake configuration. Valve animation updates collision and gates an existing link; it does not rebake the basin network.

C# runtime and editor compilation are checked. Verify visual frame ordering, authored sprite collision shapes and button/water behavior in Unity for your actual placement.
