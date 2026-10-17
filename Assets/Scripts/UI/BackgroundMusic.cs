using UnityEngine;

namespace GameJamPlus.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip clip;
        private float baseVolume;

        private void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
            baseVolume = source.volume;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.clip = clip;
        }

        private void OnEnable()
        {
            GameAudioSettings.MusicVolumeChanged += ApplyMusicVolume;
            ApplyMusicVolume(GameAudioSettings.MusicVolume);
            if (source.clip != null) source.Play();
        }

        private void OnDisable()
        {
            GameAudioSettings.MusicVolumeChanged -= ApplyMusicVolume;
            source.Stop();
        }

        private void ApplyMusicVolume(float volume)
        {
            source.volume = baseVolume * volume;
        }
    }
}
