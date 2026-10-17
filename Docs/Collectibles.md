# Raccoglibili e messaggi

In LoopScene, Cheese si mangia premendo F / pulsante ovest mentre il suo trigger interseca il collider del player. Non servono tag, layer specifici o Rigidbody2D.

## Componenti

- `CollectibleCollector`, sul player, controlla le sovrapposizioni del collider assegnato a `Collection Area`. Con `Requires Interaction` attivo, serve una nuova pressione di F / pulsante ovest; tenere premuto entrando nel trigger non consuma l'oggetto. Una pressione consuma un solo oggetto, quello piu vicino al centro del collider del player. Disattivando il flag si ripristina la raccolta al contatto.
- `Collectible`, sull'oggetto da raccogliere, espone `Type`, `Requires Interaction`, `Pickup Area` e l'evento `On Collected`. Il collider trigger e il componente devono stare sullo stesso GameObject. La raccolta invoca l'evento una sola volta, poi disattiva l'intero GameObject. Riattivarlo non azzera lo stato di raccolta.
- `DialogueMessagePlayer` invia un messaggio alla UI ed è indipendente dalla raccolta. `Play()` sceglie casualmente una stringa non vuota da `Messages`, usando `Avatar` e `Duration`. Assegnare il riferimento `Hud` dalla scena; la durata segue il limite di 1–30 secondi della UI.

## Configurare altri oggetti

1. Aggiungere un Collider2D trigger e `Collectible`, quindi assegnare il collider a `Pickup Area`.
2. Aggiungere `DialogueMessagePlayer` se l'oggetto deve mostrare un messaggio. Assegnare Gameplay HUD, avatar opzionale e lista di frasi.
3. Nell'evento `On Collected`, trascinare l'oggetto con `DialogueMessagePlayer` e selezionare `DialogueMessagePlayer → Play()`.

Cheese ha già questo collegamento e tre frasi di esempio sostituibili. Non è stato assegnato un avatar. Gli altri listener dell'evento possono gestire effetti aggiuntivi. Il conteggio avviene separatamente dal dialogo, tramite il collector del player.

La visualizzazione viene eseguita dal Gameplay HUD, quindi continua dopo la disattivazione del raccoglibile. Come per gli altri messaggi della UI, una nuova comunicazione sostituisce quella in corso.

## Verifica manuale

In Play Mode raggiungere Cheese e premere F / pulsante ovest: deve scomparire e mostrare una sola frase dalla lista. Provare altri testi, un avatar e un secondo listener nell'evento. Duplicare Cheese per creare altri raccoglibili; il riferimento al Gameplay HUD va assegnato sulle istanze della scena.

## Tipi e contatori

`Type` usa l'enum `CollectibleType`: `Cheese` per il formaggio normale e `GreenCheese` per quello verde. Ogni raccolta aggiunge un elemento del tipo indicato al `CollectibleCollector` del player; `GetCount(type)` legge il totale e `CountChanged` notifica gli aggiornamenti. Non servono tag Unity.

Il `GameplayHudController` riceve il riferimento al collector da Inspector e mostra `Cheese` nel primo contatore, `GreenCheese` nel secondo. I due totali partono da zero a ogni nuova istanza del player e vengono riallineati quando l'HUD viene riattivato. In LoopScene il secondo contatore usa l'icona verde gia disponibile; il terzo contatore conserva la configurazione precedente.

Per un nuovo formaggio verde, impostare `Type = Green Cheese` e scegliere lo sprite appropriato. Il dialogo resta configurabile tramite `On Collected`, indipendentemente dal tipo.

## Formaggio verde e fine test

Il prefab `CheeseGreen` ha `Requires Interaction` attivo. Il suo evento `On Collected` chiama `TestEndEvent.EndTest()`, con le frasi dello scienziato configurate nel `DialogueMessagePlayer` dello stesso prefab e una lista separata `Result Messages` per la schermata finale. L'invio del commento e gestito dal Game Manager; non aggiungere anche `DialogueMessagePlayer.Play()` allo stesso evento di morte.

I prefab con `Hud` non assegnato usano l'HUD del Game Manager della scena per i messaggi ordinari. Un riferimento `Hud` esplicito ha la precedenza.

## Foto collezionabili

`Photo Collectible.prefab` usa l'icona PNG fornita, copiata senza modifiche in `Assets/Art/Pixel Art/Props/PhotoCollectible.png` (Point, 32 PPU). Una sua istanza e gia presente vicino al player in LoopScene.

Il tipo `Photo` incrementa il quarto contatore, all'estrema destra del gruppo, che parte da zero e usa la nuova icona. La raccolta richiede F / pulsante ovest e mostra il suggerimento Collect photo.

Nel componente `CollectiblePhoto`, assegnare la texture della fotografia al campo `Photo`. Il campo accetta Texture2D, anche senza importarla come Sprite. Il riferimento `Hud` puo rimanere vuoto nei prefab: viene usato quello del Game Manager. Senza una foto assegnata si apre il pannello segnaposto.

L'evento `Collectible.On Collected` chiama `CollectiblePhoto.Play()`. La UI mostra la foto quasi a schermo intero, mantenendo le proporzioni, e mette in pausa gioco e cronometro. `Pause While Photo Open`, sul Gameplay HUD, regola questa pausa.

Invio, Y/Triangolo o il pulsante Next della foto attivano l'evento `CollectiblePhoto.On Continue`. Nel prefab e gia collegato a `TestEndEvent.EndTest()`: dopo la foto parte il flash bianco, seguito da una frase casuale dello scienziato e dalla schermata Test completato. L'avanzamento successivo diminuisce l'universo e ricarica LoopScene, oppure porta a EndGame a zero.

Per ogni foto si possono modificare:

- `CollectiblePhoto.Photo`: immagine da mostrare.
- `DialogueMessagePlayer.Messages` e `Avatar`: frasi di circostanza dello scienziato e suo ritratto. La lista include Che curioso! Sara meglio fare una pausa ora.
- `TestEndEvent.Result Messages`: frasi per il risultato del test.

Il GameObject raccolto puo disattivarsi: la visualizzazione resta sull'HUD e il suo evento viene eseguito alla conferma della foto. La chiusura tecnica dell'HUD o il cambio di scena non invocano On Continue.

La finestra delle foto conserva il rapporto larghezza/altezza di ogni texture: verticale, quadrato o panoramico. L'immagine viene centrata e ingrandita fino al massimo spazio disponibile senza ritagli o deformazioni. Il calcolo si aggiorna anche cambiando risoluzione; il pulsante Next mantiene il proprio spazio.
