# Suggerimenti contestuali e dispositivo attivo

LoopScene collega GameplayInteractionAvailability al PlayerInputReader, CollectibleCollector, PlayerRopeBite e PlayerDigging tramite Inspector. Il componente valuta le azioni disponibili dopo il tick delle interazioni ed emette Changed solo quando cambiano. Il Gameplay HUD mostra il riquadro in basso a destra, nella stessa area del dialogo dello scienziato, senza acquisire focus o click. Il dialogo ha precedenza: i suggerimenti si nascondono durante apertura, visualizzazione e chiusura e tornano quando il dialogo e scomparso.

| Azione disponibile | Tastiera | Controller Xbox / generico |
| --- | --- | --- |
| Cassa raggiungibile, sbloccata e appoggiata a terra | Hold E + direction · push / pull | Hold X + direction · push / pull |
| Formaggio nell'area di raccolta | F · Eat cheese | X · Eat cheese |
| Formaggio verde nell'area di raccolta | F · Eat green cheese | X · Eat green cheese |
| Corda raggiungibile e tagliabile | Hold F · Cut rope | Hold B · Cut rope |
| Terreno scavabile raggiungibile | Hold F + direction · Dig | Hold B + direction · Dig |

Il tasto F raccoglie i collezionabili; E resta dedicato alle casse; X è il pulsante ovest del controller, non un nuovo binding sulla tastiera. I binding del giocatore non sono stati cambiati.

Più righe possono apparire contemporaneamente, per esempio Eat cheese e Cut rope nello stesso punto. Taglio e scavo condividono lo stesso comando: il taglio ha precedenza come nella logica di PlayerRopeBite, quindi non vengono pubblicizzati insieme sullo stesso pulsante.

Le condizioni sono quelle delle interazioni reali: il collector espone il bersaglio più vicino ancora raccoglibile; il taglio riutilizza portata della bocca, orientamento, segmenti intatti, corda impugnata e linea di vista; lo scavo riutilizza HasEarth e i vincoli delle altre interazioni. Il suggerimento Dig richiede anche una direzione, indicata nel testo. Le query di disponibilità non prendono possesso degli input e non eseguono raccolte, tagli o scavi. In pausa e durante le schermate di fine test i suggerimenti di gameplay vengono nascosti.

InputDevicePrompts ascolta gli eventi di Input System, senza scansioni di oggetti nella scena. Tastiera, click e rotella del mouse riportano ai comandi desktop; pulsanti o stick del gamepad sopra soglia passano ai comandi controller. Il semplice movimento del mouse e i controlli rumorosi vengono ignorati per evitare cambi involontari. Scollegando il controller attivo si torna alla tastiera.

I nomi dei pulsanti provengono dal layout del gamepad: su PlayStation appaiono Square / Circle / Triangle anziché X / B / Y. I controller devono essere riconosciuti da Input System come Gamepad, coerentemente con i binding di gameplay già esistenti.

I tasti sono ora rappresentati dagli sprite bianchi Kenney importati in `Assets/UI Toolkit/MainMenu/InputKenny`, collegati tramite `Assets/UI Toolkit/Gameplay/InputPromptIcons.asset`. Il catalogo è assegnato al GameplayHudController nell'Inspector. Sono configurati tastiera (E/F/Enter), Xbox (X/B/Y), PlayStation (Square/Circle/Triangle) e Nintendo Switch (Y/A/X).

I suggerimenti sono righe separate con icona e testo inglese, così possono convivere più azioni contestuali. Anche il pulsante dello scienziato e Next del risultato del test mostrano l'icona corrente. Le immagini restano bianche nei file originali: USS applica la tinta con `-unity-background-image-tint-color`, usando `--scientist-green` = rgb(92, 255, 123), la stessa variabile del testo dello scienziato. Se un riferimento allo sprite manca, resta disponibile il nome testuale del comando. La scelta fra varianti colorate e bianche è già risolta nel catalogo: sono usate le versioni bianche senza suffisso color.

Nel dialogo dello scienziato il pulsante mostra solo il comando corrente: Enter oppure il pulsante nord del controller (Y / Triangle), con etichette inglesi Reveal, Next e Close. La schermata di risultato del test usa lo stesso tasto con Next. Il cambio di dispositivo aggiorna anche una battuta già aperta, senza riavviare digitazione o timeout. I testi narrativi rimangono quelli forniti al dialogo.

## Prove manuali in Unity

Non sono stati eseguiti test automatici o Play Mode.

1. Avvicinarsi/allontanarsi da formaggio e corda: i suggerimenti devono apparire e scomparire con la reale portata dell'interazione. Una parete davanti alla corda deve impedire Cut rope.
2. Provare un formaggio accanto a una corda: entrambi i suggerimenti devono apparire, con tasti diversi.
3. Avvicinarsi al terreno scavabile: verificare Dig, direzione richiesta e priorità del taglio se una corda è raggiungibile.
4. Alternare E/F, stick e pulsanti del controller mentre un dialogo è aperto: verificare X/B/Y oppure Square/Circle/Triangle e Reveal/Next/Close.
5. Controllare che drift leggero e semplice movimento del mouse non cambino i suggerimenti. Provare scollegamento, pausa e ripresa.

Il suggerimento per le casse riusa la portata e le verifiche di PlayerCrateInteraction. Richiede che il topo sia a terra, fuori dall'acqua, senza scivolare o arrampicarsi. Le casse ancora legate hanno PushableCrate disattivato e non mostrano il suggerimento. Il riquadro segue lo stesso ancoraggio inferiore del dialogo anche cambiando risoluzione.

Le comunicazioni ora usano F al posto di Invio sulla tastiera, mantenendo il pulsante nord sul gamepad. Anche Next delle foto e dei risultati usa F. Durante una comunicazione, la pressione viene consumata dalla UI e non raccoglie contemporaneamente oggetti o avvia morso/scavo; serve rilasciare F prima di una nuova interazione.

Aggiornamento gamepad: anche le comunicazioni, le foto e i risultati usano ora il pulsante ovest, equivalente al comando dei collezionabili: X su Xbox, Square su PlayStation e Y su Switch. Le icone sono aggiornate. La pressione della UI viene consumata anche sul gamepad, impedendo la raccolta contemporanea.

## Comunicazioni SYSTEM e selezione condivisa

I suggerimenti contestuali ora si leggono nel terminale del testo sotto la dicitura SYSTEM, con le icone dei tasti esistenti. Restano attivi finché l'azione è disponibile, senza timeout e senza comando Close; si aggiornano al cambio dispositivo. L'avatar è in un terminale separato e resta vuoto durante i suggerimenti del sistema. Una comunicazione dello scienziato ha precedenza e mantiene array, macchina da scrivere, timeout e Reveal/Next/Close. I suggerimenti tornano alla fine se l'interagibile è ancora vicino.

`GameplayInteractionAvailability.Current` espone le azioni selezionate per i tasti effettivi, non tutte le azioni concorrenti. La selezione viene applicata anche a PlayerInputReader e ai componenti che eseguono le azioni:

- Tastiera: F raccoglie il bersaglio selezionato; quando c'è un raccoglibile, il taglio/scavo sullo stesso F è escluso. E per spostare una cassa resta indipendente.
- Controller: il pulsante ovest raccoglie o preme un pulsante. Il pulsante est, tenuto insieme alla direzione, spinge/tira le casse. Una cassa raggiungibile ha precedenza su taglio/scavo, che condividono il pulsante est. La presa della corda resta sul pulsante ovest.
- Taglio ha precedenza sullo scavo sul loro tasto condiviso.
- Fra più raccoglibili viene scelto quello più vicino. Alla pressione viene validato e raccolto lo stesso `InteractionTarget` selezionato per il suggerimento, senza cercare un nuovo oggetto nel mezzo della pressione. Se non è più raggiungibile non viene sostituito con un altro a sorpresa.

Il cambio dispositivo rilevato dal gestore dei prompt viene comunicato alla selezione. Senza GameplayInteractionAvailability attivo, PlayerInputReader conserva l'abilitazione di tutte le azioni per compatibilità con altre scene.

Nessun test automatico o Play Mode eseguito. Provare manualmente sovrapposizioni formaggio/foto, raccoglibile/corda con F, raccoglibile/cassa col controller, tasti diversi simultanei, cambio dispositivo e ritorno dei suggerimenti dopo lo scienziato.

### Precedenza senza buffer

Le comunicazioni prenotano il terminale appena richieste, prima dell'avvio della coroutine e dell'animazione. Fino alla fine della chiusura CRT, gli aggiornamenti di disponibilità scartano e svuotano i suggerimenti SYSTEM senza costruire righe nascoste né accodare tooltip. Alla conclusione vengono ricalcolate sovrapposizioni e azioni correnti, senza raccogliere automaticamente oggetti durante la lettura: appare soltanto un suggerimento ancora valido in quel momento. Un controllo aggiuntivo tutela il caso in cui il ricalcolo invochi callback che avviano una nuova comunicazione.

Da verificare manualmente: comunicazione e prossimità nello stesso frame; uscire dall'area durante il dialogo; restare vicino e attendere la fine; arrivare a un secondo interagibile prima della fine. Nessun test o Play Mode eseguito.
