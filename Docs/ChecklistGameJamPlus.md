# Checklist progetto rispetto a “Temi Game Jam Plus”

Analisi del 17 ottobre 2026 sul contenuto attuale della working directory, comprese le modifiche non ancora committate. Nessun test automatico, Play Mode o build eseguito.

## Perimetro del confronto

Aggiornata dopo la lettura nel browser della [conversazione condivisa](https://chatgpt.com/share/6ad3e70c-8f04-83eb-9dba-ebaa0723904e), che rende accessibili anche l'inizio della discussione e le risposte dell'assistente. Le immagini generate e lo schizzo richiedono un confronto visivo separato: qui sono valutati i contenuti testuali e i file del progetto.

Le scelte espresse dall'utente sono: topo in un labirinto con pulsanti, sostituzione della cavia a ogni morte, morte presentata come completamento, esperimenti diversi fra prove, rivelazione finale dei decessi; direzione visiva da utopia pixel art ispirata a BioShock e all'Esperimento 25; musica jazz/blues con resa SNES e malinconia distopica; cinque esiti specifici. Le proposte dell'assistente non confermate restano opzioni, non requisiti.

Le caselle aperte indicano lavoro da completare oppure verifiche manuali esplicitamente indicate. I punti aggiuntivi di rifinitura derivano dal progetto, non sono requisiti ricostruiti dalla chat.

## Base già presente nei file

- [x] Ciclo Universo-10 → Universo-1 → EndGame, con azzeramento della sessione per Nuovo Gioco.
- [x] Sequenza di fine test: pausa, flash bianco, commento dello scienziato, risultato con tempo e avanzamento.
- [x] `TestEndEvent` configurabile con commento, frasi risultato e indicazione di soggetto deceduto.
- [x] Report con test completati, soggetti impiegati, deceduti, recuperati e percentuale di completamento.
- [x] Selezione di tre finali per 0, 1–9 e almeno 10 decessi.
- [x] Piranha con ronda, cono visivo, inseguimento in acqua e ritorno alla ronda.
- [x] Movimento, nuoto, scale, scavo, interazioni con casse e corde implementati.
- [x] Formaggio verde collegato alla fine test; foto collegata alla fine test dopo la conferma.
- [x] Musica gameplay assegnata in LoopScene e regolazioni di volume disponibili.
- [x] MainMenu, LoopScene ed EndGame inclusi nella lista scene di build.
- [x] Camera configurata a 640×360 e 32 PPU, coerente con la proposta sulle proporzioni del topo.
- [x] Esito del report calcolato sui test completati: può mostrare 100% di successo anche con tutte le cavie decedute, coerentemente con il twist discusso.

“Presente” significa riscontrato in codice e/o dati serializzati, non collaudato in gioco.

## Priorità 0 — Chiudere il gameplay centrale del concept

- [ ] **Pulsanti nel mondo:** implementare pulsanti/pedane azionabili dal topo, con stato ed eventi assegnabili. Lo spritesheet `PressureButton_Sheet.png` è presente, ma non risultano componenti per pulsanti di gameplay; i pulsanti UI non coprono il modificatore della jam.
- [ ] **Conseguenze delle decisioni:** collegare i pulsanti a porte, percorsi o pericoli e realizzare almeno un enigma completo con alternative comprensibili. Porte, inversioni, esche e sacrifici sono esempi suggeriti nella chat: non occorre implementarli tutti.
- [ ] **Esperimenti diversi:** predisporre variazioni di layout, configurazione dei pulsanti o condizioni della prova fra universi. Il Game Manager attuale ricarica la stessa LoopScene e cambia il numero; il generatore di stanze lavora solo in Editor. La richiesta dell'utente dice esplicitamente che ogni volta è un esperimento diverso: non è obbligatoria la generazione procedurale.
- [ ] **Inganno coerente:** verificare che morte e sopravvivenza ricevano lo stesso trattamento positivo, senza anticipare il decesso tramite testi o effetti. Il flusso UI comune è già presente; manca il percorso di sopravvivenza nella gabbia.
- [ ] **Sostituzione della cavia:** verificare che l'avvio della prova successiva sia percepito come avanzamento e che il report finale chiarisca il costo in soggetti. Il reload ricrea già il player e la sessione conta i soggetti; la riuscita narrativa va provata con chi non conosce il twist.
- [ ] **Playtest del twist:** far giocare una persona senza spiegare il concept e verificare cosa interpreta come successo e quando comprende il significato del report.

## Priorità 1 — Completare i cinque esiti della discussione

- [x] **Piranha:** contatto letale implementato con `LethalContact` e collegato a `TestEndEvent` in LoopScene. Questo collegamento è presente nella rilettura successiva al primo audit.
- [x] Risultato piranha configurato con una variante della frase proposta: “Il soggetto ha socializzato con i pesci con notevole entusiasmo.” `Subject Died` è attivo e sono presenti commenti pertinenti dello scienziato.
- [ ] **Elettricità:** implementare la zona o trappola elettrica letale e collegarla alla fine test. Sono presenti `ElettricTrap.png` e quattro suoni ElectricArc, ma non risulta la logica di gameplay.
- [ ] Configurare il risultato elettricità: “Il soggetto ha scoperto l'elettricità”, con `Subject Died` attivo.
- [ ] **Trappola a molla:** implementare attivazione, scatto e conclusione del test. È presente `MouseTrap_Sheet.png`, ma non risulta un componente che gestisca la trappola.
- [ ] Configurare il risultato trappola: “Il soggetto mostra interesse per la meccanica”, con `Subject Died` attivo.
- [ ] **Affogamento:** definire quando avviene e implementare il rilevamento con conclusione del test. Il nuoto e i volumi d'acqua non gestiscono una morte per immersione.
- [ ] Configurare il risultato affogamento: “Il soggetto si è mostrato estremamente assetato”, con `Subject Died` attivo.
- [ ] **Ritorno alla gabbia:** creare l'uscita del labirinto con ingresso nella gabbia e collegare il completamento al Game Manager. Non risulta un evento di recupero del soggetto già configurato.
- [ ] Configurare il risultato gabbia: “Il soggetto è tornato al nido”, con `Subject Died` disattivo, così incrementa i recuperati.
- [ ] Posizionare e rendere raggiungibili tutti e cinque gli esiti nel percorso giocabile, con un commento dello scienziato pertinente per ciascuno.

La UI contiene già il titolo “TEST COMPLETATO!”: le frasi sopra vanno nei `Result Messages`, senza duplicare il titolo. È presente la variante piranha; non sono state trovate le altre quattro frasi negli script, nei prefab o nelle scene.

Vincolo tecnico: il player usa `SimulatedBody2D`, che muove il Transform tramite query fisiche senza Rigidbody2D. I nuovi rilevamenti di pericolo devono essere compatibili con questo sistema; non dare per scontato che basti aggiungere `OnTriggerEnter2D`.

## Priorità 2 — Musica e atmosfera richieste nella chat

- [ ] **Ascolto:** verificare che `Assets/Sounds/Music/Music_Gameplay.wav` rispetti la direzione scelta: influenze blues/jazz, resa SNES e malinconia con sottotesto da esperimento/distopia. L'analisi dei file conferma la presenza e il collegamento, non il risultato musicale.
- [ ] Collegare `Assets/Sounds/Music/Music_Ending.wav` alla scena EndGame: il file esiste, ma il suo GUID non risulta referenziato da altri asset di progetto.
- [ ] **Ascolto in gioco:** controllare loop, volumi relativi a passi/effetti e passaggi fra test. `BackgroundMusic` viene ricreato con LoopScene, quindi la traccia riparte a ogni caricamento.
- [ ] **Verifica narrativa:** valutare insieme musica, battute e risultati: la comicità burocratica deve sostenere il sottotesto inquietante richiesto.
- [ ] **Revisione visiva:** confrontare il livello con la direzione scelta dall'utente: utopia apparentemente accogliente, pixel art centrata sui topi e riferimenti da BioShock/Esperimento 25. La presenza di tile e sprite non certifica questa resa.

La risposta musicale propone armonie rassicuranti con risoluzioni malinconiche e tensione discreta. BPM, percentuali di atmosfera e strumenti descritti dall'assistente sono indicazioni creative, non criteri obbligatori. Il codice Strudel è un mezzo di prototipazione musicale, non una dipendenza da integrare in Unity.

## Priorità 3 — Rifiniture emerse dal progetto

Questi punti sono aggiuntivi rispetto ai requisiti leggibili della conversazione.

- [ ] Sostituire le immagini provvisorie dei finali senza decessi e senza sopravvissuti. EndGame usa ancora `Ending_NoDeaths_Placeholder.png` e `Ending_AllSubjectsLost_Placeholder.png`; il finale intermedio usa già `Assets/Art/EndGame1.png`.
- [ ] Assegnare il ritratto corretto dello scienziato al benvenuto e ai dialoghi: il benvenuto e i prefab Cheese/CheeseGreen usano attualmente lo sprite del formaggio; il prefab foto non ha avatar.
- [ ] Decidere se la foto debba davvero contare come decesso: attualmente `Photo Collectible` ha `Subject Died = true` e termina il test dopo Next. Se deve recuperare il soggetto, disattivare il flag.
- [ ] **Revisione livello:** finalizzare percorso, distribuzione dei pericoli, raccoglibili e leggibilità delle interazioni. La presenza dei sistemi non dimostra che il livello sia completo o bilanciato.
- [ ] **Revisione visiva:** verificare quali asset del menu richiedano ancora sostituzione. Diversi file si chiamano Placeholder, ma alcuni hanno modifiche locali: il nome da solo non basta a stabilire che siano ancora provvisori.
- [ ] Uniformare la lingua di pulsanti, suggerimenti e testi: attualmente convivono etichette italiane e inglesi.

## Idee discusse, da includere solo se scelte

Queste opzioni non sono lavoro obbligatorio mancante. Le caselle servono a segnare una decisione di scope, anche quando si decide di escluderle.

- [ ] Decidere se usare corpi persistenti, cloni o eco delle azioni. L'utente mostra interesse per le copie; l'assistente propone sia sostituzione solo narrativa sia persistenza fisica. Nel progetto attuale i soggetti precedenti non restano nel livello.
- [ ] Decidere come esprimere Together Is Better: attraverso i dati collettivi delle cavie oppure tramite una cooperazione meccanica fra tentativi. La seconda soluzione richiede sistemi aggiuntivi, la prima una comunicazione narrativa leggibile.
- [ ] Decidere se aggiungere indizi progressivi: seriali delle cavie, appunti, osservatori o segni dei test precedenti. Il numero Universo esiste già, ma non equivale automaticamente a un'identità della cavia.
- [ ] Decidere se aggiungere una breve fanfara comune di completamento. La chat la suggerisce; il sistema attuale mantiene la musica gameplay durante i risultati.
- [ ] Decidere se ampliare le statistiche con tempo medio e pulsanti premuti. La sessione attuale conserva i conteggi delle cavie/test; il cronometro viene mostrato per la singola prova e non viene aggregato nel report.

Non sono requisiti aggiuntivi le altre morti proposte dall'assistente (laser, acido, pressa, gelo, ecc.), il riferimento esemplificativo a 12 prove o i nomi suggeriti. Le dieci prove e i tre finali del progetto possono essere mantenuti.

## Collaudo manuale in Unity e build

- [ ] Provare ciascuno dei cinque esiti: una sola conclusione, commento e frase corretti, conteggio morte/recupero corretto.
- [ ] Provare pulsanti ed enigmi nelle diverse prove: effetti corretti, nessun percorso bloccato involontariamente e variazioni effettivamente caricate.
- [ ] Completare una partita di dieci test: Universo-10 fino a EndGame, senza salti o doppi avanzamenti; Nuovo Gioco azzera i dati.
- [ ] Verificare i tre finali con 0, alcuni e 10 decessi, confrontando report e immagine selezionata.
- [ ] Verificare formaggio verde e foto, inclusi pausa, conferma e conteggio del soggetto.
- [ ] Provare tastiera, mouse e gamepad su dialoghi, risultati, menu ed EndGame, anche con pressioni ripetute durante le transizioni.
- [ ] Controllare HUD, testi lunghi e immagini a 640×360, 1280×720, 1920×1080 e 1366×768.
- [ ] Provare l'intero percorso nella build destinata alla jam: scene, comandi, audio e ritorno al menu.

## Riferimenti principali

- `Assets/Scripts/GameLoop/GameManager.cs`, `GameSession.cs`, `TestEndEvent.cs`.
- `Assets/Scripts/GameLoop/LethalContact.cs` per il contatto letale già collegato al piranha.
- `Assets/Scripts/Enemies/PiranhaChase.cs`, `PiranhaPatrol.cs`.
- `Assets/Scripts/Player/PlayerSwimming.cs`, `SimulatedBody2D.cs`.
- `Assets/Prefabs/CheeseGreen.prefab`, `Assets/Prefabs/Photo Collectible.prefab`.
- `Assets/Scenes/LoopScene.unity`, `Assets/Scenes/EndGame.unity`.
- `Docs/GameLoop.md`, `Docs/EndGame.md`, `Docs/Rooms.md`.
