using System;
using System.Collections.Generic;
using GameJamPlus.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace MousePlatformer
{
    [DefaultExecutionOrder(300), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class PlayerStats : MonoBehaviour
    {
        public const int MaximumHealth = 5;

        [Header("References")]
        [SerializeField, Tooltip("Collider used to locate the animal's breathing point in water.")]
        private Collider2D playerCollider;
        [SerializeField, Tooltip("Collector that notifies this animal when it eats cheese.")]
        private CollectibleCollector collector;
        [SerializeField, Tooltip("Wheel interaction that applies its own satiety drain interval while in use.")]
        private PlayerWheelInteraction wheel;
        [SerializeField] private PlayerMovement movement;
        [Header("Simulation")]
        [SerializeField, Tooltip("Enable hunger consumption and cheese effects for this animal.")]
        private bool satietyEnabled = true;
        [SerializeField, Tooltip("Enable oxygen consumption, recovery and drowning for this animal.")]
        private bool oxygenEnabled = true;
        [SerializeField, Tooltip("Enable electrical charges, paralysis and charge decay for this animal.")]
        private bool electricityEnabled = true;

        [Header("Health")]
        [SerializeField, Range(0, MaximumHealth), Tooltip("Health of this animal, displayed as up to five hearts.")]
        private int health = MaximumHealth;
        public int Health => health;
        public bool IsDead => health <= 0;
        public bool IsHidden => movement != null && movement.IsHidden;
        public bool IsMovingWhileHidden => movement != null && movement.IsMovingWhileHidden;
        public bool CanBeSeenByEnemies => !IsHidden || IsMovingWhileHidden;
        public Collider2D HiddenCoverCollider => movement != null && movement.SnakeBoxInteraction != null
            ? movement.SnakeBoxInteraction.CoverCollider : null;
        public event Action HealthChanged;
        public event Action Damaged;

        [Header("Satiety")]
        [SerializeField, Tooltip("Central conclusion when satiety reaches zero.")]
        private StageEndCase starvationEnding = StageEndCase.Starvation;
        [SerializeField, Min(1), Tooltip("Maximum satiety bars for this animal.")] private int maximumSatiety = 5;
        [SerializeField, Min(0.1f), Tooltip("Gameplay seconds between hunger losses.")] private float satietyDrainInterval = 60f;
        [Header("Poison")]
        [SerializeField, Tooltip("Outcome if the immediate poison damage consumes the last heart. Uses Starvation Ending when set to None.")]
        private StageEndCase poisonEnding = StageEndCase.None;
        [SerializeField, Min(0.1f), Tooltip("Seconds between satiety losses caused by green cheese poisoning. Pauses with gameplay.")]
        private float poisonInterval = 3f;
        [SerializeField, Min(1), Tooltip("Number of one-point satiety losses caused by green cheese. Another green cheese refreshes this count; normal cheese cures it.")]
        private int poisonTicks = 5;
        private float drainProgress;
        private float wheelDrainProgress;
        private float poisonElapsed;
        private int poisonTicksRemaining;

        public int Satiety { get; private set; }
        public int MaximumSatiety => Mathf.Max(1, maximumSatiety);
        public float VisibleSatiety
        {
            get
            {
                if (!satietyEnabled || Satiety <= 0) return Satiety;
                float progress = wheel != null && wheel.IsUsing
                    ? wheelDrainProgress / wheel.DrainInterval : drainProgress;
                if (IsPoisoned) progress = Mathf.Max(progress, poisonElapsed / Mathf.Max(0.1f, poisonInterval));
                return Mathf.Max(0f, Satiety - Mathf.Clamp01(progress));
            }
        }
        public bool IsPoisoned => poisonTicksRemaining > 0;
        public bool LastSatietyLossWasPoison { get; private set; }
        public event Action<int, int> SatietyChanged;
        public event Action SatietyLost;

        [Header("Electricity")]
        [SerializeField, Min(1), Tooltip("Accumulated lightning charges that kill this animal.")]
        private int lethalCharges = 3;
        [SerializeField, Min(0.1f), Tooltip("Gameplay seconds before removing one electrical charge.")] private float electricityDecayInterval = 60f;
        private float decayElapsed;
        private readonly HashSet<UnityEngine.Object> activeShocks = new HashSet<UnityEngine.Object>();

        public int ElectricCharges { get; private set; }
        public int MaximumElectricCharges => Mathf.Max(1, lethalCharges);
        public bool IsElectricallyLethal => ElectricCharges >= MaximumElectricCharges;
        public bool IsParalyzed => electricityEnabled && activeShocks.Count > 0;
        public event Action<int> ElectricityChanged;

        [Header("Oxygen")]
        [SerializeField, Tooltip("Conclusion requested from the central StageEndManager. None disables this ending.")]
        private StageEndCase drowningEnding = StageEndCase.Drowning;
        [SerializeField, Min(1), Tooltip("Maximum oxygen notches for this animal.")] private int maximumOxygen = 5;
        [SerializeField, Min(0.1f), Tooltip("Seconds underwater before losing one oxygen notch.")] private float oxygenDrainInterval = 5f;
        [SerializeField, Min(0.1f), Tooltip("Seconds above water needed to continuously recover one full oxygen notch. Partial recovery is retained when submerging again.")] private float oxygenRecoveryInterval = 1f;
        [SerializeField, Min(0f), Tooltip("Secondi consecutivi con il punto di respirazione fuori dall'acqua prima di iniziare il recupero. Una nuova immersione azzera l'attesa; la pausa sospende il timer.")]
        private float oxygenRecoveryDelay = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("Breathing point height within the collider, from bottom (0) to top (1).")] private float breathingPointHeight = 0.85f;
        [SerializeField, Min(0f), Tooltip("Remaining oxygen time that triggers the low oxygen notification.")] private float warningSeconds = 10f;
        [SerializeField, Tooltip("Invoked once when remaining underwater oxygen reaches the warning threshold.")] private UnityEvent onLowOxygen = new UnityEvent();
        private float drainElapsed;
        private float aboveWaterElapsed;
        private bool warningIssued;

        public int Oxygen { get; private set; }
        public int MaximumOxygen => Mathf.Max(1, maximumOxygen);
        public float VisibleOxygen => Mathf.Clamp(Oxygen - drainElapsed / Mathf.Max(0.1f, oxygenDrainInterval), 0f, MaximumOxygen);
        public bool IsOxygenFull => Oxygen >= MaximumOxygen && drainElapsed <= 0f;
        public bool IsUnderwater { get; private set; }
        public float OxygenSecondsRemaining => Mathf.Max(0f, Oxygen * Mathf.Max(0.1f, oxygenDrainInterval) - drainElapsed);
        public event Action<int, int> OxygenChanged;
        public event Action OxygenLost;

        private void Awake()
        {
            if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
            if (collector == null) collector = GetComponent<CollectibleCollector>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            Satiety = MaximumSatiety;
            Oxygen = MaximumOxygen;
        }

        private void OnEnable()
        {
            if (collector != null) collector.Collected += OnCollected;
            if (wheel != null) wheel.UsageChanged += ResetWheelDrain;
        }

        private void OnDisable()
        {
            if (collector != null) collector.Collected -= OnCollected;
            if (wheel != null) wheel.UsageChanged -= ResetWheelDrain;
            activeShocks.Clear();
        }

        private void FixedUpdate()
        {
            TickElectricity();
            TickSatiety();
            TickOxygen();
        }

        public bool ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDead) return false;
            SetHealth(health - amount);
            Damaged?.Invoke();
            return true;
        }

        public void SetHealth(int value)
        {
            int next = Mathf.Clamp(value, 0, MaximumHealth);
            if (health == next) return;
            health = next;
            HealthChanged?.Invoke();
        }

        private void OnValidate()
        {
            health = Mathf.Clamp(health, 0, MaximumHealth);
            HealthChanged?.Invoke();
        }

        private void ResetWheelDrain() => wheelDrainProgress = 0f;

        private void TickPoison(float deltaTime)
        {
            if (!IsPoisoned) return;
            poisonElapsed += deltaTime;
            if (poisonElapsed < Mathf.Max(0.1f, poisonInterval)) return;
            poisonElapsed -= Mathf.Max(0.1f, poisonInterval);
            poisonTicksRemaining--;
            LoseNotch(true);
        }

        private void LoseNotch(bool fromPoison = false)
        {
            LastSatietyLossWasPoison = fromPoison;
            Satiety = Mathf.Max(0, Satiety - 1);
            SatietyChanged?.Invoke(Satiety, MaximumSatiety);
            SatietyLost?.Invoke();
            if (Satiety == 0) StageEndManager.Request(starvationEnding, this);
        }

        private void OnCollected(Collectible collectible)
        {
            if (!satietyEnabled) return;
            CollectibleType type = collectible.Type;
            if (type == CollectibleType.Cheese) RestoreFull();
            else if (type == CollectibleType.GreenCheese) RestoreAndPoison();
        }

        public void RestoreFull()
        {
            if (!satietyEnabled || Satiety <= 0) return;
            poisonTicksRemaining = 0;
            poisonElapsed = 0f;
            Refill();
        }

        private void RestoreAndPoison()
        {
            if (Satiety <= 0) return;
            poisonTicksRemaining = Mathf.Max(1, poisonTicks);
            poisonElapsed = 0f;
            Refill();
            if (ApplyDamage(1) && IsDead)
            {
                StageEndCase ending = poisonEnding != StageEndCase.None ? poisonEnding : starvationEnding;
                StageEndManager.Request(ending, this);
            }
        }

        private void Refill()
        {
            Satiety = MaximumSatiety;
            drainProgress = 0f;
            wheelDrainProgress = 0f;
            SatietyChanged?.Invoke(Satiety, MaximumSatiety);
        }

        public void BeginShock(UnityEngine.Object source)
        {
            if (electricityEnabled && isActiveAndEnabled && source != null) activeShocks.Add(source);
        }

        public void EndShock(UnityEngine.Object source)
        {
            if (source != null) activeShocks.Remove(source);
        }

        public bool ReceiveShock(int amount)
        {
            if (!electricityEnabled || !isActiveAndEnabled || amount <= 0 || IsElectricallyLethal) return false;
            if (ElectricCharges == 0) decayElapsed = 0f;
            ElectricCharges += Mathf.Min(amount, MaximumElectricCharges - ElectricCharges);
            GameSession.RecordObservation(TestObservation.Electrician);
            ElectricityChanged?.Invoke(ElectricCharges);
            return true;
        }

        public bool ConsumeOxygenNotch()
        {
            GameManager manager = GameManager.Instance;
            if (!oxygenEnabled || !isActiveAndEnabled || Oxygen <= 0 || manager == null
                || manager.State != GameManager.LoopState.Playing) return false;
            Oxygen--;
            OxygenLost?.Invoke();
            OxygenChanged?.Invoke(Oxygen, MaximumOxygen);
            CheckOxygenState();
            return true;
        }

        private void CheckOxygenState()
        {
            if (IsUnderwater && Oxygen > 0 && !warningIssued && OxygenSecondsRemaining <= warningSeconds)
            {
                warningIssued = true;
                onLowOxygen?.Invoke();
            }
            if (Oxygen == 0 && drowningEnding != StageEndCase.None) StageEndManager.Request(drowningEnding, this);
        }

        private void TickElectricity()
        {
            if (!electricityEnabled) return;
            GameManager manager = GameManager.Instance;
            if (ElectricCharges <= 0 || IsElectricallyLethal || manager == null || manager.State != GameManager.LoopState.Playing) return;
            decayElapsed += Time.fixedDeltaTime;
            if (decayElapsed < Mathf.Max(0.1f, electricityDecayInterval)) return;
            decayElapsed -= Mathf.Max(0.1f, electricityDecayInterval);
            ElectricCharges--;
            if (ElectricCharges == 0) decayElapsed = 0f;
            ElectricityChanged?.Invoke(ElectricCharges);
        }

        private void TickSatiety()
        {
            if (!satietyEnabled) return;
            GameManager manager = GameManager.Instance;
            if (Satiety <= 0 || manager == null || manager.State != GameManager.LoopState.Playing) return;
            TickPoison(Time.fixedDeltaTime);
            if (Satiety <= 0 || manager.State != GameManager.LoopState.Playing) return;
            float interval = Mathf.Max(0.1f, satietyDrainInterval);
            if (wheel != null && wheel.IsUsing)
            {
                wheelDrainProgress += Time.fixedDeltaTime;
                if (wheelDrainProgress < wheel.DrainInterval) return;
                wheelDrainProgress -= wheel.DrainInterval;
            }
            else
            {
                wheelDrainProgress = 0f;
                float sprintMultiplier = movement != null && movement.IsSprinting ? 1.1f : 1f;
                drainProgress += Time.fixedDeltaTime * sprintMultiplier / interval;
                if (drainProgress < 1f) return;
                drainProgress -= 1f;
            }
            LoseNotch();
        }

        private void TickOxygen()
        {
            if (!oxygenEnabled) return;
            GameManager manager = GameManager.Instance;
            if (Oxygen <= 0 || manager == null || manager.State != GameManager.LoopState.Playing
                || playerCollider == null || !playerCollider.enabled) return;
            Physics2D.SyncTransforms();
            Bounds bounds = playerCollider.bounds;
            Vector2 breathingPoint = new Vector2(bounds.center.x,
                Mathf.Lerp(bounds.min.y, bounds.max.y, breathingPointHeight));
            bool underwater = WaterVolume.ContainsPoint(breathingPoint);
            bool changed = underwater != IsUnderwater;
            IsUnderwater = underwater;
            float recoveryDeltaTime = 0f;
            if (underwater) aboveWaterElapsed = 0f;
            else
            {
                float delay = Mathf.Max(0f, oxygenRecoveryDelay);
                float remainingDelay = Mathf.Max(0f, delay - aboveWaterElapsed);
                aboveWaterElapsed = Mathf.Min(delay, aboveWaterElapsed + Time.fixedDeltaTime);
                // Nel tick che termina l'attesa recupera solo per il tempo residuo.
                recoveryDeltaTime = Mathf.Max(0f, Time.fixedDeltaTime - remainingDelay);
            }
            if (underwater)
            {
                drainElapsed += Time.fixedDeltaTime;
                if (drainElapsed >= Mathf.Max(0.1f, oxygenDrainInterval))
                {
                    drainElapsed -= Mathf.Max(0.1f, oxygenDrainInterval);
                    Oxygen--;
                    changed = true;
                    OxygenLost?.Invoke();
                }
            }
            else if (recoveryDeltaTime > 0f && (Oxygen < MaximumOxygen || drainElapsed > 0f))
            {
                // Usa la stessa frazione della barretta consumata: il recupero rimane
                // continuo e reale anche quando il topo torna subito sott'acqua.
                drainElapsed -= recoveryDeltaTime * Mathf.Max(0.1f, oxygenDrainInterval)
                    / Mathf.Max(0.1f, oxygenRecoveryInterval);
                while (drainElapsed < 0f && Oxygen < MaximumOxygen)
                {
                    Oxygen++;
                    drainElapsed += Mathf.Max(0.1f, oxygenDrainInterval);
                    changed = true;
                }
                if (Oxygen == MaximumOxygen && drainElapsed <= 0f)
                {
                    drainElapsed = 0f;
                    warningIssued = false;
                    changed = true;
                }
            }
            if (changed) OxygenChanged?.Invoke(Oxygen, MaximumOxygen);
            CheckOxygenState();
        }
    }
}
