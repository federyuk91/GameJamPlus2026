# Apnea e ossigeno

Player.prefab include Player Oxygen con una riserva indipendente di 5 tacche. Quando il punto di respirazione è immerso in un Water Volume, la barra della sazietà mostra APNEA e tacche azzurre.

- Maximum Notches: 5.
- Drain Interval: 5 secondi per tacca sott’acqua.
- Recovery Interval: 5 secondi per tacca fuori dall’acqua.
- Breathing Point Height: 0.85, altezza relativa nel collider (0 = piedi, 1 = sommità). Il punto usa il centro orizzontale del collider. Il topo può respirare a pelo d’acqua se questo punto è emerso.

A zero ossigeno, Drowning Outcome conclude il test con il sistema consueto: flash bianco, commento dello scienziato, risultati e universo successivo. Il Player contiene un Dialogue Message Player e un Test End Event dedicati all’apnea, con messaggi editabili.

Fuori dall’acqua la barra torna subito a SAZIETÀ. L’ossigeno si ripristina in background, una tacca ogni 5 secondi. La sazietà mantiene il proprio ritmo anche durante l’immersione; mangiare formaggio ripristina la sazietà e non modifica l’ossigeno. Ogni tacca di ossigeno persa produce un leggero flash celeste a tutto schermo, senza vocalizzi o shake aggiuntivi. Satiety Hud espone Oxygen Flash Color e Oxygen Flash Duration (0.25 secondi, alpha 0.16).

La pausa e la conclusione del test fermano consumo e recupero. Il nuovo universo riparte con l’ossigeno pieno. Le riemersioni brevi conservano il consumo parziale: un intervallo completo di recupero ripristina una tacca e azzera il consumo parziale della successiva. Rientrare in acqua prima del completamento interrompe l’intervallo di recupero.

LoopScene collega Satiety Hud al Player Oxygen del prefab. La UI si aggiorna a ogni cambio di stato o di tacca, con eventi. Altri HUD devono assegnare la reference Oxygen nel componente Satiety Hud.

Da verificare in Play Mode: perdita di una tacca ogni 5 secondi; ritorno alla barra sazietà; recupero prima di immergersi nuovamente; respirazione in superficie; pausa; morte per apnea e nuovo universo; nessun ripristino dell’ossigeno mangiando formaggio.

## Avviso a 10 secondi

Player Oxygen espone Warning Seconds (10) e lo Unity Event On Low Oxygen. Il prefab collega l’evento a Play di un Dialogue Message Player separato, con frasi di avvertimento editabili e durata di 6 secondi. Il commento normale non ferma gameplay e consumo di ossigeno; il commento di morte rimane distinto.

La soglia usa il tempo residuo effettivo, incluso il consumo parziale della tacca: con 5 tacche e intervallo 5 secondi l’avviso arriva a 2 tacche rimaste. Viene mostrato una sola volta e si riabilita dopo il recupero completo dell’ossigeno. Riemergere brevemente non ripete l’avviso.

## Bolle dopo il flash

Terminato il flash celeste, Satiety Hud chiama PlayBurst sul Player Oxygen Bubbles del topo. Il componente emette esattamente tre bolle in sequenza usando bubble1, bubble2 e bubble3, importate con Point, 32 PPU e senza compressione.

Emission Interval (0.12 secondi) regola la distanza temporale tra le bolle; Rise Speed (1.5 unità/s) la risalita; Wobble Width (0.04) l’oscillazione orizzontale; Mouth Height (0.75) l’altezza di emissione nel collider. Il lato segue Mouse Facing. Le bolle si muovono indipendentemente dal topo, sulla griglia dei pixel, e spariscono appena raggiungono la superficie/uscita dal volume d’acqua. Se il topo è già emerso, non vengono emesse bolle in aria.

Le sprite renderer vengono riutilizzate. Pausa e fine del test fermano la risalita con il resto del gameplay. Per altri HUD, assegnare Oxygen Bubbles in Satiety Hud; LoopScene è già collegata.
