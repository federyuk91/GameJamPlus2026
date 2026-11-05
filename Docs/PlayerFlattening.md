# Mouse Flattening

Sul prefab Player, PlayerFlattening gestisce la postura usando il BoxCollider2D già assegnato a SimulatedBody2D. Una nuova pressione di Giù, da terra e fuori dall'acqua, dimezza l'altezza del collider senza modificarne la larghezza o la quota inferiore. La posizione originale viene salvata al momento dell'ingresso. Non si avvia mentre il topo è su corde, scale o bordi, nella Snake Box, bloccato dai controlli o già intento a scavare.

Lo stato resta attivo dopo il rilascio di Giù. Su tenta di ripristinare il collider intero; sotto un soffitto il tentativo viene rifiutato e può essere ripetuto o mantenuto finché lo spazio diventa libero. Il controllo usa i solidi del player, ignorando i collider del topo stesso.

La velocità controllata è dimezzata anche camminando fuori da un bordo e scavando. Non si può saltare, scattare, spingere/tirare o agganciare automaticamente corde, scale e bordi. Il morso delle corde e le altre normali interazioni non vengono spostati di tasto. La massa non cambia.

Le clip in Assets/Animations/Mouse/Flattening sono ricavate dalle sprite già importate, senza modificarne metadati o pivot. L'Animator mantiene gli indici degli stati precedenti e aggiunge Flattening, FlattenedIdle, FlattenedWalk e StandingUp. La posa ferma usa l'ultimo frame della transizione; StandingUp riproduce gli stessi frame nell'ordine inverso. I tempi di transizione della postura derivano dalla clip assegnata al componente.

In acqua non si può attivare la postura. Se il topo vi entra già appiattito, tenta di rialzarsi prima di cedere il collider al nuoto; se un ostacolo impedisce l'espansione, mantiene provvisoriamente la forma ridotta finché può ripristinarla in sicurezza. Il teletrasporto azzera la postura e i relativi timer.

Da verificare in Unity: Giù/Su, passaggi bassi con rialzata bloccata, velocità rispetto alla camminata normale, salto e sprint bloccati, morso, spinta impedita, salita/discesa dei gradini, scale/corde vicine, ingresso in acqua e allineamento dei piedi nelle nuove animazioni.
