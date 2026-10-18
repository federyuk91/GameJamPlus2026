# Mouse Trap

Trascina Assets/Prefabs/Mouse Trap.prefab nella scena. Il Box Collider 2D è un trigger sulla base della trappola; Size e Offset regolano l'area letale. Non occorrono Rigidbody2D o tag. Il Player Collider viene risolto dal GameManager quando non assegnato direttamente.

Al contatto viene riservato il fine test e il gameplay va in pausa. MouseTrap riproduce i quattro frame di MouseTrap_Sheet in tempo reale, quindi mantiene l'ultimo frame per Closed Hold Duration e segnala il completamento al GameManager. Solo allora parte il flash bianco, seguito dal commento, dal risultato e dal normale avanzamento dell'universo. Non viene usato un ritardo stimato indipendente dall'animazione.

- Frame Duration: inizialmente 0.08 secondi per frame.
- Closed Hold Duration: inizialmente 0.25 secondi sulla trappola chiusa.
- Show Gizmos: mostra il contorno rosso dell'area letale nell'Editor.
- Dialogue Message Player → Messages: contiene «Quella non sarebbe dovuta stare lì! Pulire stanza sette.».
- Test End Event → Result Messages: contiene il risultato della prova; Subject Died è attivo.

Durante DeathAnimation il GameManager impedisce ad altri eventi letali di sostituire quello già avviato. Se la trappola viene disattivata durante l'animazione, sblocca comunque il passaggio al flash. Recovery Zone esclude la trappola dalle destinazioni di recupero.

Da verificare in Play Mode: contatto a terra, completamento dei quattro frame prima del flash, sovrapposizione con altri pericoli e riavvio del livello.
