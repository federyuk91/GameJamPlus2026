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
