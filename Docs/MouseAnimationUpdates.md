# Scientist portrait and updated mouse animations

Wall ledge hanging uses the dedicated WallHanging state and Mouse_Wall_Hanging.anim, now looping the four frames of Mouse_Hanging_Sheet_Wall at 6 FPS instead of the previous temporary static climb-up pose. The new sheet's existing slices, pivots and sprite IDs are preserved, as is the wall clip's GUID. Rope idle hanging retains Hanging / Mouse_Hanging.anim and Mouse_Hanging_Sheet. Wall climb-up still uses the complete Mouse_ClimbUp sequence. MotionState 11 was appended without renumbering existing states. Review hanging on a wall versus a rope in Unity; no Play Mode tests were run.

LoopScene Gameplay HUD uses Scientist.png as Scientist Portrait (default for dialogues without a custom portrait) and Welcome Portrait. Cheese and CheeseGreen also use the scientist portrait explicitly. The HUD fallback applies to normal messages, oxygen warnings and test-ending comments; an assigned custom portrait still overrides it.

The Mouse Animator now connects RopeClimb to Mouse_Rope_Climb (4 frames from Mouse_Rope_Climbing_Sheet), ClimbUp to Mouse_ClimbUp (3 frames from Mouse_Wall_ClimbUp_Sheet), and Swimming to the updated Mouse_Swimming (5 frames from Mouse_Swimming_Sheet). All use the existing 32×32 slices and 32 PPU. Rope and swimming loop at 8 FPS. ClimbUp plays once and holds the final frame until the movement leaves the climb state. LadderClimb keeps its existing dedicated clip.

Player Animation exposes Rope Animation Speed, Climb Up Animation Speed and Swimming Animation Speed. The existing movement-state selection chooses these animations without changing movement or collider behavior. RopeClimb still shares the LadderSpeed Animator parameter, with its own speed setting from Player Animation.

Check in Play Mode: climb a rope, climb a ledge from hanging and from the ground, swim, return to ladder climbing, and view a normal/death/oxygen-warning dialogue.

## Mouse button press gesture

PlayerButtonInteraction emits ButtonPressed only after InteractableButton.TryPress succeeds. PlayerAnimation subscribes through its assigned Inspector reference and plays the dedicated PressButton state once, then returns to the current locomotion state. The timer uses scaled time so pause freezes the gesture. Movement and button events retain their existing behavior; the gesture does not lock controls. Existing wall/rope/ladder states take precedence if entered during the gesture.

Mouse_PressButton.anim temporarily reuses the six Mouse_Push_Sheet frames because no dedicated mouse button-press sheet exists in the project. It is a separate non-looping clip, so crate pushing remains unchanged. Replace the PressButton state's Motion and PlayerAnimation's Press Button Clip reference when final artwork arrives; Press Button Animation Speed controls playback. Both new references are assigned in Player.prefab.

No compilation or Play Mode tests were run. Review a successful F/East press, unavailable/busy buttons, pause/resume mid-gesture and return to idle/run in Unity.
