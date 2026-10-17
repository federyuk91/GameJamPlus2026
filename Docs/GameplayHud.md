# HUD di gameplay

La scena chiamata LoopGame nella richiesta corrisponde a `Assets/Scenes/LoopScene.unity` nel progetto. Il nuovo GameObject `Gameplay HUD` contiene UIDocument e GameplayHudController, con riferimenti già assegnati al viewport della Main Camera, al documento UXML e allo sprite Cheese_0.

## Layout

Il documento è `Assets/UI Toolkit/Gameplay/GameplayHud.uxml`, lo stile è `GameplayHud.uss`. Il relativo `GameplayHudPanelSettings.asset` è separato dal pannello del main menu. Lo stile è un overlay strumentale, trasparente sul mondo, con riquadri scuri squadrati e accenti azzurri.

- Le due fasce nere restano alte 20 pixel logici, nel frame complessivo 640 × 360.
- Il cronometro è in alto al centro, dentro l'area utile di 640 × 320. Parte da 00:00:00, usa hh:mm:ss e aggiorna il testo solo al cambio del secondo. Il tempo usa Time.deltaTime: si ferma se Time.timeScale è zero.
- In basso a sinistra ci sono tre contatori. I primi due seguono le raccolte del player: formaggio normale e formaggio verde, inizialmente 0×. Il secondo usa lo sprite verde. Il terzo conserva il valore iniziale configurato ed e disponibile per dati di gameplay futuri.
- In basso a destra appare lo schermo dello scienziato. All'avvio mostra: "Benvenuto nell'universo-10, rilassati e goditi il soggiorno".

Il controller clona il PanelSettings a runtime e ne adatta la scala al PixelPerfectViewport: HUD e camera hanno lo stesso ingrandimento intero e lo stesso frame centrato, anche su risoluzioni non multiple. Il pannello originale non viene modificato. Solo il pulsante del dialogo e l'area di scorrimento del testo acquisiscono input del puntatore; il resto dell'HUD non acquisisce input o focus. Non serve un EventSystem aggiuntivo.

## Uso da altri componenti

Assegnare GameplayHudController tramite Inspector, senza ricerche nella scena.

```csharp
[SerializeField] private GameplayHudController gameplayHud;
[SerializeField] private Sprite scientistPortrait;

private void OnGameplayEvent()
{
    gameplayHud.SetCounter(0, 4);
    gameplayHud.ShowScientistComment("Ottimo lavoro. Continua pure.", scientistPortrait);
}
```

Il namespace è `GameJamPlus.UI`. Gli indici dei contatori sono 0, 1, 2, 3; i valori negativi diventano zero. `Counter Icons` permette di sostituire le singole immagini; gli slot vuoti usano `Default Counter Icon`, già collegato al formaggio.

`ShowScientistComment(string text, Sprite portrait, float duration = 30f)` mostra una nuova battuta sostituendo quella eventualmente in corso. L'overload `ShowScientistComment(string[] texts, Sprite portrait, float duration = 30f)` mostra le frasi in ordine, con lo stesso ritratto, e chiude dopo l'ultima. L'array viene copiato e le stringhe vuote vengono ignorate. Una nuova chiamata sostituisce l'intera sequenza corrente.

```csharp
gameplayHud.ShowScientistComment(new string[]
{
    "Benvenuto nell'universo-10, rilassati e goditi il soggiorno",
    "Esplora la stanza con calma."
}, scientistPortrait);
```

Il pannello conserva l'apertura da linea sottile e la chiusura inversa. Il testo appare a macchina da scrivere, a 30 caratteri al secondo, in verde da terminale. L'avatar ha una cornice verde con indicatore VIDEO, visibile anche se il ritratto non è ancora assegnato. I testi lunghi vanno a capo e scorrono per seguire l'ultima riga digitata. Nel layout a 60 pixel, avatar, testo e colonna comandi sono affiancati; le intestazioni decorative sono nascoste per preservare spazio al testo. Si può rileggere con rotella sul testo, PagSu/PagGiù o stick destro. Lo scorrimento manuale sospende il seguito automatico fino alla frase successiva e non azzera il timeout di 30 secondi.

Le altezze sono ridotte di un terzo, arrotondate al pixel logico: i contatori passano da 34 a 23 pixel, lo schermo dello scienziato è ora alto 60 pixel, inclusi i bordi. Icone, cornice, avatar e spaziature sono adattati; la larghezza dello schermo resta 300 pixel. Accensione e spegnimento simulano un CRT: punto luminoso centrale, linea orizzontale, espansione verticale e breve bagliore che lascia apparire il contenuto; in chiusura lo schermo collassa verso il centro fino al punto, che si spegne. Il bagliore è limitato al pannello. Il timeout per frase parte dopo l'accensione.

Il pulsante visibile, il tasto Invio e Y/Triangolo del controller hanno lo stesso comportamento: durante la digitazione completano subito la frase; a frase completa passano alla successiva oppure chiudono sull'ultima. Il pulsante cambia etichetta fra Reveal, Next e Close e mostra solo il tasto del dispositivo attualmente in uso (Enter oppure Y/Triangle, secondo il layout del controller). Il footer mostra posizione nella sequenza e secondi rimasti. Y/Triangolo è distinto dai comandi correnti del topo per salto, presa e morso.

Ogni frase ha un timeout di 30 secondi dall'inizio della sua visualizzazione, inclusa la digitazione. Alla scadenza si passa automaticamente alla frase successiva oppure si chiude sull'ultima, anche se il testo è ancora in digitazione. Completare manualmente la digitazione non azzera il timeout. Il parametro duration può accorciarlo, ma è limitato a 1–30 secondi. Apertura e chiusura richiedono in più le loro brevi animazioni. Animazioni, digitazione e timeout usano tempo non scalato e funzionano anche in pausa.

`HideScientistComment()` interrompe e nasconde subito la comunicazione. Non viene riprodotta una voce audio: testo e sprite sono i dati previsti per ora.

Nell'Inspector si possono configurare `Welcome Text`, `Welcome Portrait`, `Welcome Duration` e `Show Welcome On Start`. Il benvenuto nella scena è impostato a 30 secondi. Non è ancora disponibile un ritratto dello scienziato: la cornice rimane pronta per lo sprite futuro.

`ResetTimer()` riporta il cronometro a zero; `SetTimerRunning(bool)` lo mette in pausa o lo fa ripartire. `ElapsedSeconds` restituisce il tempo accumulato. Il caricamento di una nuova istanza della scena riparte da zero.

## Prove manuali in Unity

Non sono stati eseguiti test automatici o Play Mode.

1. Aprire LoopScene in Play a 640 × 360: cronometro centrale, tre formaggi in basso a sinistra, bande nere e benvenuto animato in basso a destra.
2. Provare 1280 × 720, 1920 × 1080 e 1366 × 768: HUD e bande devono seguire il frame del gameplay senza sovrapporre i contatori allo schermo dello scienziato.
3. Assegnare un ritratto a Welcome Portrait e controllarne proporzioni e apertura dello schermo.
4. Inviare un array di frasi; provare click, Invio e Y/Triangolo durante e dopo la digitazione. Controllare avanzamento in sequenza e chiusura sull'ultima frase.
5. Non premere nulla per 30 secondi per frase: verificare passaggio automatico o chiusura. Completare la digitazione poco prima della scadenza non deve riavviare il conto. Provare anche in pausa.
6. Controllare incremento dei contatori tramite SetCounter e pausa/reset del cronometro.

L'anteprima UI Builder mostra i contatori e il cronometro a zero; lo schermo dello scienziato è nascosto finché non arriva una battuta in Play Mode.

## Collegamento dei raccoglibili

`Collectible Collector` nell'Inspector del controller HUD e assegnato al componente del player in LoopScene. Il controller ascolta `CountChanged` e mostra i totali di `Cheese` nello slot 0 e `GreenCheese` nello slot 1. Questi due slot seguono i dati del collector; eventuali chiamate manuali a `SetCounter` vengono riallineate alla successiva raccolta o riattivazione dell'HUD. Vedere `Docs/Collectibles.md` per il tipo e l'evento configurabile sui raccoglibili.


Il quarto contatore usa il formaggio come immagine di default ed è aggiornabile con SetCounter(3, valore). Le dimensioni attuali di ogni contatore sono 60 × 21 pixel logici, con icona da 14 e testo da 13. I collegamenti dei primi due slot al collector rimangono invariati.

Comunicazioni e collezionabili sono a 10 pixel logici dal bordo inferiore dello schermo; il cronometro è a 10 pixel logici dal bordo superiore, sempre centrato. Le posizioni tengono conto sia delle due fasce nere da 20 pixel sia delle eventuali bande aggiuntive sulle risoluzioni non multiple. Gli elementi possono sovrapporsi alle fasce; i margini scalano insieme al gioco. L'altezza della comunicazione resta 60 pixel.

I suggerimenti di gameplay sono contestuali, in inglese e possono mostrare più azioni contemporaneamente. La tastiera usa E per Eat cheese e F tenuto premuto per Cut rope / Dig; il controller usa i pulsanti ovest ed est con i nomi del suo layout. Dettagli e condizioni di disponibilità in Docs/InteractionHints.md.

La disposizione attuale della comunicazione è [Messaggio | Pulsante | Sprite]: avatar all'estrema destra, cornice 44 × 52 pixel logici e immagine 40 × 48. Il pannello rimane alto 60 pixel. La colonna comandi è larga 70 pixel e centrata verticalmente per lasciare spazio al messaggio scorrevole.
