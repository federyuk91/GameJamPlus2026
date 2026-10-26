using MousePlatformer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class SatietyHud : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerOxygenBubbles oxygenBubbles;
        [SerializeField] private PlatformCameraController gameplayCamera;
        [SerializeField, Tooltip("Screen tint when a satiety notch is lost and hunger increases.")]
        private Color flashColor = new Color(1f, 0.85f, 0.1f, 0.25f);
        [SerializeField, Tooltip("Screen tint for a crushing impact, independent of hunger and oxygen warnings.")]
        private Color damageFlashColor = new Color(1f, 0f, 0f, 0.25f);
        [SerializeField, Tooltip("Green screen tint when poisoning consumes a satiety notch, including its final tick.")]
        private Color poisonFlashColor = new Color(64f / 255f, 201f / 255f, 119f / 255f, 0.25f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.35f;
        [SerializeField, Tooltip("White and pale-blue screen tint when an oxygen notch is consumed.")]
        private Color oxygenFlashColor = new Color(0.8f, 0.94f, 1f, 0.2f);
        [SerializeField, Tooltip("Pale-blue screen flash on electrical impact, separate from oxygen bubble feedback.")]
        private Color shockFlashColor = new Color(0.55f, 0.85f, 1f, 0.3f);
        [SerializeField, Min(0.01f)] private float oxygenFlashDuration = 0.25f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.2f;
        [SerializeField, Min(0f)] private float shakeAmplitude = 0.06f;
        private VisualElement root;
        private VisualElement notches;
        private VisualElement oxygenNotches;
        private VisualElement oxygenPanel;
        private VisualElement hearts;
        private Label caption;
        private VisualElement flash;
        private float flashRemaining;
        private VisualElement oxygenFlash;
        private float oxygenFlashRemaining;
        private bool oxygenBubblesPending;
        private Color activeFlashColor;
        private VisualElement poisonStatus;
        private VisualElement statusPanel;
        private bool lowHealth;
        private bool lowSatiety;
        private bool lowOxygen;
        private int previousElectricity;
        private VisualElement electricityStatusHost;
        private readonly List<StatusEffectIcon> electricityIcons = new List<StatusEffectIcon>(3);

        private void OnEnable()
        {
            activeFlashColor = flashColor;
            if (document == null) document = GetComponent<UIDocument>();
            if (stats != null)
            {
                stats.HealthChanged += RefreshHealth;
                stats.Damaged += PlayDamageFlash;
                stats.SatietyChanged += OnResourceChanged;
                stats.SatietyLost += OnNotchLost;
                stats.OxygenChanged += OnResourceChanged;
                stats.OxygenLost += OnOxygenNotchLost;
                stats.ElectricityChanged += OnElectricityChanged;
            }
            previousElectricity = stats != null ? stats.ElectricCharges : 0;
        }

        private void LateUpdate()
        {
            VisualElement currentRoot = document != null ? document.rootVisualElement : null;
            if (currentRoot != root)
            {
                flash?.RemoveFromHierarchy();
                oxygenFlash?.RemoveFromHierarchy();
                root = currentRoot;
                notches = root?.Q("satiety-notches");
                oxygenNotches = root?.Q("oxygen-notches");
                oxygenPanel = root?.Q("oxygen-panel");
                hearts = root?.Q("health-hearts");
                caption = root?.Q<Label>("satiety-caption");
                poisonStatus = root?.Q("status-poison");
                statusPanel = root?.Q("status-panel");
                electricityStatusHost = root?.Q("status-electricity-icons");
                electricityIcons.Clear();
                if (electricityStatusHost != null)
                    electricityStatusHost.Query<StatusEffectIcon>().ForEach(icon => electricityIcons.Add(icon));
                if (root != null)
                {
                    flash = CreateFlash("satiety-flash", activeFlashColor);
                    oxygenFlash = CreateFlash("oxygen-flash", oxygenFlashColor);
                    RefreshResources();
                }
            }
            float warningOpacity = Mathf.Lerp(0.55f, 1f, (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.5f);
            if (hearts != null) hearts.style.opacity = lowHealth ? warningOpacity : 1f;
            if (notches != null) notches.style.opacity = lowSatiety ? warningOpacity : 1f;
            if (oxygenNotches != null) oxygenNotches.style.opacity = lowOxygen ? warningOpacity : 1f;
            if (flash == null) return;
            flash.style.display = flashRemaining > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            flash.style.opacity = Mathf.Clamp01(flashRemaining / Mathf.Max(0.01f, flashDuration));
            flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);
            if (oxygenFlash == null) return;
            oxygenFlash.style.display = oxygenFlashRemaining > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            oxygenFlash.style.opacity = Mathf.Clamp01(oxygenFlashRemaining / Mathf.Max(0.01f, oxygenFlashDuration));
            oxygenFlashRemaining = Mathf.Max(0f, oxygenFlashRemaining - Time.unscaledDeltaTime);
            if (oxygenBubblesPending && oxygenFlashRemaining <= 0f)
            {
                oxygenBubblesPending = false;
                if (oxygenBubbles != null) oxygenBubbles.PlayBurst();
            }
        }

        private VisualElement CreateFlash(string name, Color color)
        {
            VisualElement overlay = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            overlay.style.position = Position.Absolute;
            overlay.style.left = overlay.style.right = overlay.style.top = overlay.style.bottom = 0f;
            overlay.style.backgroundColor = color;
            root.Add(overlay);
            return overlay;
        }

        private void OnOxygenNotchLost()
        {
            oxygenFlashRemaining = oxygenFlashDuration;
            oxygenBubblesPending = true;
        }

        private void RefreshNotches(VisualElement host, int current, int maximum)
        {
            if (host == null) return;
            if (host.childCount != maximum)
            {
                host.Clear();
                for (int index = 0; index < maximum; index++)
                {
                    VisualElement notch = new VisualElement { pickingMode = PickingMode.Ignore };
                    notch.AddToClassList("satiety-notch");
                    host.Add(notch);
                }
            }
            for (int index = 0; index < host.childCount; index++)
                host[index].EnableInClassList("satiety-notch-empty", index >= current);
        }

        private void OnResourceChanged(int current, int maximum) => RefreshResources();

        private void RefreshResources()
        {
            bool showOxygen = stats != null && (stats.IsUnderwater || stats.Oxygen < stats.MaximumOxygen);
            lowSatiety = stats != null && stats.Satiety == 1;
            lowOxygen = showOxygen && stats.Oxygen == 1;
            if (caption != null) caption.text = "SATIETY";
            if (stats != null) RefreshNotches(notches, stats.Satiety, stats.MaximumSatiety);
            if (oxygenPanel != null) oxygenPanel.style.display = showOxygen ? DisplayStyle.Flex : DisplayStyle.None;
            if (showOxygen) RefreshNotches(oxygenNotches, stats.Oxygen, stats.MaximumOxygen);
            RefreshHealth();
            RefreshStatus();
        }

        private void RefreshHealth()
        {
            if (hearts == null) return;
            int current = stats != null ? stats.Health : PlayerStats.MaximumHealth;
            lowHealth = current == 1;
            for (int index = 0; index < hearts.childCount; index++)
                hearts[index].EnableInClassList("health-heart-empty", index >= current);
        }

        private void RefreshStatus()
        {
            bool poisoned = stats != null && stats.IsPoisoned;
            notches?.EnableInClassList("poisoned-notches", poisoned);
            if (poisonStatus != null)
                poisonStatus.style.display = poisoned ? DisplayStyle.Flex : DisplayStyle.None;
            int charges = stats != null ? stats.ElectricCharges : 0;
            if (statusPanel != null) statusPanel.style.display = poisoned || charges > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (electricityStatusHost != null) electricityStatusHost.style.display = charges > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            while (electricityStatusHost != null && electricityIcons.Count < charges)
            {
                StatusEffectIcon icon = new StatusEffectIcon { Effect = StatusEffectIcon.EffectKind.Electricity };
                icon.AddToClassList("status-effect-icon");
                electricityStatusHost.Add(icon);
                electricityIcons.Add(icon);
            }
            for (int index = 0; index < electricityIcons.Count; index++)
                electricityIcons[index].style.display = index < charges ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnElectricityChanged(int charges)
        {
            RefreshStatus();
            if (charges > previousElectricity) PlayFlash(shockFlashColor);
            previousElectricity = charges;
        }

        private void OnNotchLost()
        {
            PlayFlash(stats != null && stats.LastSatietyLossWasPoison ? poisonFlashColor : flashColor);
        }

        public void PlayDamageFlash() => PlayFlash(damageFlashColor);

        private void PlayFlash(Color color)
        {
            activeFlashColor = color;
            if (flash != null) flash.style.backgroundColor = color;
            flashRemaining = flashDuration;
            if (gameplayCamera != null) gameplayCamera.PlayShake(shakeDuration, shakeAmplitude);
        }

        private void OnDisable()
        {
            if (stats != null)
            {
                stats.SatietyChanged -= OnResourceChanged;
                stats.SatietyLost -= OnNotchLost;
                stats.OxygenChanged -= OnResourceChanged;
                stats.OxygenLost -= OnOxygenNotchLost;
                stats.ElectricityChanged -= OnElectricityChanged;
                stats.HealthChanged -= RefreshHealth;
                stats.Damaged -= PlayDamageFlash;
            }
            flash?.RemoveFromHierarchy();
            oxygenFlash?.RemoveFromHierarchy();
            root = notches = flash = null;
            caption = null;
            flashRemaining = 0f;
            oxygenFlash = null;
            oxygenFlashRemaining = 0f;
            oxygenBubblesPending = false;
            poisonStatus = electricityStatusHost = statusPanel = null;
            lowHealth = lowSatiety = lowOxygen = false;
            electricityIcons.Clear();
            hearts = oxygenNotches = oxygenPanel = null;
        }
    }
}
