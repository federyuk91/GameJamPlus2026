# Loop dei test

`Game Manager` e gia presente in LoopScene, con riferimento al Gameplay HUD. La sessione parte da Universo-10. `GameSession` conserva il numero dell'universo fra i caricamenti della scena, per la durata della partita; Nuovo Gioco dal menu lo azzera prima di iniziare una nuova sessione.

## Fine di un test

1. Un evento chiama `TestEndEvent.EndTest()`.
2. Il Game Manager acquisisce il tempo del test e sceglie una frase da `Result Messages`. Eventuali ulteriori richieste di fine test vengono ignorate.
3. Gioco e cronometro si fermano. Un flash bianco di durata configurabile copre la scena e resta visibile.
4. Lo scienziato mostra una frase casuale dal `DialogueMessagePlayer` assegnato al TestEndEvent. Il dialogo usa lo stesso avatar. Per questi commenti non vale il timeout automatico: occorre confermare.
5. Click sul pulsante del dialogo, Invio o Y/Triangolo completano prima la digitazione, poi confermano Avanti. Appare la schermata `TEST COMPLETATO!`, con `Successo! ... ore ... minuti e ... secondi.`, la frase risultato e `Universo-N`.
6. Il pulsante Avanti, Invio o Y/Triangolo nella schermata risultato decrementano l'universo e ricaricano LoopScene. Dopo Universo-1 il valore diventa 0 e si carica EndGame.

Le scene MainMenu, LoopScene ed EndGame sono incluse nei Build Profiles. EndGame mostra il report finale della sessione e, dopo Continue, uno dei tre finali in base ai deceduti; vedere `Docs/EndGame.md`. Ogni ricaricamento ricomincia il cronometro e i conteggi dei raccoglibili del test. Il benvenuto usa `{universe}` nel testo per mostrare il numero attuale.

La pausa usa `Time.timeScale = 0`; flash, digitazione e transizioni della UI usano tempo non scalato. Il caricamento ripristina la scala temporale precedente. La musica continua durante il commento e la schermata risultato.

## Configurare una causa di morte

Aggiungere `TestEndEvent` all'oggetto che determina la fine del test:

- `Scientist Comment`: riferimento a un `DialogueMessagePlayer` con avatar e lista dei commenti. Il componente viene usato come contenitore dei messaggi; il Game Manager li mostra sul suo HUD.
- `Result Messages`: lista delle possibili frasi per la schermata risultato. Le stringhe vuote vengono ignorate. Con una sola frase il risultato e fisso.
- `Subject Died`: attivo per le cause di morte; disattivare per test conclusi con il soggetto vivo. Alimenta i conteggi del report finale e la selezione del finale.
- `Game Manager`: riferimento esplicito opzionale per gli oggetti della scena; nei prefab vuoto usa l'istanza del manager attivo.

Nell'evento della trappola, del nemico o del raccoglibile, selezionare `TestEndEvent → EndTest()`. Il prefab CheeseGreen e gia configurato tramite `Collectible.On Collected`. Le sue frasi dello scienziato precedenti sono state mantenute. Le altre cause di morte vanno collegate ai rispettivi eventi quando vengono implementate.

## Verifica in Unity

Mangiare un formaggio normale con E / pulsante ovest: incrementa il contatore e mostra il messaggio ordinario. Mangiare CheeseGreen: incrementa il secondo contatore, ferma il gioco, mostra bianco e commento; confermare per vedere tempo e risultato, quindi confermare per ripartire da Universo-9. Per verificare rapidamente il passaggio a EndGame, avviare una nuova sessione con `Starting Universe = 1` sul Game Manager, poi ripristinarlo a 10. Controllare anche mouse e gamepad, testi lunghi e assenza di doppi avanzamenti.

## Foto degli esperimenti

Photo Collectible apre prima la fotografia e attende Next (Invio / Y / pulsante). Il suo evento On Continue chiama TestEndEvent.EndTest, avviando lo stesso ciclo del formaggio verde. Le frasi dello scienziato sono configurabili sul prefab e invitano il soggetto a fare una pausa; le frasi del risultato sono separate. La pausa della fotografia viene ceduta al Game Manager senza un frame di ripresa del gameplay. La fotografia e il suo tipo sono descritti in Docs/Collectibles.md.

## Piranha letale

Il Piranha in LoopScene ha un BoxCollider2D trigger e `LethalContact`, collegato direttamente al collider del player e al suo `TestEndEvent`. Il controllo viene eseguito dopo il movimento del piranha; usa le distanze fra collider e funziona senza Rigidbody2D. Basta il contatto, anche durante la ronda. La richiesta di fine test viene inviata una sola volta.

Il `DialogueMessagePlayer` sul Piranha contiene i commenti dello scienziato, modificabili nell'Inspector. `TestEndEvent.Result Messages` contiene: Il soggetto ha socializzato con i pesci con notevole entusiasmo. `Subject Died` e attivo. Il contatto avvia il normale ciclo con flash, commento, risultato e avanzamento dell'universo.

`LethalContact` puo essere riutilizzato su altre trappole assegnando Contact Area, Player Collider e Test End. Il collider di contatto va mantenuto trigger per non bloccare il movimento del topo.

## Spawn Point

`Assets/Prefabs/Spawn Point.prefab` e un punto vuoto con gizmo verde nella Scene View. Posizionarlo usando il pivot del player, non i piedi. La rotazione del punto non modifica il topo.

Nel Game Manager assegnare `Player`, `Gameplay Camera` e le istanze della scena all'array `Spawn Points`. All'avvio di ogni istanza di LoopScene viene scelto casualmente un punto assegnato, attivo e con componente abilitato. I riferimenti nulli vengono ignorati. Il player viene posizionato in Awake, prima dell'inizializzazione dei suoi componenti; la camera viene riallineata all'avvio per evitare un'inquadratura dalla posizione precedente. La profondita Z del player resta invariata.

LoopScene contiene gia un'istanza assegnata nella posizione iniziale attuale del player. Duplicare il prefab nella scena e aggiungere ciascuna istanza alla lista del manager. I punti presenti nella scena ma non assegnati non partecipano alla scelta. Posizionare i punti in spazi liberi: non e previsto un controllo automatico della geometria. Senza punti validi, viene emesso un avviso e resta la posizione del player salvata nella scena.

Il controllo di disponibilita degli Spawn Point usa `enabled`, `activeInHierarchy` e una scena valida. Non usa `isActiveAndEnabled`, che puo essere ancora falso nell'Awake anticipato del Game Manager se il punto e un suo figlio. I punti disabilitati o con parent inattivi restano esclusi; i prefab del Project non sono istanze valide.
