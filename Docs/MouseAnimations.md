# Animazioni del topo

Il prefab Player usa `Assets/Animations/Mouse/Mouse.controller`. `PlayerAnimation` legge gli stati dei componenti del gameplay e imposta `MotionState`; le transizioni Any State sono immediate e non riavviano lo stato già attivo.

| MotionState | Stato | Sprite sheet | Riproduzione |
| --- | --- | --- | --- |
| 0 | Idle | Mouse_Idle_Sheet 1 | Loop esistente |
| 1 | Run | Mouse_Run_Sheet 1 | Loop esistente |
| 2 | Hanging | Mouse_Hanging_Sheet | 4 frame, 6 FPS, loop; bordo o corda ferma |
| 3 | ClimbUp | Mouse_Climbing_Sheet | 4 frame, 8 FPS, loop durante la risalita del bordo |
| 4 | LadderClimb | Mouse_Climbing_Sheet | 4 frame, 8 FPS, fermo quando il topo non sale/scende |
| 5 | Swimming | Mouse_Swimming | 6 frame, 8 FPS, loop in acqua |
| 6 | Jump | Mouse_Jump_Sheet | 5 frame, 10 FPS, una volta; mantiene l'ultimo frame in aria |
| 7 | Dig | Mouse_Dig_Sheet | 4 frame, 8 FPS, loop durante lo scavo |
| 8 | Push | Mouse_Push_Sheet | 6 frame, 10 FPS, spinta/tiro; fermo se la cassa non si sposta |
| 9 | RopeClimb | Mouse_Climbing_Sheet | 4 frame, 8 FPS, loop durante lo spostamento lungo la corda |

La risalita del bordo riusa il foglio di arrampicata: non è presente un foglio specifico per questo movimento. `Mouse_Climb_Stairs.anim` conserva il proprio GUID ma ora usa i frame di Mouse_Climbing_Sheet, sostituendo il vecchio foglio non più presente.

Precedenza: bordo, scala, corda, scavo, acqua, salto/caduta, casse, corsa, riposo. La velocità del loop di scala/corda, nuoto, scavo e spinta è regolabile in Player Animation. Durante il tiro MouseFacing mantiene lo sguardo rivolto verso la cassa.

I parametri preesistenti IsRunning, LedgeState, IsOnLadder, LadderSpeed, IsSwimming e SwimmingSpeed restano disponibili. Le transizioni usano ora MotionState: controller alternativi devono includere questo parametro e DiggingSpeed/PushingSpeed se usano PlayerAnimation.

Da controllare in Play Mode: allineamento visivo ai bordi, fermata su scala/corda, salto e atterraggio, scavo, spinta e tiro nelle due direzioni.
