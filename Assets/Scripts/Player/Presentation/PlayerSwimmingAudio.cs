using GameJamPlus.UI;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerSwimming))]
    public sealed class PlayerSwimmingAudio : MonoBehaviour
    {
        [SerializeField] private PlayerSwimming swimming;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] strokeClips;
        [SerializeField, Range(0f, 1f)] private float volume = 0.65f;

        private void Awake()
        {
            if (swimming == null) swimming = GetComponent<PlayerSwimming>();
            if (audioSource == null) return;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            if (swimming != null) swimming.StrokePerformed += PlayStroke;
            GameAudioSettings.EffectsVolumeChanged += ApplyEffectsVolume;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
        }

        private void PlayStroke()
        {
            if (audioSource == null || strokeClips == null || strokeClips.Length == 0) return;
            AudioClip clip = strokeClips[Random.Range(0, strokeClips.Length)];
            if (clip == null) return;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
            audioSource.clip = clip;
            audioSource.Play();
        }

        private void ApplyEffectsVolume(float effectsVolume)
        {
            if (audioSource != null) audioSource.volume = volume * effectsVolume;
        }

        private void OnDisable()
        {
            if (swimming != null) swimming.StrokePerformed -= PlayStroke;
            GameAudioSettings.EffectsVolumeChanged -= ApplyEffectsVolume;
            if (audioSource != null) audioSource.Stop();
        }
    }
}
