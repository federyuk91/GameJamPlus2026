# Audio

All audio files are in Assets/Sounds. Moves preserve GUIDs, WAV contents and import settings; scenes and prefabs continue using the same clips.

| Folder | Files | Original names |
| --- | --- | --- |
| Music | Music_Menu, Music_Gameplay, Music_Ending | SUPERMICE_music_menu_idea_1, SUPERMICE_music_gameplay, SUPERMICE_music_ending |
| SFX/Mouse/Footsteps | Mouse_Footstep_01–06 | SFX_Movement_MouseFS_01–06 |
| SFX/Mouse/Voices | Mouse_Voice_* | SFX_Mouse_Voice-001–009 |
| SFX/Environment/ElectricArcs | ElectricArc_01–04 | SFX_ElectricArc_01–04 |

## Assigned voices

Names and bindings follow the user's instructions. Player contains PlayerVoices with a dedicated AudioSource separate from footsteps.

| Original number | Current name | Event |
| --- | --- | --- |
| 006 | Mouse_Voice_Cheese_01 | First voice after eating normal cheese |
| 007 | Mouse_Voice_Cheese_02 | Second voice after the first finishes and Cheese Sequence Gap elapses |
| 005 | Mouse_Voice_RopeCut | Successful PlayerRopeBite cut |
| 002 | Mouse_Voice_Suffering | Every lost satiety notch, including the last; unrelated to other death events |
| 004 | Mouse_Voice_Digging | Random intervals of 5–9 seconds of active digging |
| 008 | Mouse_Voice_ClimbUp | Once when ledge climbing starts, from ground or hanging |
| 003 | Mouse_Voice_Photo | Once when the photo actually opens in UI, even while paused |
| 001 | Mouse_Voice_RopeJump | Once on a voluntary jump off a rope |
| 009 | Mouse_Voice_CrateStart | First push/pull attempt after grabbing a crate |

Cheese Sequence Gap initially equals 0.04 seconds. The sequence uses real time, waits for the first clip's actual duration, and does not randomly choose between 6 and 7. Green cheese and photos do not trigger it.

The crate voice does not repeat while the same grab is maintained, including direction changes and stops. Releasing or switching crates enables a new voice. Automatic underwater contact pushing is not a grab interaction and does not trigger it.

Volume and Cheese Sequence Gap are adjustable in Player Voices. Every voice respects Effects and Master audio settings. All nine are assigned.

Digging Voice Min Interval and Max Interval are adjustable; the effective minimum is always at least 5 seconds. The timer pauses outside digging and during game pause, retaining progress when resumed. Digging voices wait for other voices to finish.
