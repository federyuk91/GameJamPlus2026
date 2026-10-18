# Mouse animations

The Player prefab uses `Assets/Animations/Mouse/Mouse.controller`. `PlayerAnimation` reads gameplay component states and sets `MotionState`; Any State transitions are immediate and do not restart the active state.

| MotionState | State | Sprite sheet | Playback |
| --- | --- | --- | --- |
| 0 | Idle | Mouse_Idle_Sheet 1 | Existing loop |
| 1 | Run | Mouse_Run_Sheet 1 | Existing loop |
| 2 | Hanging | Mouse_Hanging_Sheet | 4 frames, 6 FPS, loop; ledge or stationary rope |
| 3 | ClimbUp | Mouse_Climbing_Sheet | 4 frames, 8 FPS, loop during ledge climbing |
| 4 | LadderClimb | Mouse_Climbing_Sheet | 4 frames, 8 FPS, paused while stationary |
| 5 | Swimming | Mouse_Swimming | 6 frames, 8 FPS, loop in water |
| 6 | Jump | Mouse_Jump_Sheet | 5 frames, 10 FPS, once; last frame held in air |
| 7 | Dig | Mouse_Dig_Sheet | 4 frames, 8 FPS, loop while digging |
| 8 | Push | Mouse_Push_Sheet | 6 frames, 10 FPS, pushing/pulling; paused if crate is stationary |
| 9 | RopeClimb | Mouse_Climbing_Sheet | 4 frames, 8 FPS, loop while moving along a rope |

Ledge climbing reuses the climbing sheet; there is no dedicated sheet. `Mouse_Climb_Stairs.anim` retains its GUID but now uses Mouse_Climbing_Sheet, replacing the removed sheet.

Priority: ledge, ladder, rope, digging, water, jumping/falling, crates, running, idle. Ladder/rope, swim, dig and push playback speeds are adjustable in Player Animation. When pulling, MouseFacing keeps the mouse looking at the crate.

Existing IsRunning, LedgeState, IsOnLadder, LadderSpeed, IsSwimming and SwimmingSpeed parameters remain available. Transitions now use MotionState: alternative controllers must include it, plus DiggingSpeed/PushingSpeed when using PlayerAnimation.

Check ledge alignment, stationary ladder/rope poses, jumping/landing, digging and pushing/pulling in both directions in Play Mode.
