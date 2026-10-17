# Please Press Continue — Main menu

Il menu introduce un laboratorio di puzzle tranquillo: titolo su tre righe, sala di benvenuto, indicatore di ambiente pronto e piccoli pulsanti decorativi. I testi non anticipano la rivelazione finale. Il contrasto macabro è riservato alla futura schermata conclusiva dopo le dieci ripetizioni.

Aprire `Assets/Scenes/MainMenu.unity` e avviare Play.

- UI Toolkit: `MainMenu.uxml` e `MainMenu.uss`; PanelSettings a 1280 × 800 con scala rispetto allo schermo.
- Controller: stick sinistro / D-pad per navigare, pulsante sud (A / Croce) per confermare, pulsante est (B / Cerchio) per tornare. Il focus iniziale è su Nuovo Gioco e ritorna al pulsante di origine quando si chiude un pannello.
- Tastiera: frecce, Tab, Invio e Esc. Mouse supportato dal modulo Input System esistente.
- Opzioni: volume globale e schermo intero; salvataggio in PlayerPrefs. Sullo slider, sinistra/destra regolano il valore e su/giù cambiano controllo.
- Crediti: modificare `Credits Text` sul MainMenuController nell'Inspector.
- Nuovo Gioco: `Gameplay Scene Name` nell'Inspector, inizialmente LoopScene. La scena deve essere inclusa nel Build Profile attivo.
- Esci: conferma prima di chiudere; in Editor interrompe Play.

MainMenu è la prima scena negli Editor Build Settings, seguita da LoopScene. Eventuali Build Profiles con lista scene personalizzata devono includere entrambe.

## Grafica provvisoria

I colori del menu sono campionati da `Assets/Art/MainPalette.png` e centralizzati nelle variabili `--menu-*` di `MainMenu.uss`: sfondo e dialoghi crema, testo quasi nero, titolo rosso mattone, pulsanti marroni, hover blu, bordi grigi, accenti rame e focus verde. `--menu-on-control` definisce i testi chiari sui controlli scuri e `--menu-title` il titolo. Anche slider e toggle usano la palette.

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

Verificare anche in Editor e build: controller fisico, slider, fullscreen, caricamento della scena, uscita, leggibilità a 16:9, 16:10 e 4:3. Il layout è progettato per schermi desktop in orizzontale.
