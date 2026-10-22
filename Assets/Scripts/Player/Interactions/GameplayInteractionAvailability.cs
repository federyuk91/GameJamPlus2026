using System;
using UnityEngine;

namespace MousePlatformer
{
    [Flags]
    public enum AvailableGameplayActions
    {
        None = 0, EatCheese = 1, EatGreenCheese = 2, CutRope = 4, Dig = 8, CollectPhoto = 16, MoveCrate = 32,
        PressButton = 64,
        ClimbLedge = 128,
        UseWheel = 256,
        CollectItem = 512,
        All = EatCheese | EatGreenCheese | CutRope | Dig | CollectPhoto | MoveCrate | PressButton | ClimbLedge | UseWheel | CollectItem
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
        [SerializeField] private PlayerWheelInteraction wheel;
        public bool IsUsingWheel => wheel != null && wheel.IsUsing;

        public AvailableGameplayActions Current { get; private set; }
        public event Action<AvailableGameplayActions> Changed;
        private bool usingGamepad;

        private void OnEnable()
        {
            if (wheel != null) wheel.UsageChanged += OnWheelUsageChanged;
        }

        private void OnWheelUsageChanged()
        {
            AvailableGameplayActions previous = Current;
            Refresh();
            // Mounting changes the prompt from enter to exit without changing its action flag.
            if (previous == Current) Changed?.Invoke(Current);
        }

        public AvailableGameplayActions RefreshCurrent()
        {
            if (!isActiveAndEnabled) return AvailableGameplayActions.None;
            if (collector != null && collector.isActiveAndEnabled) collector.RefreshInteractionTarget();
            if (buttons != null && buttons.isActiveAndEnabled) buttons.RefreshInteractionTarget();
            if (wheel != null && wheel.isActiveAndEnabled) wheel.RefreshInteractionTarget();
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
            if (input != null && input.GameplayActionsLocked)
            {
                AvailableGameplayActions lockedActions = IsUsingWheel ? AvailableGameplayActions.UseWheel : AvailableGameplayActions.None;
                input.SetSelectedInteractions(lockedActions, usingGamepad);
                SetAvailable(lockedActions);
                return;
            }
            if (movement != null && movement.IsHangingFromLedge && input != null && input.isActiveAndEnabled)
            {
                input.SetSelectedInteractions(AvailableGameplayActions.ClimbLedge, usingGamepad);
                SetAvailable(AvailableGameplayActions.ClimbLedge);
                return;
            }
            AvailableGameplayActions available = AvailableGameplayActions.None;
            if (input != null && input.isActiveAndEnabled)
            {
                if (movement != null && movement.isActiveAndEnabled && movement.CanInteractWithCrates
                    && crates != null && crates.HasReachableCrate)
                    available |= AvailableGameplayActions.MoveCrate;
                Collectible food = collector != null && collector.isActiveAndEnabled ? collector.FoodTarget : null;
                if (food != null && food.CanBeCollected)
                    available |= food.Type == CollectibleType.Cheese ? AvailableGameplayActions.EatCheese : AvailableGameplayActions.EatGreenCheese;
                else if (ropeBite != null && ropeBite.CanCutRope) available |= AvailableGameplayActions.CutRope;
                else if (ropeBite != null && ropeBite.CanStartDiggingWithSharedInput && digging != null && digging.CanStartDigging)
                    available |= AvailableGameplayActions.Dig;
            }

            AvailableGameplayActions hands = AvailableGameplayActions.None;
            float handDistance = float.PositiveInfinity;
            Collectible item = collector != null && collector.isActiveAndEnabled ? collector.InteractionTarget : null;
            if (item != null && item.CanBeCollected)
            {
                hands = item.Type == CollectibleType.Photo ? AvailableGameplayActions.CollectPhoto : AvailableGameplayActions.CollectItem;
                handDistance = collector.TargetDistanceSquared;
            }
            if (buttons != null && buttons.isActiveAndEnabled && buttons.InteractionTarget != null
                && buttons.InteractionTarget.CanPress && buttons.TargetDistanceSquared < handDistance)
            {
                hands = AvailableGameplayActions.PressButton;
                handDistance = buttons.TargetDistanceSquared;
            }
            if (wheel != null && wheel.isActiveAndEnabled && wheel.InteractionTarget != null
                && wheel.InteractionTarget.CanUse && wheel.TargetDistanceSquared < handDistance)
                hands = AvailableGameplayActions.UseWheel;
            // Front-paw press actions share a key with holding props. Mouth actions are independent.
            if (hands != AvailableGameplayActions.None)
            {
                available &= ~AvailableGameplayActions.MoveCrate;
                available |= hands;
            }
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
            if (wheel != null) wheel.UsageChanged -= OnWheelUsageChanged;
            input?.SetSelectedInteractions(AvailableGameplayActions.All);
            SetAvailable(AvailableGameplayActions.None);
        }
    }
}
