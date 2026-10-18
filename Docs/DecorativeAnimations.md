# Decorazioni animate

I prefab sono in Assets/Prefabs/Decorations: Algae 01, Algae 02, Algae 03 e Water FX. Trascinali nella scena o in una stanza prefab. Non hanno collider né componenti di gameplay.

Le animazioni e gli Animator Controller sono in Assets/Animations/Decorations. Ogni controller avvia automaticamente l’unico stato Loop. Tutti i clip hanno Loop Time attivo e 8 FPS. Algae 01 e Algae 03 hanno 8 frame (1 secondo), Algae 02 ne ha 7 (0.875 secondi), Water FX ne ha 14 (1.75 secondi). Lo stato Loop espone Speed per cambiare la velocità.

I quattro sprite sheet originali restano in Art/Pixel Art. Lo slicing automatico dei singoli filamenti/bolle è stato sostituito da frame uniformi 32×32, letti da sinistra a destra. GUID delle texture e identificatori dei frame riutilizzati sono conservati. Pivot alla base centrale per le alghe, centrale per Water FX; importazione Point a 32 PPU.

Il renderer usa Order in Layer 0, modificabile sulle istanze per sistemare sovrapposizioni con acqua e terreno. Le animazioni seguono il tempo di gioco e si fermano quando il gioco viene messo in pausa. Nessuna istanza è stata aggiunta automaticamente a LoopScene.

Da controllare in Unity: animazioni in loop, posizione delle basi delle alghe e ordine di rendering nella scena scelta.

## Water FX del piranha

Il piranha in LoopScene ha Periodic Sprite Effect collegato al prefab Water FX. Interval (10 secondi) è il tempo tra gli inizi dei cicli, con prima apparizione dopo 10 secondi. Position Offset (0, 0.35, 0) permette di regolare dove compare rispetto al pesce.

Ogni apparizione riusa una sola istanza dell’effetto, la posiziona sul piranha e riparte dal primo frame. L’effetto rimane fermo nello spazio mentre il pesce continua a muoversi. Al termine di un ciclo Animator viene spento e riportato sulla posizione corrente del piranha; alla successiva apparizione la posizione viene aggiornata nuovamente. La durata viene letta dal progresso dell’Animator, non fissata nel codice. Il prefab decorativo originale mantiene il loop e resta riutilizzabile autonomamente.

Pausa e fine del test fermano il timer; disabilitare/distruggere il piranha spegne/rimuove il suo effetto. Il componente non modifica il comportamento del pesce.
