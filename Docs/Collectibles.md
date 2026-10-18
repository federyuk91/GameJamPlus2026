# Collectibles and messages

In LoopScene, eat Cheese by pressing F / gamepad West while its trigger intersects the player collider. No tags, special layers or Rigidbody2D are needed.

## Components

- `CollectibleCollector` on the player checks overlaps with `Collection Area`. With `Requires Interaction`, a fresh F/West press is required; holding before entering does not consume the item. One press consumes only the item nearest the player collider center. Disabling the flag restores contact collection.
- `Collectible` exposes `Type`, `Requires Interaction`, `Pickup Area` and `On Collected`. Its trigger must be on the same GameObject. Collection invokes the event once, then deactivates the object unless CollectibleRemains is assigned. With remains, it disables pickup and replaces the sprite while keeping the object visible. Reactivation does not reset collected state.
- `DialogueMessagePlayer` is independent of collection. `Play()` randomly selects a nonempty `Messages` entry with `Avatar` and `Duration`. Assign scene `Hud`; UI duration is constrained to 1–30 seconds.

## Setup

1. Add a trigger Collider2D and Collectible; assign Pickup Area.
2. Optionally add DialogueMessagePlayer with Gameplay HUD, optional avatar and messages.
3. Wire On Collected to DialogueMessagePlayer → Play().

Cheese already has this binding and three replaceable examples, without an avatar. Other listeners can add effects. The collector handles counts separately. Gameplay HUD owns display, so it survives collectible deactivation; a new communication replaces the current one.

Check one pickup/one message, alternative texts/avatar, another event listener and duplicated Cheese. Assign scene HUD references on instances.

## Types and counters

`CollectibleType` distinguishes Cheese and GreenCheese. Every pickup increments its type; GetCount(type) reads totals and CountChanged signals updates. No Unity tags are required. GameplayHudController receives the collector through Inspector: Cheese appears first, GreenCheese second, using the green icon. The original implementation initialized per-player counts and resynchronized on HUD enable; current session persistence is documented in `CollectibleProgress.md`.

For green cheese choose Type = Green Cheese and its sprite; dialogue remains independently configurable through On Collected.

## Green cheese and test ending

CheeseGreen requires interaction. On Collected calls TestEndEvent.EndTest(), using the prefab's DialogueMessagePlayer for scientist comments and a separate Result Messages list. GameManager sends the comment; do not also wire DialogueMessagePlayer.Play() to the same death event. Prefabs without Hud use GameManager's HUD for ordinary messages; an explicit Hud takes precedence.

## Photos

Photo Collectible.prefab uses the supplied PNG unchanged at Assets/Art/Pixel Art/Props/PhotoCollectible.png (Point, 32 PPU). An instance is near the player in LoopScene. Type Photo increments the fourth/rightmost counter and displays the Collect photo hint; F/West collects it.

Assign a Texture2D to CollectiblePhoto.Photo; Sprite import is unnecessary. Empty Hud falls back to GameManager. An empty photo opens a placeholder. On Collected calls CollectiblePhoto.Play(). The HUD displays the photo nearly full-screen, preserving aspect ratio, and pauses game/timer according to Pause While Photo Open.

Enter, Y/Triangle or the photo's Next button closes the preview and invokes CollectiblePhoto.On Continue. The first two photos resume gameplay. Closing the third photo collected by the current subject calls the assigned Curiosity Ending (TestEndEvent): white flash, scientist comment about excessive curiosity, TEST COMPLETE, then the next universe or EndGame at zero. Photos To End Test defaults to 3 and is editable on CollectiblePhoto. Do not bind EndTest directly to On Continue, since that bypasses the threshold.

Customize Photo, DialogueMessagePlayer.Messages/Avatar and TestEndEvent.Result Messages. One comment is “Three photographs already? Such curiosity! We had better take a break now.” Display and confirmation survive collectible deactivation; technical HUD closure or scene changes do not invoke On Continue.

Portrait, square and panoramic textures are centered and enlarged to fit without cropping/distortion. Layout updates on resolution changes, reserving space for Next.

## Five-cheese completion

LoopScene's Game Manager has a CollectibleTestGoal wired to the player collector and a dedicated TestEndEvent. Collecting five normal Cheese items during the current test completes it with an enthusiastic scientist comment. Subject Died is disabled: the mouse is counted as recovered. GreenCheese is a separate type and keeps its existing outcome. Required Count defaults to 5 and can be edited on the scene component.

GameSession tracks per-test counts separately from cumulative HUD totals. Advancing to the next subject resets per-test progress. The existing HUD blocks ordinary pickup dialogue during test ending, so the fifth cheese does not replace the completion comment. Check in Unity that cheeses one through four keep gameplay active, the fifth ends exactly once, and a new subject starts at zero progress.
