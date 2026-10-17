using System;
using UnityEngine;

namespace GameJamPlus.UI
{
    public static class GameAudioSettings
    {
        private const string MasterVolumeKey = "Settings.MasterVolume";
        private const string MusicVolumeKey = "Settings.MusicVolume";
        private const string EffectsVolumeKey = "Settings.EffectsVolume";

        public static event Action<float> MusicVolumeChanged;
        public static event Action<float> EffectsVolumeChanged;

        public static float MasterVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            set
            {
                float volume = Mathf.Clamp01(value);
                AudioListener.volume = volume;
                PlayerPrefs.SetFloat(MasterVolumeKey, volume);
            }
        }

        public static float MusicVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            set
            {
                float volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicVolumeKey, volume);
                MusicVolumeChanged?.Invoke(volume);
            }
        }

        public static float EffectsVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsVolumeKey, 1f));
            set
            {
                float volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(EffectsVolumeKey, volume);
                EffectsVolumeChanged?.Invoke(volume);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscriptions()
        {
            MusicVolumeChanged = null;
            EffectsVolumeChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void ApplySavedVolume()
        {
            AudioListener.volume = MasterVolume;
            MusicVolumeChanged?.Invoke(MusicVolume);
            EffectsVolumeChanged?.Invoke(EffectsVolume);
        }
    }
}
