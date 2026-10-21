# Final screen

Open Assets/Scenes/EndGameCredits.unity directly. Its UI Toolkit panel fills the screen and uses a dark monitor palette, CRT opening/closing and sequential typing. Preview text and logos are editable in Assets/UI Toolkit/EndGameCredits. The existing EndGame scene flow is unchanged; the new scene is registered in Build Settings and can be loaded with SceneManager.LoadScene("EndGameCredits").

Space/Enter or controller South completes typing; the next press returns to MainMenu with CRT shutdown. Escape/controller East follows the same sequence. Menu button appears after typing. Each printed character plays a keystroke through a serialized AudioSource, scaled by Effects Volume and the existing Master AudioListener volume. Characters Per Second and Typing Volume are Inspector settings. Skipping reveals the remaining text silently.

Sound: Keyboard_KeyPress.wav, typewriter/key_press.wav from ArahKarya/thock, CC0-1.0. Source: https://github.com/ArahKarya/thock . Logo: official Made with Unity artwork from https://unity.com/legal/branding-trademarks (https://cdn.sanity.io/images/fuvbjjlp/production/ed8ae4aba799bd64a04b6dee9e9f4eaee5735bc6-415x100.png). Existing GameJamPlus, IGDA Italy and SGS logos are reused. Logos are not recolored.

No compilation, automated tests or Play Mode were run. Manually check direct scene play, fullscreen layout, typing, audio volume controls and return to menu.

## Slower CRT opening and supplied audio

The final screen now waits unlit for 2 seconds, opens horizontally in 0.45 seconds, expands vertically in 0.85 seconds and fades in the contents over 0.4 seconds. Startup Delay, Line Opening Duration and Screen Opening Duration are Inspector fields. The root, monitor, panel clear and camera clear share the same near-black color; the initial collapsed monitor is hidden before the first rendered frame.

Logo backplates are transparent. Unity uses the official white transparent variant downloaded from the Unity branding collection: https://brandfolder.com/s/fj5fnvgrqhwv9g9rtbrc3 , asset Logo - Made with Unity/Logo_T1_MadeWith_White_RGB.png. Existing Special Thanks logos retain their alpha and original colors.

The user's supplied WAV was copied to Assets/Sounds/Music/EndGameCredits_Piano.wav. A dedicated non-looping AudioSource starts it with a one-second fade-in, under Music/Master controls; its recorded decay is retained and it fades out at shutdown. The supplied television MP3 was copied to Assets/Sounds/SFX/UI/TV_SwitchOff.mp3 and plays on another source when closing, under Effects/Master controls. Shutdown waits for the effect to finish before loading the menu. Typing remains on its independent Effects source. User Downloads originals are unchanged. No compilation or Play Mode was run.

Shutdown Delay defaults to 5 seconds after the logo fade-in finishes, then CRT shutdown automatically returns to MainMenu. The Main Menu button appears when typing finishes and starts shutdown immediately, skipping the automatic wait. The television clip also plays at the start of the opening animation, after the existing two-second unlit delay. Repeated exit requests are ignored. No compilation or Play Mode was run.

The Unity logo is shown separately after CRT shutdown and its sound finish, centered at 70% screen width on the near-black background. Unity Logo Fade Duration defaults to 1.5 seconds, followed by Unity Logo Hold Duration (3 seconds), then MainMenu loads. Main Menu skips the credits wait but still shows this final logo sequence.

Strudel piano playback now begins as the CRT contents appear, without an added audio fade-in: the supplied recording has its attack at the beginning and decays mostly within 5–6 seconds. Its natural decay is preserved and audio data is preloaded. Music/Master volume controls still apply.

Continuous piano: the scene now uses EndGameCredits_Piano_Loop.wav, derived from the original Strudel recording without changing it. The first nine seconds are folded into a three-second loop with overlapping tails, a short attack ramp and normalized peak (0.55) for audibility. The dedicated music source loops at base volume 0.8 from CRT opening until its shutdown fade. The 180-second original contains mostly silence after the initial decay and remains preserved. Listening and Play Mode checks are left to the user.

The final music now uses the user's new Strudel export 2026-10-20T11_49_12.118Z.wav, imported as Assets/Sounds/Music/EndGameCredits_PianoChord.wav. It replaces the repeating piano loop and plays once after CRT opening, preserving the recorded attack and reverb decay. Music/Master controls and shutdown fade remain active. Original Downloads file preserved; no Play Mode or listening test performed.

Updated EndGameCredits_PianoChord.wav with the user's replacement export 2026-10-20T11_50_45.787Z.wav. Asset GUID and scene reference preserved; playback settings unchanged. No Play Mode performed.

Piano playback no longer applies an envelope or shutdown fade in Unity. The imported PianoChord recording supplies its own attack and decay; only the constant Inspector volume and Music/Master user volume settings apply. Playback remains non-looping and ends when the clip finishes or the scene unloads.
