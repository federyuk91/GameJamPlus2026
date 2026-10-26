# Green Conical Flask Lens

Prefab: `Assets/Prefabs/Decorations/Green Conical Flask Lens.prefab`.
Materiale dedicato: `Assets/Materials/GreenConicalFlaskLens.mat`.

Usa la sprite e la tinta della Green Conical Flask del laboratorio, con il componente MagnifyingLens2D e lo shader condiviso della lente. Il liquido verde agisce da apertura ottica; bordi, collo azzurro e riflessi conservano il disegno originale. La maschera e calibrata sul verde scuro della sprite, con Green Dominance 0.08.

## Uso

Trascinare il prefab in una scena Universe. Per ingrandire anche il topo, inserirlo nel piano **01 Foreground - Desk Decorations**; il prefab ha Default / 600 anche quando non e raggruppato. In un altro piano segue la priorita effettiva del relativo Sorting Group: ingrandisce soltanto gli elementi dietro.

DeskSurfaceAnchor e gia presente con Auto Offset From Editor Position attivo. Assegnare Desk Surface oppure premere Configure All Depths sul ParallaxConfigurator per collegarlo alla scrivania.

View Camera puo restare vuota: il componente memorizza la MainCamera che renderizza la stessa scena tramite il callback della pipeline, senza cercarla nella gerarchia. Un riferimento esplicito mantiene la precedenza. Prima della prima cattura viene mostrata la sprite originale.

## Regolazioni

- **Magnification**, sul componente: ingrandimento iniziale 2.
- **Lens Center UV**, sul componente: centro iniziale `(0.5, 0.225)`, nell'area del liquido.
- **Lens Radius UV**, sul componente: semidimensioni iniziali `(0.48, 0.215)` per calibrare l'arco sulla sprite.
- **Lens Distortion**, sul materiale: slider da -1 a +1. Negativo incurva l'immagine verso il basso, positivo verso l'alto. Zero conserva lo zoom senza deformazione. Il profilo segue un arco circolare orizzontale ed e orientato secondo l'asse verticale della sprite.

La distorsione e un parametro del materiale e non viene sovrascritta dal componente. Il materiale della fiala e separato da quello della lente dello scienziato, quindi le regolazioni sono indipendenti. Per varianti con deformazioni differenti, duplicare il materiale e assegnarlo sia al renderer sia a Magnifying Material.

La cattura mantiene il filtro Point per la pixel art. Il costo resta un render aggiuntivo per lente/camera attiva. La fiala e una decorazione: non aggiunge fisica o collisioni di gameplay.
