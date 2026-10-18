# Collectible progress

GameSession retains Cheese, GreenCheese, Photo and Knowledge totals across universes in the same run. Reloading LoopScene recreates the Player without clearing counters. New Game and a new Play Mode session reset them; totals are not saved to disk.

| HUD slot | Type | Enum value |
| --- | --- | --- |
| Primary | Cheese | 0 |
| Secondary | GreenCheese | 1 |
| Tertiary | Knowledge | 3 |
| Quaternary | Photo | 2 |

Existing serialized enum values remain unchanged. Photos retain their assigned slot; the unused slot becomes Research / Knowledge.

CollectibleCollector.GetCount reads session totals. Collection increments the total before collectible events, so green cheese and photos are recorded before ending the test. Cheese and GreenCheese remain consumed across level reloads and display PlateEmpty; other ordinary scene collectibles still respawn on reload. Photos have unique Collection Id values: their collected identities are retained for the run, and already collected photos deactivate on scene startup before gameplay updates. New Game and a new Play Mode session clear this memory. Each new scene photo must have a distinct Collection Id, including when duplicating an existing instance.

Set Collectible → Type = Knowledge for one point per pickup, or call CollectibleCollector.AddKnowledge(int amount) from a UnityEvent. Other actions do not automatically award knowledge.

The bulb is a temporary UI-drawn icon when Gameplay HUD → Counter Icons → Element 2 is empty. Assign a sprite there to replace it. Demonstration Initial Counter Values have been removed: counters start from GameSession totals.

Check in Play Mode: normal pickups, green cheese/photo test endings, LoopScene reload and New Game from the menu.

Photo curiosity uses a separate PhotosCollectedThisTest counter. It resets when advancing to the next subject and on New Game, while the HUD photo total remains cumulative. Closing the third photo ends the current test with the subject alive and counted as recovered; the first two only close the preview. The photo prefab's TestEndEvent has Subject Died disabled, so this outcome does not increase the death count used to select ending artwork.

## Persistent empty plates

Cheese and CheeseGreen compose Collectible with CollectibleRemains. Each placed item has a unique Collection Id, a SpriteRenderer reference and PlateEmpty as Consumed Sprite (32 PPU, Point filtering). Collection records its identity before counters/events, disables the pickup collider and replaces the sprite without hiding the GameObject. On subsequent level loads, consumed items restore the empty plate without rewards, dialogue or test completion. Session Reset/New Game clears consumed identities. All ten current LoopScene cheese instances have distinct IDs. Assign a new unique ID after duplicating or placing another cheese prefab.

Check in Unity: eat cheese, verify PlateEmpty and no second interaction, advance to another subject and verify the same plate remains empty, then choose New Game and verify cheese returns. Repeat for GreenCheese. Counts for the five-cheese goal still apply to the current subject.
