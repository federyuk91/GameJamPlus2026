# Satiety

Il prefab Player contiene PlayerSatiety: inizia con Maximum Notches = 5 e perde una tacca ogni Drain Interval = 60 secondi di gioco. Il timer si ferma in pausa e quando il GameManager non è Playing. Ogni nuova LoopScene riparte a sazietà piena.

Il formaggio normale (CollectibleType.Cheese) ripristina tutte le tacche e azzera il progresso del timer. Formaggio verde e foto conservano il proprio ciclo di fine test.

L'HUD mostra le tacche sopra i contatori in basso a sinistra. Gameplay HUD → Satiety Hud regola Flash Color, Flash Duration, Shake Duration e Shake Amplitude. Il flash copre l'intero schermo senza intercettare gli input; lo shake mantiene l'allineamento della camera alla griglia dei pixel e non altera la sua posizione di inseguimento. Entrambi terminano anche quando l'ultima perdita mette il gioco in pausa per il fine test.

A zero, PlayerSatiety chiama Starvation Outcome: sul Player, Dialogue Message Player contiene i commenti sulla dieta e Test End Event contiene i risultati casuali e Subject Died = true. Viene usato il normale ciclo flash bianco → commento → risultato → prossimo universo.

Trascina Assets/Prefabs/Starvation Death Zone.prefab nelle aree desiderate. Box Collider 2D → Size/Offset definisce il volume. Starvation Death Zone → Drain Interval è inizialmente 5 secondi; Show Gizmos controlla il leggero contorno rosso, invisibile in gioco. Non occorrono riferimenti al Player, tag o Rigidbody2D: la zona viene registrata automaticamente e verificata contro il collider del topo.

Le zone accelerano il timer esistente: il progresso parziale viene conservato quando si entra/esce. In caso di sovrapposizione si applica l'intervallo più breve, senza sommare le perdite.

Da verificare in Play Mode: perdita normale, permanenza nella zona, pausa, ripristino con formaggio normale e fine test all'ultima tacca. Per una verifica rapida puoi ridurre temporaneamente Drain Interval sul Player.
