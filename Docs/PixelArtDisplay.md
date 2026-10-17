# Visualizzazione pixel art — 640 × 360

## Gioco

La Main Camera di LoopScene usa il Pixel Perfect Camera di URP:

- Assets PPU: 32, coerente con sprite e tile.
- Reference Resolution: 640 × 360, rapporto 16:9.
- Grid Snapping: Upscale Render Texture, per renderizzare il mondo alla risoluzione base.
- Filter Mode: Point, senza interpolazione fra pixel.
- Crop Frame: Windowbox, per ingrandire a multipli interi senza deformare o tagliare la scena.

La camera mantiene una vista completa di 20 × 11,25 unità, con una zona centrale visibile di 20 × 10 unità dopo le bande. Il controller della camera conserva lo smoothing interno e arrotonda solo la posizione finale alla griglia dei pixel.

| Schermo / Game View | Area visibile di gioco | Bande nere |
| --- | --- | --- |
| 640 × 360 | 640 × 320, scala 1× | 20 px sopra e sotto |
| 1280 × 720 | 1280 × 640, scala 2× | 40 px sopra e sotto |
| 1920 × 1080 | 1920 × 960, scala 3× | 60 px sopra e sotto |
| 1366 × 768 | 1280 × 640, scala 2× | 64 px sopra/sotto e 43 px ai lati |
| 2560 × 1440 | 2560 × 1280, scala 4× | 80 px sopra e sotto |
| 1024 × 768 | 640 × 320, scala 1× | 224 px sopra/sotto e 192 px ai lati |

La base complessiva è 640 × 360: 20 righe nere sopra, 320 righe visibili e 20 righe nere sotto. Le 40 righe riservate partecipano al calcolo della scala intera. Gli schermi con dimensioni o rapporto diversi possono aggiungere altre bande esterne. Le bande del gameplay sono una maschera nera disegnata da PixelPerfectViewport dopo il rendering della camera, senza ricevere input.

Le bande sono intenzionali: occupare tutto uno schermo di dimensioni non multiple richiederebbe pixel di dimensione non uniforme oppure interpolazione. L'inquadratura resta la stessa a qualsiasi rapporto dello schermo.

`PixelPerfectViewport` è collegato al Pixel Perfect Camera tramite Inspector. Quando una dimensione dello schermo è inferiore alla base, passa a Stretch Fill mantenendo il rapporto 16:9, per mostrare tutta la scena anziché tagliarla. Sotto 640 × 360 si perde inevitabilmente dettaglio: non è possibile visualizzare tutti i pixel originali in meno pixel fisici. Quando lo schermo torna abbastanza grande, ripristina Windowbox. Il controllo delle dimensioni è necessario perché Unity non espone un evento generale di ridimensionamento della finestra.

## Rendering e importazione

URP mantiene Render Scale 1, MSAA disattivato e upscaling Nearest-Neighbor. Le due camere non usano HDR, MSAA, post-processing o risoluzione dinamica. HDR e Adaptive Performance sono disabilitati nell'asset URP. Le texture delle luci 2D sono renderizzate a scala 1. Tutti i livelli di qualità hanno anti-aliasing e riduzione globale delle texture disattivati.

Le texture in `Assets/Art/Pixel Art` e `Assets/Art/Tiles` usano Point, 32 PPU, niente mipmap, niente ridimensionamento NPOT e niente compressione con perdita. La dimensione massima di importazione deve essere sufficiente per lo spritesheet originale. Il postprocessor applica queste regole alle nuove importazioni e conserva modalità e slicing manuale degli sprite esistenti. Gli override di piattaforma attivi sono resi non compressi.

La dimensione iniziale della finestra desktop resta 640 × 360; la finestra è ridimensionabile e il fullscreen mantiene la risoluzione dello schermo. Non chiamare `Screen.SetResolution(640, 320)` per imporre la risoluzione del mondo: il render texture del Pixel Perfect Camera svolge già questa funzione.

## Menu

UI Toolkit resta un overlay renderizzato alla risoluzione dello schermo. Non attraversa il render texture a bassa risoluzione del gameplay, quindi i caratteri possono restare nitidi anche su monitor grandi.

PanelSettings usa Scale With Screen Size, Reference Resolution 640 × 360 e Screen Match Mode Expand. Il layout ha una larghezza di riferimento di 640 unità, centrata sullo schermo. Un contenitore nero riserva 20 unità sopra e sotto anche nel menu, lasciando 320 unità utili alla risoluzione base. Expand garantisce uno spazio logico almeno pari alla base anche con rapporti differenti; la UI può scalare in modo frazionario per privilegiare la leggibilità dei caratteri, indipendentemente dalla scala intera del gioco.

I pulsanti principali sono alti 32 unità, con testo da 15; i testi secondari sono da 12, il titolo da 24. La scritta del laboratorio è da 11. Dialoghi, slider audio e toggle sono compatti. Gli stili inline di grandi dimensioni sono stati rimossi dall'UXML, per evitare che prevalgano sul layout compatto in USS. Titolo e testi personalizzati del menu restano invariati.

## Prove manuali in Unity

`PixelPerfectViewport` usa ExecuteAlways: le bande sono disegnate nella Game View anche fuori dal Play Mode. La Scene View mostra il mondo di lavoro e non questa maschera della camera. Il ridimensionamento della Game View aggiorna le bande anche durante i repaint in Edit Mode.

Non sono stati eseguiti test automatici o Play Mode per questa modifica.

1. Impostare una Game View a risoluzione fissa 640 × 360, scala di anteprima 1×; controllare menu, Opzioni, Crediti e conferma di uscita.
2. Provare 1280 × 720, 1920 × 1080, 1366 × 768 e 1024 × 768. Verificare inquadratura costante, bande, bordi dei pixel uniformi e testo leggibile.
3. Muovere il topo e la camera: controllare nitidezza di sprite, tilemap e oggetti in movimento.
4. Navigare Opzioni con controller, regolare i tre volumi e tornare al menu.
5. Ridimensionare una build in finestra, passare al fullscreen e provare una finestra sotto la base per controllare il fallback.

Il ridimensionamento dell'anteprima Game View può introdurre sfocatura propria dell'Editor: valutare i pixel a scala 1× o a multipli interi dell'anteprima, e nella build. Per eventuali futuri effetti HDR/bloom servirà rivedere consapevolmente le impostazioni HDR.

Riferimenti: codice del Pixel Perfect Camera incluso in URP 17.3, `PixelPerfectCamera.cs` e `PixelPerfectCameraInternal.cs`; [riferimento Unity per PanelSettings e le modalità di scala](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/Core/GameObjects/PanelSettings.cs).
