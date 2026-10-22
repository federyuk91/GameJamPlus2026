# Hand release intro

Welcome's existing HandRelease has an Animator and HandReleaseIntro. GameManager
references this component in Hand Release Intro. Other scenes leave that optional
reference empty and retain their normal spawn behavior.

Assets:
- Art/Pixel Art/Characters/Scientist/HandRelease/handRelease1.png through
  handRelease6.png: renamed from mano sinistra1-6, preserving all GUIDs.
- Animations/HandRelease/HandRelease.controller: Idle and Release states.
- HandRelease_Idle.anim: six frames, looping at 8 fps.
- HandRelease_Release.anim: temporary hand-only frame. The native Sprite asset
  crops the same source texture above the drawn tail/mouse, without editing PNGs.

GameManager chooses its normal random Spawn Point or checkpoint in Awake. It
enters Spawning state so hazards and gameplay cannot end the stage. In Start,
after all player Awake initialization, the intro captures the chosen position,
disables the real Player GameObject and detaches it from the hand at runtime.
Keep the authored Player active: it is automatically disabled before the first
gameplay tick, allowing vitals and input Awake to initialize normally.

The hand appears at spawn plus Spawn Offset (world units; default Y +1). Its
artwork includes the suspended mouse, so the real player's renderer is inactive
along with its controls, physics, interactions and vitals. The HUD timer waits.
Any keyboard key, mouse button or gamepad button initiates release. Stick movement
alone does not. Pause and dialogue advance are suppressed while Spawning; the
initiating key is separated from gameplay activation by one frame.

After Release Delay, the player activates at the captured spawn position exactly.
Normal input, timer and gameplay resume. The hand finishes the release clip,
travels upward by Exit Distance and fades over Exit Duration, then deactivates.
The detached player stays active and does not follow the hand.

When the final animation exists, assign it to Release Animation on HandReleaseIntro.
An instance AnimatorOverrideController replaces the Release motion without changing
the shared controller. Release Delay sets the activation moment; alternatively,
an Animation Event calling ReleasePlayer may activate earlier than that delay.
Exit starts after the release clip (or delay, if longer) completes.

Tools > Gameplay > Configure Hand Release Intro safely configures the loaded
Welcome scene with Undo. It does not open, reload or save scenes. Save Welcome
normally afterwards. Review Spawn Offset, release timing and visual alignment in
Play Mode. No automated gameplay tests were run.

A requested persistent repair additionally verifies the live Game Manager reference,
backs up the previous scene file under Recovery/HandRelease_timestamp, and saves
the current in-memory scene through Unity. Play-mode exit defers setup until
EnteredEditMode so transient play-scene changes cannot be mistaken for a repair.

GameManager checks the hand with enabled + activeInHierarchy in Awake, rather
than isActiveAndEnabled: the manager runs at execution order -200, before the
hand OnEnable, so the latter can report false despite a valid active reference.

## Intro framing and first prompt

While awaiting release, Gameplay HUD sets PixelPerfectViewport to its maximum
zoom-in step (+1), keeping integer pixel scaling. Normal zoom input is blocked by
Spawning state. The lower communication monitor displays the persistent system
hint "Press a button to start", without a scientist avatar, typing sounds, speech
or timeout. The welcome comment waits until release. Once the real player is
activated, the previous zoom level returns, the prompt disappears and the normal
welcome communication begins (when Show Welcome On Start is enabled).
