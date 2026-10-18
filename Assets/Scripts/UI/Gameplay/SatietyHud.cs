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
        [SerializeField] private PlatformCameraController gameplayCamera;
        [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.25f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.35f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.2f;
        [SerializeField, Min(0f)] private float shakeAmplitude = 0.06f;
        private VisualElement root;
        private VisualElement notches;
        private VisualElement flash;
        private float flashRemaining;

        private void OnEnable()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (satiety == null) return;
            satiety.Changed += RefreshNotches;
            satiety.NotchLost += OnNotchLost;
        }

        private void LateUpdate()
        {
            VisualElement currentRoot = document != null ? document.rootVisualElement : null;
            if (currentRoot != root)
            {
                flash?.RemoveFromHierarchy();
                root = currentRoot;
                notches = root?.Q("satiety-notches");
                if (root != null)
                {
                    flash = new VisualElement { name = "satiety-flash", pickingMode = PickingMode.Ignore };
                    flash.style.position = Position.Absolute;
                    flash.style.left = flash.style.right = flash.style.top = flash.style.bottom = 0f;
                    flash.style.backgroundColor = flashColor;
                    root.Add(flash);
                    if (satiety != null) RefreshNotches(satiety.Current, satiety.Maximum);
                }
            }
            if (flash == null) return;
            flash.style.display = flashRemaining > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            flash.style.opacity = Mathf.Clamp01(flashRemaining / Mathf.Max(0.01f, flashDuration));
            flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);
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

        private void OnNotchLost()
        {
            flashRemaining = flashDuration;
            if (gameplayCamera != null) gameplayCamera.PlayShake(shakeDuration, shakeAmplitude);
        }

        private void OnDisable()
        {
            if (satiety != null)
            {
                satiety.Changed -= RefreshNotches;
                satiety.NotchLost -= OnNotchLost;
            }
            flash?.RemoveFromHierarchy();
            root = notches = flash = null;
            flashRemaining = 0f;
        }
    }
}
