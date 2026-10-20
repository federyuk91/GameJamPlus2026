# Please Press Continue — Main menu

The menu follows the October 18 reference: cream paper, a navy UNIVERSE-10 logo with a red orbit, serif buttons and a welcome room featuring the mouse. Visible text is English; the subtitle is “A home for every mouse”. The menu does not reveal the ending. The macabre contrast is reserved for the conclusion after ten repetitions.

Open `Assets/Scenes/MainMenu.unity` and enter Play Mode.

- UI Toolkit: `MainMenu.uxml` and `MainMenu.uss`; PanelSettings uses 640 × 360, Scale With Screen Size / Expand. The compact centered layout has 20-unit black bars above and below, with fonts rendered at screen resolution.
- Controller: left stick / D-pad navigates; the south button (A / Cross) confirms; the east button (B / Circle) goes back. Initial focus is New Game; closing a panel restores focus to its originating button.
- Keyboard: arrows, Tab and Esc. The original Enter confirmation was subsequently replaced with Space, as described below. The existing Input System module supports the mouse.
- Options: Master, Music, Effects and fullscreen, saved in PlayerPrefs. Left/right adjusts sliders; up/down changes controls.
- Credits: edit the four names in the MainMenuController Inspector's `Credits` section. Roles and names have separate labels; roles use Cardo Bold.
- New Game: `Gameplay Scene Name` defaults to LoopScene, which must be included in the active Build Profile.
- Quit: requires confirmation; in the Editor it stops Play Mode.

MainMenu is first in Editor Build Settings, followed by LoopScene. Build Profiles with custom scene lists must include both.

## Audio

`GameAudioSettings` persists the three volumes across scenes and sessions. Master controls `AudioListener.volume`; Music and Effects notify components through events without searching the scene for AudioSources.

- `BackgroundMusic` multiplies its AudioSource's Inspector base volume by the Music setting.
- `PlayerFootsteps` applies Effects; local footstep volume still controls individual sound intensity.
- Connect new sounds to the appropriate channel. Use `BackgroundMusic` for music, or subscribe to `MusicVolumeChanged` / `EffectsVolumeChanged`, apply the current value in OnEnable and unsubscribe in OnDisable. Master affects sources that do not ignore AudioListener volume.

Manually check that Master at zero silences music and footsteps, Music at zero silences only music, and Effects at zero silences only footsteps. Check controller navigation and persistence after loading LoopScene and restarting.

## Menu artwork

`MainMenu.uss` centralizes the palette in `--menu-*` variables: cream paper, navy text and frames, brick-red selection/focus and green ONLINE / ENVIRONMENT READY indicators. Buttons respond to mouse hover and keyboard/controller focus.

Assets under `Assets/Art/UI/MainMenu`:

- `Branding/Universe10_TitleLogo.png`: transparent logo and orbit.
- `Illustrations/WelcomeRoom_Mouse.png`: mouse in the laboratory, GUEST 010 sign.
- `Backgrounds/WelcomeProgram_Paper.png`: cream paper without text.
- `Fonts/Cardo-Regular.ttf` and `Cardo-Bold.ttf`: serif fonts; license in `Fonts/OFL.txt`.

The PNGs were generated from the reference using imagegen; they are not identical crops. Frames, indicators, buttons and text are editable UI Toolkit elements. The title is baked into the logo PNG: replace that PNG to change it. Other text is in UXML; credit names come from the Inspector. Preserve .meta files when replacing images.

See `Docs/MainMenuVisualAssets.md` for provenance and prompts. Older placeholders remain in their original folders.

## Validation

`GameJamPlus.Editor.MainMenuValidation.Run` is a batch check intended for a temporary project copy. It checks references, buttons and navigation with a virtual gamepad in Play Mode, changes that copy's Play Mode settings and exits the Editor on completion.

Manually check physical controllers, sliders, fullscreen, scene loading, quitting and readability at 640 × 360, 1280 × 720, 1920 × 1080 and 1024 × 768. The layout targets landscape desktop screens. See `Docs/PixelArtDisplay.md` for gameplay configuration, bars and behavior below the base resolution. The changes described here were not automatically tested; visual checks remain manual.

## Options and Credits

The original centered panels used navy frames, monitor headings, cream paper and brick-red accents. Options separates AUDIO and DISPLAY, retaining volume channels, fullscreen, saving and navigation.

Credits has four rows without icons: bold role on the left, name on the same row on the right, aligned in columns. Names remain editable in the controller.

The former `creditsText` field was replaced by `designerProgrammerName`, `artistName`, `pixelArtistName` and `soundMusicName`. MainMenu scene values were migrated. Other custom controller instances must move their names to these fields.

Manually check panel readability at 640 × 360, slider/fullscreen/Back navigation and focus restoration. No automated tests or Play Mode checks were run for that change.

### Special Thanks

The last credit row shows GJ+, IGDA Italy and SGS side by side under Special Thanks. Logos retain their original colors and aspect ratios; the four roles have no icons. Compact 32 px name rows keep the panel inside the 640 × 360 viewport with black bars.

- GJ+: existing `Assets/Art/UI/Branding/GameJamPlus_Icon.png`.
- IGDA Italy: `Assets/Art/UI/MainMenu/Branding/IGDAItaly_Logo.png`, downloaded from the [official website](https://www.igdaitaly.com/), using its [published PNG](https://cdn.prod.website-files.com/647cbef7c011590d322288e1/647cbf27b0177009b56949f3_00_Igda%20italy_destra-p-500.png), October 18, 2026.
- SGS: `Assets/Art/UI/MainMenu/Branding/SGS_Logo.png`, copied from the user's `codex-clipboard-abcc00bd-3f13-4239-adaf-a217b5f203d1.png` attachment.

Logos are decorative and excluded from controller navigation. Size and readability require manual checks at 640 × 360; no tests or Play Mode checks were run.

## Contextual right panel

Hovering or focusing a menu item updates the right panel. New Game keeps the mouse illustration and starts LoopScene only after confirmation. Options previews the same Master/Music/Effects sliders and Fullscreen control. Click, confirmation or Right activates the panel and focuses the first control. Back or Esc/B returns to Options, keeping its preview visible. Preview controls can also be clicked directly.

Credits shows roles, names and Special Thanks logos. Confirmation does not navigate; Credits no longer has a Back button. Quit shows confirmation in the right column, without an overlay. Originally, choosing a left menu item left right-panel editing and saved settings; the subsequent focus lock described below supersedes this behavior while editing.

Right-panel controls participate in navigation only after activation. Preview navigation stays on the four main items. The selected main item remains highlighted while focus is inside Options.

The right column is 348 × 280 at the 640 × 360 base size. Credit roles and names share rows sized for that width. `modal` and `credits-back` were removed from UXML; custom copies must adopt `welcome-panel` / `options-panel` / `credits-panel` / `quit-panel`. Serialized settings, audio, fullscreen and gameplay scene remain unchanged.

No automated tests, compilation or Play Mode checks were run for that change. Manually check hover, D-pad/arrows, Options activation, sliders, Back/Esc, passive credits, quitting and readability.

### Focus locked to the active panel

Activating Options or Quit disables the left main-panel: hover, clicks and Tab cannot change its preview. Back/Cancel or Esc/B re-enables it and restores focus. Selection remains highlighted; other items do not react to hover while locked.

Keyboard confirmation uses Space through a temporary Submit action assigned to MainMenu's UI module, referenced in the scene Inspector. Controllers use the south button. Disabling the menu restores the original module action; shared input assets and gameplay controls are unchanged. The footer shows Space / A. Manually check Space, Esc and hover during the lock; no tests or Play Mode checks were run.

### Right-panel channel switching

Hover and focus immediately update selection; right-hand content changes through a short CRT transition. The original effect darkened the screen with scanlines and a narrow interference band. `Channel Switch Delay` defaults to 0.12 s, followed by a 0.16 s appearance. Initial content appears immediately.

Changing selection during a transition cancels the previous destination, retaining only the latest. Right controls remain disabled during switching. Confirming Options/Quit before the transition ends focuses its first control after appearance; Esc/Back cancels that focus transfer. Left-menu locking remains in effect while editing. Transition time is unscaled and independent of Time.timeScale.

No automated tests or Play Mode checks were run; manually check rapid hovering and confirmation/cancellation during transitions.

### Permanent navy frame and CRT content transition

New Game's room-card now serves as the shared monitor: navy background, heading, ONLINE indicator and footer stay fixed. `channel-title` retains the WELCOME ROOM style and displays WELCOME ROOM, OPTIONS, CREDITS or QUIT. The footer caption changes with the channel; Play. Eat. Learn. Relax. remains below the monitor.

All content fits within the original 186 px image viewport: the mouse, compact Options controls, inline credit roles/names and Special Thanks logos, or Quit confirmation. Inner panels are transparent with light text. Special Thanks logos have transparent containers and appear directly on the navy background.

Channel switching follows Loop communications: collapse vertically to a thin line, compress toward the center, pause for Channel Switch Delay (0.12 s), reopen the line and display, then fade the inner glow. Only `channel-aperture` animates; `channel-content` retains full dimensions to avoid text reflow. The navy frame and heading stay visible at full size. The heading changes during the pause between closing and reopening.

Unscaled timing, latest-selection behavior, left-menu locking after Options/Quit confirmation and focus transfer after animation remain unchanged. No tests, compilation or Play Mode checks were run; manually check readability at 640 × 360 and rapid channel changes.

Fullscreen is enabled by default when Settings.Fullscreen is absent. The Options toggle uses the same default and reflects the saved preference; existing windowed/fullscreen choices are preserved. Player Settings already starts desktop builds in Fullscreen Window mode.
