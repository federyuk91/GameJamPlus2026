# Rope interaction

Player Rope Bite e Player Rope Interaction selezionano automaticamente le corde più vicine tramite il registro delle RopeSimulation abilitate. Non occorre assegnare una lista Ropes sul Player. Le precedenti liste non limitano più le interazioni.

- Presa: il segmento collegato all'ancoraggio più vicino alla mano, entro Physics Settings → Interaction Areas → Rope Grab Radius. La corda deve essere Available.
- Morso: il segmento integro tagliabile più vicino alla bocca, entro Player Rope Bite → Reach, davanti al centro del collider del topo o allineato ad esso (tolleranza 0.05 unità), senza ostacoli. Il filtro direzionale usa il centro del collider: la bocca può aver già oltrepassato la corda. Rope Simulation → Can Be Cut deve essere attivo.
- Da appeso: il morso dà priorità alla corda impugnata, senza applicare Reach o la direzione della bocca; resta il controllo degli ostacoli.

Su una corda sospesa dall'alto, quando taglia da appeso il topo si riposiziona sull'estremità della parte ancora collegata all'ancoraggio; la parte sottostante cade. Il morso da appeso non taglia il segmento 0: vicino all'ancoraggio taglia il successivo, mantenendo almeno un pezzo su cui restare. Se rimane solo quel pezzo, non è più mordibile da appeso; da terra resta possibile tagliarlo.

Se al primo taglio la cassa è in acqua e il suo punto di attacco è sopra l'ancoraggio, viene conservato il lato cassa. La cassa perde il vincolo al fondo (Attached = false), riabilita PushableCrate, ma mantiene il collegamento al tratto superiore (HasRopeConnection = true). Il topo si riaggancia al primo punto integro verso la cassa e risale con essa; può continuare a scalare o saltare. Il tratto inferiore resta sul fondo. La scelta del lato viene mantenuta anche quando la cassa raggiunge la superficie. I morsi successivi preservano almeno un segmento sul lato cassa.

In entrambi i casi il riposizionamento rispetta i collider solidi: se il nuovo appiglio non è raggiungibile senza attraversarli, il topo si stacca.

Le corde vengono registrate in OnEnable e rimosse in OnDisable, includendo oggetti istanziati, riattivati o rimossi durante il gioco. Le ricerche di presa e morso non usano FindObjects a ogni frame. Il progresso del morso riparte quando cambia la corda bersaglio, come prima.

Da verificare in Play Mode: più corde vicine, corda non tagliabile, muro fra bocca e corda, corda creata dopo l'avvio, disattivazione della corda durante il morso.
