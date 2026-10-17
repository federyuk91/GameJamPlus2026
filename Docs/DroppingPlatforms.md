# Dropping Platform

Le quattro DroppingPlatform di LoopScene hanno un BoxCollider2D solido e il componente DroppingPlatform, con riferimento al SimulatedBody2D del player gia assegnato.

- Fall Delay: secondi prima del cedimento, inizialmente 0.4.
- Disappear Delay: secondi dalla caduta alla scomparsa, inizialmente 2.
- Fall Acceleration e Max Fall Speed: accelerazione e velocita massima della caduta.

Il conto alla rovescia parte solo quando il topo e appoggiato sopra e il Ground Collider e la piattaforma. Il contatto laterale o dal basso non lo attiva. Una volta iniziato, il countdown continua anche se il topo si allontana. Al cedimento il collider viene disabilitato, la piattaforma cade liberamente verso il basso e infine il GameObject viene disattivato. Le piattaforme tornano caricando nuovamente la scena; non ricompaiono automaticamente durante il test.

Per duplicarle, copiare una piattaforma configurata. Per crearne una nuova, aggiungere DroppingPlatform (richiede BoxCollider2D), assegnare Shape e Player e scegliere Fall Delay. La scala attuale delle quattro piattaforme e conservata. Caduta e timer rispettano la pausa del Game Manager e delle foto.

Verificare in Unity il primo appoggio, l'allontanamento durante il countdown e il salto verso la piattaforma successiva. Regolare Fall Delay nell'Inspector di ciascuna piattaforma.
