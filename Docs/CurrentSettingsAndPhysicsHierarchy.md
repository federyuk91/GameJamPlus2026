# Gerarchia attuale di settings e fisica custom

Inventario dell'implementazione presente nel progetto. Le rientranze indicano contenimento o composizione, non ereditarietà. Sono esclusi settings di UI/audio e sistemi di progressione non fisici.

## Settings globali

`PhysicsSettings` è ancora uno ScriptableObject. Non esiste ancora un contenitore `SettingsScriptableObject` con una classe `PhysicsSettings` serializzabile: quella era una proposta.

```text
PhysicsSettings : ScriptableObject
├─ gravityAcceleration
├─ simulationSubsteps
├─ ropeSimulationSubsteps
├─ ropeConstraintIterations
├─ fullSubmersionWeightMultiplier
│
├─ player : SharedPlayerMovementSettings [Serializable]
│  ├─ maxStepHeight
│  ├─ coyoteTime
│  ├─ jumpBuffer
│  ├─ launchControlTime
│  ├─ launchControlMultiplier
│  ├─ runFootstepsPerSecond
│  └─ walkFootstepsPerSecond
│
├─ interactions : PlayerInteractionSettings [Serializable]
│  ├─ ropeJumpUpSpeed
│  ├─ ropeJumpSwingBoost
│  ├─ crateMoveSpeed
│  ├─ ropeClimbSpeed
│  ├─ ropeSwingAcceleration
│  └─ ropeRegrabDelay
│
├─ water : SwimMotion [Serializable]
│  ├─ bottomWalkSpeedMultiplier
│  ├─ acceleration
│  ├─ strokeAcceleration
│  ├─ strokeControlTime
│  ├─ braking
│  ├─ gravityMultiplier
│  ├─ maxSinkSpeed
│  └─ strokeSpeed
│
├─ ladder : LadderSettings [Serializable]
│  ├─ jumpHorizontalSpeed
│  ├─ jumpUpSpeed
│  └─ regrabDelay
│
├─ ledgeClimb : LedgeClimbSettings [Serializable]
│  ├─ enabled
│  ├─ maxHeight
│  ├─ hangHeightFraction
│  └─ retryDelay
│
└─ digging : DiggingSettings [Serializable]
   ├─ enabled
   ├─ radius
   ├─ reach
   ├─ excavatedAlpha
   └─ excavatedTint
```

L'asset condiviso è `Assets/Settings/PhysicsSettings.asset`. La classe espone anche valori/istanze statiche di default per i componenti senza asset assegnato. `SwimMotion` e `LadderSettings` contengono anche metodi di calcolo, non soltanto campi.

## Settings locali dell'animale

```text
PlayerMovement : MonoBehaviour
└─ movementSettings : PlayerMovementSettings [Serializable]
   ├─ moveSpeed
   ├─ climbSpeedMultiplier
   ├─ groundAcceleration
   ├─ airAcceleration
   ├─ jumpHeight
   ├─ jumpReleaseMultiplier
   ├─ fallGravityMultiplier
   ├─ maximumPushWeight
   ├─ handOffset
   ├─ handGrabReach
   ├─ horizontalHandInteraction
   └─ verticalHandInteraction
```

`PlayerSwimming` contiene il riferimento al `PlayerMovement` locale e `swimSpeedMultiplier` (default 1). La velocità orizzontale di nuoto è calcolata come `movementSettings.moveSpeed × swimSpeedMultiplier`, anche durante il controllo in superficie dopo una bracciata.

`PlayerDigging` contiene il riferimento al `PlayerMovement` locale e `digSpeedMultiplier` (default 0.3). La velocità di scavo è calcolata come `movementSettings.moveSpeed × digSpeedMultiplier`: con Move Speed 5 rimane 1.5.

Ogni animale possiede la propria istanza. Corda, scala, bordi e spinta leggono i parametri locali necessari attraverso il riferimento a PlayerMovement. Gli altri settings dell'animale elencati nello ScriptableObject sono tuttora condivisi.

## Corpo fisico e contatto con il terreno

```text
GameObject fisico
├─ BoxCollider2D
├─ SimulatedBody2D : MonoBehaviour
│  ├─ physicsSettings → PhysicsSettings condiviso
│  ├─ airDrag
│  ├─ waterDrag
│  ├─ groundFriction
│  ├─ maxSlopeAngle
│  ├─ maxSpeed
│  ├─ gravityScale
│  ├─ shape → BoxCollider2D usato dai cast
│  ├─ ignorePlayerColliders
│  ├─ solidLayers
│  ├─ skin
│  ├─ weight
│  └─ groundContact → GroundContact2D
│
└─ GroundContact2D : MonoBehaviour
   ├─ body → SimulatedBody2D
   └─ Stato calcolato
      ├─ HasContact
      ├─ IsWalkable / IsSteep
      ├─ Normal
      ├─ Point
      ├─ Collider
      └─ Angle
```

SimulatedBody2D esegue cast, spostamenti e limitazione della velocità. Espone inoltre HitWall/HitCeiling e inoltra lo stato del terreno attraverso Grounded, HasGroundContact, GroundNormal, GroundPoint, GroundCollider, GroundAngle e OnSteepSlope.

Il body non applica autonomamente la gravità. La velocità appartiene ancora al motore di movimento, PlayerMovement oppure PropsMotion. La gravità effettiva del body è gravityAcceleration × Gravity Scale ed è usata sia dai props sia dal player. Fall Gravity Multiplier modifica solo la discesa aerea; il nuoto applica il proprio gravityMultiplier.

Ground Friction è un coefficiente di attrito, non il precedente Ground Drag come smorzamento percentuale. Max Slope Angle decide la percorribilità; la tenuta/scivolamento deriva dalle forze e dall'attrito. Max Speed è il tetto comune, mentre camminata e nuoto conservano velocità richieste dai controlli.

## Motori e behaviour dei props

```text
GameObject del prop
├─ SimulatedBody2D + GroundContact2D
├─ PropsMotion : MonoBehaviour
│  ├─ body
│  ├─ initialVelocity
│  ├─ followSupport
│  ├─ buoyancy → PropsBuoyancy [opzionale]
│  ├─ tether → RopeAttachment [opzionale]
│  ├─ pushable → Pushable [opzionale]
│  ├─ balloon → BalloonLift [opzionale]
│  └─ Stato: velocità, immersione, passeggero e gruppi verticali
│
├─ Pushable : MonoBehaviour [opzionale]
│  ├─ body / motion / ropeAttachment
│  ├─ showGizmos
│  └─ Calcola il carico dello stack e trasporta i corpi superiori
│
├─ PropsBuoyancy : MonoBehaviour [opzionale]
│  ├─ body
│  ├─ buoyancyAcceleration
│  └─ verticalDampingRatio
│
├─ BalloonLift : MonoBehaviour [opzionale]
│  ├─ rope / attachment / motion
│  ├─ liftCapacity
│  ├─ hoverWeightTolerance
│  ├─ liftAcceleration
│  └─ swingInfluence
│
└─ RopeAttachment : MonoBehaviour [opzionale]
   ├─ rope → RopeSimulation
   ├─ motion
   ├─ pushable [facoltativo]
   ├─ attachmentOffset
   └─ Stato: Attached / HasRopeConnection
```

PropsMotion coordina i props e i gruppi verticali una volta per tick, con sottopassi. I follower vengono simulati dopo i props ordinari. Il body fornisce massa, attriti, gravità relativa e limite di velocità; i behaviour forniscono capacità e vincoli specifici.

## Player: composizione fisica

```text
Animale
├─ SimulatedBody2D + GroundContact2D
├─ PlayerMovement + PlayerMovementSettings locale
├─ PlayerInputReader
├─ PlayerSwimming
├─ PlayerCrateInteraction
├─ PlayerRopeInteraction
├─ PlayerRopeBite
├─ PlayerLadderInteraction
├─ PlayerLedgeClimb
├─ PlayerPlatformRide
├─ PlayerDigging
└─ PlayerSurfaceAlignment → solo allineamento grafico
```

Questi componenti collaborano con PlayerMovement; non sono suoi figli C#. I componenti fisici dell'animale leggono il riferimento ai settings globali dal body. Gli altri sistemi del personaggio, come animazione, ossigeno, sazietà e audio, sono separati da questa gerarchia fisica.

## Corde e ambiente

```text
Oggetto corda
└─ RopeSimulation : MonoBehaviour
   ├─ physicsSettings
   ├─ gravityScale
   ├─ length / pieces / restDirection / segmentLength
   ├─ tautTolerance
   ├─ damping
   ├─ swingLimits : RopeSwingConstraint [Serializable]
   │  ├─ minInnerAngle / minOuterAngle
   │  └─ maxInnerAngle / maxOuterAngle
   ├─ canBeCut / showGizmos
   ├─ balloonAnchor [opzionale]
   └─ chain : RopeChain → simulazione interna dei segmenti

Oggetto acqua
└─ WaterVolume : MonoBehaviour
   └─ shape → volume trigger

Oggetto turbina
└─ TurbineWind : MonoBehaviour
   ├─ windArea / outlet / fanAnimator
   ├─ running
   ├─ acceleration / farEndStrength
   ├─ blockedByWalls / blockingLayers
   └─ showWindGizmos / gizmoColumns / gizmoRows
```

Corde e volumi ambientali non richiedono automaticamente un body. RopeSimulation conserva il proprio riferimento ai settings. Le forze di acqua e vento vengono consumate dai motori fisici.

## Formule e simulazione senza MonoBehaviour

```text
GroundMotion [static] → integrazione tangenziale, attrito e limite di velocità
JumpPhysics [static] → velocità iniziale e integrazione del salto
CrateFlotation [static, internal] → peso immerso e integrazione verticale smorzata
RopeChain [classe ordinaria] → punti, velocità e vincoli della corda
RopeSwingConstraint [Serializable] → limiti e frenata dell'oscillazione
```

Il nome CrateFlotation è ancora quello effettivo nel codice, nonostante le altre rinomine Crate → Props.

## Composizioni concrete

```text
Cassa
└─ Body + GroundContact + PropsMotion + Pushable
   └─ PropsBuoyancy / RopeAttachment se necessari

Palloncino
└─ Body + GroundContact + PropsMotion + BalloonLift
   └─ Collegamenti alla corda e all'eventuale carico

Formaggio / piatto
└─ Body + GroundContact + PropsMotion (Follow Support)
   └─ Impronta trigger separata dal trigger di raccolta

DroppingPlatform [contenitore]
├─ Corda ancorata
└─ Suspended Plank
   ├─ Body + GroundContact + PropsMotion + RopeAttachment
   └─ DroppingPlatform → attivazione, taglio temporizzato e scomparsa
```

Non esistono attualmente PropsRotation o una simulazione angolare dei props. PlayerSurfaceAlignment ruota solo la grafica del personaggio. CollectibleGravity è stato eliminato.

Questo documento descrive lo stato attuale; non modifica codice, prefab o comportamento.

La precedente GravitySettings è stata rimossa: non esistono più playerGravity, crateGravity e ropeGravity come accelerazioni globali indipendenti. Le velocità di lancio dalla corda restano condivise in PlayerInteractionSettings.

La distanza di presa di corde e scale è ora unificata in PlayerMovementSettings.handGrabReach, pari a 0.6 sul prefab Player. Gli ex interactions.ropeGrabRadius e ladder.grabReach sono stati rimossi.

La portata laterale di spinta/trazione e presa del bordo è condivisa in PlayerMovementSettings.horizontalHandInteraction = 0.15. Le distanze si misurano dal lato del collider; handGrabReach e la tolleranza verticale del bordo restano separate.

La tolleranza verticale della presa del bordo è PlayerMovementSettings.verticalHandInteraction = 0.15, locale all’animale. Sostituisce ledgeClimb.grabVerticalReach; maxHeight resta un limite distinto per l’altezza dell’ostacolo.

Il nuoto legge WaterDrag da SimulatedBody2D per lo smorzamento verticale. I props leggono lo stesso parametro dal proprio body e lo modulano con l’immersione. Il player usa 1.5, conservando il precedente Vertical Drag; SwimMotion non conserva più un duplicato globale.

Scala e risalita del bordo condividono PlayerMovementSettings.ClimbSpeed = Move Speed × Climb Speed Multiplier. Il moltiplicatore è locale all’animale (default 0.6): nel player attuale la velocità è 3, conservando la scala e riducendo il bordo da 5 a 3. Rimossi ladder.climbSpeed e ledgeClimb.climbSpeed dai settings globali. La salita della corda mantiene il proprio parametro.
