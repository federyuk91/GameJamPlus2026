# Scientist text-driven mouth animation

Gameplay HUD > Scientist Speech references Assets/Settings/ScientistSpeech.asset. The UI portrait follows the existing communication typewriter for normal dialogues, welcome messages and end-level scientist comments. It runs on unscaled time, matching the existing dialogue coroutine, and pauses when the communication is suspended.

| Letters | Sprite sequence |
| --- | --- |
| AEIOQW | Scientist4, Scientist3, Scientist2, Scientist1 |
| BMPFV | Scientist4, Scientist5, Scientist7 |
| U | Scientist6 |
| Other letters | Scientist4, Scientist5 |

Matching ignores case and accents. Numbers, spaces and punctuation request a return to the looping idle sequence Scientist8 through Scientist13. The current short mouth cycle finishes before resting or switching to the latest revealed letter group. The animator does not queue one full cycle per character: that would make the mouth lag behind the typewriter. Revealing all text immediately, hiding the communication or cancelling it returns the mouth to the idle loop. Other avatar sprites remain static unless explicitly recognized by the speech settings. Idle Frames contains Scientist8, Scientist9, Scientist10, Scientist11, Scientist12 and Scientist13 in that order, looping at Idle Frames Per Second (8 by default).

Mouth Frames Per Second (20) controls the duration of each mouth frame. Text Characters Per Second (30) preserves the existing text reveal speed and can be adjusted separately. Letter Animations and Other Letter Frames expose the associations. Recognized Portraits includes the existing Scientist_QWO portrait so existing dialogue references continue to work. The shared HUD uses Scientist8 as the initial idle portrait. The previous Scientist_Idle remains a recognized alias for existing dialogue references. No Animator component is required for the UI Toolkit Image; the existing portrait element displays the sprites directly.

Compilation was checked for runtime and editor assemblies. No automated gameplay or visual tests were run. Check a vowel-heavy phrase, BMPFV, U, an accented word, F to reveal, a paused end-level comment and a message with a different avatar in Unity.

## Typewriter audio

Gameplay HUD > Typing Clip uses the existing Sounds/SFX/UI/Keyboard_KeyPress.wav shared with EndGameCredits. Typing Audio references a dedicated 2D AudioSource in the HUD prefab. Typing Volume defaults to 0.25 and is multiplied by Effects Volume and the global Master volume. Each normally revealed non-whitespace text element emits a CharacterRevealed event, including characters caught up within a single rendered frame. Combining accents do not cause duplicate taps. F to reveal the entire text emits no new typing sounds. Suspension pauses character emission, and closing/cancelling a communication stops the source. The event is separate from the portrait animation and works for ordinary and paused end-level communications.

## Gibberish voice

Gameplay HUD has a Scientist Voice component and a dedicated 2D AudioSource.
Letters revealed by the typewriter select a short synthetic syllable: A, E,
I/Y, O/Q/W, U, M/N, B/P/F/V; other letters use Duh. Accents use the base vowel.
Pitch variation is derived from the letter rather than random. Only recognized
scientist portraits vocalize. Spaces and numbers remain silent; punctuation
adds a short pause (apostrophes excluded).

Voice Volume, Voice Pitch, Pitch Variation, Minimum Interval and Punctuation
Pause are editable on Scientist Voice. Letter Sounds and Other Letter Clip
can be replaced with recorded clips. A playing syllable finishes before the
next one starts; closely spaced letters are skipped rather than queued.
Audio uses unscaled time, Master and SFX options. F reveal, closing the message,
and pausing stop the current syllable. The keyboard sound remains independent.

The initial sounds are deliberately synthetic placeholders, not recorded speech.
Regenerate them with `python Tools/Audio/generate_scientist_voice.py`.

### Voice preview

In Scientist Voice, enter Test Text and click Test Voice to hear the phrase in
Edit Mode or Play Mode, including on the prefab. Test Characters Per Second
controls the preview typing speed (default 30, matching the speech profile).
Stop Test Voice interrupts playback. Preview uses the same letter mapping,
pitch, spacing and volume settings, through a temporary source that is removed
when playback finishes, the Inspector closes or Play Mode changes.
