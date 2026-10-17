# Tilemap e Tile Palette

LoopScene contiene il prefab Gameplay Tilemaps, con griglia rettangolare
1x1 unita. I fogli Art/Tiles sono gia suddivisi in celle 32x32 a 32 PPU;
le importazioni usano Point e non generano mipmap.

In Window > 2D > Tile Palette si trovano tre palette:
TilemapFloor, TilemapTube e TilePipes. I 39 Tile asset sono salvati in
Assets/Tilemaps/Tiles. La disposizione nelle palette conserva quella dei fogli.
Il menu Tools > Mouse Platformer > Tilemaps offre un comando per aprire
Tile Palette e uno per creare asset eventualmente mancanti.

Scegliere la palette e la Tilemap di destinazione tramite Active Target:

- Terrain - Solid: pavimenti, pareti, piattaforme e profili del livello.
- Pipes - Solid: tubi che devono bloccare movimento e vista.
- Background - Decorative: sfondo, senza collider, ordine -20.
- Foreground - Decorative: elementi davanti al topo, senza collider, ordine 5.

Tutte le palette possono essere usate su tutte le Tilemap. I collider dei Tile
sono di tipo Sprite, quindi seguono la Physics Shape della sprite invece di
riempire automaticamente l'intera cella. Per cambiare un profilo usare Sprite
Editor > Custom Physics Shape, oppure scegliere Grid sul singolo Tile asset
se deve occupare un quadrato intero. I decori non collidono anche quando il
Tile asset contiene un profilo, perche la Tilemap non ha TilemapCollider2D.

Le due mappe solide usano Rigidbody2D Static, TilemapCollider2D e
CompositeCollider2D Polygons, con una piccola tolleranza di accorpamento.
Il layer Default e incluso nelle collisioni del player e delle casse e negli
Occluder Layers del piranha. Movimento, gradini, climb, appoggio sulle
piattaforme e occultamento visivo utilizzano cosi i collider esistenti.
Per superfici continue preferire la stessa Tilemap per accorpare le giunzioni.

La griglia in scena parte vuota: la geometria gia presente rimane il riferimento
per le prove. Le celle dipinte nell'istanza di LoopScene si salvano insieme alla
scena come override; il prefab rimane riutilizzabile per altre scene.

## Proposta futura per terra scavabile e acqua

Quando saranno disponibili i relativi tile, usare due mappe dedicate:

- Terra scavabile: le celle dipinte definiscono la terra iniziale; suddividere
  l'area in porzioni con maschera di scavo fine, shader e collider aggiornabili.
  I tunnel mantengono il raggio attuale, senza essere limitati a celle da 1 unita.
- Acqua: le celle contigue definiscono volumi trigger accorpati; un componente
  acqua per Tilemap li espone al nuoto e al galleggiamento, mantenendo coerente
  la rilevazione usata dal piranha. La parte visiva puo usare un materiale acqua.

Queste due mappe non sono state create: in questa configurazione restano
operative la terra scavabile e l'acqua gia presenti nella scena.

Verifica in Unity: dipingere una superficie nella mappa solida e camminarci,
provare il climb su un bordo raggiungibile e verificare che una parete oscuri
la vista del piranha. I tile decorativi devono lasciar passare il topo.


## Aggiornare un'immagine senza mescolare i tile

Le coordinate dei ritagli Unity partono dal basso dell'immagine. Aggiungere
una riga sotto il disegno, mantenendo i pezzi originali in alto, richiede quindi
lo spostamento verso l'alto dei loro rettangoli nel Sprite Editor. Conservare
i nomi e soprattutto Sprite ID/internalID: i Tile asset e le stanze li usano
per ritrovare i pezzi. Evitare una nuova suddivisione che elimini i vecchi sprite.
Aggiungere nuove celle a destra evita questo specifico spostamento verticale.

Riparazione TilemapTube: i 16 sprite originali ora usano y=32 nell'immagine
544x64 aggiornata; i sei ritagli della riga inferiore sono TilemapTube_New_0..5.
I relativi Tile asset si trovano nella cartella TilemapTube e sono presenti
nella riga aggiuntiva della palette. I due Tile asset creati manualmente durante
l'aggiornamento sono conservati, con gli stessi GUID.
