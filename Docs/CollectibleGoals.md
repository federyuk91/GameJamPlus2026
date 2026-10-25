# Collectible quantities and persistence

## Ownership

Collectible owns collectionId, pickup state, interaction mode and the onCollected presentation event. CollectibleRemains only changes the consumed sprite and plate physics. CollectiblePhoto only displays its texture and reports closure. Physics remains configured on the individual objects.

GameSession.Collections owns a plain CollectibleSession, containing persistent IDs, session totals and attempt counts. Normal and green cheese share the Cheese counter; their original pickup types remain distinct for satiety, poison and voice effects. Attempt counts reset on GameSession.Advance, while IDs and session totals survive stage/universe changes until a new run. The observation count for the current subject remains separate, preserving its existing reset behavior.

## Ordered collection

1. Validate the item and input, reserve its ID, and disable its pickup area.
2. Add the quantity and notebook observations.
3. Emit CollectibleCollector.Collected with the actual item. PlayerSatiety applies food effects and PlayerVoices handles normal-cheese audio.
4. Emit CountChanged for HUD/counter observers.
5. CollectionGoalController evaluates immediate goals through a separate commit phase.
6. Invoke local onCollected UnityEvents, then show remains or hide the item.

This keeps accepted endings ahead of pickup presentation that can release pressure plates, and keeps food effects ahead of goal evaluation. Changes such as AddKnowledge do not masquerade as eating an item.

PhotoClosed is emitted on the originating collector after the photo is closed and its onContinue callbacks have run. The already-hidden photo collectible retains its collector reference for this callback. Goal evaluation occurs after PhotoClosed, when the HUD has restored its photo-owned pause.

## Central goals

Universe Rig has CollectionGoalController on the same GameObject as GameManager. Its serialized rule list contains:

- Cheese: threshold 10, session total, evaluate on collection, StageEndCase.CheeseGoal.
- Photo: every multiple of 3, session total, evaluate after closing the photo, StageEndCase.PhotoGoal.

Each CollectionGoalRule is a normal serializable class: enabled flag, collectible counter, required quantity, session/attempt scope, threshold/multiple match, evaluation timing and ending case. Rules are evaluated in list order and stop at the first accepted ending. GameManager references the controller explicitly. The standalone older gameplay scenes have equivalent controllers beside their GameManager.

## Migration

Existing IDs were moved from remains/photo components into their owning Collectible, including placed-instance overrides. No IDs were regenerated. Photo textures, callbacks, plate configuration and physical properties were retained. Empty source-prefab IDs remain empty; placed instances should have unique IDs.

The old Player/Cheese Collection Goal component and its empty container were removed. CollectibleTestGoal.cs and its meta remain temporarily, marked Obsolete and unreferenced by operational code/assets, pending explicit deletion approval. Recovery snapshots under Assets/_Recovery were not migrated.

## Editor tools

- Repair Collectible IDs In Scenes now reads IDs from Collectible. Existing non-empty unique IDs are retained; blank/duplicate IDs are repaired when explicitly running the tool or saving a scene. Photo references/events are checked without moving goal settings back onto photos. Archived recovery scenes are excluded.
- Repair Collectible Goal Prefabs configures Universe Rig's central controller and the photo Play event. Existing non-empty rule lists are preserved; defaults are created only for an empty list.
- Audit Collectible Scene Setup reports collectible IDs, types, callbacks and central rule counts. It is not a gameplay test.

No compilation, Play Mode or automated tests were run. In Unity check persisted plates/photos across loops, cheese/poison effects, HUD quantities, the tenth cheese, photo closure at totals 3/6/9, plate stacking, and a fresh run resetting IDs/totals.
