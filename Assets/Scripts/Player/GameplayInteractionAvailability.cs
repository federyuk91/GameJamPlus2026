using System;
using UnityEngine;

namespace MousePlatformer
{
    [Flags]
    public enum AvailableGameplayActions { None = 0, EatCheese = 1, EatGreenCheese = 2, CutRope = 4, Dig = 8, CollectPhoto = 16, MoveCrate = 32 }

    [DefaultExecutionOrder(130), DisallowMultipleComponent]
    public sealed class GameplayInteractionAvailability : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CollectibleCollector collector;
        [SerializeField] private PlayerRopeBite ropeBite;
        [SerializeField] private PlayerDigging digging;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCrateInteraction crates;

        public AvailableGameplayActions Current { get; private set; }
        public event Action<AvailableGameplayActions> Changed;

        private void FixedUpdate()
        {
            AvailableGameplayActions available = AvailableGameplayActions.None;
            if (input != null && input.isActiveAndEnabled)
            {
                if (movement != null && movement.isActiveAndEnabled && movement.CanInteractWithCrates
                    && crates != null && crates.HasReachableCrate)
                    available |= AvailableGameplayActions.MoveCrate;
                Collectible collectible = collector != null && collector.isActiveAndEnabled ? collector.InteractionTarget : null;
                if (collectible != null && collectible.CanBeCollected)
                {
                    switch (collectible.Type)
                    {
                        case CollectibleType.Cheese:
                            available |= AvailableGameplayActions.EatCheese;
                            break;
                        case CollectibleType.GreenCheese:
                            available |= AvailableGameplayActions.EatGreenCheese;
                            break;
                        case CollectibleType.Photo:
                            available |= AvailableGameplayActions.CollectPhoto;
                            break;
                    }
                }

                // Cutting takes priority over digging on their shared action, exactly as in PlayerRopeBite.
                if (ropeBite != null && ropeBite.CanCutRope) available |= AvailableGameplayActions.CutRope;
                else if (ropeBite != null && ropeBite.CanStartDiggingWithSharedInput && digging != null && digging.CanStartDigging)
                    available |= AvailableGameplayActions.Dig;
            }
            SetAvailable(available);
        }

        private void SetAvailable(AvailableGameplayActions available)
        {
            if (Current == available) return;
            Current = available;
            Changed?.Invoke(Current);
        }

        private void OnDisable() => SetAvailable(AvailableGameplayActions.None);
    }
}
