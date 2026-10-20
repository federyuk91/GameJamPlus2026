# Satiety

The Player prefab contains PlayerSatiety: Maximum Notches starts at 5 and one notch drains every Drain Interval = 60 game seconds. The timer pauses with the game and whenever GameManager is not Playing. Each new LoopScene starts at full satiety.

Normal cheese (CollectibleType.Cheese) restores every notch and resets timer progress. Green cheese and photos retain their test-ending behavior.

The HUD displays notches above the bottom-left counters. Gameplay HUD → Satiety Hud exposes Flash Color, Flash Duration, Shake Duration and Shake Amplitude. The full-screen flash does not intercept input. Shake preserves pixel-grid camera alignment and does not alter its following position. Both finish even when the final loss pauses gameplay for the test ending.

At zero, PlayerSatiety calls Starvation Outcome: on the Player, Dialogue Message Player contains diet comments and Test End Event contains random results with Subject Died = true. The normal sequence is white flash → comment → result → next universe.

Drag Assets/Prefabs/Hazards/Starvation Death Zone.prefab into desired areas. Box Collider 2D → Size/Offset defines the volume. Its Drain Interval initially equals 5 seconds. Show Gizmos controls the faint red outline, invisible in gameplay. No Player references, tags or Rigidbody2D are needed: zones register automatically and are checked against the mouse collider.

Zones accelerate the existing timer, preserving partial progress on entry/exit. Overlapping zones use the shortest interval without stacking losses.

Check normal drain, zone residence, pause, normal cheese restoration and the last-notch test ending. Temporarily lower Player Drain Interval for a quick check.
