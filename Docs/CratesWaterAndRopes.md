# Casse, acqua e corde

## Esempi nella LoopScene

Il gruppo `Crate Water Rope Examples` contiene tre casi, utilizzando il volume `Water` già presente nella scena:

| Oggetto | Posizione iniziale | Comportamento |
| --- | --- | --- |
| `01 Free Floating Crate` | `(15.5, 5.235)` | Parte immersa e risale fino a un equilibrio sulla superficie. |
| `02 Horizontal Swinging Crate` | `(-3.78, -0.871)` | Sospesa circa tre unità a sinistra della posizione iniziale del Player. Oscilla mantenendo il collider e la grafica orizzontali. |
| `03 Seabed Tethered Crate` | `(17.5, -11.695)` | Legata al fondo del bacino esistente. La spinta idrostatica tende la corda verso l'alto. Tagliandola, la cassa risale. |

Le corde dei due esempi sono tagliabili. I riferimenti del Player includono anche la corda già presente nella scena.

## Componenti delle casse

- `CrateMotion`: integra velocità, gravità, collisioni e vincoli. Rimane attivo anche quando la cassa è legata. Tutte le casse della LoopScene, comprese le due preesistenti, hanno questo componente.
- `CrateBuoyancy`: spinta a immersione completa e resistenza nell'acqua, configurabili sulla singola cassa. La percentuale immersa deriva dai bounds del collider e dai volumi d'acqua attivi. La spinta completa deve superare la gravità per permettere il galleggiamento; con i valori iniziali di 100 e 66 l'equilibrio è circa al 66% di immersione.
- `PushableCrate`: abilita la presa con E / pulsante ovest e contiene `Weight`, divisore della velocità di spinta e tiro. Non governa più la caduta della cassa.
- `CrateRopeAttachment`: reference alla corda e punto locale di attacco. Il vincolo limita la distanza dall'ancora senza applicare rotazioni alla cassa; una corda può quindi allentarsi. La cassa sospesa usa l'attacco superiore, quella ancorata al fondo quello inferiore.

`CrateRopeAttachment` disabilita `PushableCrate` quando si collega alla corda e lo riattiva al taglio. La velocità della cassa rimane conservata al distacco. Il peso usato per spinta e tiro è indipendente dai parametri di galleggiamento.

Per altre casse già create in scene o prefab esterni alla LoopScene, aggiungere `CrateMotion` e assegnare i settings della fisica; per farle galleggiare aggiungere anche `CrateBuoyancy`. Le nuove casse create aggiungendo `PushableCrate` ricevono automaticamente il componente di movimento.

## Contatto con il topo

In acqua il movimento del topo sposta le casse al contatto senza richiedere E, comprese quelle ancora legate; il vincolo della corda continua a limitarle. Il peso limita anche questa velocità di spostamento. La caduta lenta del nuotatore appoggiato sopra una cassa non viene convertita in una spinta verso il basso che annulli il galleggiamento.

`PlayerPlatformRide` trasporta il topo appoggiato sulla cassa mentre questa si sposta e trasferisce la velocità della piattaforma al salto. Una cassa che risale da sotto può sollevare il topo mentre è in acqua. Il movimento viene limitato dagli ostacoli sopra il topo per evitare di trascinarlo attraverso un soffitto; non è introdotta una meccanica di danno da schiacciamento.

## Morso

Tenere premuto **F** oppure il **pulsante est del gamepad** per mordere una corda davanti alla bocca. `PlayerRopeBite` espone:

- `Hold Duration`: durata iniziale 0.6 secondi.
- `Reach`: portata del morso.
- `Mouth Offset`: posizione locale della bocca rispetto alla parte visiva.
- `Ropes`: corde raggiungibili dal sistema, assegnate da Inspector.

Rilasciare il pulsante o perdere la corda interrompe il morso. Un taglio richiede una nuova pressione per essere ripetuto. Gli ostacoli solidi bloccano il morso. L'azione non include ancora una clip grafica dedicata al morso.

Su `RopeSimulation`, disattivare **Can Be Cut** per un cavo d'acciaio. Il flag è indipendente dalla possibilità di arrampicarsi: il topo può usare anche una corda non tagliabile.

Il taglio elimina un collegamento della catena, libera la cassa e mantiene i segmenti restanti visibili. La parte superiore resta ancorata, quella separata cade; il topo perde la presa se il suo punto di aggancio non è più collegato all'ancora. Le parti della corda restano simulate come punti, senza collider solidi sui segmenti. Le corde ancorate sul fondo invertono correttamente la coordinata di arrampicata per mantenere il comando verticale nello spazio.

## Verifica in Unity

Provare equilibrio sulla superficie, spinta per contatto prima e dopo il taglio, risalita con il topo sopra, salto dalla cassa sospesa, vincoli vicino a pareti/soffitti e morso interrotto. Verificare anche `Can Be Cut` disattivato. Il modello di immersione usa i bounds dei volumi con superficie d'acqua orizzontale; volumi ruotati o forme d'acqua irregolari richiedono un campionamento diverso.

## Lunghezza delle corde in Editor

Nel componente `Rope Simulation`, `Length` indica il numero di pezzi (minimo 1).
Cambiarlo aggiunge o rimuove automaticamente i pezzi; `Segment Length (units)` stabilisce la lunghezza di ciascuno.
Nel prefab `Rope` ogni pezzo misura 1 unità, quindi `Length = 5` corrisponde a 5 unità.
I prefab delle casse mantengono i segmenti da 0.25 unità e le rispettive lunghezze iniziali.

L'ancoraggio resta fisso. I pezzi vengono allineati senza spazi, nella direzione iniziale della corda (verso l'alto per quella sul fondo).
Il pulsante `Ricostruisci e allinea pezzi` ripristina l'allineamento dopo modifiche manuali.
La geometria si modifica fuori dal Play Mode; la simulazione usa poi il nuovo numero di segmenti anche per arrampicata, morso e vincolo delle casse.
Le corde non usano un Box Collider unico: le interazioni seguono i segmenti simulati.
