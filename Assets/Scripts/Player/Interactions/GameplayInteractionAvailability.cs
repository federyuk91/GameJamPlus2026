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
        All = EatCheese | EatGreenCheese | CutRope | Dig | CollectPhoto | MoveCrate | PressButton | ClimbLedge | UseWheel
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
            float selectedDistance = collection == AvailableGameplayActions.PressButton ? buttons.TargetDistanceSquared
                : collection != AvailableGameplayActions.None ? collector.TargetDistanceSquared : float.PositiveInfinity;
            if (wheel != null && wheel.isActiveAndEnabled && wheel.InteractionTarget != null
                && wheel.InteractionTarget.CanUse && wheel.TargetDistanceSquared < selectedDistance)
            {
                available &= ~(AvailableGameplayActions.EatCheese | AvailableGameplayActions.EatGreenCheese
                    | AvailableGameplayActions.CollectPhoto | AvailableGameplayActions.PressButton);
                available |= AvailableGameplayActions.UseWheel;
                collection = AvailableGameplayActions.UseWheel;
            }
            // Press interactions win F/East; on gamepad this also excludes crate movement.
            if (collection != AvailableGameplayActions.None)
            {
                available &= ~(AvailableGameplayActions.CutRope | AvailableGameplayActions.Dig);
                if (usingGamepad) available &= ~AvailableGameplayActions.MoveCrate;
            }
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
            if (wheel != null) wheel.UsageChanged -= OnWheelUsageChanged;
            input?.SetSelectedInteractions(AvailableGameplayActions.All);
            SetAvailable(AvailableGameplayActions.None);
        }
    }
}
