using UnityEngine;
using MousePlatformer;
using UnityEngine.Rendering.Universal;
using GameJamPlus.UI;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(240), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D), typeof(TestEndEvent))]
    public sealed class ElectricTrap : MonoBehaviour
    {
        [SerializeField] private Collider2D contactArea;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private TestEndEvent testEnd;
        [SerializeField] private bool automaticActivation = true;
        [SerializeField, Min(0.01f)] private float activationDuration = 3f;
        [SerializeField] private Light2D electricLight;
        [SerializeField, Min(0.1f)] private float flickerFrequency = 18f;
        [SerializeField, Min(0f)] private float minimumLightIntensity = 0.15f;
        [SerializeField, Min(0f)] private float maximumLightIntensity = 2f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] dischargeClips;
        [SerializeField, Range(0f, 1f)] private float dischargeVolume = 0.65f;
        [SerializeField] private bool showGizmos = true;
        private bool triggered;
        private float remainingDuration;

        public bool AutomaticActivation => automaticActivation;
        public bool IsActivated => remainingDuration > 0f;
        public bool IsDangerous => isActiveAndEnabled && IsActivated
            && contactArea != null && contactArea.enabled && contactArea.gameObject.activeInHierarchy;

        private void Awake()
        {
            if (contactArea == null) contactArea = GetComponent<Collider2D>();
            if (testEnd == null) testEnd = GetComponent<TestEndEvent>();
            contactArea.isTrigger = true;
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
            }
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (!IsActivated || manager == null || manager.State != GameManager.LoopState.Playing) return;
            remainingDuration = Mathf.Max(0f, remainingDuration - Time.fixedDeltaTime);
            if (!IsActivated)
            {
                Deactivate();
                return;
            }
            ApplyDischarge();
        }

        public void Activate()
        {
            if (!isActiveAndEnabled || triggered) return;
            remainingDuration = Mathf.Max(0.01f, activationDuration);
            PlayDischargeSound();
            UpdateLight();
            ApplyDischarge();
        }

        public void Deactivate()
        {
            remainingDuration = 0f;
            automaticActivation = false;
            if (audioSource != null) audioSource.Stop();
            UpdateLight();
        }

        private void ApplyDischarge()
        {
            GameManager manager = GameManager.Instance;
            if (!isActiveAndEnabled || triggered || manager == null
                || manager.State != GameManager.LoopState.Playing || testEnd == null
                || contactArea == null || !contactArea.enabled || !contactArea.gameObject.activeInHierarchy) return;
            // The player uses custom movement without a Rigidbody2D, so trigger callbacks are insufficient.
            Physics2D.SyncTransforms();
            foreach (PiranhaDeath piranha in PiranhaDeath.ActivePiranhas)
            {
                if (piranha == null || piranha.IsDead || piranha.Shape == null || !piranha.Shape.enabled) continue;
                ColliderDistance2D fishContact = contactArea.Distance(piranha.Shape);
                if (fishContact.isValid && fishContact.distance <= 0f) piranha.Die();
            }

            if (playerCollider == null) playerCollider = manager.PlayerCollider;
            if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy) return;
            ColliderDistance2D contact = contactArea.Distance(playerCollider);
            if (!contact.isValid || contact.distance > 0f) return;
            triggered = true;
            testEnd.EndTest();
        }

        public void EnableAutomaticActivation() => SetAutomaticActivation(true);
        public void DisableAutomaticActivation() => SetAutomaticActivation(false);

        public void SetAutomaticActivation(bool enabled)
        {
            automaticActivation = enabled;
            if (enabled) Activate();
            else Deactivate();
        }

        private void OnEnable()
        {
            GameAudioSettings.EffectsVolumeChanged += ApplyEffectsVolume;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
            if (automaticActivation) Activate();
            else UpdateLight();
        }

        private void OnDisable()
        {
            GameAudioSettings.EffectsVolumeChanged -= ApplyEffectsVolume;
            if (audioSource != null) audioSource.Stop();
            remainingDuration = 0f;
            if (electricLight != null) electricLight.enabled = false;
        }

        private void Update() => UpdateLight();

        private void PlayDischargeSound()
        {
            if (audioSource == null || dischargeClips == null || dischargeClips.Length == 0) return;
            AudioClip clip = dischargeClips[Random.Range(0, dischargeClips.Length)];
            if (clip == null) return;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
            audioSource.clip = clip;
            audioSource.Play();
        }

        private void ApplyEffectsVolume(float effectsVolume)
        {
            if (audioSource != null) audioSource.volume = dischargeVolume * effectsVolume;
        }

        private void UpdateLight()
        {
            if (electricLight == null) return;
            electricLight.enabled = isActiveAndEnabled && IsActivated;
            if (!electricLight.enabled) return;
            float phase = Time.time * Mathf.Max(0.1f, flickerFrequency);
            float noise = Mathf.PerlinNoise(phase, 7.31f);
            float flash = Mathf.Sin(phase * Mathf.PI * 2f) > 0.25f ? 1f : noise * 0.25f;
            electricLight.intensity = Mathf.Lerp(minimumLightIntensity,
                Mathf.Max(minimumLightIntensity, maximumLightIntensity), flash);
        }

        private void Reset()
        {
            contactArea = GetComponent<Collider2D>();
            testEnd = GetComponent<TestEndEvent>();
            if (contactArea != null) contactArea.isTrigger = true;
        }

        private void OnValidate()
        {
            if (contactArea != null) contactArea.isTrigger = true;
            if (Application.isPlaying) ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || contactArea == null) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.5f);
            if (contactArea is BoxCollider2D box)
            {
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else
            {
                Gizmos.DrawWireCube(contactArea.bounds.center, contactArea.bounds.size);
            }
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
