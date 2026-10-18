# Audio

Tutti i file audio sono in Assets/Sounds. Gli spostamenti conservano GUID, contenuto WAV e impostazioni di importazione: scene e prefab continuano a usare gli stessi clip.

| Cartella | File | Provenienza |
| --- | --- | --- |
| Music | Music_Menu, Music_Gameplay, Music_Ending | SUPERMICE_music_menu_idea_1, SUPERMICE_music_gameplay, SUPERMICE_music_ending |
| SFX/Mouse/Footsteps | Mouse_Footstep_01–06 | SFX_Movement_MouseFS_01–06 |
| SFX/Mouse/Voices | Mouse_Voice_* | SFX_Mouse_Voice-001–009 |
| SFX/Environment/ElectricArcs | ElectricArc_01–04 | SFX_ElectricArc_01–04 |

## Vocalizzi assegnati

Le associazioni e i nomi seguenti seguono le indicazioni dell'utente. Il prefab Player contiene PlayerVoices, con una AudioSource dedicata separata dai passi.

| Numero originale | Nome attuale | Evento |
| --- | --- | --- |
| 006 | Mouse_Voice_Cheese_01 | Primo vocalizzo dopo aver mangiato formaggio normale |
| 007 | Mouse_Voice_Cheese_02 | Secondo vocalizzo, dopo il completamento del primo e Cheese Sequence Gap |
| 005 | Mouse_Voice_RopeCut | Taglio riuscito di una corda tramite PlayerRopeBite |
| 002 | Mouse_Voice_Suffering | Perdita di una tacca di sazietà, compresa l'ultima; nessun collegamento agli altri eventi di morte |
| 004 | Mouse_Voice_Digging | Durante lo scavo, con intervallo casuale di 5–9 secondi di scavo attivo |
| 008 | Mouse_Voice_ClimbUp | Una volta all'inizio della risalita di un bordo, sia da terra sia da appeso |
| 003 | Mouse_Voice_Photo | Una volta all'apertura effettiva della foto nella UI, anche a gioco in pausa |
| 001 | Mouse_Voice_RopeJump | Una volta al salto volontario di distacco dalla corda |
| 009 | Mouse_Voice_CrateStart | Primo tentativo di spinta o tiro dopo la presa della cassa |

Cheese Sequence Gap è inizialmente 0.04 secondi. La sequenza usa il tempo reale e rispetta la durata del primo clip; non sceglie casualmente fra 6 e 7. Formaggio verde e foto non attivano la sequenza.

Il vocalizzo della cassa non si ripete mentre il topo mantiene la stessa presa, anche quando cambia direzione o si ferma. Dopo il rilascio o il cambio di cassa, una nuova interazione lo riattiva. La spinta automatica per contatto sott'acqua non è un'interazione di presa e non lo attiva.

Volume e Cheese Sequence Gap sono regolabili in Player Voices. Tutti i vocalizzi rispettano Effects e Master delle opzioni audio. Tutti i nove vocalizzi sono ora assegnati.

Digging Voice Min Interval e Digging Voice Max Interval sono regolabili in Player Voices; il minimo effettivo è sempre almeno 5 secondi. Il timer si ferma quando non si scava o il gioco è in pausa, senza azzerarsi quando si riprende. Il vocalizzo attende che gli altri vocalizzi abbiano finito.
