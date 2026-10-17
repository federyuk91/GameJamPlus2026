# Stanze prefab e assemblaggio procedurale

## Disegnare una stanza

1. Duplicare Assets/Prefabs/Rooms/Room Template.prefab e rinominare la copia.
2. Aprire la copia in Prefab Mode. Il figlio Gameplay Tilemaps contiene le mappe
   solide e decorative gia configurate: dipingere con le Tile Palette esistenti.
3. Sulla radice, RoomDefinition espone Footprint: l'ingombro rettangolare deve
   contenere la stanza completa. Si modifica dall'Inspector o con le maniglie
   ciano nella Scene view. Mantenere rotazione zero e scala 1 sulla radice.
4. I pulsanti Up, Down, Left, Right aggiungono uscite figlie e assegnano i
   riferimenti. Spostare i figli Exit esattamente al punto di aggancio del tubo.
   Le frecce verdi indicano il verso e non generano grafica o collider.
   Il pulsante Allinea uscite alla griglia applica lo snap anche nel prefab.
5. Salvare il prefab. Le uscite devono essere al massimo una per direzione.
   Per rimuoverne una, eliminare il figlio e il riferimento nell'array Ports.

I punti compatibili sono Up-Down e Left-Right. Quando si collegano, le loro
posizioni coincidono. Non si ruotano o specchiano le stanze per adattarle.
Le dimensioni dei tubi devono essere coerenti fra prefab; il verso verifica
solo la compatibilita, non la larghezza del passaggio.

Una stanza terminale deve avere esattamente un'uscita, che verra occupata dal
collegamento. Le altre pareti devono essere chiuse anche nella grafica e nei
collider: il generatore non disegna tappi automaticamente.

## Generare il layout

Trascinare Assets/Prefabs/Rooms/Room Layout Generator.prefab nella scena.
Assegnare Start Room, Rooms (stanze intermedie) e Terminal Rooms.
Room Count e il totale, compresa partenza e terminali. Seed rende riproducibile
la scelta con lo stesso catalogo e la stessa configurazione dei prefab.
Premere Genera stanze nell'Inspector: il risultato appare sotto Generated Rooms,
conservando i collegamenti ai prefab originali. Il generatore funziona solo dal
pulsante in Editor, senza generazione automatica all'avvio della partita.

La ricerca allinea punti opposti, usa ogni uscita una volta e rifiuta gli
incroci degli ingombri con area positiva. I bordi dei rettangoli possono toccarsi.
Le connessioni formano un albero: l'implementazione non crea cicli fra stanze
gia collocate. Il layout deve avere il numero esatto richiesto e nessuna uscita
libera. La ricerca prova alternative fino a Search Limit; se non trova una
soluzione lascia intatto il risultato precedente e mostra un rapporto.

Rigenerare sostituisce solo il contenitore creato da questo generatore.
Generazione e rimozione supportano Undo. Se il contenitore viene spostato fuori
dal generatore, viene lasciato intatto e occorre ripristinarlo prima di rigenerare.
Il generatore richiede rotazione mondo zero e scala mondo 1. La posizione del
risultato viene agganciata alle celle della griglia mondiale. Salvare la scena per conservare il layout.

## Esempi inclusi

Example Up Left: stanza iniziale con le due uscite dell'esempio concordato.
Example Left Right e Example Up Down: corridoi utilizzabili nel catalogo.
Terminal Up, Down, Left e Right: terminali per chiudere le rispettive direzioni.
Gli esempi sono semplici stanze 8x6 con pareti e aperture dipinte; servono come
base modificabile. Il generatore prefab e gia collegato a questi esempi,
con totale iniziale 10 e seed 12345. Il modello vuoto non e nel catalogo.

Per il primo controllo in Unity: aprire un esempio, verificare uscite e ingombro,
trascinare il generatore in una zona libera e premere Genera stanze. Controllare
le giunzioni, i collider e il passaggio del topo; cambiare seed e numero di stanze.
Mantenere player, camera e sistemi globali fuori dai prefab delle stanze.


## Griglia comune del layout generato

Generated Rooms contiene una sola Grid, persistente anche in Play Mode.
Le Grid locali restano nei prefab usati per disegnare, ma vengono rimosse solo
dalle istanze generate; le loro Tilemap utilizzano la Grid comune.
Cell Size sul generatore deve coincidere con la dimensione delle celle dei
prefab (1x1 nel progetto). Sono richieste griglie rettangolari XY, senza Cell Gap,
rotazione o scala. Le Tilemap devono avere origine su celle intere rispetto
alla propria Grid; eventuali trasformazioni fuori griglia vengono segnalate.

Ogni punto di uscita viene agganciato al bordo della cella lungo la direzione
e al centro della cella lungo l'asse trasversale. Up/Down hanno quindi X al
centro e Y sul bordo; Left/Right hanno X sul bordo e Y al centro. I Gizmos
mostrano il punto agganciato e una linea gialla quando quello disegnato differisce.
La generazione applica questi punti alle istanze senza alterare i prefab sorgenti.

Il planner allinea le origini delle Grid, compensando gli spostamenti locali
fra radice della stanza e Grid. Non basta arrotondare la posizione della radice:
una Grid spostata nel prefab deve essere compensata per mantenere le celle in fase.
Il risultato iniziale viene agganciato alla griglia mondiale anche se il
GameObject generatore si trova a coordinate frazionarie.

Dopo questa modifica premere Genera stanze per sostituire il layout precedente.
Continuare a modificare i prefab originali in Prefab Mode e rigenerare il risultato.
Lo snap allinea le celle; resta necessario disegnare tubi con larghezze e profili
compatibili e impostare ingombri che includano realmente la geometria della stanza.


## Composizione manuale con snap

LoopScene contiene Manual Room Grid, un contenitore vuoto con una Grid di
riferimento (celle 1x1) e il componente ManualGridAlignment. Il prefab omonimo
si trova in Assets/Prefabs/Rooms. Per crearne altri usare Tools > Mouse Platformer
> Stanze > Crea contenitore griglia manuale.

Trascinare sotto il contenitore le radici dei prefab stanza o direttamente
le Grid, posizionarle liberamente e selezionare il contenitore. Il pulsante
Allinea griglie figlie aggancia le loro origini alle intersezioni della Grid
di riferimento. Il contenitore definisce l'origine comune; lasciarlo a zero
per allinearsi alla griglia mondiale, oppure assegnare un'altra Reference Grid.

Per una stanza viene spostata tutta la radice RoomDefinition, compensando la
posizione della Grid interna. Sono inclusi i figli annidati e inattivi.
L'allineamento esegue solo traslazioni: celle, rotazioni e Cell Gap devono essere
compatibili con il riferimento. Se una stanza contiene griglie che richiedono
spostamenti diversi, l'operazione viene annullata prima di modificare gli oggetti.
Il pulsante e disponibile in Edit Mode e supporta Undo e gli override dei prefab.

Questo flusso mantiene le Grid locali delle stanze: allinea le celle senza
fonderle e senza scegliere automaticamente le connessioni dei tubi. Posizionare
le stanze vicine al punto desiderato prima di applicare lo snap.
