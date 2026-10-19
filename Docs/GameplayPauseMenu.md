# Loop pause menu

Open/close with Esc (keyboard) or Start/Options (controller). The main page shows Resume, Controls, Options, Main Menu and Quit Game in English, matching the rest of the game. The compact centred panel follows the supplied pause reference: dark background, pale outline, session heading, selected row and footer with the current input hints.

- Arrow keys/WASD or left stick/D-pad navigate; Space or controller South confirms.
- Esc/controller East returns from Controls/Options to the pause main page; from the main page it resumes. Start resumes from any pause page.
- Mouse buttons and slider dragging are supported.
- Options uses existing GameAudioSettings channels Master, Music and Effects, plus persisted fullscreen. Fullscreen defaults to enabled when no preference has been saved, including when starting LoopScene directly. Existing windowed preferences are respected. Changes apply immediately and are saved when leaving pause.
- Controls and confirmation/back hints follow the last actively used device and display its button names (including PlayStation, Xbox and Switch). The main menu footer follows the same device tracking.
- Main Menu returns to MainMenu; Quit Game exits the build or stops Play in the Editor.

GameplayHudController owns GameplayPauseMenuView. The player input reference is assigned in LoopScene. Opening stores the previous time scale and input enabled state, disables player input and sets Time.timeScale to zero. Resume restores the saved state and clears gameplay input queues via PlayerInputReader's existing OnDisable, so Space used to resume does not leave a queued jump.

The HUD timer and gameplay simulation stop. Scientist typing, timeout and CRT transitions are explicitly suspended while the menu is open; advance/scroll input is disabled and restored on resume. Music can continue and its volume remains adjustable. SYSTEM hints are refreshed from current availability after resume, never queued.

Pause cannot open while a photo or test result is visible, during test ending, or while time scale is already zero for another owner. If a new photo is shown while pause is open, the pause menu closes before the photo acquires its own pause state. Scene exit/disabling disposes the view and restores any pause it owns.

UI lives in GameplayHud.uxml/GameplayHud.uss under pause-overlay, appended above the existing photo/result overlays. Other manually edited HUD elements remain unchanged. No compiler, automated tests or Play Mode were run, following project rules.

Manual Unity review: Esc and Start, controller East and South, mouse sliders, current device controls, fullscreen and volumes, resume during a scientist message, no jump after Space-resume, blocked opening on a photo/test result, return to main menu, exit in Editor/build, and resizing at 640 × 360.
