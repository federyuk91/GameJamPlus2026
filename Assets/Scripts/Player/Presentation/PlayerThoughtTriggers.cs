using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(310), DisallowMultipleComponent, RequireComponent(typeof(PlayerThoughtBalloons))]
    public sealed class PlayerThoughtTriggers : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerThoughtBalloons balloons;
        [SerializeField] private GameplayInteractionAvailability interactionAvailability;

        [SerializeField] private Sprite airLosses;
        [SerializeField] private Sprite poison;
        [SerializeField] private Sprite cheese;
        [SerializeField] private Sprite interactionLeft;
        [SerializeField] private Sprite interactionRight;
        [SerializeField, Min(0f)] private float interactionDelay = 3f;

        private const float StationarySpeed = 0.05f;
        private int lastOxygen;
        private int oxygenLosses;
        private int poisonedSatietyLosses;
        private Vector2 previousPosition;
        private AvailableGameplayActions idleActions;
        private float stationarySeconds;
        private bool showingInteraction;
        private bool wasPlaying;

        private void Awake()
        {
            if (balloons == null) balloons = GetComponent<PlayerThoughtBalloons>();
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
            wasPlaying = CanShowThoughts();
            if (stats == null) return;
            lastOxygen = stats.Oxygen;
            stats.OxygenLost += OnOxygenLost;
            stats.OxygenChanged += OnOxygenChanged;
            stats.PoisonStarted += OnPoisonStarted;
            stats.SatietyLost += OnSatietyLost;
        }

        private void OnDisable()
        {
            if (stats != null)
            {
                stats.OxygenLost -= OnOxygenLost;
                stats.OxygenChanged -= OnOxygenChanged;
                stats.PoisonStarted -= OnPoisonStarted;
                stats.SatietyLost -= OnSatietyLost;
            }
            ResetInteraction();
            oxygenLosses = 0;
            poisonedSatietyLosses = 0;
            wasPlaying = false;
        }

        private void OnOxygenLost()
        {
            if (!stats.IsUnderwater || stats.Oxygen <= 0) return;
            oxygenLosses++;
            if (oxygenLosses < 2 && stats.Oxygen != 1) return;
            oxygenLosses = 0;
            balloons.Show(airLosses);
        }

        private void OnOxygenChanged(int oxygen, int maximum)
        {
            if (oxygen > lastOxygen) oxygenLosses = 0;
            lastOxygen = oxygen;
        }

        private void OnPoisonStarted()
        {
            poisonedSatietyLosses = 0;
            balloons.Show(poison);
        }

        private void OnSatietyLost()
        {
            if (stats.IsPoisoned || stats.LastSatietyLossWasPoison)
            {
                poisonedSatietyLosses++;
                if (poisonedSatietyLosses < 2) return;
                poisonedSatietyLosses = 0;
                balloons.Show(poison);
            }
            else if (stats.Satiety <= 3)
            {
                balloons.Show(cheese);
            }
        }

        private void FixedUpdate()
        {
            Vector2 position = transform.position;
            Vector2 displacement = position - previousPosition;
            previousPosition = position;

            GameManager manager = GameManager.Instance;
            GameplayInteractionAvailability availability = interactionAvailability;
            if (availability == null && manager != null && manager.Hud != null)
                availability = manager.Hud.InteractionAvailability;
            AvailableGameplayActions available = availability != null && availability.isActiveAndEnabled
                ? availability.Current : AvailableGameplayActions.None;
            float stationaryDistance = StationarySpeed * Time.fixedDeltaTime;
            if (!CanShowThoughts() || available == AvailableGameplayActions.None
                || displacement.sqrMagnitude > stationaryDistance * stationaryDistance)
            {
                ResetInteraction();
                return;
            }

            if (available != idleActions)
            {
                ResetInteraction();
                idleActions = available;
                return;
            }
            if (showingInteraction) return;
            stationarySeconds += Time.fixedDeltaTime;
            if (stationarySeconds < interactionDelay) return;

            showingInteraction = true;
            balloons.SetPersistent(interactionLeft, interactionRight);
        }

        private void LateUpdate()
        {
            bool playing = CanShowThoughts();
            if (wasPlaying && !playing)
            {
                ResetInteraction();
                balloons.Clear();
            }
            wasPlaying = playing;
        }

        private bool CanShowThoughts()
        {
            GameManager manager = GameManager.Instance;
            return (manager == null || manager.State == GameManager.LoopState.Playing)
                && (stats == null || !stats.IsDead);
        }

        private void ResetInteraction()
        {
            idleActions = AvailableGameplayActions.None;
            stationarySeconds = 0f;
            if (!showingInteraction) return;
            showingInteraction = false;
            balloons.SetPersistent(null);
        }
    }
}
