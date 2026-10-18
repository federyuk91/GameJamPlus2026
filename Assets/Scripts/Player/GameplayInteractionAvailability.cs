using System;
using UnityEngine;

namespace MousePlatformer
{
    [Flags]
    public enum AvailableGameplayActions
    {
        None = 0, EatCheese = 1, EatGreenCheese = 2, CutRope = 4, Dig = 8, CollectPhoto = 16, MoveCrate = 32,
        PressButton = 64,
        All = EatCheese | EatGreenCheese | CutRope | Dig | CollectPhoto | MoveCrate | PressButton
    }

    [DefaultExecutionOrder(130), DisallowMultipleComponent]
    public sealed class GameplayInteractionAvailability : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CollectibleCollector collector;
        [SerializeField] private PlayerRopeBite ropeBite;
        [SerializeField] private PlayerDigging digging;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCrateInteraction crates;
        [SerializeField] private PlayerButtonInteraction buttons;

        public AvailableGameplayActions Current { get; private set; }
        public event Action<AvailableGameplayActions> Changed;
        private bool usingGamepad;

        public AvailableGameplayActions RefreshCurrent()
        {
            if (!isActiveAndEnabled) return AvailableGameplayActions.None;
            if (collector != null && collector.isActiveAndEnabled) collector.RefreshInteractionTarget();
            if (buttons != null && buttons.isActiveAndEnabled) buttons.RefreshInteractionTarget();
            Refresh();
            return Current;
        }

        public void SetUsingGamepad(bool value)
        {
            usingGamepad = value;
            Refresh();
        }

        private void FixedUpdate()
        {
            Refresh();
        }

        private void Refresh()
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
            AvailableGameplayActions collection = available & (AvailableGameplayActions.EatCheese
                | AvailableGameplayActions.EatGreenCheese | AvailableGameplayActions.CollectPhoto);
            if (buttons != null && buttons.isActiveAndEnabled && buttons.InteractionTarget != null
                && buttons.InteractionTarget.CanPress
                && (collection == AvailableGameplayActions.None || buttons.TargetDistanceSquared < collector.TargetDistanceSquared))
            {
                available &= ~collection;
                available |= AvailableGameplayActions.PressButton;
                collection = AvailableGameplayActions.PressButton;
            }
            // The nearest collectible/button wins its physical input; other controls remain available.
            if (!usingGamepad && collection != AvailableGameplayActions.None)
                available &= ~(AvailableGameplayActions.CutRope | AvailableGameplayActions.Dig);
            // Crate movement and cutting/digging share East on the gamepad.
            if (usingGamepad && (available & AvailableGameplayActions.MoveCrate) != 0)
                available &= ~(AvailableGameplayActions.CutRope | AvailableGameplayActions.Dig);
            input?.SetSelectedInteractions(available, usingGamepad);
            SetAvailable(available);
        }

        private void SetAvailable(AvailableGameplayActions available)
        {
            if (Current == available) return;
            Current = available;
            Changed?.Invoke(Current);
        }

        private void OnDisable()
        {
            input?.SetSelectedInteractions(AvailableGameplayActions.All);
            SetAvailable(AvailableGameplayActions.None);
        }
    }
}
