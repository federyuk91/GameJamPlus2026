# Player platform shadow

Player.prefab contains the imported Platform Shadow prefab as a nested child at the collider feet (local Y -0.495). The package keeps its original license and author attribution.

Select Player > Platform Shadow to tune Base Width (0.5 units), Max Height (3 units), Shrink Factor, Darkest Shade and Lightest Shade. The pixel texture uses 32 PPU and Point filtering. Its renderer uses Default sorting layer, order 199. Platform Shadow has an independent Sorting Group (Sort At Root enabled) at order 501, above the composed scene terrain group at 500, even when the player belongs to a group at 200.

Collision Layers includes Default and Diggable. The package raycasts ignore triggers and the assigned player root hierarchy, avoiding self-shadow hits and water/pickup surfaces. The shadow follows underlying collider geometry and fades/shrinks with height; it is hidden where no ground is detected. Continue Off Edge remains disabled.

Review appearance on flat terrain, slopes, ledges and moving crates in Unity Play Mode.
