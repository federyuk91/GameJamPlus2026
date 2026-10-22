# Rope interaction

After each successful cut, every contiguous fragment with fewer than two intact pieces is removed, on both the anchor/crate side and the falling side. Exactly two pieces remain visible. Cleanup runs before grip reassignment and cut callbacks, so the mouse detaches and crate connections release when their last surviving fragment disappears. If no pieces survive, RopeSimulation is disabled and unregisters from interaction searches; its piece visuals are hidden without deleting the parent object. Uncut one-piece ropes remain unchanged. Review cuts producing 1+1, 2+1 and 2+2 fragments and cuts while riding/tethered in Unity; no Play Mode tests were run.

Player Rope Bite and Player Rope Interaction automatically choose nearby ropes from the registry of enabled RopeSimulation instances. No Player Ropes list is required; old lists no longer restrict interactions.

- Grab: nearest anchor-connected segment to the hand within Physics Settings → Interaction Areas → Rope Grab Radius. The rope must be Available.
- Bite: nearest intact cuttable segment within Player Rope Bite → Reach of the mouth, in front of or aligned with the mouse collider center (0.05-unit tolerance), without obstacles. Direction uses the collider center; the mouth may already be past the rope. Can Be Cut must be enabled.
- While hanging: biting prioritizes the held rope, ignores Reach and mouth direction, but still checks obstacles.

When biting a rope hanging from above, the mouse moves to the end of the still-anchored section and the lower section falls. A hanging bite never cuts segment 0: near the anchor it cuts the next segment, retaining at least one piece to hold. If only that piece remains, hanging bites are unavailable; it can still be cut from the ground.

If the crate is in water and its attachment point is above the anchor at the first cut, the crate side is retained. The crate loses its seabed constraint (Attached = false), reenables Pushable, but remains connected to the upper section (HasRopeConnection = true). The mouse reattaches to the first intact point toward the crate and rises with it, remaining able to climb or jump. The lower section stays on the seabed. This side selection persists at the surface. Later bites retain at least one segment on the crate side.

In both cases repositioning respects solid colliders; an unreachable new grip causes detachment.

Ropes register in OnEnable and unregister in OnDisable, including objects created/reactivated/removed during play. Grab/bite searches do not run FindObjects every frame. Bite progress resets when the target rope changes.

Check multiple nearby ropes, uncuttable ropes, obstructions, ropes created after startup and disabling a rope during a bite in Play Mode.

## Rope jump versus neutral release

Jump with horizontal directional input retains the existing boosted jump: the
chosen left/right direction redirects horizontal momentum, adds Rope Jump Swing
Boost and adds Rope Jump Up Speed. Jump without horizontal input detaches with
only a fraction of the actual current grip velocity, retaining its direction and
adding no impulse. A stationary rope therefore produces a simple drop.

PhysicsSettings > Rope Movement > Rope Release Momentum Multiplier controls this
neutral release (default 0.35, retaining 35% of grip velocity). The actual current
motion is used, not a remembered swing direction, so turning points do not receive
a stale launch impulse. Existing air control, collision limits and regrab delay
remain active after detachment.

On a rope jump, MouseFacing receives the final launch velocity and immediately
faces its horizontal direction, including neutral releases following the swing.
Vertical/stationary releases keep the previous facing. The collider mirrors through
the existing facing code. Ordinary directional input resumes normal facing control.

Rope biting now uses the mouth group (F / gamepad North). Targeting is unavailable
in free air: the mouse must be grounded, holding a rope or underwater. This
condition also controls the HUD hint. See PlayerActions.md for grouped controls.
