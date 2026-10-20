# Rope-supported dropping planks

DroppingPlatform now represents a solid plank held by one breakable RopeSimulation, using RopeAttachment + PropsMotion + SimulatedBody2D. The prefab keeps its original root transform, placement and scale, but the former rendering/physics/gameplay components now belong to the Suspended Plank child. Their original file IDs are preserved so existing scene player references and component overrides remain valid. New scripts looking up those components from the assembly root must use an assigned reference or the plank child.

Hierarchy:

- DroppingPlatform: stationary assembly root at the existing placement.
- Suspended Plank: original sprite/collider, body, motion, rope attachment and DroppingPlatform controller.
- Support Rope Anchor: fixed anchor above the plank, with one RopeSimulation and four visual pieces.

The default rope is 4 x 0.25 = 1 world unit long. It attaches at the plank's top centre. The anchor is placed to keep the original plank location unchanged; the default prefab compensates for the assembly's original Y scale of 0.2. Keep the root stationary during gameplay and move the Suspended Plank only through its physics. Configure rope geometry with the existing RopeSimulation Inspector, and move the anchor/attachment as needed when editing dimensions or scaling.

PropsMotion stays enabled from the start; the rope constraint holds the plank rather than a disabled motor. Standing on the plank starts Fall Delay (0.4 s). Leaving does not cancel it. On expiry, DroppingPlatform calls the existing TryCutSegment on the last rope segment, closest to the plank. A manual player cut also releases the plank immediately and starts the disappearance timer. If cutting is disabled on the rope, the timed attempt cannot forcibly detach it.

RopeAttachment handles the actual detachment. DroppingPlatform only watches rope state/cut events, player contact and timers. After a break the plank remains solid: it can carry the player and collide with terrain until it is hidden after Disappear Delay (2 s). The fixed anchor and surviving rope fragments remain. This intentionally replaces the former ghost fall through terrain/player. Rope fragment cleanup follows the existing minimum-two-piece rule.

Body Max Speed stays 15 units/s and Gravity Scale is 20/30 relative to the unified world gravity of 30. Air Drag is 0. Weight is 1 kg. Physical settings remain per object; no Rigidbody2D or duplicated falling integration is introduced. Reset happens on scene reload.

RopeAttachment no longer requires Pushable. An assigned/local Pushable is still enabled/disabled exactly as before for existing crates; the plank has no Pushable component.

No compilation, automated tests or Play Mode were run. In Unity check initial rope alignment, top-only countdown, automatic/manual cutting, player riding during the fall, terrain contacts and disappearance. Review the new child hierarchy when assigning references or editing instances.

## Activation after prefab edits

Player is optional. If assigned, only that animal standing on Shape starts the countdown. Otherwise DroppingPlatform uses PropsMotion.HasStandingPassenger, populated by PlayerPlatformRide before the platform controller tick. New instances therefore work without a scene-specific player reference. Existing LoopScene player overrides were retargeted to the current gameplay component after prefab child recreation changed its file ID. Rope geometry and manual prefab transforms were preserved.
