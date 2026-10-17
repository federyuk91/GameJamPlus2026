# EndGame

La scena `Assets/Scenes/EndGame.unity` contiene `EndGame UI`, con UIDocument ed EndGameController già collegati. Il PanelSettings e lo stile sono separati dal main menu e dal gameplay, con base 640 × 360 e le due fasce nere da 20 pixel.

## Report finale

Il monitor è centrato, largo e alto due terzi del frame: circa 427 × 240 unità alla risoluzione base. Usa lo stile nero/verde delle comunicazioni e l'accensione CRT da punto a linea fino allo schermo completo. I dati sono organizzati come nel riferimento:

- Test completati / test previsti.
- Cavie impiegate.
- Cavie decedute.
- Cavie recuperate.
- Esito percentuale dell'esperimento.

L'esito usa i test completati, non il tasso di sopravvivenza: dieci test completati risultano 100% successo anche con dieci morti. Questa scelta mantiene la rivelazione del concetto di gioco.

`Continue` si può cliccare o confermare con Invio / pulsante sud del controller (A / Croce), usando l'EventSystem già presente nella scena. Il pulsante riceve il focus dopo l'accensione. La conferma spegne il report con il collasso CRT e mostra l'immagine del finale; richieste ripetute durante la transizione sono ignorate.

## Tre finali

`EndGameOutcomeSelector.Select(int deceasedSubjects)` seleziona:

| Deceduti | Finale | Campo Inspector |
| --- | --- | --- |
| 0 | NoDeaths | No Deaths Ending Image |
| 1–9 | PartialLoss | Partial Loss Ending Image |
| 10 o più | AllSubjectsLost | All Subjects Lost Ending Image |

La soglia di dieci segue il numero dei soggetti previsto dal concept. `Starting Universe` diverso da dieci resta utile per prove rapide: il report mostra il numero effettivo dei test, ma la classificazione del finale continua a usare le soglie 0 / 1–9 / 10.

I tre PNG provvisori sono in `Assets/Art/UI/EndGame/Endings`:

- `Ending_NoDeaths_Placeholder.png`
- `Ending_PartialLoss_Placeholder.png`
- `Ending_AllSubjectsLost_Placeholder.png`

Sono copie ridimensionate dell'immagine placeholder già fornita, non illustrazioni finali. Quando saranno disponibili le immagini definitive, assegnarle ai tre campi del controller oppure sostituire i PNG mantenendo i file .meta. Il finale mostra anche un titolo distinto e il riepilogo dei soggetti. `Menu principale` permette di tornare al menu senza avviare automaticamente una nuova partita.

## Dati della partita

GameSession ora conserva TotalTests, CompletedTests, SubjectsUsed, DeceasedSubjects e RecoveredSubjects durante i caricamenti. Il GameManager registra una sola volta l'esito quando accetta EndTest, prima delle schermate intermedie; il suo controllo di stato esistente evita il doppio conteggio. Nuovo Gioco azzera la sessione.

`TestEndEvent` espone `Subject Died`, attivo per default per mantenere il significato delle cause di morte già esistenti, incluso il formaggio verde. Per un evento che conclude la prova con il soggetto vivo, disattivare questa opzione nell'Inspector. Ogni prova conclusa registra un soggetto impiegato; un soggetto non deceduto è conteggiato come recuperato. Non si deducono morti dal testo casuale del risultato.

Aprendo direttamente EndGame senza risultati registrati, il report usa dieci test e il valore `Preview Deceased Subjects` (0–10) dell'Inspector, inizialmente impostato a 5. Dopo Continue viene quindi mostrato il caso 1–9, collegato a `Assets/Art/EndGame1.png`. Una partita con dati reali ha precedenza su questa anteprima. Il selettore non richiede immagini o servizi esterni.

## Verifica manuale

Non sono stati eseguiti test automatici o Play Mode.

1. Aprire EndGame direttamente, provando Preview Deceased Subjects 0, 5 e 10: confermare Continue e controllare il finale selezionato.
2. Provare a 640 × 360 e su monitor più grandi: report centrale, dati leggibili, immagine correttamente proporzionata e fasce nere.
3. Controllare click, Invio e A / Croce, apertura/spegnimento CRT, assenza di doppi avanzamenti e ritorno al menu.
4. Iniziare una nuova partita: completare test con Subject Died attivo/disattivo, poi controllare il report reale. Riavviare Nuovo Gioco per verificare l'azzeramento dei conteggi.

Non è stata aggiunta persistenza su disco dei risultati: i conteggi valgono per la sessione corrente, come il numero dell'universo già esistente.
