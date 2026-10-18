# Recovery Zone

Place Assets/Prefabs/Recovery Zone.prefab below or around the map, separate from playable areas. It is invisible in gameplay with a faint red Editor gizmo. Box Collider 2D → Size/Offset defines its volume.

Recovery Zone seeks a nearly horizontal surface with full mouse-collider clearance and support at left, center and right. It uses the Player SimulatedBody2D's Solid Filter. Short vertical rays also find floors beneath ceilings in the same CompositeCollider2D. The nearest valid sampled destination wins.

- Search Radius: maximum distance from Player collider center; initially 24 units.
- Sample Spacing: horizontal step; initially 0.25 units. Smaller values are more accurate but more expensive. Search occurs only on zone entry.
- Flat Surface Angle: maximum accepted slope; initially 1 degree.
- Show Gizmos: Editor volume visibility.

Destinations inside solids or active recovery, death or starvation zones are excluded. If no surface exists within range, choose the nearest active GameManager Spawn Point with clear space outside those zones. Designers must place Spawn Points safely.

Recovery preserves satiety, pickups, timer and universe. It clears velocity, jump buffer and sliding, cancels digging/biting, and detaches ropes, ladders, ledges and moving platforms. The camera snaps to the player.

PlayerRecovery is included in the Player prefab and assigned on LoopScene's GameManager. Other scenes must assign their own PlayerRecovery. No tags or Rigidbody2D are required.

With no valid destination the player stays put and a warning is logged once per entry. Increase Search Radius or assign a valid Spawn Point. Recovery is a safeguard; it does not repair the colliders that let the player leave the map.

Check falls out of bounds, floors under ceilings, rejected narrow ledges, lethal-zone overlaps, Spawn Point fallback and recovery while attached.
