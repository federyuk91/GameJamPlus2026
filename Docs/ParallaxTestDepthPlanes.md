# Parallax Test: piani di profondità

La scena `Assets/Scenes/Parallax Test.unity` contiene **Parallax Test - Depth Planes (Front To Back)** con ParallaxConfigurator. La camera guarda da Z negativa verso Z positiva.

| Piano | Z | Contenuto | Order in Layer |
| --- | ---: | --- | ---: |
| 01 | -4 | Decorazioni foreground sulla scrivania | 600 |
| 02 | -3 | Tilemap Terrain e tubi solidi | 500 |
| 03 | -2.5 | Lente mobile, subito dietro Terrain | 450 |
| 04 | -2 | Acqua e terreno scavabile | 400 |
| 05 | -1 | Decorazioni di gameplay, ad esempio alghe | 300 |
| 06 | 0 | Player e personaggi di gameplay | 200 |
| 07 | 1 | Collezionabili, interagibili, trappole, casse e props | 100 |
| 08 | 2 | Decorazioni di background sulla scrivania | 500 |
| 09 | 3 | Scrivania fissa | 400 |
| 10 | 4 | Scienziati e assistenti di background | 300 |
| 11 | 5 | Pareti, lavagne, scaffali e finestre | 200 |
| 12 | 6 | Vista oltre la finestra | 100 |

I primi sette piani usano Default; gli altri Lens Background. I Sorting Group mantengono coerente l'ordine dei renderer figli. Il fondale opaco interno delle stanze rimane nel piano dei props, dietro al player.

## Configurazione

1. Assegnare il parent di ogni piano e il renderer **Desk Surface** al configuratore. I riferimenti sono già assegnati in Parallax Test.
2. Attivare **Rest On Desk** sui piani i cui oggetti devono poggiare sulla scrivania. È già attivo per foreground, decorazioni di background e scienziati di background.
3. Premere **Configure All Depths**. Il pulsante applica Z, Sorting Layer e ordine, supportando Undo. I figli vengono portati a Z locale zero; scala, X e ordine relativo sono preservati. Y viene allineata al tavolo soltanto sui piani con Rest On Desk.
4. Per cambiare il parallasse, modificare BackgroundParallaxLayer sui normali contenitori: Follow Factor X regola quanto seguono la camera; Y resta quasi fermo sullo schermo quando vale 0.98. La scrivania è fissa nel mondo.

I parent dei piani devono essere distinti, non contenersi fra loro e non contenere la camera. La scrivania deve stare fuori dai parent che supporta.

## Appoggio generico

**DeskSurfaceAnchor** mantiene il bordo inferiore delle sprite appoggiato al bordo superiore di Desk Surface, anche durante il parallasse. **Height Offset** permette di sollevare un oggetto rispetto al tavolo.

Per un oggetto composto da più sprite, aggiungere DeskSurfaceAnchor sul suo parent e assegnare le sprite in **Visuals**: verrà spostato come un unico oggetto. Il configuratore rispetta i vincoli esistenti e attraversa i contenitori senza sprite per individuare le singole decorazioni. Lo stesso componente è usato per libri, fiale, foreground, scienziato e assistente; non ci sono eccezioni dedicate allo scienziato nel configuratore.

## Lente e scienziato

Lo scienziato conserva soltanto i comportamenti generici BackgroundParallaxLayer e DeskSurfaceAnchor. Non usa ScientistDeskAnchor o ScientistParallax. Movimento e ingrandimento della lente sono separati dal busto.

La lente è separata dal parent dello scienziato e rimane nel piano 03, tra Terrain e Water: Z -2.5, Default, ordine 450. Non ha DeskSurfaceAnchor né un proprio parallasse. LensPlayerFollower muove soltanto X/Y del centro del vetro; non cambia Z, Sorting Layer, Sorting Group o configurazione dello scienziato.

**LensPlayerFollower** usa riferimenti espliciti a Player, busto e MagnifyingLens2D. La posizione desiderata è una frazione dello spostamento del player rispetto al centro dell’area del braccio, espressa nelle coordinate locali del busto. Lo smoothing avviene nello stesso spazio locale: l’area segue quindi immediatamente il parallasse e l’allineamento allo spawn dello scienziato. La lente rimane dentro i limiti anche mentre il busto si muove.

Nell’Inspector della lente:

- **Minimum/Maximum Local Position** delimitano la portata del braccio: i due handle sono trascinabili nella Scene view.
- **Elliptical Reach** limita il movimento a un’ellisse all’interno dei limiti; disattivarlo per un’area rettangolare.
- **Follow Scale** regola quanto il movimento del giocatore influenza ciascun asse. Zero blocca l’asse rispetto al busto; uno segue completamente il target entro i limiti.
- **Smooth Time** regola la gradualità del gesto.
- **Center Lens In Bounds** centra il vetro nell’area per l’anteprima in editor.

**MagnifyingLens2D** gestisce l’ingrandimento tramite una cattura separata, senza modificare layer o ordini degli oggetti. **Magnification** è impostato a 2. Il componente riconosce automaticamente la gerarchia dei Sorting Group: modificare Sorting Layer / Order in Layer del gruppo esterno della lente cambia quali oggetti sono dietro e vengono ingranditi. Gli oggetti davanti sono esclusi dalla cattura e mantengono l'aspetto normale. Il riferimento manuale Lens Sorting Group non è più necessario. La selezione è dinamica, anche per oggetti generati durante il gioco. Per la nuova scena composta, vedere `Docs/Universo10Composed.md`: il suo piano lente è dietro agli elementi di gameplay.

Le altre scene e il prefab originale non vengono modificati. Nessun Play Mode eseguito.

In Parallax Test, BackgroundParallaxLayer del parent dello scienziato ha Optional Spawn Alignment configurato: Spawn Manager è il GameManager della scena, Horizontal Alignment Point è il busto e Spawn Offset X è 0. All’inizio il centro del busto viene allineato alla X dello spawn effettivamente scelto, prima di memorizzare l’origine del parallasse. Y e Z non cambiano: l’appoggio resta gestito da DeskSurfaceAnchor e la profondità dal piano. La stessa opzione è utilizzabile per qualsiasi decorazione, senza una gestione dedicata allo scienziato.

**Auto Offset From Editor Position** di DeskSurfaceAnchor è attivo per impostazione predefinita, anche sugli anchor presenti in Parallax Test. In editor Height Offset viene calcolato dalla distanza fra il fondo delle sprite e il piano della scrivania, anche quando la distanza è negativa. Spostare o scalare l’oggetto aggiorna il valore; Configure All Depths conserva quella disposizione. Durante il gioco l’offset rimane fisso mentre l’anchor compensa il movimento del parallasse. Disattivare il flag per impostare Height Offset manualmente.

Nel piano 10 scienziato e assistente usano entrambi Follow Factor X = 0.75: sullo schermo scorrono al 25% dello spostamento della camera. I libri del piano 08 usano 0.48 (52% sullo schermo), scaffali lontani 0.78 (22%) e parete 0.94 (6%). Il fattore indica la quota di movimento seguita dalla camera, quindi valori maggiori rendono il fondale più lento sullo schermo. L’asse Y dei personaggi appoggiati al tavolo rimane vincolato da DeskSurfaceAnchor.
