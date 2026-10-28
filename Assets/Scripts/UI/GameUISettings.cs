using System;
using UnityEngine;

namespace GameJamPlus.UI
{
    public static class GameUISettings
    {
        private const string HideTooltipKey = "Settings.HideTooltip";
        public static event Action Changed;

        public static bool HideTooltip
        {
            get => PlayerPrefs.GetInt(HideTooltipKey, 0) == 1;
            set
            {
                if (HideTooltip == value) return;
                PlayerPrefs.SetInt(HideTooltipKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscriptions() => Changed = null;
    }
}
