# Project structure

Assets are grouped by responsibility. File names, GUIDs and serialized gameplay values are preserved; every existing asset was moved with its original `.meta`. Explicit paths in scripts and documents follow the new locations.

| Folder | Contents |
| --- | --- |
| `Assets/Prefabs/Characters` | Player and piranha |
| `Assets/Prefabs/Collectibles` | Cheese and photos |
| `Assets/Prefabs/Props` | Crates, balloons and ropes |
| `Assets/Prefabs/Interactables/Buttons` | All button variants |
| `Assets/Prefabs/Environment` | Platforms, ladders, terrain, water and turbines |
| `Assets/Prefabs/Hazards` | Traps and lethal zones |
| `Assets/Prefabs/GameLoop` | Spawn, recovery and goal |
| `Assets/Prefabs/Rooms`, `Decorations` | Existing room and decoration prefabs |
| `Assets/Scripts/Physics` | Shared body, ground detection and force calculations |
| `Assets/Scripts/Props` | Prop motion, buoyancy, pushing and balloon lift |
| `Assets/Scripts/Player` | Input, movement, interactions, presentation and vitals |
| `Assets/Scripts/Environment` | Buttons, platforms, ladders, water and wind |
| `Assets/Scripts/Camera`, `Effects` | Camera behavior and sprite effects |
| `Assets/Scripts/Rope`, `Terrain`, `Collectibles`, `Enemies`, `GameLoop`, `Rooms`, `Settings`, `UI` | Related systems |
| `Assets/Editor` | Gameplay, physics, rooms, tilemaps, importing and manual validation tools |
| `Assets/Animations` | Mouse, piranha, balloon, turbine and decoration animations |
| `Assets/Art/UI/EndGame` | Ending artwork and source files |
| `Assets/Art/Pixel Art/Effects` | Algae and oxygen effects |
| `Assets/Art/References` | Visual reference images |
| `Assets/Art/Pixel Art` | Gameplay pixel art, with existing importer rules |

Scenes, Resources, settings assets and the existing UI Toolkit packages retain their established roots. No compilation, validation tools or Play Mode were run.

## Asset moves

| Previous path | Current path |
| --- | --- |
| `Assets/Animations/Balloon.controller` | `Assets/Animations/Balloon/Balloon.controller` |
| `Assets/Animations/Balloon_Float.anim` | `Assets/Animations/Balloon/Balloon_Float.anim` |
| `Assets/Animations/Piranha.controller` | `Assets/Animations/Piranha/Piranha.controller` |
| `Assets/Animations/Piranha_Swim.anim` | `Assets/Animations/Piranha/Piranha_Swim.anim` |
| `Assets/Animations/Turbine.controller` | `Assets/Animations/Turbine/Turbine.controller` |
| `Assets/Animations/Turbine_Run.anim` | `Assets/Animations/Turbine/Turbine_Run.anim` |
| `Assets/Art/End Game 0 Morti.JPG` | `Assets/Art/UI/EndGame/End Game 0 Morti.JPG` |
| `Assets/Art/End Game Molte Morti.JPG` | `Assets/Art/UI/EndGame/End Game Molte Morti.JPG` |
| `Assets/Art/EndGame 1 morto.JPG` | `Assets/Art/UI/EndGame/EndGame 1 morto.JPG` |
| `Assets/Art/Full TileSet.png` | `Assets/Art/References/Full TileSet.png` |
| `Assets/Art/Pixel Art/Balloon_Sheet.aseprite` | `Assets/Art/Pixel Art/Props/Balloon_Sheet.aseprite` |
| `Assets/Art/Pixel Art/Goal.png` | `Assets/Art/Pixel Art/Props/Goal.png` |
| `Assets/Art/Pixel Art/Piranha.png` | `Assets/Art/Pixel Art/Characters/Piranha.png` |
| `Assets/Art/Pixel Art/Scientist.png` | `Assets/Art/Pixel Art/Characters/Scientist.png` |
| `Assets/Art/Pixel Art/waterFx2.png` | `Assets/Art/Pixel Art/Effects/waterFx2.png` |
| `Assets/Art/R0009085.JPG` | `Assets/Art/References/R0009085.JPG` |
| `Assets/Editor/DiggableTerrainEditor.cs` | `Assets/Editor/Gameplay/DiggableTerrainEditor.cs` |
| `Assets/Editor/GameplayTilemapSetup.cs` | `Assets/Editor/Tilemaps/GameplayTilemapSetup.cs` |
| `Assets/Editor/InteractionAreaGizmos.cs` | `Assets/Editor/Gameplay/InteractionAreaGizmos.cs` |
| `Assets/Editor/JumpValidation.cs` | `Assets/Editor/Validation/JumpValidation.cs` |
| `Assets/Editor/LadderEditor.cs` | `Assets/Editor/Gameplay/LadderEditor.cs` |
| `Assets/Editor/LadderValidation.cs` | `Assets/Editor/Validation/LadderValidation.cs` |
| `Assets/Editor/LedgeClimbValidation.cs` | `Assets/Editor/Validation/LedgeClimbValidation.cs` |
| `Assets/Editor/MainMenuValidation.cs` | `Assets/Editor/Validation/MainMenuValidation.cs` |
| `Assets/Editor/ManualGridAlignmentEditor.cs` | `Assets/Editor/Rooms/ManualGridAlignmentEditor.cs` |
| `Assets/Editor/PhysicsSettingsEditor.cs` | `Assets/Editor/Physics/PhysicsSettingsEditor.cs` |
| `Assets/Editor/PiranhaEditors.cs` | `Assets/Editor/Gameplay/PiranhaEditors.cs` |
| `Assets/Editor/PixelArtTexturePostprocessor.cs` | `Assets/Editor/Importing/PixelArtTexturePostprocessor.cs` |
| `Assets/Editor/PlatformerValidation.cs` | `Assets/Editor/Validation/PlatformerValidation.cs` |
| `Assets/Editor/PlayerFeedbackImportRepair.cs` | `Assets/Editor/Importing/PlayerFeedbackImportRepair.cs` |
| `Assets/Editor/PlayerRopeBiteEditor.cs` | `Assets/Editor/Gameplay/PlayerRopeBiteEditor.cs` |
| `Assets/Editor/RoomEditors.cs` | `Assets/Editor/Rooms/RoomEditors.cs` |
| `Assets/Editor/RoomPrefabSetup.cs` | `Assets/Editor/Rooms/RoomPrefabSetup.cs` |
| `Assets/Editor/RopeSimulationEditor.cs` | `Assets/Editor/Physics/RopeSimulationEditor.cs` |
| `Assets/Editor/RopeValidation.cs` | `Assets/Editor/Validation/RopeValidation.cs` |
| `Assets/Editor/SwimmingValidation.cs` | `Assets/Editor/Validation/SwimmingValidation.cs` |
| `Assets/Editor/TilePaletteToolbar.cs` | `Assets/Editor/Tilemaps/TilePaletteToolbar.cs` |
| `Assets/Editor/TilemapColliderTools.cs` | `Assets/Editor/Tilemaps/TilemapColliderTools.cs` |
| `Assets/Prefabs/01 Free Floating Crate.prefab` | `Assets/Prefabs/Props/Crates/01 Free Floating Crate.prefab` |
| `Assets/Prefabs/Balloon Ground Tether.prefab` | `Assets/Prefabs/Props/Balloons/Balloon Ground Tether.prefab` |
| `Assets/Prefabs/Balloon With Crate.prefab` | `Assets/Prefabs/Props/Balloons/Balloon With Crate.prefab` |
| `Assets/Prefabs/Balloon.prefab` | `Assets/Prefabs/Props/Balloons/Balloon.prefab` |
| `Assets/Prefabs/Cheese.prefab` | `Assets/Prefabs/Collectibles/Cheese.prefab` |
| `Assets/Prefabs/CheeseGreen.prefab` | `Assets/Prefabs/Collectibles/CheeseGreen.prefab` |
| `Assets/Prefabs/Diggable ground.prefab` | `Assets/Prefabs/Environment/Diggable ground.prefab` |
| `Assets/Prefabs/DroppingPlatform.prefab` | `Assets/Prefabs/Environment/DroppingPlatform.prefab` |
| `Assets/Prefabs/Electric Trap.prefab` | `Assets/Prefabs/Hazards/Electric Trap.prefab` |
| `Assets/Prefabs/Fall Death Zone.prefab` | `Assets/Prefabs/Hazards/Fall Death Zone.prefab` |
| `Assets/Prefabs/Goal.prefab` | `Assets/Prefabs/GameLoop/Goal.prefab` |
| `Assets/Prefabs/Horizontal Swinging Crate.prefab` | `Assets/Prefabs/Props/Crates/Horizontal Swinging Crate.prefab` |
| `Assets/Prefabs/Interactable Button 1.prefab` | `Assets/Prefabs/Interactables/Buttons/Interactable Button 1.prefab` |
| `Assets/Prefabs/Interactable Button Blue.prefab` | `Assets/Prefabs/Interactables/Buttons/Interactable Button Blue.prefab` |
| `Assets/Prefabs/Interactable Button Purple.prefab` | `Assets/Prefabs/Interactables/Buttons/Interactable Button Purple.prefab` |
| `Assets/Prefabs/Interactable Button Yellow.prefab` | `Assets/Prefabs/Interactables/Buttons/Interactable Button Yellow.prefab` |
| `Assets/Prefabs/Interactable Button.prefab` | `Assets/Prefabs/Interactables/Buttons/Interactable Button.prefab` |
| `Assets/Prefabs/Mouse Trap.prefab` | `Assets/Prefabs/Hazards/Mouse Trap.prefab` |
| `Assets/Prefabs/Photo Collectible.prefab` | `Assets/Prefabs/Collectibles/Photo Collectible.prefab` |
| `Assets/Prefabs/Piranha.prefab` | `Assets/Prefabs/Characters/Piranha.prefab` |
| `Assets/Prefabs/Player.prefab` | `Assets/Prefabs/Characters/Player.prefab` |
| `Assets/Prefabs/Recovery Zone.prefab` | `Assets/Prefabs/GameLoop/Recovery Zone.prefab` |
| `Assets/Prefabs/Rope.prefab` | `Assets/Prefabs/Props/Ropes/Rope.prefab` |
| `Assets/Prefabs/Seabed Tether Rope.prefab` | `Assets/Prefabs/Props/Ropes/Seabed Tether Rope.prefab` |
| `Assets/Prefabs/Spawn Point.prefab` | `Assets/Prefabs/GameLoop/Spawn Point.prefab` |
| `Assets/Prefabs/Stair.prefab` | `Assets/Prefabs/Environment/Stair.prefab` |
| `Assets/Prefabs/Starvation Death Zone.prefab` | `Assets/Prefabs/Hazards/Starvation Death Zone.prefab` |
| `Assets/Prefabs/Turbine.prefab` | `Assets/Prefabs/Environment/Turbine.prefab` |
| `Assets/Prefabs/Water Volume.prefab` | `Assets/Prefabs/Environment/Water Volume.prefab` |
| `Assets/Scripts/DroppingPlatform.cs` | `Assets/Scripts/Environment/DroppingPlatform.cs` |
| `Assets/Scripts/Ladder.cs` | `Assets/Scripts/Environment/Ladder.cs` |
| `Assets/Scripts/PeriodicSpriteEffect.cs` | `Assets/Scripts/Effects/PeriodicSpriteEffect.cs` |
| `Assets/Scripts/PixelPerfectViewport.cs` | `Assets/Scripts/Camera/PixelPerfectViewport.cs` |
| `Assets/Scripts/PixelPerfectZoom.cs` | `Assets/Scripts/Camera/PixelPerfectZoom.cs` |
| `Assets/Scripts/PlatformCameraController.cs` | `Assets/Scripts/Camera/PlatformCameraController.cs` |
| `Assets/Scripts/Player/BalloonLift.cs` | `Assets/Scripts/Props/BalloonLift.cs` |
| `Assets/Scripts/Player/CrateFlotation.cs` | `Assets/Scripts/Physics/CrateFlotation.cs` |
| `Assets/Scripts/Player/GameplayInteractionAvailability.cs` | `Assets/Scripts/Player/Interactions/GameplayInteractionAvailability.cs` |
| `Assets/Scripts/Player/GroundContact2D.cs` | `Assets/Scripts/Physics/GroundContact2D.cs` |
| `Assets/Scripts/Player/GroundMotion.cs` | `Assets/Scripts/Physics/GroundMotion.cs` |
| `Assets/Scripts/Player/InteractableButton.cs` | `Assets/Scripts/Environment/InteractableButton.cs` |
| `Assets/Scripts/Player/JumpPhysics.cs` | `Assets/Scripts/Player/Movement/JumpPhysics.cs` |
| `Assets/Scripts/Player/MouseFacing.cs` | `Assets/Scripts/Player/Presentation/MouseFacing.cs` |
| `Assets/Scripts/Player/PlayerAnimation.cs` | `Assets/Scripts/Player/Presentation/PlayerAnimation.cs` |
| `Assets/Scripts/Player/PlayerButtonInteraction.cs` | `Assets/Scripts/Player/Interactions/PlayerButtonInteraction.cs` |
| `Assets/Scripts/Player/PlayerCrateInteraction.cs` | `Assets/Scripts/Player/Interactions/PlayerCrateInteraction.cs` |
| `Assets/Scripts/Player/PlayerDigging.cs` | `Assets/Scripts/Player/Interactions/PlayerDigging.cs` |
| `Assets/Scripts/Player/PlayerFootsteps.cs` | `Assets/Scripts/Player/Presentation/PlayerFootsteps.cs` |
| `Assets/Scripts/Player/PlayerLadderInteraction.cs` | `Assets/Scripts/Player/Interactions/PlayerLadderInteraction.cs` |
| `Assets/Scripts/Player/PlayerLedgeClimb.cs` | `Assets/Scripts/Player/Interactions/PlayerLedgeClimb.cs` |
| `Assets/Scripts/Player/PlayerMovement.cs` | `Assets/Scripts/Player/Movement/PlayerMovement.cs` |
| `Assets/Scripts/Player/PlayerOxygen.cs` | `Assets/Scripts/Player/Vitals/PlayerOxygen.cs` |
| `Assets/Scripts/Player/PlayerOxygenBubbles.cs` | `Assets/Scripts/Player/Presentation/PlayerOxygenBubbles.cs` |
| `Assets/Scripts/Player/PlayerPlatformRide.cs` | `Assets/Scripts/Player/Interactions/PlayerPlatformRide.cs` |
| `Assets/Scripts/Player/PlayerRecovery.cs` | `Assets/Scripts/Player/Vitals/PlayerRecovery.cs` |
| `Assets/Scripts/Player/PlayerRopeBite.cs` | `Assets/Scripts/Player/Interactions/PlayerRopeBite.cs` |
| `Assets/Scripts/Player/PlayerRopeInteraction.cs` | `Assets/Scripts/Player/Interactions/PlayerRopeInteraction.cs` |
| `Assets/Scripts/Player/PlayerSatiety.cs` | `Assets/Scripts/Player/Vitals/PlayerSatiety.cs` |
| `Assets/Scripts/Player/PlayerSurfaceAlignment.cs` | `Assets/Scripts/Player/Presentation/PlayerSurfaceAlignment.cs` |
| `Assets/Scripts/Player/PlayerSwimming.cs` | `Assets/Scripts/Player/Movement/PlayerSwimming.cs` |
| `Assets/Scripts/Player/PlayerSwimmingAudio.cs` | `Assets/Scripts/Player/Presentation/PlayerSwimmingAudio.cs` |
| `Assets/Scripts/Player/PlayerVoices.cs` | `Assets/Scripts/Player/Presentation/PlayerVoices.cs` |
| `Assets/Scripts/Player/PlayerWheelInteraction.cs` | `Assets/Scripts/Player/Interactions/PlayerWheelInteraction.cs` |
| `Assets/Scripts/Player/PropsBuoyancy.cs` | `Assets/Scripts/Props/PropsBuoyancy.cs` |
| `Assets/Scripts/Player/PropsMotion.cs` | `Assets/Scripts/Props/PropsMotion.cs` |
| `Assets/Scripts/Player/Pushable.cs` | `Assets/Scripts/Props/Pushable.cs` |
| `Assets/Scripts/Player/RopeAttachment.cs` | `Assets/Scripts/Rope/RopeAttachment.cs` |
| `Assets/Scripts/Player/SimulatedBody2D.cs` | `Assets/Scripts/Physics/SimulatedBody2D.cs` |
| `Assets/Scripts/Player/SwimMotion.cs` | `Assets/Scripts/Player/Movement/SwimMotion.cs` |
| `Assets/Scripts/Terrain/TurbineWind.cs` | `Assets/Scripts/Environment/TurbineWind.cs` |
| `Assets/Scripts/WaterVolume.cs` | `Assets/Scripts/Environment/WaterVolume.cs` |
| `Assets/Art/Pixel Art/algheEffect.png` | `Assets/Art/Pixel Art/Effects/Algae/algheEffect.png` |
| `Assets/Art/Pixel Art/algheEffectLV2.png` | `Assets/Art/Pixel Art/Effects/Algae/algheEffectLV2.png` |
| `Assets/Art/Pixel Art/algheEffectLV3.png` | `Assets/Art/Pixel Art/Effects/Algae/algheEffectLV3.png` |
| `Assets/Art/Pixel Art/bubble1.png` | `Assets/Art/Pixel Art/Effects/Oxygen/bubble1.png` |
| `Assets/Art/Pixel Art/bubble2.png` | `Assets/Art/Pixel Art/Effects/Oxygen/bubble2.png` |
| `Assets/Art/Pixel Art/bubble3.png` | `Assets/Art/Pixel Art/Effects/Oxygen/bubble3.png` |
