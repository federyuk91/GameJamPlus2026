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
Il generatore richiede rotazione mondo zero e scala mondo 1; la posizione puo
essere scelta liberamente. Salvare la scena per conservare il layout.

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
