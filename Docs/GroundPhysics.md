# Shared ground physics

## Responsibilities

- `GroundContact2D` detects the support using its assigned body's shape, skin and collision filter. It owns contact, normal, point, collider, angle and walkable/steep state. It is explicitly refreshed by the movement driver and after body moves, rather than running a second independent physics tick.
- `SimulatedBody2D` owns Max Slope Angle, Ground Friction, Weight and Max Speed, forwards contact state for existing consumers, and performs collision-constrained movement. It requires a ground detector; authored bodies reference it through the Inspector.
- `GroundMotion` is a stateless shared calculation. It integrates tangential force and friction without depending on a MonoBehaviour or settings asset.
- `PlayerMovement` supplies animal gravity and requested movement. `PropsMotion` supplies prop gravity, loads and environmental forces. Both use the same ground calculation instead of separate sliding coefficients.

## Formula

For tangent `t`, upward surface normal `n`, total applied force `F`, positive mass `m` and coefficient `mu`:

```text
Normal reaction = max(0, -dot(F, n))
Tangential acceleration before friction = dot(F, t) / m
Tentative tangential speed = dot(velocity, t) + acceleration * dt
Tangential speed = MoveTowards(tentative speed, 0, mu * normalReaction / m * dt)
Final velocity = tangent motion + any outward normal motion
Final velocity magnitude <= Body.MaxSpeed
```

At rest, a tangential force within the friction capacity is cancelled. Friction cannot by itself reverse movement. Forces pushing away from the surface can lift the body. With gravity alone, the threshold is `tan(angle) <= mu`; mass cancels from acceleration, while it still determines force and response to externally applied forces.

The player keeps requested walking speed separate from passive slope velocity. Uphill input can brake passive sliding using its existing Ground Acceleration. On slopes beyond the body's Max Slope Angle, uphill walking is blocked while passive motion remains physical. The slope solver uses normal player gravity on the ground, and the existing water gravity when walking underwater; Fall Gravity Multiplier remains for airborne descent only.

Props refresh contact each substep. Ordinary supported props use the solver directly, including water lift/drag when touching ground. A grounded balloon passes its lift driver's resulting impulse to the ground calculation without applying lift twice. Floating groups retain their existing coordinated vertical solver; their supported/stack movement needs particular manual review on ramps.

## Migration and changed behaviour

- Removed global `maxSlopeAngle`, `slopeSlideAngle`, `slideOnSteepSlopes`, `slopeSlideAcceleration` and `slopeSlideMaxSpeed`.
- Player Max Slope Angle preserves 45 degrees. Ground Friction is 1, corresponding to tan(45 degrees), so the player holds at rest on gravity-only slopes through 45 degrees. The initial migration retained the old 30-degree threshold; this was subsequently aligned to the intended 45-degree hold angle. Acceleration and sliding speed do not preserve the old hand-tuned values: they now follow the force formula and Body.MaxSpeed.
- Existing authored props receive Max Slope Angle 89 degrees, preserving their broad support acceptance. Their former Ground Drag numbers become friction coefficients (normally 1, holding up to 45 degrees under gravity alone).
- Ground Friction replaces the old 0-1 damping fraction. It has no upper limit of 1 and different units/meaning. `FormerlySerializedAs("groundDrag")` preserves unconverted serialized values numerically; review any external or unsaved overrides.
- GroundContact2D components and references were added to prefab bodies and full scene bodies. Stripped prefab instances inherit their source detector.
- General prop movement now uses the same swept surface projection as player movement. Pushable horizontal movement follows the contact tangent; PropsMotion compensates that vertical rise to avoid applying it twice.
- The body's `RefreshGround` no longer takes a slope angle. `MovePlayer` no longer takes a separate maximum slope parameter. Both read the body's single setting.

## Manual Unity review

No compilation, automated tests or Play Mode were run. Review flat-ground rest/braking, player slopes below/at/above 45 degrees, props around their friction threshold, uphill/downhill input, jump takeoff/landing, ledges, pushed stacks, crates in water near ramps and rope-supported planks. Changing mass alone should not change gravity-only slope acceleration; changing friction should change holding and braking. All motion remains capped by each body's Max Speed.

## Floor/wall corner casts

SimulatedBody2D now casts a box inset by at most Skin on each side, while leaving the real BoxCollider2D unchanged. This avoids starting the cast inside a floor contact, which can mask a neighboring wall belonging to the same tilemap/composite collider. The inset is converted back into full-body travel distance using the hit normal, the box axes and the approach direction; existing callers still apply their normal Skin clearance. The query extends far enough to account for that conversion on oblique contacts. The hit buffer is now a reusable list instead of a fixed 32-hit array.

No scene, tile collider, movement speed or grab setting is changed. Manual Unity checks: ladder jump into the lower-right wall/floor corner from the reported screenshot, rope release beside a wall, walking/sliding on ramps, low-step ascent and prop collisions. This source change has not been compiled or exercised in Play Mode; the reported crossing needs confirmation in Unity.
