# Universo_10_Composed

Scena: `Assets/Scenes/Universes/Universo_10_Composed.unity`.

Usa il gameplay di Universo_10 e la composizione visiva di Parallax Test. I prefab di gameplay conservano i collegamenti e le personalizzazioni della scena sorgente. Le scene sorgenti non vengono sostituite.

```text
Level_Universe_10_Composed
├── Systems
│   ├── Game Manager (dialoghi, fine prova, collezione)
│   ├── Main Camera
│   ├── Gameplay HUD
│   ├── HandRelease
│   ├── Global Light
│   └── Gameplay Music
├── Spawn Points
└── World (ParallaxConfigurator)
    ├── 01 Foreground - Desk Decorations
    ├── 02 Terrain
    ├── 03 Water And Diggable Ground
    ├── 04 Gameplay Decorations
    ├── 05 Player And Characters
    ├── 06 Collectibles And Props
    ├── 07 Lens - Behind Gameplay
    ├── 08 Background - Desk Decorations
    ├── 09 Desk - Fixed Background
    ├── 10 Background - Scientists And Assistants
    ├── 11 Wall - Shelves Pictures Windows And Walls
    └── 12 Outside - Behind The Window
```

## Costruire altri livelli

Duplicare questa scena con un nuovo nome. Inserire gli oggetti nel piano corrispondente; all'interno dei piani si possono creare cartelle per stanza. Conservare insieme le parti di un prefab composto, per esempio una cassa con la sua corda. I piani Terrain, Water e Props hanno un Grid per le tilemap.

Camera, HUD e manager restano fuori dai piani. Su World, **Configure All Depths** applica Z e sorting. Gli offset e le percentuali di parallasse restano modificabili sui componenti BackgroundParallaxLayer. DeskSurfaceAnchor mantiene gli appoggi sulla scrivania; l'offset automatico conserva la posizione scelta nell'editor. La lente usa LensPlayerFollower e MagnifyingLens2D, con riferimenti allo scienziato, al player e alla camera della nuova scena.

La lente usa Z 1.5 e Sorting Order 50 sul layer Default: resta dietro a player, props, acqua e terreno, davanti ai fondali Lens Background. L'ingrandimento cattura quindi lo sfondo senza includere il player.

MagnifyingLens2D genera una texture separata con una camera interna non salvata nella scena. Include solo gli elementi dietro alla lente secondo Sorting Layer, Order in Layer e gerarchia dei Sorting Group. Gli elementi davanti vengono esclusi soltanto durante questa cattura sincrona; la loro visibilita viene ripristinata in finally prima del rendering principale. Non vengono modificati layer, ordini o posizioni. La texture viene assegnata al materiale della singola lente; non usa la cattura globale CameraSortingLayerTexture.

Per cambiare quali elementi sono ingranditi, modificare Sorting Layer / Order in Layer del Sorting Group esterno che contiene la lente. Se la lente non ha un gruppo, valgono i parametri del suo SpriteRenderer. Esempio: con player a 200 e lente a 50 il topo resta davanti; portando la lente a 250 il topo entra nello sfondo ingrandito, mentre acqua a 400 e terreno a 500 rimangono davanti. Non usare la sola Z quando layer o ordini sono diversi. A parita completa di priorita all'interno dello stesso gruppo, assegnare ordini distinti per rendere esplicito il rapporto davanti/dietro.

La cattura rispetta la camera di gioco assegnata e dispone anche di un'anteprima per Scene View. Costa un render aggiuntivo per lente/camera, con filtro Point per la pixel art. Gli oggetti generati durante il gioco entrano immediatamente nella selezione. Il componente aggiorna l'effetto dopo il movimento di camera, sfondo e lente; non interviene sul loro movimento.

## Ordini predefiniti dei prefab

I prefab in `Assets/Prefabs` usano gli stessi Sorting Layer e fasce di ordine della scena, anche quando vengono istanziati senza un parent di piano:

| Categoria | Sorting Layer | Ordine base |
| --- | --- | ---: |
| Mano di ingresso | Default | 600 |
| Terreno e tubi solidi | Default | 500 |
| Acqua e terreno scavabile | Default | 400 |
| Decorazioni di gameplay | Default | 300 |
| Player e piranha | Default | 200 |
| Pulsanti, collezionabili, trappole, corde e props | Default | 100 |
| Fondale interno delle stanze | Default | 99 |
| Lente | Default | 50 |
| Scrivania dello scienziato | Lens Background | 400 |
| Busto dello scienziato | Lens Background | 300 |

I prefab con piu sprite conservano le differenze relative tra i propri ordini, quindi alcune parti hanno valori leggermente superiori alla base. Il terreno scavabile usa 410 per la visuale mascherata a runtime. Sono stati corretti anche gli override di sorting dello scienziato presenti nel prefab Universe Rig.

I Sorting Group dei piani continuano a determinare la priorita effettiva dei loro figli. I prefab misti, come stanze complete e scienziato con scrivania e lente, vanno tenuti fuori dal Sorting Group di un singolo piano oppure separati nei parent appropriati: inserirli tutti sotto un unico piano ne vincolerebbe l'intero ordine. Eventuali override gia presenti nelle scene continuano a prevalere sui valori dei prefab. I layer fisici, i collider e le impostazioni delle collisioni non sono stati modificati.

## Campagna

La scena usa `Assets/Settings/UniverseCampaign_Composed.asset`: è una copia della campagna originale, con l'universo 10 indirizzato a questa scena per conservarla anche nei tentativi successivi. È stata aggiunta alle scene di build. Il menu e la campagna originale conservano i loro riferimenti: per provarla aprire direttamente Universo_10_Composed.

Quando si duplica il livello per sostituire un universo della campagna, aggiornare esplicitamente il riferimento nella campagna scelta.

## Verifica

Durante la composizione sono stati controllati l'inventario dei componenti di gameplay, la presenza di un solo player, camera, HUD e GameManager e l'assenza di riferimenti ad altre scene. Non è stato eseguito Play Mode. In Unity verificare l'inquadratura allo spawn, il parallasse e la lente durante lo spostamento della camera.
