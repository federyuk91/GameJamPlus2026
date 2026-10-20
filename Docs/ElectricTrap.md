# Electric Trap

Drag `Assets/Prefabs/Hazards/Electric Trap.prefab` into the scene. Its top pivot supports ceiling placement. Resize/move Box Collider 2D (Is Trigger) to define the lethal area.

- `Automatic Activation`: starts an electrified period when enabled. If off, waits for external input. The period expires and requires reactivation.
- `Activate()`: electrifies the area for `Activation Duration` (default 3 seconds), including later entrants. Calling it again restarts the duration. Kills overlapping piranhas and ends the test if the mouse is inside.
- `EnableAutomaticActivation()`: activates for the configured duration, including existing occupants.
- `DisableAutomaticActivation()` / `Deactivate()`: immediately switches off the area and light.
- `SetAutomaticActivation(bool)`: boolean UnityEvent configuration.
- `Show Gizmos`: red Scene View area outline.

Add the trap to Interactable Button → On Pressed and choose a public ElectricTrap method. Disable Automatic Activation for button-only use.

The prefab includes Dialogue Message Player and Test End Event for scientist comments/results. Discharge uses the standard white flash, comment, results and next-universe sequence. No additional tags/layers are required; it recognizes GameManager's Player Collider. The trigger stays enabled when automatic mode is off to support manual activation.

Player recovery avoids the area when automatic activation is enabled.

## Electrocuted piranhas

LoopScene's piranha includes Piranha Death, Simulated Body 2D, initially disabled Crate Motion, and Crate Buoyancy. Duplicates retain this setup; other piranhas need Piranha Death and its required flotation components. Traps automatically find active piranhas without Inspector lists.

Manual/automatic discharge hits all piranhas overlapping the trigger. Piranha Death disables patrol, chase and lethal contact, flips the sprite on Y and makes the collider solid. The body floats using the crate system and carries the mouse standing on it.

`Passenger Downward Acceleration` (default 45) adds downward acceleration while the mouse stands on the body. Increase it for faster sinking. Crate Buoyancy → Buoyancy Acceleration and Water Drag control rise/flotation. When the mouse leaves, buoyancy raises the body again. Existing crates are unchanged.

Check activation with multiple fish, fish without the mouse present, jumping on bodies, sinking under weight and resurfacing afterward.

## Light and duration

`Electric Light 2D` is a cyan Point Light 2D. Position it freely and edit Light 2D color/radius. Electric Trap switches it on/off and varies intensity using Flicker Frequency, Minimum Light Intensity and Maximum Light Intensity. At Activation Duration expiry, area and light switch off together. The gameplay-time timer pauses during game pauses, photos and test endings.

`Activate()` now starts an electrified period instead of an instantaneous discharge. Existing UnityEvents remain valid. Use Activate or EnableAutomaticActivation to switch it on again.

## Discharge sound

Every Activate / EnableAutomaticActivation / initial activation chooses one ElectricArc_01–04 variant randomly. The prefab has a dedicated 2D AudioSource with Play On Awake disabled. Discharge Volume (0.65) is multiplied by Effects volume; Master still uses AudioListener. Option changes affect sounds already playing.

The approximately 1.4-second sound plays once per activation, not on every trigger check. Deactivate or disabling the trap stops it. Reactivation restarts discharge without overlapping sounds.
