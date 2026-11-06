# Viste delle comunicazioni dello scienziato

Le comunicazioni durante il gioco e quelle a fine livello hanno due viste indipendenti, entrambe istanziate da GameplayHud.uxml:

| Vista | File da aprire nel UI Builder | Stile dedicato |
| --- | --- | --- |
| Durante il gioco | Assets/UI Toolkit/Gameplay/ScientistGameplay.uxml | ScientistGameplay.uss |
| Fine livello, prima dei risultati | Assets/UI Toolkit/Gameplay/ScientistLevelEnding.uxml | ScientistLevelEnding.uss |

Si possono modificare separatamente posizione e dimensioni del monitor, avatar, area del testo e pulsante. Gli stili hanno scope distinti: modificare la vista di fine livello non modifica quella di gioco. Nel layout di fine livello il pulsante è già collocato nel footer; non viene spostato dal codice.

La HUD ospita i due template, ma crea due istanze separate di ScientistCommentView. La logica condivisa gestisce scrittura progressiva, ritratto parlante, effetto CRT e comando di avanzamento. Voce, input, scorrimento e pausa seguono la vista attiva. Le normali comunicazioni vengono chiuse quando inizia la conclusione del livello; la chiusura nasconde anche il footer dedicato.

Il vecchio ScientistEndingLayout, che spostava e ridimensionava il pannello di gameplay modificandone gli stili inline, è stato rimosso. Dimensioni e posizione della presentazione finale sono ora nei suoi file UXML/USS. L'animazione CRT continua a dimensionare temporaneamente scientist-display dentro il viewport definito dalla vista.

Mantenere i nomi degli elementi collegati a ScientistCommentView: scientist-screen, scientist-display-viewport, scientist-display, scientist-avatar, scientist-content, scientist-transcript, scientist-text, scientist-advance, scientist-signal e scientist-power-glow. Gli stessi nomi nelle due viste sono intenzionali: ogni istanza cerca soltanto nel proprio template. Mantenere anche i nomi degli host in GameplayHud.uxml e le classi radice che delimitano gli stili.

Questa separazione riguarda i commenti di fine livello; il quaderno dei risultati e la scena finale della campagna conservano le loro viste esistenti. Testi e ritratti provengono ancora dai dialoghi e dalle definizioni di conclusione, senza duplicare i contenuti.

Verifica in Unity: messaggi durante il gioco, conclusione con commento, avanzamento e scorrimento con tastiera/controller/mouse, pausa del messaggio di gameplay, ritratto parlante e passaggio dal commento finale al quaderno dei risultati.
