# LoopScene hierarchy

Four main groups organize the scene:

- Systems: Game Manager and Spawn Points, Main Camera, Global Light 2D, Gameplay Music and Gameplay HUD.
- Level: room grid, diggable terrain and Water Volume (previously named Square).
- Actors: Player and Piranha.
- Interactables: Crates and Tethers, Ropes and Ladders, Dropping Platforms and Collectibles.

Added groups have zero position, identity rotation and unit scale. Object positions, tilemap data, prefab overrides and component references are unchanged. Existing room structures and crate/water/rope examples are retained. The groups only organize the Hierarchy.

Place new objects in the matching subgroup. Spawn Points used for initial selection must still be assigned in GameManager: placing them in the hierarchy does not register them.
