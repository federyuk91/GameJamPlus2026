# Please Press Continue — Main menu

Il menu riprende il riferimento del 18 ottobre: carta crema, logo UNIVERSE-10 blu con orbita rossa, pulsanti serif e sala di benvenuto con il topo. Tutti i testi visibili sono in inglese; il sottotitolo è “A home for every mouse”. I testi non anticipano la rivelazione finale. Il contrasto macabro è riservato alla futura schermata conclusiva dopo le dieci ripetizioni.

Aprire `Assets/Scenes/MainMenu.unity` e avviare Play.

- UI Toolkit: `MainMenu.uxml` e `MainMenu.uss`; PanelSettings a 640 × 360, Scale With Screen Size / Expand. Layout compatto, centrato, con bande nere da 20 unità sopra e sotto e caratteri renderizzati alla risoluzione dello schermo.
- Controller: stick sinistro / D-pad per navigare, pulsante sud (A / Croce) per confermare, pulsante est (B / Cerchio) per tornare. Il focus iniziale è su Nuovo Gioco e ritorna al pulsante di origine quando si chiude un pannello.
- Tastiera: frecce, Tab, Invio e Esc. Mouse supportato dal modulo Input System esistente.
- Opzioni: volumi Master, Musica, Effetti e schermo intero; salvataggio in PlayerPrefs. Sugli slider, sinistra/destra regolano il valore e su/giù cambiano controllo.
- Crediti: modificare i quattro nomi nella sezione `Credits` sul MainMenuController nell'Inspector. Ruoli e nomi hanno etichette separate; i ruoli usano Cardo Bold.
- Nuovo Gioco: `Gameplay Scene Name` nell'Inspector, inizialmente LoopScene. La scena deve essere inclusa nel Build Profile attivo.
- Esci: conferma prima di chiudere; in Editor interrompe Play.

MainMenu è la prima scena negli Editor Build Settings, seguita da LoopScene. Eventuali Build Profiles con lista scene personalizzata devono includere entrambe.

## Audio

`GameAudioSettings` conserva i tre volumi tra le scene e le sessioni. Master regola `AudioListener.volume`; Musica ed Effetti notificano i componenti tramite eventi, senza ricerche di AudioSource nella scena.

- `BackgroundMusic` applica Musica alla propria sorgente, moltiplicandola per il volume base dell'AudioSource impostato nell'Inspector.
- `PlayerFootsteps` applica Effetti alla propria sorgente; il volume locale dei passi continua a regolare l'intensità relativa dei singoli suoni.
- I nuovi suoni devono essere collegati al canale appropriato: usare `BackgroundMusic` per le musiche oppure sottoscrivere `MusicVolumeChanged` / `EffectsVolumeChanged`, applicare il valore corrente in OnEnable e rimuovere la sottoscrizione in OnDisable. Master si applica a tutte le sorgenti che non ignorano il volume dell'AudioListener.

Da provare in Unity: Master a zero silenzia musica e passi; Musica a zero silenzia solo le musiche; Effetti a zero silenzia solo i passi. Controllare anche navigazione controller fra i tre slider e persistenza dopo caricamento di LoopScene e riavvio.

## Grafica del menu

Palette centralizzata nelle variabili `--menu-*` di `MainMenu.uss`: carta crema, blu scuro per testo e cornice, rosso mattone per selezione e focus, verde per gli indicatori ONLINE e ENVIRONMENT READY. I pulsanti reagiscono sia al mouse sia al focus da controller/tastiera.

Asset separati sotto `Assets/Art/UI/MainMenu`:

- `Branding/Universe10_TitleLogo.png`: logo con trasparenza e orbita.
- `Illustrations/WelcomeRoom_Mouse.png`: topo nel laboratorio, cartello GUEST 010.
- `Backgrounds/WelcomeProgram_Paper.png`: carta crema senza testi.
- `Fonts/Cardo-Regular.ttf` e `Cardo-Bold.ttf`: caratteri serif; licenza inclusa in `Fonts/OFL.txt`.

I PNG sono derivazioni generate dal riferimento con imagegen, non ritagli identici dei pixel originali. Cornice, indicatori, pulsanti e testi sono elementi UI Toolkit modificabili. Il logo contiene il titolo nell'immagine: per cambiarlo, sostituire il PNG; gli altri testi sono in UXML (i nomi nei crediti provengono dai quattro campi `Credits` nell'Inspector). Conservare i file .meta quando si sostituiscono le immagini.

Origine degli asset e prompt in `Docs/MainMenuVisualAssets.md`. I vecchi placeholder restano disponibili nelle cartelle originali.

## Verifica

`GameJamPlus.Editor.MainMenuValidation.Run` è un controllo batch da eseguire in una copia temporanea del progetto: verifica riferimenti, pulsanti e navigazione con gamepad virtuale entrando in Play Mode. Modifica le impostazioni Play Mode della copia e termina l'Editor al completamento.

Verificare in Editor e build: controller fisico, slider, fullscreen, caricamento della scena, uscita e leggibilità a 640 × 360, 1280 × 720, 1920 × 1080 e 1024 × 768. Il layout è progettato per schermi desktop in orizzontale. Vedere `Docs/PixelArtDisplay.md` per configurazione del gameplay, bande e fallback sotto la risoluzione base. Per questa modifica non sono stati eseguiti test: le verifiche visive restano manuali.

## Options e Credits

Pannelli centrati con cornice blu scuro, intestazione da monitor, carta crema e accenti rosso mattone. Options separa AUDIO e DISPLAY e conserva i canali Master/Music/Effects, fullscreen, salvataggio e navigazione esistenti.

Credits presenta quattro righe senza icone: ruolo in grassetto a sinistra e nome sulla stessa riga a destra, con colonne allineate. I nomi restano modificabili nei campi Credits del controller.

Il vecchio campo unico `creditsText` è stato sostituito da `designerProgrammerName`, `artistName`, `pixelArtistName` e `soundMusicName`; i nomi della scena MainMenu sono già migrati. Eventuali altre istanze personalizzate del controller devono riportare i nomi nei nuovi campi.

Da verificare manualmente in Unity: leggibilità dei pannelli a 640 × 360, navigazione fra slider/fullscreen/Back e ritorno al pulsante Credits. Nessun test automatico o Play Mode eseguito.
### Special Thanks

L'ultima riga dei crediti mostra tre loghi affiancati sotto il titolo inglese Special Thanks: GJ+, IGDA Italy, SGS. I marchi conservano i loro colori e proporzioni originali; i quattro ruoli rimangono senza icone. Righe dei nomi compatte da 32 px per mantenere il pannello nel viewport 640 × 360 con bande nere.

- GJ+: asset esistente `Assets/Art/UI/Branding/GameJamPlus_Icon.png`.
- IGDA Italy: `Assets/Art/UI/MainMenu/Branding/IGDAItaly_Logo.png`, scaricato dal [sito ufficiale](https://www.igdaitaly.com/), asset [PNG pubblicato nel sito](https://cdn.prod.website-files.com/647cbef7c011590d322288e1/647cbf27b0177009b56949f3_00_Igda%20italy_destra-p-500.png), 18 ottobre 2026.
- SGS: `Assets/Art/UI/MainMenu/Branding/SGS_Logo.png`, copia dell'allegato dell'utente `codex-clipboard-abcc00bd-3f13-4239-adaf-a217b5f203d1.png`.

I loghi sono elementi decorativi, esclusi dalla navigazione controller. Da verificare manualmente: dimensioni e leggibilità a 640 × 360. Nessun test o Play Mode eseguito.

## Pannello destro contestuale

Le voci del menu aggiornano il pannello destro quando ricevono hover del mouse oppure focus da tastiera/controller. New Game mantiene l'illustrazione attuale del topo e avvia LoopScene solo alla conferma. Options mostra gli stessi slider Master/Music/Effects e Fullscreen: click, Invio/A oppure freccia destra entrano nel pannello e trasferiscono il focus al primo controllo. Back o Esc/B ritornano alla voce Options, mantenendo l'anteprima visibile. È possibile anche cliccare direttamente un controllo nell'anteprima Options.

Credits mostra ruoli, nomi e loghi Special Thanks; la conferma non avvia alcuna navigazione e non esiste più un tasto Back nei crediti. Quit mostra la conferma di uscita nella stessa colonna destra. Nessun overlay o finestra separata: la colonna sinistra resta visibile e utilizzabile. Selezionare una voce della colonna sinistra abbandona l'eventuale modifica del pannello destro e salva le impostazioni.

I controlli della colonna destra entrano nella navigazione solo quando si attiva il relativo pannello; finché è una semplice anteprima, la navigazione resta sulle quattro voci principali. La selezione rimane evidenziata anche mentre il focus è sui controlli Options.

La colonna destra misura 348 × 280 alla base 640 × 360. Ruoli e nomi restano sulla stessa riga nei crediti, con dimensioni adattate alla larghezza della colonna. Rimossi `modal` e `credits-back` dalla UXML; eventuali copie personalizzate devono adottare la nuova struttura `welcome-panel`/`options-panel`/`credits-panel`/`quit-panel`. Campi serializzati, audio, fullscreen e scena di gioco non cambiano.

Nessun test automatico, compilazione o Play Mode eseguito. Il vecchio controllo editor è stato soltanto aggiornato nei riferimenti rimossi, senza eseguirlo. Da provare manualmente: hover tra le voci, D-pad/frecce, conferma su Options, regolazione slider, Back/Esc, crediti senza click, conferma d'uscita e leggibilità a 640 × 360.

### Focus vincolato al pannello attivo

Dopo l'attivazione di Options o Quit, il main-panel a sinistra è disabilitato: né hover, click né focus da Tab cambiano l'anteprima. Back/Cancel oppure Esc/B riabilitano il menu e ripristinano il focus sulla voce di origine. La voce selezionata rimane evidenziata; le altre non reagiscono all'hover durante il blocco.

La conferma da tastiera è Space, tramite un'azione Submit temporanea assegnata al modulo UI di MainMenu (reference Inspector già collegata nella scena). Controller: pulsante sud A/Croce. L'azione originale del modulo viene ripristinata quando il menu viene disabilitato; gli asset di input condivisi e i comandi del gameplay non vengono modificati. Il footer indica Space / A. Nessun test o Play Mode eseguito; verificare manualmente Space, Esc e hover durante il blocco.

### Cambio canale del pannello destro

Hover e focus aggiornano subito la voce selezionata, mentre il contenuto a destra cambia con una breve transizione CRT: oscuramento, scanline e una sottile banda di disturbo, poi comparsa del nuovo contenuto. `Channel Switch Delay` nell'Inspector del MainMenuController regola il ritardo (default 0,12 s); la comparsa richiede altri 0,16 s. Il pannello iniziale è mostrato direttamente.

Se la selezione cambia durante l'effetto, la transizione precedente viene annullata: rimane solo l'ultima destinazione, senza coda di pannelli. I controlli del pannello destro non sono attivi durante il cambio. Se si conferma Options/Quit prima della fine della transizione, il focus passa al primo controllo dopo la comparsa; Esc/Back annulla tale trasferimento. Il blocco delle voci a sinistra durante la modifica resta invariato. Il tempo della transizione è non scalato e non dipende da Time.timeScale.

Nessun test automatico o Play Mode eseguito; verificare manualmente hover rapido tra voci e conferma/annulla durante il cambio canale.

### Cornice blu permanente e cambio CRT dei contenuti

La room-card del New Game è ora il monitor comune a tutti i canali: fondo blu, intestazione, ONLINE e barra inferiore restano fissi. `channel-title` mantiene lo stile originale di WELCOME ROOM e mostra WELCOME ROOM, OPTIONS, CREDITS o QUIT; la barra inferiore aggiorna la didascalia. La scritta Play. Eat. Learn. Relax. resta sotto il monitor.

Tutti i contenuti stanno nel viewport originale dell'immagine, alto 186 px: il topo in Welcome Room, controlli compatti per Options, ruoli e nomi sulla stessa riga e loghi Special Thanks per Credits, conferma per Quit. I pannelli interni sono trasparenti e i testi chiari sul blu. I loghi hanno piccole basi chiare per mantenere leggibile anche il marchio IGDA con lettere nere.

Il cambio canale applica lo stesso principio delle comunicazioni Loop: chiusura verticale fino a linea sottile, compressione al centro, pausa Channel Switch Delay (default 0,12 s), apertura della linea e del display, dissolvenza del bagliore interno. Si anima soltanto `channel-aperture`; `channel-content` conserva le dimensioni complete per evitare che il testo vada a capo durante l'effetto. La cornice blu e l'intestazione non vengono ridimensionate o nascoste. Il titolo cambia quando viene selezionato il nuovo contenuto nella pausa fra spegnimento e accensione.

Tempo non scalato, ultima selezione senza coda, blocco del menu sinistro dopo conferma Options/Quit e trasferimento del focus a fine animazione restano invariati. Nessun test, compilazione o Play Mode eseguito; controllare manualmente leggibilità dei contenuti nel monitor a 640 × 360 e cambio rapido tra i canali.
