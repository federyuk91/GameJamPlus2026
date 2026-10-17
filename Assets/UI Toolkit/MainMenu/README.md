# Please Press Continue — Main menu

Il menu introduce un laboratorio di puzzle tranquillo: titolo su tre righe, sala di benvenuto, indicatore di ambiente pronto e piccoli pulsanti decorativi. I testi non anticipano la rivelazione finale. Il contrasto macabro è riservato alla futura schermata conclusiva dopo le dieci ripetizioni.

Aprire `Assets/Scenes/MainMenu.unity` e avviare Play.

- UI Toolkit: `MainMenu.uxml` e `MainMenu.uss`; PanelSettings a 640 × 360, Scale With Screen Size / Expand. Layout compatto, centrato, con bande nere da 20 unità sopra e sotto e caratteri renderizzati alla risoluzione dello schermo.
- Controller: stick sinistro / D-pad per navigare, pulsante sud (A / Croce) per confermare, pulsante est (B / Cerchio) per tornare. Il focus iniziale è su Nuovo Gioco e ritorna al pulsante di origine quando si chiude un pannello.
- Tastiera: frecce, Tab, Invio e Esc. Mouse supportato dal modulo Input System esistente.
- Opzioni: volumi Master, Musica, Effetti e schermo intero; salvataggio in PlayerPrefs. Sugli slider, sinistra/destra regolano il valore e su/giù cambiano controllo.
- Crediti: modificare `Credits Text` sul MainMenuController nell'Inspector.
- Nuovo Gioco: `Gameplay Scene Name` nell'Inspector, inizialmente LoopScene. La scena deve essere inclusa nel Build Profile attivo.
- Esci: conferma prima di chiudere; in Editor interrompe Play.

MainMenu è la prima scena negli Editor Build Settings, seguita da LoopScene. Eventuali Build Profiles con lista scene personalizzata devono includere entrambe.

## Audio

`GameAudioSettings` conserva i tre volumi tra le scene e le sessioni. Master regola `AudioListener.volume`; Musica ed Effetti notificano i componenti tramite eventi, senza ricerche di AudioSource nella scena.

- `BackgroundMusic` applica Musica alla propria sorgente, moltiplicandola per il volume base dell'AudioSource impostato nell'Inspector.
- `PlayerFootsteps` applica Effetti alla propria sorgente; il volume locale dei passi continua a regolare l'intensità relativa dei singoli suoni.
- I nuovi suoni devono essere collegati al canale appropriato: usare `BackgroundMusic` per le musiche oppure sottoscrivere `MusicVolumeChanged` / `EffectsVolumeChanged`, applicare il valore corrente in OnEnable e rimuovere la sottoscrizione in OnDisable. Master si applica a tutte le sorgenti che non ignorano il volume dell'AudioListener.

Da provare in Unity: Master a zero silenzia musica e passi; Musica a zero silenzia solo le musiche; Effetti a zero silenzia solo i passi. Controllare anche navigazione controller fra i tre slider e persistenza dopo caricamento di LoopScene e riavvio.

## Grafica provvisoria

I colori del menu sono campionati da `Assets/Art/Palettes/GamePalette.png` e centralizzati nelle variabili `--menu-*` di `MainMenu.uss`: sfondo e dialoghi crema, testo quasi nero, titolo rosso mattone, pulsanti marroni, hover blu, bordi grigi, accenti rame e focus verde. `--menu-on-control` definisce i testi chiari sui controlli scuri e `--menu-title` il titolo. Anche slider e toggle usano la palette.

Tutti i PNG derivano dall'immagine fornita, ridimensionata mantenendo le proporzioni con padding:

| Cartella sotto Assets/Art/UI/MainMenu | Asset | Dimensioni |
| --- | --- | --- |
| Backgrounds | MainMenu_Background_Placeholder | 1920 × 1080 |
| Branding | MainMenu_Logo_Placeholder | 512 × 256 |
| Illustrations | MainMenu_KeyArt_Placeholder | 720 × 900 |
| Icons | MainMenu_NewGame_Icon_Placeholder | 64 × 64 |
| Icons | MainMenu_Options_Icon_Placeholder | 64 × 64 |
| Icons | MainMenu_Credits_Icon_Placeholder | 64 × 64 |
| Icons | MainMenu_Quit_Icon_Placeholder | 64 × 64 |

Sostituire i PNG mantenendo i relativi file .meta per conservare i riferimenti. Titolo e testi generici sono modificabili in UXML.

## Verifica

`GameJamPlus.Editor.MainMenuValidation.Run` è un controllo batch da eseguire in una copia temporanea del progetto: verifica riferimenti, pulsanti e navigazione con gamepad virtuale entrando in Play Mode. Modifica le impostazioni Play Mode della copia e termina l'Editor al completamento.

Verificare in Editor e build: controller fisico, slider, fullscreen, caricamento della scena, uscita e leggibilità a 640 × 360, 1280 × 720, 1920 × 1080 e 1024 × 768. Il layout è progettato per schermi desktop in orizzontale. Vedere `Docs/PixelArtDisplay.md` per configurazione del gameplay, bande e fallback sotto la risoluzione base. Per questa modifica non sono stati eseguiti test: le verifiche visive restano manuali.
