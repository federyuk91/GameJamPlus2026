# Diggable ground

The Diggable ground prefab uses layer Diggable (6), a DiggableTerrain component,
its area collider, SpriteRenderer, DiggableTerrain material and shared PhysicsSettings.
The player's solid mask includes Default and Diggable (65). Digging does not use
the Diggable tag to find terrain; the component supplies the excavatable area.

PlayerDigging / Use Scene Terrains When Unassigned is enabled on the Player prefab.
Without valid entries in Terrains it uses the active DiggableTerrain registry,
restricted to the player's scene. Placing or spawning Diggable ground therefore
does not require manually wiring scene objects into the player. Disabled or
destroyed terrain unregisters automatically. An explicit non-empty Terrains list
takes precedence and preserves existing authored restrictions.

Scavo requires the mouth action plus a movement direction into nearby earth.
The shared PhysicsSettings must have Digging / Enabled on. When digging is
blocked by another solid collider, that collider still stops the mouse even if
it overlaps the diggable area; avoid covering the same space with solid tiles.
