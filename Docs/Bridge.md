# Flexible one-way bridge

`Assets/Scenes/Sequence Room Test1.unity` contains Bridge with BridgeSimulation, Bridge Start, Bridge End and one Bridge Slice. Existing sprites from Ponte.png are retained at 32 PPU. Every board has BoxCollider2D and OneWayPlatform2D on the same object. No Rigidbody2D or Unity joints are required.

## Changing length

Select Bridge, then edit **Slice Count** in Bridge Simulation outside Play Mode. The custom Inspector creates or removes middle slices, moves End and aligns all deck colliders. Start keeps its position. A bridge always has at least one middle slice. **Rebuild and align bridge** repairs the layout manually. Undo supports layout changes. For prefab assets, use Prefab Mode.

Start, End and Slice Template must be distinct direct children of Bridge, each with a SpriteRenderer and sprite. Slice widths come from sprite bounds: the current start is 14/32 units, each slice 15/32 units and end 11/32 units. Total length is `(14 + 15 * Slice Count + 11) / 32` units. Additional slices extend rightward from Start. Rebuilding normalizes piece scale/rotation; keep Bridge at unit scale for values to match world units.

**Deck Surface Height** places the walking surface relative to each sprite center, and **Deck Thickness** sets collider thickness. Defaults are -3/32 and 4/32 units. One-way collision comes from the existing simulated-body queries, allowing upward passage and landing from above.

Press **Down** while standing on a board with **Allow Drop Through** enabled to descend. A press just before landing is buffered for 0.15 seconds. The drop ignores neighboring drop-enabled boards on the same bridge until the mouse clears the deck, so their seams do not catch it. Holding Down does not trigger another drop after landing on a separate platform.

## Flexibility and weight

BridgeFlexMotion is the numerical spring model. BridgeSimulation reads the existing active-body registry, counts only bodies grounded on its own boards, and distributes their mass along a fixed-end tension profile. Bodies passing underneath or jumping through it do not load the deck.

- **Rest Sag**: unloaded sag in units, default 0.02.
- **Sag Per Kilogram**: load response in units/kg, default 0.1.
- **Maximum Sag**: downward displacement limit in units, default 0.2. Upward rebound is limited to 15% of this value.
- **Frequency**: spring frequency in Hz, default 2.5.
- **Damping Ratio**: bounce damping, default 0.45; raise toward 1 for less oscillation.
- **Maximum Vertical Speed**: node movement cap in units/s, default 0.5.

The outer deck endpoints stay anchored. Boards rotate and stretch slightly between shared spring nodes so their top edges stay connected, including bridges with only one slice. Start and End can tilt around their outer anchored edges. The bridge updates before PlayerMovement, moves its colliders and carries standing simulated bodies through their normal collision sweeps. It does not reparent or teleport the player. An obstruction can prevent carrying. This bridge uses its own carrying logic rather than PropsMotion or PlayerPlatformRide.

## Manual checks in Unity

No automated tests or Play Mode checks were run for this change. In Sequence Room Test1:

1. Change Slice Count to 1, 5 and 10; inspect alignment and Undo, and verify that only middle slices are added/removed.
2. Place the bridge across a gap with enough clearance. Jump through it from below, land on it, walk across both directions and jump away.
3. Stand still and check subtle sag without repeated loss of ground contact; leave and watch the damped return. Observe that passing underneath causes no load response.
4. Adjust sag, frequency and damping; verify ceiling collision during upward carrying. Very large sag can create slopes steeper than the player's walkable-angle limit.
5. If using movable props, check their additional weight and support movement. Fixed endpoints follow the bridge root if it moves.
