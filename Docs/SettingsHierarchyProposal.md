# Proposta di gerarchia: settings e fisica custom

Documento di valutazione. Descrive una possibile organizzazione futura, non quella già implementata. Nessuna modifica a codice, asset, prefab o scene è prevista da questo documento.

## Criterio di divisione

- **SettingsScriptableObject:** contiene soltanto le regole condivise del mondo e della simulazione.
- **SimulatedBody2D:** contiene le caratteristiche e lo stato del singolo corpo.
- **PlayerMovementSettings:** classe serializzabile contenuta nel componente PlayerMovement del singolo animale. Non viene ospitata nello ScriptableObject globale.
- **Behaviour aggiuntivi:** contengono le caratteristiche della capacità che aggiungono a quell'oggetto.

Le due categorie di settings sono quindi **PhysicsSettings** e **PlayerMovementSettings**. Le sezioni interne elencate sotto servono a organizzare l'Inspector: non rappresentano ulteriori classi.

I nomi sono proposti con la grafia `PhysicsSettings` e `GravityAcceleration`.

## 1. Settings globali

```text
SettingsScriptableObject : ScriptableObject
└─ Physics : PhysicsSettings [Serializable]
   ├─ GravityAcceleration
   │  └─ Intensità positiva della gravità di riferimento del mondo.
   │     La direzione verticale verso il basso viene applicata dal motore.
   ├─ WaterGravityModifier
   │  └─ Moltiplicatore ambientale della gravità quando il corpo è in acqua.
   │     1 = nessuna modifica ambientale; i corpi conservano i propri modificatori.
   ├─ FullSubmersionWeightMultiplier
   │  └─ Regola di gameplay attuale: a immersione completa il carico
   │     gravitazionale dei props vale il 50%. Non modifica la massa inerziale.
   ├─ SimulationSubsteps
   │  └─ Numero di sottopassi della simulazione dei corpi.
   ├─ RopeSimulationSubsteps
   │  └─ Numero di sottopassi della simulazione delle corde.
   └─ RopeConstraintIterations
      └─ Numero di iterazioni con cui risolvere le distanze tra i segmenti.
```

Questi sono parametri condivisi: cambiarli può influenzare tutti gli oggetti. I valori di precisione e la regola del 50% oggi sono presenti nella logica del progetto, non tutti nell'attuale ScriptableObject. Il loro inserimento qui è parte della proposta.

Non vengono aggiunti a questo asset velocità del topo, altezza di salto, forza del palloncino, massa delle casse o caratteristiche delle singole turbine.

## 2. Corpo fisico di ogni oggetto

```text
GameObject / prefab
└─ SimulatedBody2D : MonoBehaviour
   ├─ Settings
   │  └─ Riferimento al SettingsScriptableObject condiviso.
   │
   ├─ Proprietà del corpo [sezione Inspector]
   │  ├─ Weight
   │  │  └─ Massa a secco in kg. Unica fonte del peso dell'oggetto.
   │  ├─ Shape
   │  │  └─ Collider usato per le verifiche fisiche.
   │  ├─ SolidLayers
   │  └─ Skin
   │     └─ Margine geometrico delle collisioni di questo corpo.
   │
   ├─ Risposta fisica [sezione Inspector]
   │  ├─ GravityScale
   │  │  └─ Moltiplicatore individuale della gravità del mondo.
   │  ├─ WaterGravityScale
   │  │  └─ Modificatore individuale aggiuntivo quando immerso.
   │  ├─ AirDrag
   │  ├─ WaterDrag
   │  ├─ MaximumHorizontalSpeed
   │  ├─ MaximumVerticalSpeed
   │  └─ MaximumWaterVerticalSpeed
   │     └─ Limiti fisici del singolo corpo, distinti dalla velocità
   │        che un personaggio può ottenere con i comandi.
   │
   └─ Stato calcolato [sola lettura]
      ├─ Velocity
      ├─ FrameDisplacement
      ├─ Grounded / HitWall / HitCeiling
      ├─ GroundCollider / GroundNormal / GroundPoint
      ├─ SubmergedFraction
      ├─ SupportedLoad
      └─ EffectiveLoad
```

I dati di stato sono risultati della simulazione, non impostazioni da salvare nel prefab. La loro collocazione nel body consentirebbe ai behaviour di condividere le stesse informazioni.

`AirDrag`, `WaterDrag` e i limiti fisici avrebbero qui una sola configurazione per corpo. I behaviour applicherebbero la resistenza e il limite appropriati al contesto, evitando valori equivalenti ripetuti in Motion, Buoyancy e BalloonLift.

Le interrogazioni sull'ambiente e sui supporti andrebbero aggiornate durante i sottopassi e dopo i movimenti rilevanti: non basterebbe memorizzarle una volta per frame.

## 3. Player e altri animali

```text
Prefab dell'animale
├─ SimulatedBody2D
│  └─ Peso e risposta fisica specifici di quell'animale.
│
└─ PlayerMovement : MonoBehaviour
   └─ MovementSettings : PlayerMovementSettings [Serializable]
      │
      ├─ Movimento [sezione Inspector]
      │  ├─ MoveSpeed
      │  ├─ GroundAcceleration
      │  └─ AirAcceleration
      │
      ├─ Salto [sezione Inspector]
      │  ├─ JumpHeight
      │  ├─ JumpReleaseMultiplier
      │  ├─ FallGravityMultiplier
      │  │  └─ Modifica della caduta per il comportamento di questo personaggio.
      │  ├─ CoyoteTime
      │  ├─ JumpBuffer
      │  ├─ LaunchControlTime
      │  └─ LaunchControlMultiplier
      │
      ├─ Terreno, pendenze e bordi [sezione Inspector]
      │  ├─ MaximumStepHeight
      │  ├─ MaximumSlopeAngle
      │  ├─ SlideOnSteepSlopes
      │  ├─ SlopeSlideAngle
      │  ├─ SlopeAccelerationMultiplier
      │  ├─ MaximumSlopeSlideSpeed
      │  ├─ LedgeClimbEnabled
      │  ├─ LedgeMaximumHeight
      │  ├─ LedgeForwardReach
      │  ├─ LedgeHangHeightFraction
      │  ├─ LedgeGrabVerticalReach
      │  ├─ LedgeClimbSpeed
      │  └─ LedgeRetryDelay
      │
      ├─ Nuoto [sezione Inspector]
      │  ├─ SwimSpeed
      │  ├─ SwimAcceleration
      │  ├─ SwimBraking
      │  ├─ StrokeSpeed
      │  ├─ StrokeAcceleration
      │  ├─ StrokeControlTime
      │  └─ BottomWalkSpeedMultiplier
      │
      ├─ Spinta e trazione [sezione Inspector]
      │  ├─ PushMoveSpeed
      │  ├─ PushGrabReach
      │  └─ MaximumPushWeight
      │     └─ Forza dell'animale: non è una proprietà dell'oggetto spinto.
      │
      ├─ Corde e scale [sezione Inspector]
      │  ├─ RopeClimbSpeed
      │  ├─ RopeSwingAcceleration
      │  ├─ RopeGrabRadius
      │  ├─ RopeRegrabDelay
      │  ├─ RopeJumpUpSpeed
      │  ├─ RopeJumpHorizontalBoost
      │  ├─ LadderClimbSpeed
      │  ├─ LadderGrabReach
      │  ├─ LadderRegrabDelay
      │  ├─ LadderJumpHorizontalSpeed
      │  └─ LadderJumpUpSpeed
      │
      ├─ Scavo [sezione Inspector]
      │  ├─ DiggingEnabled
      │  ├─ DiggingSpeed
      │  ├─ DiggingRadius
      │  ├─ DiggingReach
      │  ├─ ExcavatedAlpha
      │  └─ ExcavatedTint
      │
      └─ Passi [sezione Inspector]
         ├─ RunFootstepsPerSecond
         └─ WalkFootstepsPerSecond
```

I componenti di interazione del personaggio leggerebbero questa stessa istanza locale. Un secondo animale avrebbe un altro prefab, un altro body e un'altra istanza di PlayerMovementSettings. Nessun cambiamento al topo modificherebbe l'altro animale.

I suoni, gli sprite e gli altri riferimenti specifici del personaggio continuerebbero a restare sui suoi componenti audio e visivi. Non vengono trasferiti nel SettingsScriptableObject.

## 4. Behaviour specifici degli oggetti

Questi restano componenti opzionali sul GameObject. I campi sotto appartengono all'istanza o al prefab, non all'asset globale.

```text
GameObject / prefab
├─ SimulatedBody2D
│
├─ PropsMotion : MonoBehaviour [opzionale]
│  ├─ Body
│  ├─ UseGravity
│  └─ InitialVelocity
│     └─ Integra il moto dei props usando forze, vincoli e dati del body.
│
├─ Pushable : MonoBehaviour [opzionale]
│  ├─ Body
│  └─ ShowInteractionGizmos
│     └─ Rende il corpo spingibile; massa e carico arrivano dal body.
│        Distanza, velocità e forza di chi spinge arrivano dal personaggio.
│
├─ PropsBuoyancy : MonoBehaviour [opzionale]
│  ├─ Body
│  ├─ LiftStrength
│  └─ VerticalDampingRatio
│     └─ Spinta e stabilità specifiche di questo oggetto galleggiante.
│        La resistenza dell'acqua arriva dal body.
│
├─ BalloonLift : MonoBehaviour [opzionale]
│  ├─ Body
│  ├─ LiftCapacity
│  ├─ HoverWeightTolerance
│  ├─ LiftResponse
│  │  └─ Rapidità con cui raggiunge il moto previsto dal carico residuo.
│  ├─ SwingInfluence
│  └─ Rope
│     └─ Il bilancio deriva da capacità e carico effettivo.
│        Massa, resistenza e limiti di velocità arrivano dal body.
│
└─ RopeAttachment : MonoBehaviour [opzionale]
   ├─ Body
   ├─ Rope
   └─ AttachmentOffset
      └─ Vincola questo corpo alla corda e gestisce il rilascio al taglio.
         Non rende automaticamente l'oggetto spingibile.
```

`LiftResponse` è una proposta per distinguere la prontezza del palloncino dalla sua capacità di sollevamento. Non rappresenterebbe una seconda capacità indipendente in grado di contraddire il valore in kg.

La suddivisione tra massa, spinta e resistenza del fluido va mantenuta esplicita: la regola di gameplay del peso ridotto in acqua e la forza di galleggiamento sono due contributi diversi già presenti nel gioco.

## 5. Corde e ambiente

```text
Oggetto corda
└─ RopeSimulation
   ├─ Settings → SettingsScriptableObject
   ├─ GravityScale
   ├─ PieceCount / SegmentLength
   ├─ Damping
   ├─ SwingLimits
   ├─ CanBeCut
   ├─ AnchorBody [opzionale]
   └─ AttachedBody [opzionale]
      └─ RopeChain conserva la simulazione interna dei segmenti.

Oggetto acqua
└─ WaterVolume
   └─ VolumeCollider

Oggetto turbina
└─ TurbineWind
   ├─ WindArea / Outlet
   ├─ Running
   ├─ WindStrength / FarEndStrength
   ├─ BlockedByWalls / BlockingLayers
   ├─ FanAnimator
   └─ Impostazioni dei gizmo
```

Acqua, vento e corde non richiedono automaticamente un SimulatedBody2D: un volume o un ancoraggio statico può esistere senza essere un corpo mobile. I collegamenti ai corpi sono necessari quando devono trasferire carichi o movimento.

## 6. Formule e dati derivati

Le formule non sono ulteriori campi da configurare. Una possibile classe statica `PhysicsMath` raccoglierebbe i calcoli condivisi.

```text
PhysicsMath : static class
├─ CalculateGravity
│  ├─ In aria: GravityAcceleration × Body.GravityScale
│  └─ In acqua: gravità in aria × WaterGravityModifier × Body.WaterGravityScale
├─ CalculateSlopeAcceleration
│  └─ Gravità applicata × sin(angolo della pendenza)
│     con direzione lungo la discesa.
├─ CalculateEffectiveLoad
│  └─ Riduzione progressiva del carico gravitazionale con l'immersione.
├─ CalculateBalloonBalance
│  └─ Capacità − carico totale, considerando la fascia neutra.
└─ IntegrateDampedVelocity
   └─ Integrazione di forza, massa e smorzamento.
```

Per lo scivolamento del personaggio si applicano inoltre soglia, abilitazione, moltiplicatore e velocità massima del suo PlayerMovementSettings. `SlopeAcceleration` è quindi un risultato calcolato, non un numero globale scollegato dalla gravità.

La massa inerziale resta positiva. Il carico residuo può essere negativo quando una spinta supera il peso: quel segno determina la salita, senza trasformare la massa in un valore negativo.

## 7. Responsabilità del movimento

```text
SimulatedBody2D
├─ Conserva dati e stato comuni.
├─ Esegue cast e movimenti compatibili con le collisioni.
└─ Espone i behaviour presenti nell'Inspector.

Un solo responsabile del movimento per corpo
├─ PropsMotion per un oggetto autonomo.
└─ PlayerMovement per un animale controllato.

Coordinamento tra corpi
└─ Un solver condiviso gestisce stack, passeggeri e carichi collegati.
```

Il pulsante `Add Buoyancy Behaviour` potrebbe aggiungere PropsBuoyancy e il motore necessario a un prop. Su un player dovrebbe rispettare il motore già presente. I pulsanti dovrebbero assegnare i riferimenti ed evitare configurazioni con due motori che spostano lo stesso corpo.

## 8. Come conservare gli attuali valori

- I campi specifici del personaggio dell'attuale PhysicsSettings.asset vengono copiati sul prefab Player nelle sezioni sopra.
- La massa del topo rimane nel suo SimulatedBody2D.
- Le proprietà dei props continuano a essere salvate nei rispettivi prefab e nelle istanze di scena.
- Per conservare gravità oggi diverse si possono derivare i GravityScale: con una gravità globale di riferimento pari a 30, il topo usa 1 e un prop con gravità attuale 66 usa 2.2.
- Con WaterGravityModifier inizialmente pari a 1, il topo può conservare la sua gravità in acqua pari a 7 tramite WaterGravityScale = 7/30; i props mantengono il loro comportamento precedente con il proprio modificatore.
- L'attuale accelerazione di scivolamento va convertita nel moltiplicatore rispetto alla gravità applicata, anziché assegnare un valore nuovo arbitrario.
- I limiti e le resistenze passano sul body conservando i valori attualmente usati nel contesto corrispondente.

Queste conversioni richiedono una verifica successiva del comportamento, soprattutto per nuoto, scivolamento e palloncini. La gerarchia è una proposta da approvare prima di affrontare tale migrazione.

## Punti da valutare

1. Due categorie principali: PhysicsSettings globale e PlayerMovementSettings locale.
2. Resistenze e limiti fisici centralizzati nel body del singolo oggetto.
3. Capacità e caratteristiche speciali sui rispettivi behaviour.
4. Un solo stato di velocità e un solo responsabile del movimento per corpo.
5. Nessun profilo globale del player che impedisca di configurare animali differenti.
