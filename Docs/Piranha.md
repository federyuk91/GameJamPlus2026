# Piranha: ronda e inseguimento

Il Piranha gia presente in LoopScene usa due componenti indipendenti:

- PiranhaPatrol muove il pesce lungo un circuito chiuso di waypoint, a velocita
  configurabile. Gestisce anche il verso della sprite.
- PiranhaChase controlla il player assegnato e sostituisce temporaneamente la
  destinazione di ronda con la posizione del collider del topo.

Selezionare Piranha nella Scene view e abilitare Gizmos:

- I punti ciano etichettati Ronda si trascinano con le maniglie. Il circuito
  mostra i collegamenti; i punti si aggiungono o rimuovono nell'array Waypoints.
- La maniglia Distanza visiva regola la portata. Apertura cono regola l'angolo.
  Entrambi i valori sono anche nell'Inspector di PiranhaChase.
- Il cono giallo diventa rosso durante l'inseguimento.

I waypoint sono offset in unita mondo rispetto alla posizione iniziale del
piranha, indipendenti dalla scala della sprite. Durante il gioco il percorso
rimane fermo anche quando il pesce si allontana per inseguire il topo.

Il player e visibile quando il centro del suo collider e in acqua, nel cono,
entro la portata e senza collider solidi sulla linea di vista. Il controllo
usa PlayerSwimming.CheckWater, coerente con il sistema di nuoto. Water trigger,
collider del piranha e collider del player non oscurano la vista. Default e
Diggable sono inclusi negli Occluder Layers; aggiungere eventuali altri layer
solidi usati nella scena.

Quando una condizione manca, il piranha torna al punto piu vicino del circuito
e riprende dal waypoint successivo. Le velocita iniziali sono 1.5 unita/s in
ronda e 3 in inseguimento; portata 5 unita e cono 100 gradi. I riferimenti al
player, alla sprite e alla ronda sono assegnati in LoopScene.

Gli spostamenti seguono tratti rettilinei: il percorso e il rientro devono
avere spazio libero. Questa implementazione non calcola deviazioni intorno
agli ostacoli. Disabilitando PiranhaChase rimane attiva la sola ronda.

Verifica in Play Mode: entrare nel cono sott'acqua, uscire dal cono o dall'acqua,
e frapporre una cassa o terreno solido; controllare inseguimento e rientro.
