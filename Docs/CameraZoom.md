# Camera zoom

La camera di LoopScene ha Pixel Perfect Zoom, collegato a Pixel Perfect Viewport.

- Rotella verso l’alto / R1 (RB Xbox): un livello più vicino.
- Rotella verso il basso / L1 (LB Xbox): un livello più lontano.
- Tre livelli: Zoom Out, Normal, Zoom In. Parte sempre da Normal e si ferma agli estremi.
- I comandi sono sospesi durante pausa, foto e fine del test.

Il livello Normal mantiene risoluzione di riferimento e inquadratura originali (640×360, 32 PPU). Gli altri livelli usano la scala intera immediatamente inferiore o superiore alla scala normale, calcolata sulla finestra. A 1920×1080 le scale sono rispettivamente 2×, 3× e 4×. Non ci sono interpolazioni di zoom. PPU, filtro Point e grid snapping della Pixel Perfect Camera rimangono invariati.

La risoluzione originale viene conservata da Pixel Perfect Viewport per UI, bande e cornice, che non cambiano dimensione durante lo zoom. Solo la risoluzione di rendering della scena e il crop della Pixel Perfect Camera cambiano. Il ridimensionamento della finestra ricalcola i livelli; la disabilitazione del viewport ripristina i valori originali della camera. Non modificare la risoluzione della Pixel Perfect Camera da altri script mentre questo sistema è attivo.

Se la scala normale è già 1×, Zoom Out è bloccato: una scala inferiore non può conservare tutti i pixel. Sotto 640×360 resta il comportamento preesistente del livello normale, che adatta l’immagine alla finestra.

Da verificare in Unity: i tre livelli con rotella e shoulder buttons; dimensioni della UI; limiti di zoom; cambio risoluzione; pausa/foto; movimento Su/Giù della camera al 25% della nuova vista; arresto del Play Mode senza alterazioni alla camera salvata.
