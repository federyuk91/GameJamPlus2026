# Player platformer

Scena principale: Assets/Scenes/LoopScene.unity (gia abilitata nel build).

## Composizione

- PlayerInputReader: Input System, stick analogico/D-pad; A/D o frecce. Salto: pulsante sud del gamepad (A Xbox / croce PlayStation) oppure spazio. Conserva la pressione fino al tick fisico.
- SimulatedBody2D: BoxCast lungo X e Y, collisioni con layer solidLayers (Default nella scena), esclusione trigger e collider del player, skin di 0.005 unita. Sposta il Transform senza Rigidbody2D. Progettato per geometria statica, pavimenti e pareti; non implementa piattaforme mobili, one-way, spinte o risalita delle pendenze.
- PlayerMovement: velocita, accelerazione a terra/in aria, gravita, salto variabile al rilascio, coyote time e jump buffer. Parametri modificabili nell'Inspector. Simulazione in FixedUpdate.
- MouseFacing: orienta lo SpriteRenderer senza ribaltare collider o Transform.

## Scala

32 pixel = 1 unita. Sprite del topo: canvas 32x32, PPU 32, point filtering, niente mipmap/compressione. Collider 24x22 pixel per evitare che orecchie e coda blocchino il movimento. Pixel Perfect Camera: 640x320, 32 PPU, render texture con ingrandimento intero e filtro Point, bordi per mantenere le proporzioni. Area di riferimento 20x10 unita. Disegnare le risorse mantenendo 32 PPU e dimensioni in multipli di 1/32 di unita.

La geometria esistente di StartRoom e mantenuta. Il muro destro e parziale: si puo uscire dalla stanza e cadere; checkpoint e respawn non sono implementati.

## Verifica

Assets/Editor/PlatformerValidation.cs espone PlatformerValidation.Run per esecuzione batch Unity. Controlla componenti, assenza Rigidbody2D, riferimenti script, sprite/camera e sweep contro pavimento, pareti e soffitto. Usare in batch: termina l'Editor al termine e non salva la scena modificata dai controlli.

Da verificare in Play Mode con controller: movimento analogico, rilascio del salto, salto al bordo e salto premuto poco prima dell'atterraggio.

## Caduta e camera

La gravita in discesa e 66 unita/s^2 (30 x 2.2), con velocita massima di 26 unita/s. La gravita in salita resta 30 per mantenere l'altezza del salto. fallGravityMultiplier e terminalSpeed sono assegnati sul componente PlayerMovement nella scena.

PlatformCameraController e assegnato alla Main Camera e segue il Transform del Player in LateUpdate. Zona morta, smorzamento per asse, anticipo orizzontale e anticipo verso il basso durante la caduta sono regolabili nell'Inspector. La caduta usa un inseguimento verticale piu rapido. Il movimento della camera e arrotondato a 1/32 di unita, mantenendo lo stato dello smorzamento a precisione completa. La profondita Z resta costante. SnapToTarget() permette di riallineare la camera dopo un teletrasporto/respawn. Non sono imposti limiti di stanza.

## Unita dei parametri

Tutte le distanze fisiche sono in unita Unity (1 unita = 32 pixel). Velocita orizzontale, salto e caduta massima sono in unita/s; accelerazione e gravita sono in unita/s^2. I tempi sono in secondi e i moltiplicatori sono adimensionali. L'Inspector esplicita queste unita. Esempio: speed = 5 significa 5 unita/s, ossia 160 pixel/s. La simulazione integra accelerazione x deltaTime in velocita e velocita x deltaTime in spostamento; i PPU riguardano la rappresentazione grafica.

## Cassa: spinta e trascinamento

Cassa spostabile nella LoopScene usa SimulatedBody2D + PushableCrate, senza Rigidbody2D. PushableCrate applica la gravita prima del tick del player; non conserva inerzia orizzontale. PlayerCrateInteraction sul player gestisce la spinta camminando contro il fianco della cassa e il trascinamento con E (tastiera) o pulsante ovest (X Xbox / quadrato PlayStation) + direzione. Funziona da entrambi i lati. Il player e la cassa devono essere a terra; saltare o lasciare il tasto interrompe la presa.

La velocita di interazione e 2.5 unita/s (80 pixel/s), portata della presa 0.12 unita. Entrambi i parametri sono nell'Inspector di PlayerCrateInteraction. Le query limitano il movimento della coppia contro gli ostacoli sia davanti alla cassa sia dietro il player; durante il trascinamento lo spostamento orizzontale pianificato ignora soltanto il collider della cassa afferrata. La cassa resta solida per i movimenti verticali: il topo puo salirci con un salto. Non sono implementate spinte a catena, rotazioni o trasporto del player sopra casse in movimento.

PlatformerValidation.Run include una scena temporanea per controllare spinta, tiro, rilascio, separazione dei collider e blocco da muri sui due lati. La scena temporanea non viene salvata.

## Corda: arrampicata, oscillazione e salto

Rope nella LoopScene contiene cinque Rope Piece collegati da vincoli di distanza simulati, senza Rigidbody2D o Joint2D Unity. Il Transform di Rope e l'ancoraggio superiore; i pezzi sono ordinati dall'alto verso il basso nel campo pieces. Ogni segmento misura 0.5 unita (16 pixel), per una lunghezza totale di 2.5 unita. Gli oggetti inizialmente sovrapposti sono disposti in catena anche nella scena salvata.

- Agganciarsi: E / pulsante ovest (X Xbox, quadrato PlayStation), oppure Su/W vicino alla corda. Una volta agganciato non occorre tenere il tasto.
- Arrampicarsi: Su/Giu, W/S, stick verticale o D-pad. Velocita: 2 unita/s. La presa e limitata agli estremi della corda.
- Oscillare: Destra/Sinistra, A/D, stick orizzontale o D-pad. Accelerazione sulla corda: 70 unita/s^2; alternare la direzione assecondando l'oscillazione.
- Mollare e saltare: Jump (spazio / pulsante sud). Conserva la velocita della corda al punto di presa e aggiunge 4 unita/s tangenziali nella direzione dell'oscillazione e 7 unita/s verso l'alto. A corda ferma la direzione orizzontale sceglie il lato del salto. Per 0.25 secondi il controllo aereo e ridotto per preservare lo slancio.

RopeChain gestisce la simulazione numerica con integrazione Verlet, quattro sottopassi e 24 iterazioni dei vincoli. RopeSimulation collega il solver ai pezzi della scena. PlayerRopeInteraction gestisce presa, arrampicata e rilascio; PlayerMovement coordina il singolo tick e disattiva la presa della cassa quando si passa alla corda. Portata, velocita, accelerazione, smorzamento e tempi sono modificabili nell'Inspector con unita esplicite. La ri-presa e bloccata per 0.3 s dopo il rilascio.

Il player continua a usare i suoi sweep contro pareti, pavimenti e casse; un ostacolo che impedisce di seguire la corda interrompe la presa. I segmenti sono visivi: non hanno collisioni proprie con lo scenario e non supportano avvolgimento attorno agli ostacoli. Non muovere o ridimensionare l'ancoraggio durante la simulazione.

RopeValidation.RunChainChecks verifica stabilita dell'ancoraggio, lunghezze, oscillazione, velocita in unita/s e limiti della presa a 30, 50 e 100 Hz. La verifica numerica e stata eseguita con gli assembly Unity reali fuori dall'Editor; per controllare input e interazioni con i collider resta necessaria una prova in Play Mode.

## Limiti reali di oscillazione e gizmo inner/outer

RopeSimulation > Swing Limits definisce ora vincoli reali, in gradi rispetto alla verticale verso il basso (0). Min indica il lato sinistro, Max il destro. Valori della scena: Min Inner -45, Min Outer -60, Max Inner 45, Max Outer 60. I lati sono regolabili indipendentemente; ogni inner rimane fra il centro e il relativo outer.

Nell'area centrale il movimento e libero. Fra inner e outer una velocita angolare massima basata sulla distanza di arresto riduce progressivamente lo spostamento verso l'esterno. Anche la forza di oscillazione verso l'esterno diminuisce nella fascia; l'input verso il centro resta disponibile. All'outer il solver ferma tutti i segmenti per il resto del tick e azzera la loro velocita, senza rimbalzo istantaneo. Nel tick seguente la gravita avvia il ritorno verso il centro. La correzione ruota la catena attorno all'ancoraggio, conservando le lunghezze dei collegamenti. Il vincolo misura l'angolo globale fra ancoraggio e punta della corda, non l'angolo locale di ciascun pezzo. La velocita trasferita al salto proviene dallo stato del solver dopo la frenata.

Selezionando Rope con Gizmos attivi: raggi e arco azzurri mostrano gli inner, rossi gli outer, settori arancioni trasparenti mostrano le fasce di decelerazione; in Play il raggio giallo mostra l'angolo corrente. I gizmo leggono direttamente i limiti della simulazione: i vecchi angoli di anteprima sono rimossi.

Verifiche numeriche superate a 30/50/100 Hz: frenata prima dell'outer, rispetto dei limiti asimmetrici, velocita zero di tutti i nodi al limite, inversione anche mantenendo input verso l'esterno, conservazione dei collegamenti e regressioni del solver. Compilazione C# verificata anche con UNITY_EDITOR per i gizmo.

## Nuoto

Water della LoopScene ha WaterVolume e un BoxCollider2D trigger, allineato al suo sprite e alla sua scala. Il player ha PlayerSwimming; il modello numerico SwimMotion contiene i parametri. Nessun Rigidbody2D viene aggiunto. La presenza in acqua e rilevata ogni tick con una query sul centro del collider del topo, quindi non dipende da OnTriggerEnter/Exit o dalla simulazione Rigidbody. Volumi sovrapposti funzionano senza conteggi di ingresso/uscita. Per aggiungere altra acqua, aggiungere WaterVolume a un GameObject e dimensionare il suo BoxCollider2D.

In acqua: direzione orizzontale da tastiera/stick/D-pad, velocita 3 unita/s, accelerazione 10 unita/s^2 e frenata 6 unita/s^2. Il topo affonda spontaneamente con gravita 6 unita/s^2 fino a 2.2 unita/s; la resistenza verticale e 0.8 al secondo. Ogni nuova pressione di Jump (spazio / pulsante sud) rinnova una spinta verticale di 4.8 unita/s. Tenere Jump premuto non ripete la spinta e rilasciarlo non taglia la bracciata. Premere ripetutamente permette di risalire. Gli impulsi non si sommano senza limite.

Quando il centro del collider esce dall'acqua riprende il movimento normale, conservando lo slancio della bracciata per emergere dalla superficie. Il nuoto mantiene le collisioni contro i solidi, azzera coyote time e buffer del salto terrestre, e interrompe la presa delle casse. Una corda afferrata ha priorita sul nuoto: si puo usare per uscire dall'acqua e rilasciarla con Jump. Le casse conservano la loro fisica: questa modifica non introduce galleggiamento degli oggetti.

Compilazione C# e test numerici del nuoto superati a 30/50/100 Hz. PlatformerValidation include anche controlli dei volumi (query senza Rigidbody, volumi disabilitati/sovrapposti, uscita dalla superficie e dai lati), da eseguire nell'Editor; il comportamento completo con collider e input resta da verificare in Play Mode.

## Settings condivisi: gravita e acqua

Selezionare Assets/Settings/PhysicsSettings.asset nel Project di Unity. Nell'Inspector espandere Gravity per gravita e salto del player, gravita/caduta delle casse, gravita della corda e velocita del salto dalla corda. Espandere Water per tutti i valori del nuoto: velocita orizzontale, accelerazione, frenata, gravita di affondamento, velocita massima di caduta, velocita della bracciata e resistenza verticale.

L'asset e condiviso da PlayerMovement, PlayerSwimming, PlayerRopeInteraction, RopeSimulation e dalle due PushableCrate della scena. I valori vengono letti direttamente dall'asset a ogni tick, quindi possono essere regolati in Play Mode. Gli asset modificati in Play Mode possono conservare le modifiche dopo l'uscita dal Play. Le vecchie copie dei parametri sui componenti sono state rimosse.

Per l'acqua sono stati trasferiti i valori modificati nello script SwimMotion (speed 5, acceleration 10, braking 6, gravity 7, maxSinkSpeed 3.5, strokeSpeed 4.8, verticalDrag 1.5), sostituendo i precedenti valori inline della scena. Le altre impostazioni fisiche mantengono i valori della scena. I default nel codice servono alla creazione di nuovi asset e come fallback per componenti senza asset; nella LoopScene tutti i sei componenti interessati hanno il riferimento assegnato.

Nuovi profili si creano con Create > Mouse Platformer > Physics Settings. Assegnare il profilo al campo Physics Settings dei componenti interessati. La scala resta 32 pixel per unita; velocita in unita/s, accelerazioni in unita/s^2, resistenza in 1/s e moltiplicatori adimensionali.

## Scala: Stair

Stair e composto con cinque StairPiece usati come pioli e due montanti figli. Il volume Ladder e un BoxCollider2D trigger di 1x5 unita. Il player usa PlayerLadderInteraction, coordinato da PlayerMovement: Su/W/stick verticale aggancia e sale, Giu/S scende. Senza direzione verticale mantiene la presa senza applicare gravita. Jump + Destra/Sinistra salta dal lato scelto; senza direzione usa l'ultimo lato indicato.

La posizione di presa e memorizzata nello spazio locale della scala. Spostando Stair o il suo genitore, il player segue lo stesso punto; la velocita del supporto viene aggiunta al salto. PlayerMovement esegue il tick dopo gli script con ordine predefinito, cosi un mover della scala in FixedUpdate puo aggiornare prima il supporto. Non riparentare il player e non aggiungere Rigidbody2D. Le collisioni continuano a usare SimulatedBody2D; se un ostacolo impedisce di raggiungere il punto della scala la presa si interrompe. Arrivare agli estremi salendo/scendendo interrompe la presa; in cima si cerca di portare i piedi sopra il bordo. Le piattaforme solide soprastanti non vengono attraversate.

PhysicsSettings.asset > Ladder raccoglie velocita di arrampicata (3 unita/s), salto orizzontale (6 unita/s), verticale (9 unita/s), portata (0.65 unita) e ritardo di riaggancio dopo il salto (0.3 s). Scala e corde sono alternative; una corda gia afferrata mantiene la priorita. La scala puo essere usata dall'acqua e interrompe la presa delle casse.

Compilazione C# e test numerici superati a 30/50/100 Hz, anche con scale diverse e salto che eredita la velocita del supporto. PlatformerValidation include un controllo con scala mobile, salita/discesa, mantenimento della coordinata locale, salto e cooldown; questo controllo con oggetti Unity e da eseguire nell'Editor/Play Mode.

## Movimento completo del player nei settings

PhysicsSettings.asset raccoglie anche Player (velocita orizzontale, accelerazione/frenata a terra, accelerazione in aria, coyote time, jump buffer, durata e moltiplicatore del controllo ridotto dopo un salto da scala/corda) e Interactions (velocita e portata spinta/tiro casse, arrampicata e accelerazione di oscillazione sulla corda, portata e ritardo di riaggancio). Le vecchie copie sui componenti sono rimosse; i valori della scena sono trasferiti all'asset. Gravita/salti, Water e Ladder restano nelle rispettive sezioni.

L'Inspector dedicato mostra Player — movimento in cima. Nell'Inspector predefinito espandere Player. Tutti i parametri sono letti dall'asset durante la simulazione; non occorre modificare gli script per regolarli.

## Jump Height

PhysicsSettings > Gravity > Jump Height (unità) sostituisce Jump Speed per il salto terrestre. Il valore e la salita massima rispetto al punto da cui parte il salto completo, tenendo Jump premuto e senza ostacoli sopra il topo. Per esempio 2 unita = 64 pixel. La velocita iniziale viene ricavata con sqrt(2 * gravita * altezza). L'integrazione della salita usa lo spostamento analitico e termina il tick esattamente all'apice con velocita zero, evitando l'errore del passo Euler e la perdita dell'apice fra due tick. La discesa mantiene gravita maggiorata e limite di caduta esistenti.

Il rilascio anticipato di Jump conserva il salto corto: Jump Height indica l'altezza del salto completo. Soffitti/collisioni possono interrompere prima la salita. Durante un salto mantenere costante la gravita per ottenere l'altezza impostata. Gravita del player positiva (minimo 0.001). La velocita precedente di 11 con gravita 30 e stata migrata a circa 2.016667 unita; nuovi asset partono da 2 unita. Nuoto e salti da corda/scala mantengono i loro parametri dedicati.

JumpValidation verifica 5 altezze, 3 gravita e tick a 10/30/50/100 Hz: apice entro 0.0001 unita dall'altezza richiesta, velocita zero all'apice, successiva discesa, rilascio anticipato e altezza zero. Verifica numerica e compilazione C# superate.

## Climb-up automatico sui bordi

PlayerLedgeClimb e assegnato al player e legge PhysicsSettings > Ledge Climb. Spingendo verso la parete, il componente cerca la superficie superiore appena oltre l'angolo con una raycast verso il basso. La superficie deve appartenere al collider incontrato e il dislivello dai piedi deve essere entro Max Height (default 0.75 unita, 24 pixel). Non si aggancia a trigger o semplici pareti troppo alte.

Prima di iniziare verifica spazio sopra il corpo, corridoio orizzontale in alto e ingombro finale. La risalita muove il corpo prima verso l'alto e poi oltre il bordo, con sweep a ogni tick, senza teletrasporti. Ostacoli nuovi interrompono il movimento. Il punto finale e locale al collider di appoggio per seguire piccoli spostamenti del supporto durante la manovra. Al termine effettua una breve sonda di atterraggio e torna al movimento normale. La manovra automatica blocca il movimento ordinario fino al completamento; non aggiunge un salto durante la risalita.

Parametri nell'Inspector dedicato: abilitazione, altezza massima, portata oltre il bordo (0.12 unita), velocita (5 unita/s), pausa tra tentativi (0.15 s). L'aggancio a corde/scale ha priorita finche gia attivo; la risalita funziona anche contro un bordo dall'acqua. Le prese delle casse vengono rilasciate durante la manovra.

Compilazione C# e riferimenti della scena verificati. Tools > Mouse Platformer > Verifica risalita bordi esegue controlli con collider reali su entrambi i lati, a 30/50/100 Hz, inclusi soffitto basso, parete troppo alta, trigger e atterraggio senza sovrapposizioni. Questi controlli richiedono Unity e non sono stati eseguiti qui; comportamento da verificare in Play Mode.

## Controllo orizzontale dopo la bracciata

Dopo Jump in acqua, PlayerSwimming mantiene per 0.35 s una finestra di controllo orizzontale piu rapido: accelerazione 45 unita/s^2 invece della normale accelerazione di nuoto 10. La velocita massima e invariata e la bracciata conserva la velocita laterale acquisita. La finestra continua appena fuori dall'acqua: in aria conserva velocita e risposta del nuoto fino alla scadenza, evitando un cambio immediato ai parametri del movimento terrestre. L'aggancio a corda/scala e il climb-up interrompono la finestra.

PhysicsSettings > Water espone strokeAcceleration (Accelerazione dopo Jump) e strokeControlTime (Durata controllo dopo Jump). Lo stato temporale e del singolo player, non dell'asset condiviso. La velocita verticale della bracciata e l'affondamento restano separati dal controllo orizzontale.

Test numerici a 30/50/100 Hz superati, inclusi mantenimento della velocita durante Jump, raggiungimento rapido della velocita massima da fermo, conservazione del controllo in superficie e inversione di direzione. Compilazione C# verificata; prova completa con input/collider in Play Mode ancora necessaria.
