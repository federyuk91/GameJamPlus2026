# Animated decorations

Assets/Prefabs/Decorations contains Algae 01, Algae 02, Algae 03 and Water FX. Drag them into scenes or room prefabs. They have no colliders or gameplay components.

Animations and Animator Controllers are in Assets/Animations/Decorations. Each controller automatically starts its only Loop state. Clips loop at 8 FPS: Algae 01/03 have 8 frames (1 second), Algae 02 has 7 (0.875 seconds), Water FX has 14 (1.75 seconds). Change playback using the Loop state's Speed.

Original sheets remain in Art/Pixel Art. Automatic slicing of individual strands/bubbles was replaced with uniform 32×32 frames read left to right. Texture GUIDs and reused frame IDs are preserved. Algae pivots are bottom-center; Water FX is centered. Import uses Point at 32 PPU.

Order in Layer is 0 and can be changed per instance for water/terrain overlap. Animations follow game time and pause with gameplay. No instances were automatically added to LoopScene. Check looping, algae base positions and rendering order in Unity.

## Piranha Water FX

LoopScene's piranha has Periodic Sprite Effect assigned to Water FX. Interval is 10 seconds between cycle starts, with the first appearance after 10 seconds. Position Offset (0, 0.35, 0) controls placement relative to the fish.

Each appearance reuses one effect instance, places it on the piranha and restarts at frame one. The effect stays fixed in world space while the fish moves. After one Animator cycle it is disabled and moved to the fish's current position; placement is updated again on the next appearance. Duration comes from Animator progress rather than a hardcoded delay. The original decorative prefab still loops independently.

Pauses and test endings stop the timer. Disabling/destroying the piranha disables/removes its effect. Fish behavior is unchanged.
