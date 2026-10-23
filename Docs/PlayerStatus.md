# Player status

The Gameplay HUD shows health and satiety in the compact bottom-right resource panel. Oxygen appears underwater and during recovery. Active status icons have their own conditional row. Collectible totals are visible in the top-right strip; tooltips and scientist communications are independent panels.

## Health and poison

PlayerHealth starts at five hearts. ApplyDamage(int) clamps health, emits Changed and Damaged, and reports whether damage was applied. Damage sources select their own lethal outcome. SatietyHud listens to Damaged for the red damage flash and light camera shake; SetHealth(int) remains available for direct health assignment.

Eating green cheese refills satiety, starts the existing poison ticks and immediately removes one heart. Poison turns the satiety notches green and shows Poison.png, referenced as a Single sprite (fileID 21300000). Normal cheese clears poison and restores the yellow bar. Poison completion also clears the status. Poison Damage at zero health uses PlayerSatiety's Poison Outcome when assigned, otherwise Starvation Outcome.

## Mouse traps

MouseTrap has editable Damage (default 3). The contact area is disabled on the first hit, preventing repeat damage. Closing frames and the gore sound still play; the trap deactivates after the animation and closed hold. Surviving the hit does not pause gameplay or end the stage. A lethal hit retains the trap's TestEndEvent and waits for closing before the stage transition.

## Electricity

PlayerElectricity defaults to three lethal charges. ElectricTrap's Electricity Amount adds charges once per activation. Reaching the lethal threshold invokes that trap's TestEndEvent. Electrical effects on piranhas are unchanged.

Decay Interval defaults to 60 seconds of active gameplay. While charges remain, one is removed per interval; reaching zero resets the timer. Additional shocks while already charged do not postpone the next decay. Pause and non-playing game-loop states suspend decay. Increasing charges triggers a pale-blue impact flash; natural decay only updates the icons.

## Unity review

Check green cheese at full and one-heart health, poison curing/completion, nonlethal and lethal mouse-trap contacts, and electric-charge decay including pause/resume. C# compilation and static asset references were checked; gameplay review remains manual.

## Electrical paralysis and effects

A successful ElectricTrap impact registers that trap on PlayerElectricity until its active discharge ends. PlayerInputReader blocks gameplay actions and PlayerMovement suspends movement while any shock remains. Other input locks, including camera inspection and wheel interactions, are retained independently. Disabling or deactivating a trap releases only its own shock; disabling the player clears active shock sources.

PlatformCameraController adds continuous vibration while the player is paralyzed; Shock Shake Amplitude controls its strength without removing pixel-grid snapping. SatietyHud's Shock Flash Color is the pale-blue impact overlay, independent of oxygen flashes and bubbles.

Activation Duration remains the single lifetime for the discharge, light, FX visibility and paralysis. Entering an already active discharge uses its remaining duration. The trap updates FX visibility every frame, stops on deactivation/disable or stage ending, and suspends its countdown during pause.

## Gamepad shock feedback

GameplayHudController drives ShockGamepadFeedback using the same active controller as InputDevicePrompts. Vibration remains active while the player is paralyzed by any trap. Shock Low Frequency Motor and Shock High Frequency Motor (0–1) control strength on the Gameplay HUD prefab. No motor commands are repeated while device and strengths are unchanged. Keyboard/mouse usage, controller disconnection, pause, application focus loss, stage ending and HUD shutdown stop the feedback; resuming an ongoing shock restores it.
