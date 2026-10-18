# Piranha: patrol and chase

LoopScene's Piranha uses two independent components:

- PiranhaPatrol moves along a closed waypoint circuit at a configurable speed and controls sprite facing.
- PiranhaChase monitors the assigned player and temporarily replaces the patrol destination with the mouse collider position.

Select Piranha in Scene View with Gizmos enabled:

- Drag cyan Patrol points using the handles. The circuit displays links; add/remove points in Waypoints.
- View distance controls range; Cone angle controls the field of view. Both are also available in PiranhaChase's Inspector.
- The yellow cone turns red while chasing.

Waypoints are world-unit offsets from the piranha's initial position, independent of sprite scale. During play the route stays fixed even while the fish leaves it to chase the mouse.

The player is visible when its collider center is in water, inside the cone and range, with no solid collider blocking the line of sight. Detection uses PlayerSwimming.CheckWater. Water triggers and the player/piranha colliders do not obstruct visibility. Default and Diggable are included in Occluder Layers; add other solid layers used by the scene.

When any condition fails, the piranha returns to the nearest point on the circuit and resumes at the next waypoint. Initial speeds: patrol 1.5 units/s, chase 3; range 5 units, cone 100 degrees. Player, sprite and patrol references are assigned in LoopScene.

Movement follows straight segments; the route and return path require clear space. Obstacle avoidance is not implemented. Disabling PiranhaChase leaves patrol active.

Check entering/leaving the cone underwater, leaving water and blocking sight with a crate or solid terrain; verify chase and return in Play Mode.
