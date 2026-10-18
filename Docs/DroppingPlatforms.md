# Dropping Platform

LoopScene's four DroppingPlatforms have solid BoxCollider2D shapes and DroppingPlatform components assigned to the player's SimulatedBody2D.

- Fall Delay: time before collapse, initially 0.4 seconds.
- Disappear Delay: falling time before disappearance, initially 2 seconds.
- Fall Acceleration and Max Fall Speed: falling acceleration and speed cap.

The countdown starts only when the mouse stands on top and Ground Collider is the platform. Side/bottom contact does not trigger it. Once started it continues even if the mouse leaves. On collapse, the collider is disabled, the platform falls freely and its GameObject is eventually deactivated. Platforms return when the scene reloads, not automatically during the test.

Duplicate configured platforms, or add DroppingPlatform (requires BoxCollider2D), assign Shape and Player, and set Fall Delay. Existing platform scales are preserved. Falling and timers respect GameManager/photo pauses.

Check first contact, leaving during countdown and jumping to the next platform in Unity. Adjust each platform's Fall Delay in its Inspector.
