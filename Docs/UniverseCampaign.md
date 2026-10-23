# Campagna degli universi

La Nuova partita passa dalla schermata Controls a Universo_10. Le dieci scene
in Assets/Scenes/Universes sono registrate nei Build Settings senza cambiare
gli indici delle scene precedenti.

## Configurazione

Assets/Settings/UniverseCampaign.asset definisce scene, massimo di 10 tentativi
per universo, massimo di 100 morti e scena finale EndGame.
Assets/Prefabs/GameLoop/Universe Rig.prefab contiene player, camera, Game Manager,
HUD, illuminazione, musica, cinque spawn e HandRelease con la configurazione
della Welcome. La mano si posiziona sopra lo spawn selezionato, mostra il topo
sospeso nell'idle e mantiene il player reale disattivo fino alla pressione di
un tasto. La HUD usa lo zoom massimo e mostra "Press a button to start";
dopo il rilascio ripristina lo zoom, mostra il messaggio di benvenuto e la mano
risale svanendo. Offset e tempi sono modificabili nel componente HandReleaseIntro
del prefab Universe Rig. Le scene hanno anche un pavimento
provvisorio da sostituire e un contenitore Puzzle per il contenuto del livello.
Non contengono ancora puzzle o obiettivi di completamento.

Il Game Manager di ciascuna scena ha Starting Universe coerente con il nome.
Per aggiungere spawn, assegnarli al suo array Spawn Points. La selezione mescola
i punti validi senza ripetizioni fino a esaurimento, poi avvia un nuovo ciclo;
con almeno due punti evita anche una ripetizione al confine tra i cicli.

## Progressione e identità

- La prima cavia è U10-R0-G0, al tentativo 1/10.
- La morte consuma un tentativo e introduce una nuova cavia, se la campagna continua.
- Dopo la decima morte nello stesso universo si passa al successivo.
- La conclusione da vivo porta subito all'universo successivo.
- Universo_1 concluso oppure 100 morti terminano la campagna.
  Alla centesima morte non viene creata una cavia G100.
- Solo la morte cambia l'identificativo: G aumenta di uno; R aumenta se
  l'universo coincide con quello dell'identificativo precedente, altrimenti
  riparte da zero. U indica l'universo di introduzione della cavia.
- Una cavia viva conserva identificativo, salute e osservazioni tra universi.
  Sazietà, ossigeno, veleno e saette ripartono dai valori iniziali dei prefab.
- I contatori dei collezionabili rimangono cumulativi durante la partita.

La HUD mostra separatamente universo/tentativo correnti e identificativo.
La nuova campagna usa gli spawn dell'universo e la mappa scene del relativo
asset: le destinazioni Checkpoint restano disponibili per i Game Manager
precedenti, senza Campaign assegnata, ma non sostituiscono la progressione
della nuova campagna.

Per avviare una campagna completa usare New Game. Aprire direttamente una
scena in Play Mode avvia da quell'universo se non esiste una sessione attiva.
Il menu Tools/Gameplay/Create Missing Universe Scenes ricrea soltanto gli
asset mancanti, preservando le scene esistenti e quella aperta.
