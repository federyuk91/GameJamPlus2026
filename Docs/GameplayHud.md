# Gameplay HUD

The requested LoopGame scene is Assets/Scenes/LoopScene.unity. Gameplay HUD contains UIDocument and GameplayHudController, assigned to Main Camera's viewport, UXML and Cheese_0.

## Initial layout

Assets/UI Toolkit/Gameplay/GameplayHud.uxml and GameplayHud.uss use separate GameplayHudPanelSettings.asset. The initial overlay had transparent world space, dark squared panels and cyan accents, two 20-pixel bands in 640×360, a centered hh:mm:ss timer, three bottom-left counters and a bottom-right scientist display welcoming the player. Timer text changes only each second and uses Time.deltaTime, pausing at timeScale zero. Later layout revisions below supersede initial dimensions/counter placement.

The controller clones PanelSettings at runtime and matches PixelPerfectViewport's integer scale/centered frame, including non-multiple screens. Original settings stay unchanged. Only dialogue buttons and text scrolling capture pointer input; other HUD elements ignore input/focus. No additional EventSystem is required.

## Component API

Assign references through Inspector. Namespace: GameJamPlus.UI.

```csharp
[SerializeField] private GameplayHudController gameplayHud;
[SerializeField] private Sprite scientistPortrait;

private void OnGameplayEvent()
{
    gameplayHud.SetCounter(0, 4);
    gameplayHud.ShowScientistComment("Excellent work. Keep going.", scientistPortrait);
}
```

Counter indices are 0–3; negative values become zero. Counter Icons overrides per-slot images; empty slots use Default Counter Icon, assigned to cheese.

ShowScientistComment(string text, Sprite portrait, float duration = 30f) replaces current dialogue. The string[] overload shows nonempty messages in order with one portrait and closes after the last. It copies the array; a new call replaces the entire sequence.

```csharp
gameplayHud.ShowScientistComment(new string[]
{
    "Welcome to Universe-10. Relax and enjoy your stay.",
    "Take your time exploring the room."
}, scientistPortrait);
```

The CRT opens from a central point to horizontal line to vertical expansion, with a panel-confined glow; closing reverses this. Content stays hidden until startup, then typing runs at 30 characters/s in terminal green. Per-message timeout starts after startup and includes typing. duration is constrained to 1–30 seconds; manual reveal does not reset it. Expiry advances/closes even unfinished typing. Animations, typing and timeout use unscaled time and work while paused.

Long text wraps and scrolls to the latest typed line. Mouse wheel over text, Page Up/Down and right stick allow review. Manual scrolling suspends automatic following until the next message without resetting timeout. Button actions reveal unfinished text, advance completed text or close the final message, labeled Reveal / Next / Close for the active device. Historical Enter/North bindings were later replaced by F/West; see InteractionHints.md.

HideScientistComment() immediately interrupts/hides communication. No voice audio is attached to dialogue. Welcome Text, Portrait, Duration and Show Welcome On Start are Inspector settings; scene welcome duration is 30 seconds and portrait assignment is ready for final artwork.

ResetTimer() clears time; SetTimerRunning(bool) pauses/resumes it; ElapsedSeconds reads it. A new scene instance restarts at zero.

## Manual Unity review

No automated tests/Play Mode were run for the HUD revisions. Check 640×360 plus 1280×720, 1920×1080 and 1366×768; band/frame alignment, portrait proportions, message arrays, reveal/advance/close, timeout behavior including pause, counters and timer pause/reset. Historical UI Builder previews hid dialogue; current preview behavior is described below.

## Collectible wiring and lower-band layout

HUD Collectible Collector references the player. CountChanged drives Cheese slot 0 and GreenCheese slot 1; manual SetCounter changes are resynchronized on pickup/HUD reenable. See Collectibles.md and CollectibleProgress.md for later four-counter persistence.

Four counters share a 280×60 resource/satiety panel with 20-pixel icons, 11-pixel text and cyan borders. The original fourth slot used default cheese imagery and SetCounter(3, value); later photo wiring replaces it.

Resource/communication panels sit 10 logical pixels above the bottom within an 80-pixel reserved band. The timer moved to the top-left 20-pixel band, originally 70×18. Communications originally occupied 300×60, with a right portrait frame 60×52, image 54×39 and SCIENTIST caption; later split terminals supersede this structure. Sequence position/remaining seconds are no longer displayed, while internal timeouts and Reveal/Next/Close remain.

## Terminal restyling — 18 October 2026

Reference: codex-clipboard-d16df1c8-8e60-4a3a-809f-1af39e2362f5.png. Dark navy, cream/gold/cyan borders, green status/text. Satiety and four counters share a frame. Collectible sprites, procedural bulb and input icons were retained.

Only LoopScene sets PixelPerfectViewport.Bottom Border Pixels to 80, retaining 20 above. Visible world area is 640×260 in the 640×360 base. Existing masking reserves 60 extra lower rows without changing rendering grid/framing; HUD does not overlap visible gameplay. -1 on other instances matches bottom to top.

ScientistCommentView API/coroutines remain compatible: game-provided portrait, individual/array messages, typing, timeout, device-aware controls, scrolling and CRT transitions. Background artwork contains no fixed portrait/message. Asset provenance/prompts: GameplayHudVisualAssets.md. Check legibility, CRT frame behavior, long text, varied portrait ratios, timer, pickups, satiety, device switching and player framing near the lower edge.

### Borders and backgrounds

Counters have transparent backgrounds and no USS borders. .scientist-portrait-frame, .scientist-message-panel and .scientist-advance also have no USS borders and remain editable even when scientist-screen is hidden. CommunicationTerminal_Background contains the outer frame art.

### Persistent terminal and status light

The outer frame remains visible without dialogue. The light is red at rest and green from communication startup through closing, then red again. The internal-channel heading was removed. CRT animation affects the centered scientist-display clip window; content dimensions remain constant, glow stays internal and the light remains outside animation.

ScientistCommentView.IsVisible means active communication, including transitions, rather than persistent-frame visibility. Hide() clears text/portrait and restores red. Hint/input priorities, arrays, typing and timeouts remain unchanged.

### Separate terminals and nine-slicing

Two unique panels replace duplicates: scientist-screen for text (176×60, right 77), scientist-avatar-screen for portrait (60×60, right 4), retaining manual-layout positions. One LED under the portrait is green for communication/SYSTEM hints, red at rest. No duplicate text/buttons/labels appear in the avatar panel.

GeometryChangedEvent sizes animation/content from scientist-display-viewport. The old 290-pixel fixed content and manual 20-pixel left offset were removed. Text fills viewport width and scrolls vertically. Buttons reserve a row only for scientist dialogue; ordinary communications reserve no heading row.

USS uses -unity-slice-left/right/top/bottom, -unity-slice-scale and -unity-slice-type: sliced. Resources/text/avatar borders use 6 px; timer uses 6 on sides and 3 vertically. Original textures/icons are unchanged. Edit UI Builder → Background → Slice. [Unity background styling reference](https://docs.unity.com/en-us/engine/6000.3/manual/uitoolkits/uielements/uie-uss/properties/uib-styling-ui-backgrounds).

Public message APIs remain compatible, but custom UXML copies must adopt distinct panel names/new containers. Check long text, resizing, sliced frames, isolated portrait and action priority manually.

### Preview before Play

UXML previews “Welcome to Universe-10. Relax and enjoy your stay.”, F Close and a green LED. Select scientist-text or .scientist-text in UI Builder to edit typography, spacing and color. Display/content fill the preview interior. Play initialization calls Hide() to clear the placeholder; gameplay then owns messages/LED/animation. The placeholder is not queued dialogue.

### Manual layout without footer

scientist-footer was intentionally removed; the view directly controls scientist-advance. scientist-dialogue reserves button space only for dialogue/preview; SYSTEM hints use full height. Text is in the scrolling transcript with manually configured font 10, middle-left alignment and padding. Fixed dimensions became min-height and width 100% to allow expansion/scrolling. Button anchors remain bottom 4/right 2, minimum height 12 and adaptive width.

CRT glow is a sibling of content rather than a ScrollView child, so it neither scrolls nor gets clipped there. Portrait sits directly in its own terminal with frame/LED margins. Manual avatar right 6 and LED left 7/bottom 7 positions were retained at this revision; later alignment below centers the LED. Placeholder, slicing and priorities remain.

### Internal alignment

Display margins: 8 px horizontally, 7 vertically. SYSTEM and first icon share the left edge; header is 7 px with a 3-pixel hint gap. Commands use a 12-pixel icon column and 5-pixel text gap, aligned to the first wrapped line. Ordinary messages do not add another inner padding. LED is centered below portrait (left 27, bottom 7 in a 60-pixel terminal). Frames, icons, terminal positions and logic stay unchanged.

### Satiety, counters and timer alignment

Resources use 8-pixel horizontal/7-pixel vertical margins, a 12-pixel heading and 6-pixel counter gap. Satiety caption is 8 px; notches 14×7 centered. Four equal columns retain 20-pixel icons, a 6-pixel value gap, 10-pixel numbers vertically centered in a 20-pixel region, without backgrounds.

Timer is now 80×18 in the upper band: 5-pixel title on a 6-pixel row, 7-pixel digits on a 9-pixel row, padding top 1/bottom 2. Rows do not shrink; text is centered and digits reduced to avoid bottom clipping. No logic/icons changed. Visual review remains manual.

## Automatic communication timeout

All existing scientist dialogue components and the welcome message use a 7-second timeout per message. Test-ending comments also close automatically after 7 seconds, then show the result screen via the existing completion callback. Reveal/Next/Close remain available to advance earlier. The timeout uses unscaled time, including while the game is paused for a test ending. SYSTEM interaction hints keep their contextual visibility.

## SYSTEM command portrait

Contextual hints now put the active device's green-tinted key sprite in the avatar terminal, at 40 × 40 logical pixels. The text terminal contains English usage instructions (press, hold, direction) without inline key sprites. When two different keys have available actions, both appear side by side at 22 × 32; the instructions include key names to associate each action with its sprite. Missing sprites fall back to the key name in the avatar terminal.

The key display is rebuilt from current availability, without a tooltip queue. Scientist communications retain priority and restore the normal portrait; leaving interaction range clears the keys and returns the LED to idle. The runtime container `system-command-keys` uses the existing avatar terminal and does not modify its manually edited frame or LED positioning. Visual review remains manual.

## Collected cheese plate

The primary collectible counter uses Cheese-Sheet.png (ten 64×64 cells, Point filtering, no mipmaps or texture compression). GameplayHudController exposes Cheese Counter Frames ordered full to empty, and LoopScene assigns all ten frames. Zero cheese uses frame 10 (empty); each collected cheese advances one frame toward frame 1 (full at nine collected). Higher totals keep the full plate while the adjacent numeric count remains exact. Green cheese and satiety indicators keep their existing visuals and behavior. No compilation or Play Mode was run.

## Shared gameplay HUD prefab

`Assets/Prefabs/UI/Gameplay HUD.prefab` contains UIDocument, GameplayHudController, GameplayInteractionAvailability, RopeBiteProgressView and SatietyHud, including all ten cheese plate frames. LoopScene and Levels/1_WelcomeToUniverse both instantiate this prefab. Shared artwork and UI settings belong on the prefab; scene instances override only their player/camera/resource references. Existing scene-local IDs for HUD components are retained, preserving references from game managers and interaction systems.

To use the prefab in another scene, assign the scene-local player, collector, interaction, viewport, camera, satiety and oxygen references in its components. These references intentionally remain empty in the prefab asset because they belong to a scene. Both migrated scenes retain their assigned references. No compilation or Play Mode was run; manually open the level and check counters, dialogue, pause, oxygen/satiety and input hints.

## Native sprite pixels

Collectible counter images now use each assigned sprite's rect width and height in base-resolution pixels, independent of its Pixels Per Unit import value. The cheese plate is 64×64, the green cheese 31×21 and the photo 32×32; the procedural knowledge bulb uses its 16×16 design grid. Scientist portraits also use their sprite rect dimensions (currently 32×32) and are centered on integer coordinates. The shared viewport still provides the display scale.

The resource dashboard is 300×80 with the satiety row beside the upper part of the cheese plate. The bottom reserved band is 90 base pixels in LoopScene and 1_WelcomeToUniverse so the native 64-pixel plate and panel margins do not cover gameplay. Existing communication frame sizes and animation remain unchanged. Input prompt/button icon sizing is unchanged. No compilation or Play Mode was run; manually inspect at 640×360 and integer multiples.

## Edit-time artwork preview

GameplayHud.uxml includes explicit image sources for the full cheese plate, green cheese, photo and scientist portrait at their native sizes, plus the existing KnowledgeCounterIcon exposed as a UXML element. UI Builder can display and select them without entering Play Mode. The scientist message already contains sample text. At runtime GameplayHudController assigns the actual configured icons and cheese count frame, and ScientistCommentView assigns the current speaker/avatar; no extra preview object is spawned in the scene. Runtime knowledge setup reuses the declared element instead of adding a duplicate.

For artwork used during play, configure the references on the shared Gameplay HUD prefab; for layout edit GameplayHud.uxml/uss in UI Builder. No compilation or Play Mode was run.
