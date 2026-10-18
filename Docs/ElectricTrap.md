# Electric Trap

Trascina `Assets/Prefabs/Electric Trap.prefab` in scena. Il pivot è in alto, così la grafica può essere appesa al soffitto. Ridimensiona e sposta il Box Collider 2D (Is Trigger) per definire l’area letale.

- `Automatic Activation`: se attivo, avvia il periodo elettrificato all’abilitazione della trappola. Se disattivo, aspetta un comando esterno. Alla scadenza viene disattivato: serve una nuova attivazione.
- `Activate()`: elettrifica l’area per `Activation Duration` secondi (3 di default), colpendo anche chi entra durante il periodo attivo. Richiamarlo fa ripartire la durata. Uccide i piranha presenti e termina il test se il topo si trova nel trigger.
- `EnableAutomaticActivation()`: attiva l’area per la durata configurata, colpendo anche chi è già presente.
- `DisableAutomaticActivation()` / `Deactivate()`: spegne subito l’area e la luce.
- `SetAutomaticActivation(bool)`: permette di configurare lo stesso comportamento con un parametro booleano nello Unity Event.
- `Show Gizmos`: mostra il contorno rosso dell’area nella Scene View.

Nel componente Interactable Button, aggiungi la trappola a `On Pressed` e scegli uno dei metodi pubblici di ElectricTrap. Per usarla solo attraverso il pulsante, disattiva `Automatic Activation`.

Il prefab include Dialogue Message Player e Test End Event: modifica qui il commento dello scienziato e i risultati. La scarica usa il ciclo standard (flash bianco, commento, risultati e universo successivo). Non richiede tag o layer aggiuntivi: riconosce il Player Collider del Game Manager. Il trigger resta abilitato anche quando la modalità automatica è spenta, per consentire le scariche manuali.

Il recupero del player evita l’area quando la modalità automatica è attiva.

## Piranha folgorati

Il piranha di LoopScene include Piranha Death, Simulated Body 2D, Crate Motion (inizialmente disabilitato) e Crate Buoyancy. Le copie dell’oggetto mantengono queste impostazioni; altri piranha devono avere lo stesso componente Piranha Death, che richiede i componenti di galleggiamento. Le trappole individuano automaticamente i piranha attivi, senza liste nell’Inspector.

La scarica, manuale o automatica, colpisce tutti i piranha sovrapposti al trigger. Piranha Death disattiva ronda, inseguimento e contatto letale, capovolge la sprite su Y e rende solido il collider. Il corpo galleggia usando lo stesso sistema delle casse e trasporta il topo quando ci sale sopra.

`Piranha Death → Passenger Downward Acceleration` (45 di default) aggiunge accelerazione verso il basso finché il topo è in piedi sul corpo. Aumentala per farlo affondare più rapidamente. In `Crate Buoyancy`, Buoyancy Acceleration e Water Drag regolano risalita e galleggiamento. Quando il topo lascia il corpo, la spinta di galleggiamento lo fa risalire. Le casse esistenti mantengono il loro comportamento.

Da provare in Play Mode: scarica manuale e automatica con più piranha, sola presenza di pesci senza topo nell’area, salto sul corpo, affondamento sotto il peso e risalita dopo averlo lasciato.

## Luce e durata

Il figlio `Electric Light 2D` è una Point Light 2D azzurra. Puoi posizionarlo liberamente e modificare colore e raggio nel componente Light 2D. Electric Trap gestisce accensione e spegnimento e varia l’intensità durante l’attivazione: `Flicker Frequency`, `Minimum Light Intensity` e `Maximum Light Intensity` regolano il lampeggio. Alla scadenza di `Activation Duration` l’area e la luce si spengono insieme. Il timer usa il tempo di gameplay e si ferma durante pausa, foto e conclusione del test.

`Activate()` ora crea un periodo di elettrificazione, anziché una scarica istantanea. I collegamenti Unity Event esistenti restano validi. Per riaccendere la trappola usa Activate o EnableAutomaticActivation.

## Suono della scarica

Ogni Activate / EnableAutomaticActivation / attivazione iniziale avvia una delle quattro varianti ElectricArc_01–04, scelta casualmente. Il prefab ha un Audio Source dedicato, 2D e senza Play On Awake. Discharge Volume (0.65) regola il volume locale, moltiplicato per il volume Effects nelle opzioni; Master continua a usare AudioListener. Le variazioni delle opzioni si applicano anche a un suono in corso.

Il suono viene riprodotto una volta per attivazione (circa 1.4 secondi), senza ripartire a ogni controllo del trigger. Deactivate e disabilitare la trappola lo interrompono. Una nuova Activate fa ripartire la scarica senza sovrapporre più suoni.
