# Scene Universe: base comune

Le scene `Assets/Scenes/Universes/Universo_1.unity` fino a `Universo_10.unity` contengono un'istanza di **Assets/Prefabs/GameLoop/Universe Base.prefab**. Ogni scena imposta il proprio Starting Universe e il proprio nome di scena sul GameManager. I GUID delle scene esistenti sono conservati: la campagna originale e le scene di build continuano a puntare agli stessi asset.

Il prefab comune deriva da Universo_10_Composed, che resta disponibile come scena di riferimento con il livello disegnato. Parallax Test resta separata.

## Contenuti comuni

- Systems: GameManager, DialogueManager, StageEndManager, CollectionGoalController, camera pixel perfect, HUD, pausa, luci, musica e mano di ingresso.
- Player e HUD restano istanze dei loro prefab originali, annidate nella base.
- Spawn Points: un solo Spawn 1 iniziale a `(0, 0.3)`, spostabile nella singola scena. Il GameManager lo ha gia assegnato.
- World: i 12 piani di profondita, il relativo ParallaxConfigurator e i Grid per le tilemap.
- Fondale del laboratorio: scrivania, libri, fiale, scaffali, scienziato, assistente, lente e decorazioni di foreground, con i collegamenti a camera, player e spawn.
- Quattro tilemap vuote: terreno solido, tubi, foreground e fondale interno delle stanze. Mantengono renderer, materiali e componenti di collisione.
- Water Geometry Baker con le tilemap sorgente assegnate, senza bacini, maschera o bake del livello precedente.

Non sono presenti collezionabili, goal, casse, corde, piattaforme, trappole, acqua di livello, terreno scavabile o decorazioni di gameplay del livello precedente. Le regole automatiche di completamento basate sui collezionabili sono vuote; dialoghi e casistiche di fine prova restano disponibili nei manager.

## Creare un livello

1. Aprire la scena Universo_N desiderata e dipingere le tilemap sotto World. La geometria va aggiunta prima di provare il movimento: non e presente un pavimento provvisorio.
2. Inserire i prefab nei piani appropriati: acqua/scavo, decorazioni di gameplay, personaggi o props. Mantenere insieme le parti di ciascun oggetto composto.
3. Spostare Spawn 1. Per aggiungere altri spawn, inserirli nella lista Spawn Points del GameManager della scena.
4. Aggiungere goal e condizioni di raccolta del livello. Configurare le regole sul CollectionGoalController quando necessarie.
5. Dopo aver disegnato stanze e tubi, usare il Water Geometry Baker per generare la geometria dell'acqua del nuovo livello.
6. Salvare la scena. Geometria, oggetti aggiunti, spawn e condizioni del singolo universo devono rimanere modifiche della scena.

Modificare **Universe Base.prefab** per cambiamenti condivisi di sistemi, fondale e struttura dei piani. Modificare i prefab Player e Gameplay HUD per gli aggiornamenti comuni del personaggio e dell'interfaccia. Non applicare alla base comune le tilemap dipinte o gli oggetti specifici di un singolo livello tramite Apply All.

La priorita visiva della lente dipende dal suo Sorting Group; la cattura ingrandisce solo gli elementi dietro al suo ordine effettivo. Il configuratore conserva i valori impostati per ciascun piano.

## Archivio

Le versioni precedenti delle dieci scene sono conservate in **Assets/Scenes/Archive/Before Universe Templates/** con suffisso `_PreTemplate`. Le copie archiviate hanno nuovi GUID e non sono incluse nelle scene di build. Per recuperare contenuto, aprire la copia corrispondente e trasferire soltanto gli elementi desiderati nella nuova scena.

## Controlli

Sono stati verificati struttura YAML e riferimenti locali del prefab, numero dei piani, tilemap vuote, rimozione dei dati di bake, spawn unico, numerazione delle dieci scene e conservazione dei collegamenti di campagna/build. E stato eseguito anche il controllo caricando il prefab nell'editor Unity, con esito positivo. Il comando **Tools > Gameplay > Audit Universe Base** consente di ripeterlo. Nessun test in Play Mode e stato eseguito.
