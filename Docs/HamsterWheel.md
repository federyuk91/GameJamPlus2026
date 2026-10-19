# Hamster wheel interaction

StarvationDeathZone now represents an interactable wheel. Entering its trigger alone no longer starts the wheel, hides the player or increases hunger. PlayerWheelInteraction on the Player prefab selects a reachable wheel; GameplayInteractionAvailability shares F / controller East with collectibles and buttons, selecting the nearest action on that input. SYSTEM hints show the appropriate device sprite and “Press … to use the wheel” / “Press … to step off the wheel”. Scientist communications retain their existing priority.

Interacting consumes the press, clears movement/attachment state, disables PlayerMovement and automatic collection, hides the assigned SpriteRenderer and locks gameplay input except wheel interaction. Interacting again stops the wheel, restores the original component states and returns to the entry position. Movement and other action input remain suppressed until the exit interaction key is released, avoiding an accidental cut/dig on the same held key. Pause remains available. Disabling the wheel/player or ending the test releases wheel ownership and restores the player.

Optional Mount Point and Exit Point on StarvationDeathZone can specify precise world positions. Without them, entry/exit use the player's original position. The existing Animator reference and `isActive` bool are retained. Only the occupied wheel runs; nearby inactive wheels cannot override its animation or player visibility.

The wheel Animator switches immediately between Wheel Stay (`isActive = false`) and Mouse Run wheel (`isActive = true`), without exit-time delays or blending. The previous unconditional running self-transition was replaced by a return to Wheel Stay. The idle clip explicitly sets the original Wheel stay1 sprite so stopping restores the initial stationary wheel rather than retaining a running frame.

PlayerSatiety uses a separate wheel timer, losing one notch every Drain Interval while the wheel is occupied (default and LoopScene value: 1 second). Ordinary 60-second hunger progress is preserved while riding; wheel progress resets on each entry/exit. Pause stops hunger through scaled FixedUpdate. Reaching zero still invokes the existing starvation outcome. Using the wheel records TestObservation.Exercises for the current subject's notebook checklist.

Inspector references are assigned in Player.prefab and LoopScene. New manually created players need PlayerWheelInteraction wired to input/body/movement/visual/collector, plus the wheel reference on PlayerSatiety and GameplayInteractionAvailability.

No compilation or Play Mode tests were run. Review entry/exit with keyboard and controller, blocked jump/grab/cut/dig/collection, one notch per second, pause/resume while mounted, starvation, overlapping interactables/wheels and disabling an occupied wheel in Unity.
