# Gravità unificata

## Configurazione attuale

```text
PhysicsSettings : ScriptableObject
├─ gravityAcceleration = 30
├─ water : SwimMotion
│  └─ gravityMultiplier = 7/30
└─ interactions : PlayerInteractionSettings
   ├─ ropeJumpUpSpeed = 7
   └─ ropeJumpSwingBoost = 4

SimulatedBody2D
├─ gravityScale
└─ GravityAcceleration = world gravity × gravityScale [calcolato]

RopeSimulation
└─ gravityScale
   └─ Gravità dei segmenti e della tensione = world gravity × gravityScale

PlayerMovementSettings [locale]
└─ fallGravityMultiplier = 2.2
```

Il contenitore globale resta PhysicsSettings, uno ScriptableObject. La classe serializzabile GravitySettings è stata eliminata insieme a playerGravity, crateGravity e ropeGravity.

## Conservazione delle accelerazioni

| Oggetto | Scala migrata | Accelerazione di base |
| --- | --- | --- |
| Player | 1 | 30 |
| Props ordinari e palloncini | 2.2 | 66 |
| Formaggi / piatti | 1 | 30 |
| Asse DroppingPlatform | circa 0.666666667 | 20 |
| Corde | 1 | 30 |

Per i props è stata applicata `nuova scala = vecchia scala × vecchia crateGravity / nuova gravità globale`, conservando i moltiplicatori specifici già assegnati. La scala del player, prima non consumata dal suo motore, ora produce la precedente playerGravity. I prefab e i componenti completi di scena sono stati migrati; le istanze stripped ereditano dal prefab.

Il player usa la gravità del body per salto normale, salto dal bordo e forze sulle pendenze. Soltanto la discesa aerea applica anche il locale Fall Gravity Multiplier: il player conserva quindi 30 × 2.2 = 66 durante quella fase.

Il nuoto usa `gravità del body × water.gravityMultiplier`: con il player attuale resta 30 × 7/30 = 7. Questo è un modificatore della risposta del nuotatore, non una riduzione ambientale della gravità dei props. PropsBuoyancy, la riduzione del peso immerso e la capacità dei palloncini rimangono separati. Le accelerazioni di sollevamento conservano i propri valori assoluti.

RopeSimulation usa il moltiplicatore della corda sia per i segmenti sia per il calcolo della tensione del rider. Le due velocità di lancio restano condivise in PlayerInteractionSettings: sono impulsi di gameplay espressi come velocità, non gravità.

## API e nuovi oggetti

- Assegnare la gravità del mondo su PhysicsSettings; personalizzare il corpo tramite SimulatedBody2D.Gravity Scale.
- I nuovi body hanno scala 1 e quindi ricevono la gravità globale 30; i prefab migrati conservano i valori in tabella.
- SwimMotion.Step riceve esplicitamente la gravità effettiva del body. I chiamanti del progetto, inclusa la validazione esistente nell'Editor, sono stati aggiornati senza eseguire la validazione.
- JumpPhysics gestisce scala zero senza introdurre una gravità minima artificiale.
- Il limite di velocità resta SimulatedBody2D.Max Speed.

Non sono stati avviati compilazione, test automatici o Play Mode. In Unity verificare salto completo/interrotto, discesa, nuoto, corde e relativo rilascio, galleggiamento, equilibrio dei palloncini e caduta delle assi.
