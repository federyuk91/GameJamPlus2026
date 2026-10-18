using MousePlatformer;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class SatietyHud : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private PlayerSatiety satiety;
        [SerializeField] private PlayerOxygen oxygen;
        [SerializeField] private PlayerOxygenBubbles oxygenBubbles;
        [SerializeField] private PlatformCameraController gameplayCamera;
        [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.25f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.35f;
        [SerializeField] private Color oxygenFlashColor = new Color(0.35f, 0.8f, 1f, 0.16f);
        [SerializeField, Min(0.01f)] private float oxygenFlashDuration = 0.25f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.2f;
        [SerializeField, Min(0f)] private float shakeAmplitude = 0.06f;
        private VisualElement root;
        private VisualElement notches;
        private Label caption;
        private VisualElement flash;
        private float flashRemaining;
        private VisualElement oxygenFlash;
        private float oxygenFlashRemaining;
        private bool oxygenBubblesPending;

        private void OnEnable()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (satiety != null)
            {
                satiety.Changed += OnResourceChanged;
                satiety.NotchLost += OnNotchLost;
            }
            if (oxygen != null)
            {
                oxygen.Changed += OnResourceChanged;
                oxygen.NotchLost += OnOxygenNotchLost;
            }
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
                caption = root?.Q<Label>("satiety-caption");
                if (root != null)
                {
                    flash = CreateFlash("satiety-flash", flashColor);
                    oxygenFlash = CreateFlash("oxygen-flash", oxygenFlashColor);
                    RefreshResources();
                }
            }
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

        private void RefreshNotches(int current, int maximum)
        {
            if (notches == null) return;
            if (notches.childCount != maximum)
            {
                notches.Clear();
                for (int index = 0; index < maximum; index++)
                {
                    VisualElement notch = new VisualElement { pickingMode = PickingMode.Ignore };
                    notch.AddToClassList("satiety-notch");
                    notches.Add(notch);
                }
            }
            for (int index = 0; index < notches.childCount; index++)
                notches[index].EnableInClassList("satiety-notch-empty", index >= current);
        }

        private void OnResourceChanged(int current, int maximum) => RefreshResources();

        private void RefreshResources()
        {
            bool showOxygen = oxygen != null && oxygen.IsUnderwater;
            if (caption != null) caption.text = showOxygen ? "OXYGEN" : "SATIETY";
            if (showOxygen) RefreshNotches(oxygen.Current, oxygen.Maximum);
            else if (satiety != null) RefreshNotches(satiety.Current, satiety.Maximum);
            notches?.EnableInClassList("oxygen-notches", showOxygen);
        }

        private void OnNotchLost()
        {
            flashRemaining = flashDuration;
            if (gameplayCamera != null) gameplayCamera.PlayShake(shakeDuration, shakeAmplitude);
        }

        private void OnDisable()
        {
            if (satiety != null)
            {
                satiety.Changed -= OnResourceChanged;
                satiety.NotchLost -= OnNotchLost;
            }
            if (oxygen != null)
            {
                oxygen.Changed -= OnResourceChanged;
                oxygen.NotchLost -= OnOxygenNotchLost;
            }
            flash?.RemoveFromHierarchy();
            oxygenFlash?.RemoveFromHierarchy();
            root = notches = flash = null;
            caption = null;
            flashRemaining = 0f;
            oxygenFlash = null;
            oxygenFlashRemaining = 0f;
            oxygenBubblesPending = false;
        }
    }
}
