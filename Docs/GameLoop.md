# Test loop

LoopScene contains Game Manager assigned to Gameplay HUD. Sessions start at Universe-10. GameSession keeps the universe across scene loads for the run; New Game resets it before starting again.

## Ending a test

1. An event calls TestEndEvent.EndTest().
2. GameManager captures test time and randomly selects Result Messages. Further ending requests are ignored.
3. Gameplay/timer stop; a configurable white flash covers the scene and remains visible.
4. The assigned DialogueMessagePlayer provides a random scientist comment and avatar. These comments require confirmation rather than timing out.
5. Dialogue-button click, Enter or Y/Triangle first completes typing, then confirms Next. TEST COMPLETE displays “Success! ... hours ... minutes and ... seconds.”, the result and Universe-N.
6. Next / Enter / Y/Triangle decrements the universe and reloads LoopScene. After Universe-1, zero loads EndGame.

MainMenu, LoopScene and EndGame are in Build Profiles. EndGame reports session results and selects one of three death-count endings after Continue; see EndGame.md. Reloading restarts the test timer and recreates scene collectibles; session collectible totals are described in CollectibleProgress.md. Welcome text uses {universe} for the current number.

Pause uses Time.timeScale = 0; flashes, typing and UI transitions use unscaled time. Loading restores the previous scale. Music continues during comments/results.

## Death-cause setup

Add TestEndEvent to the source object:

- Scientist Comment: DialogueMessagePlayer containing avatar/comments, displayed by GameManager on its HUD.
- Result Messages: nonempty candidate results; one entry gives a fixed result.
- Subject Died: enabled for death causes, disabled for surviving outcomes; feeds report totals and ending selection.
- Game Manager: optional explicit scene reference; empty prefab fields use the active manager.

Wire trap/enemy/collectible events to TestEndEvent → EndTest(). CheeseGreen already uses Collectible.On Collected with its existing scientist comments. Connect other death events as implemented.

## Unity review

Normal cheese increments its counter and displays an ordinary message. Green cheese increments the second counter, pauses, shows white/comment, then time/result; the next confirmation starts Universe-9. For quick EndGame review start a new run with Starting Universe = 1, then restore 10. Check mouse/gamepad, long messages and duplicate-advance prevention. Current collection input is F / West.

## Experiment photos

Photo Collectible opens a photo first and waits for Next (Enter / Y / button). The first two photos collected by the current subject resume gameplay after Next. Closing the third invokes the assigned curiosity TestEndEvent, using the same loop as green cheese. Scientist comments and results explain excessive curiosity; the per-subject count resets on advancing, independently of cumulative HUD totals. Photo pause ownership transfers to GameManager without one gameplay-resume frame. See Collectibles.md.

## Lethal piranha

LoopScene's Piranha has a trigger BoxCollider2D and LethalContact assigned to Player Collider and TestEndEvent. Post-movement collider-distance checks work without Rigidbody2D. Contact is lethal even during patrol and requests the ending once.

Edit comments on DialogueMessagePlayer and results on TestEndEvent. Subject Died is enabled. LethalContact is reusable with assigned Contact Area, Player Collider and Test End; keep the area a trigger so it does not block movement.

## Spawn Points

Assets/Prefabs/GameLoop/Spawn Point.prefab is an empty point with a green Scene View gizmo. Position it by the player pivot, not feet. Point rotation does not rotate the mouse.

Assign Player, Gameplay Camera and scene instances in GameManager.Spawn Points. Each LoopScene instance randomly chooses an assigned active point with an enabled component; nulls are ignored. Awake places the player before component initialization. Startup snaps the camera to avoid the old position; player Z is preserved.

LoopScene includes one assigned point at the original player position. Duplicate and assign additional instances; unassigned scene points do not participate. Place points in free space: initial spawning does not automatically validate geometry. Without valid points, a warning is logged and the saved player position is retained.

Availability uses enabled, activeInHierarchy and a valid scene rather than isActiveAndEnabled, which may still be false during GameManager's early Awake for child points. Disabled points/inactive parents are excluded; Project prefab assets are not valid instances.

## Goal exit

LoopScene's Goal is a level escape. Its 1×1-unit BoxCollider2D is a trigger; TestEndContact compares it with the assigned player collider after simulated movement, without Rigidbody2D callbacks. Contact invokes the assigned TestEndEvent once and follows the existing comment/results/next-test sequence. Subject Died is disabled, so the mouse is recovered alive and does not increase ending death counts. Comments and result messages are editable on Goal. Selecting Goal shows the contact area in green; adjust collider Size/Offset in the Inspector as needed.

Check in Unity: enter Goal from both sides, verify one completion and one recovery, then confirm progression to the next test (or EndGame after the last test).

Goal is now the reusable prefab at Assets/Prefabs/GameLoop/Goal.prefab. LoopScene contains an instance at its original position. Sprite, trigger, contact detection, survival outcome and scientist dialogue are stored in the prefab. Player, GameManager and HUD references are assigned as scene overrides; newly placed instances can fall back to the scene GameManager for those references.
