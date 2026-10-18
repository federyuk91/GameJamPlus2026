using GameJamPlus.UI;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(110), DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement), typeof(AudioSource))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        public enum FootstepGait
        {
            Run,
            Walk
        }

        [SerializeField] private PlayerMovement movement;
        [SerializeField] private AudioSource source;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [SerializeField, Tooltip("Select the audio cadence; does not change movement speed.")]
        private FootstepGait gait = FootstepGait.Run;

        private float remaining;
        private int lastClip = -1;
        private float baseSourceVolume;
        private PlayerMovementSettings Settings => physicsSettings != null ? physicsSettings.player : PhysicsSettings.DefaultPlayer;

        public FootstepGait Gait
        {
            get => gait;
            set => gait = value;
        }

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (source == null) source = GetComponent<AudioSource>();
            baseSourceVolume = source.volume;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            GameAudioSettings.EffectsVolumeChanged += ApplyEffectsVolume;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
        }

        private void ApplyEffectsVolume(float effectsVolume)
        {
            source.volume = baseSourceVolume * effectsVolume;
        }

        private void FixedUpdate()
        {
            float frequency = gait == FootstepGait.Walk ? Settings.walkFootstepsPerSecond : Settings.runFootstepsPerSecond;
            if (!movement.isActiveAndEnabled || !movement.IsMovingOnGround || frequency <= 0f
                || clips == null || clips.Length == 0)
            {
                remaining = 0f;
                return;
            }

            remaining -= Time.fixedDeltaTime;
            if (remaining > 0f) return;
            remaining = Mathf.Max(0f, remaining + 1f / frequency);
            PlayStep();
        }

        private void PlayStep()
        {
            int index;
            if (clips.Length > 1 && lastClip >= 0 && lastClip < clips.Length)
            {
                // Uniform random selection among the clips other than the previous one.
                index = Random.Range(0, clips.Length - 1);
                if (index >= lastClip) index++;
            }
            else index = Random.Range(0, clips.Length);
            lastClip = index;
            if (clips[index] != null) source.PlayOneShot(clips[index], volume);
        }

        private void OnDisable()
        {
            GameAudioSettings.EffectsVolumeChanged -= ApplyEffectsVolume;
            remaining = 0f;
            if (source != null) source.Stop();
        }
    }
}
