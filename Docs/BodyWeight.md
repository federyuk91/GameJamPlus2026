# Body weight and supported loads

SimulatedBody2D owns the only serialized Weight field, expressed in kg. The Player prefab is configured to 0.03 (30 g); props retain their existing weights (normally 1 kg). Change SimulatedBody2D > Weight in the Inspector, including during Play Mode. Pushable.Weight and PropsMotion.Weight are read-only forwards to that body value. BalloonLift.LiftCapacity uses the same kg scale. Weight changes supported loads, not the character's jump/movement settings.

A 30 g mouse is a plausible design default: JAX reports 12-week C57BL/6J means of 21.7 g for females and 29.3 g for males. A guinea pig is a different animal and can exceed 1 kg; choose a larger value if that is the intended species.

- Mouse data: https://www.jax.org/jax-mice-and-services/strain-data-sheet-pages/body-weight-chart-000664
- Guinea pig data: https://nc3rs.org.uk/3rs-resources/housing-and-husbandry-guinea-pig

## Supported body weight

PlayerPlatformRide registers its SimulatedBody2D on the supporting PropsMotion, which reads that body's current Weight. The player's weight is added to gravity and inertial mass for flotation and coordinated vertical groups. Jumping, leaving the support or disabling the player releases the load. The dead piranha uses this calculation too; its previous fixed extra downward acceleration has been removed.

## Hanging body weight

PlayerRopeInteraction exposes the same weight to RopeSimulation. If the rider is connected to only one endpoint, that endpoint carries its full weight. If both endpoints remain connected, a linear share based on grip distance is assigned to each end; the shares sum to one body weight. This is a gameplay approximation, avoiding double-counting in a balloon/cargo group. Cutting a segment or releasing the rope updates the load without retaining a stale weight.

RopeAttachment supplies the endpoint share to its PropsMotion. BalloonLift supplies the balloon-anchor share. Static vertical load enters the weight balance once; horizontal swing steering uses the weight-scaled rope tension separately. Swing Influence adjusts steering strength, not body mass. A rider may steer less strongly with a realistic 30 g mass than with the earlier unit-weight approximation; tune Swing Influence if needed for the desired gameplay response.

## Balloon balance

Capacity is still 0.5 kg with a default neutral margin of +/-0.1 kg. Include the mouse when judging the total load: a 0.58 kg crate with a 0.03 kg mouse standing on it totals 0.61 kg, beyond the neutral band. A 0.35 kg crate with that mouse totals 0.38 kg and still rises, more slowly than without the rider. Wind, water and other stacked objects also contribute.

The free balloon uses its remaining capacity for a standing/hanging rider too. The neutral margin is intentionally larger than the default mouse weight; the mouse shifts the balance most noticeably near its boundaries. Hover Weight Tolerance remains independently configurable.

## Weight and naming migration

The standalone PlayerWeight component and Pushable's former serialized weight were removed after their existing values were migrated into the corresponding SimulatedBody2D in prefabs and scenes. Script field references to PlayerWeight must now use SimulatedBody2D.Weight. Own-body consumers explicitly require SimulatedBody2D; rope endpoints read their connected bodies instead of creating an extra physical body on a static rope anchor.


PushableCrate is now Pushable; CrateRopeAttachment is now RopeAttachment. Both source files and their .meta files were renamed together, keeping the original script GUIDs. C# references, editor scripts and documentation use the new names; MovedFrom attributes record the former class names. External C# scripts using the old type names need to use the new names. Existing serialized component/field references retain their GUIDs and field names.

No compilation or Play Mode checks have been run. In Unity, verify stepping onto/off floating objects, jumping, grabbing/releasing/cutting ropes, and crossing the balloon's hover band by adding/removing the mouse.

## Shared body settings and prop resistance

SimulatedBody2D now owns the PhysicsSettings asset reference. PropsMotion and player physics behaviours read it from their body; footsteps use PlayerMovement.Body. Prefabs and full scene components were migrated by matching components on the same GameObject; stripped prefab references inherit their source configuration. Standalone RopeSimulation keeps its own world settings reference. Assign settings once on the body for future objects.

Air Drag and Water Drag also live on the body, with explanatory tooltips. Ordinary props preserve Air Drag 0.35; balloons preserve their active Air Resistance value 2.5. Water Drag preserves 5 and is read by PropsBuoyancy, including vertical group damping. The old serialized fields on PropsMotion, BalloonLift and PropsBuoyancy were removed. Character control acceleration, water braking and vertical swim damping remain separate; the body resistance fields drive prop motion, not player control tuning. A balloon whose lift behaviour is disabled now retains its body Air Drag rather than switching to the removed ordinary-prop value.

Ground Friction is now a force-based coefficient on the body. It replaces the former Ground Drag damping fraction and is used by the shared GroundMotion solver for player and props. See GroundPhysics.md for the formula and migration.

RopeSimulation owns Taut Tolerance (0.01 world units). BalloonLift and RopeAttachment both use it to decide whether the rope is taut; unrelated geometric tolerances remain separate.

The mouse Animator uses different RopeClimb and LadderClimb clips. Both serialized hand offsets were (0, 0.2), so PlayerMovement.MovementSettings.Hand Offset now owns that shared value. Both interaction components reference the same PlayerMovement. Changing it adjusts both grips.

SimulatedBody2D.Max Speed is now the single movement speed ceiling, in world units/s and applied to vector magnitude. It replaces the balloon's horizontal/vertical limits, PropsMotion's water vertical limit and the global crate/player terminal speeds. The Player prefab preserves 26; props use their previous water limit (normally 2); balloons use the strictest of their former limits (normally 1.5). Free prop falls are therefore slower than the former 26 cap, and balloon horizontal movement is now capped at 1.5 rather than 3. Coordinated groups use the lowest member ceiling and prop motion/corrections share a total travel budget. Balloon ascent still slows near neutral payload balance. Player movement, swimming and interaction targets can impose lower requested speeds; they are not additional universal physical ceilings.

No compilation, automated validation or Play Mode was run. In Unity review balloon drift, water damping, dry-ground stopping, rope tension and both grip animations.

Ground Friction is a dimensionless coefficient; Air/Water Drag remain damping rates in inverse seconds. Ground Friction 1 holds a gravity-only body at rest through 45 degrees; it no longer means an unconditional instant stop. Direct transform teleports are outside the movement methods and do not represent simulated velocity.

## Non-blocking props and individual gravity

SimulatedBody2D.Shape is now assignable in the Inspector, retaining the local collider fallback for existing bodies. Ignore Player Colliders optionally filters the animal from movement casts. Gravity Scale multiplies the single world gravity (30) and is consumed by both PropsMotion and the animal controller. Migrated ordinary props use 2.2, cheese/plates 1 and the player 1. The default scale for new bodies is 1.

PropsMotion coordinates ordinary props before optional Follow Support objects within each fixed tick. Followers use the same gravity/casts/speed limits but follow a support-local point and are excluded from load-bearing stack grouping. Cheese and empty plates use trigger footprints and keep a distinct pickup trigger.

GroundContact2D now owns supporting-surface detection and state, with references assigned on each authored body. SimulatedBody2D forwards those results and owns Max Slope Angle. Player and prop motors share the force/friction formula documented in GroundPhysics.md.

## Unified world gravity

PhysicsSettings.gravityAcceleration is the only world gravity acceleration. SimulatedBody2D.GravityAcceleration computes world gravity times its local Gravity Scale. RopeSimulation has its own Gravity Scale and uses the same world value for segments and rider tension. Swimming applies water.gravityMultiplier to the animal body gravity only; it does not alter prop buoyancy. Rope launch speeds moved to interactions. See GravityUnification.md for converted values.
