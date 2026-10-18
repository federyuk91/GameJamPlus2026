# Recovery Zone

Trascina Assets/Prefabs/Recovery Zone.prefab sotto o attorno alla mappa, separandola dalle normali aree giocabili. È invisibile in gioco e mostra un leggero gizmo rosso nell'Editor. Modifica Box Collider 2D → Size/Offset per definirne il volume.

Recovery Zone cerca un piano quasi orizzontale con spazio libero per il collider del topo e appoggio a sinistra, centro e destra. Le superfici vengono cercate usando il Solid Filter del SimulatedBody2D del Player. La ricerca usa raggi verticali corti anche per trovare pavimenti sotto soffitti dello stesso CompositeCollider2D, e sceglie la destinazione valida più vicina fra quelle campionate.

- Search Radius: raggio massimo, inizialmente 24 unità, misurato dal centro del collider del Player.
- Sample Spacing: passo orizzontale, inizialmente 0.25 unità. Valori minori rendono la ricerca più precisa ma più costosa. La ricerca avviene solo all'ingresso nella zona.
- Flat Surface Angle: inclinazione massima accettata, inizialmente 1 grado.
- Show Gizmos: visibilità del volume nell'Editor.

Sono esclusi punti che occupano solidi o zone attive di recupero, morte o starvation. Se non viene trovato un piano nel raggio, viene scelto lo Spawn Point attivo più vicino assegnato al GameManager con spazio libero e fuori da queste zone. Gli Spawn Point devono essere posizionati dal designer in luoghi sicuri.

Il Player conserva sazietà, raccolte, cronometro e universo. Il recupero azzera velocità, buffer del salto e scivolata, annulla scavo/morso e sgancia corde, scale, bordi e piattaforme mobili. La camera si riallinea immediatamente.

Il prefab Player contiene PlayerRecovery; il GameManager in LoopScene ha il riferimento assegnato. Altre scene devono assegnare il proprio PlayerRecovery al GameManager. Non occorrono tag o Rigidbody2D.

Se non ci sono destinazioni valide, il Player non viene spostato e viene emesso un warning, una volta per ingresso nella zona. Aumenta Search Radius o assegna uno Spawn Point valido. Il recupero è una protezione: non corregge i collider che hanno causato l'uscita dalla mappa.

Da verificare in Play Mode: caduta fuori mappa, piano sotto un soffitto, bordo stretto da rifiutare, sovrapposizione con zone letali, fallback a Spawn Point e recupero durante una presa.
