# Breath-holding and oxygen

Player.prefab includes Player Oxygen with an independent five-notch reserve. When the breathing point is submerged in a Water Volume, the satiety bar displays OXYGEN with cyan notches.

- Maximum Notches: 5.
- Drain Interval: 5 seconds per submerged notch.
- Recovery Interval: 1 second per notch outside water.
- Breathing Point Height: 0.85 within the collider (0 = feet, 1 = top), horizontally centered. The mouse can breathe at the surface when this point is above water.

At zero oxygen, Drowning Outcome ends the test through the usual white flash, scientist comment, results and next universe. Player has dedicated Dialogue Message Player and Test End Event components with editable messages.

Outside water the bar immediately returns to SATIETY. Oxygen recovers in the background, one notch every second. Satiety continues draining while submerged; cheese restores satiety but does not affect oxygen. Each lost oxygen notch causes a faint full-screen cyan flash without extra voices or shake. Satiety Hud exposes Oxygen Flash Color and Duration (0.25 seconds, alpha 0.16).

Pauses and test endings stop drain/recovery. A new universe starts at full oxygen. Brief resurfacing preserves partial drain: a complete recovery interval restores one notch and clears partial consumption of the next. Reentering water before completion interrupts recovery.

LoopScene links Satiety Hud to the prefab's Player Oxygen. Events update UI on state/notch changes. Other HUDs must assign Oxygen in Satiety Hud.

Check five-second drain, return to satiety display, recovery/reentry, surface breathing, pause, drowning/new universe, and no oxygen restoration from cheese.

## Ten-second warning

Player Oxygen exposes Warning Seconds (10) and On Low Oxygen. The prefab invokes Play on a separate Dialogue Message Player with editable warnings lasting 6 seconds. This normal comment does not pause gameplay or oxygen drain; the death comment is separate.

The threshold uses actual remaining time, including partial-notch consumption: with five notches at five seconds each it triggers at two remaining notches. It appears once and rearms only after full recovery. Brief resurfacing does not repeat it.

## Bubbles after the flash

After the cyan flash, Satiety Hud calls PlayBurst on Player Oxygen Bubbles. Exactly three bubbles are emitted sequentially using bubble1, bubble2 and bubble3, imported with Point, 32 PPU and no compression.

Emission Interval (0.12 seconds), Rise Speed (1.5 units/s), Wobble Width (0.04) and Mouth Height (0.75 within the collider) control the effect. Mouse Facing determines the side. Bubbles move independently of the mouse on the pixel grid and disappear on reaching the surface/leaving the water volume. If the mouse has surfaced, no bubbles are emitted into the air.

SpriteRenderers are reused. Pause/test ending stops bubble motion. Assign Oxygen Bubbles in Satiety Hud for other HUDs; LoopScene is already connected.
