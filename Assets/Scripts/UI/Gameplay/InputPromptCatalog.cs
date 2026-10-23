using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamPlus.UI
{
    public readonly struct InputPromptBinding
    {
        public readonly string Path;
        public readonly string Label;
        public readonly InputPromptFamily Family;
        public InputPromptBinding(string path, string label, InputPromptFamily family)
        {
            Path = path; Label = label; Family = family;
        }
    }

    [CreateAssetMenu(menuName = "Game/UI/Input Prompt Catalog")]
    public sealed class InputPromptCatalog : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            public InputPromptFamily family;
            public string control;
            public Texture2D icon;
        }

        [SerializeField] private Entry[] entries;
        [SerializeField] private Texture2D keyboardFallback;
        [SerializeField] private Texture2D controllerFallback;
        private Dictionary<string, Texture2D> lookup;
        private static InputPromptCatalog defaultCatalog;
        public static InputPromptCatalog Default => defaultCatalog != null ? defaultCatalog
            : defaultCatalog = Resources.Load<InputPromptCatalog>("InputPrompts");

        public Texture2D Resolve(InputPromptBinding binding, out bool overlayLabel)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
                if (entries != null)
                    foreach (Entry entry in entries)
                        if (entry != null && entry.icon != null) lookup[Key(entry.family, entry.control)] = entry.icon;
            }
            string control = ControlName(binding.Path);
            // Physical key paths can have a different printed letter on layouts such as AZERTY.
            if (binding.Family == InputPromptFamily.Keyboard && binding.Label != null
                && binding.Label.Length == 1 && char.IsLetterOrDigit(binding.Label[0])) control = binding.Label;
            overlayLabel = false;
            if (lookup.TryGetValue(Key(binding.Family, control), out Texture2D icon)) return icon;
            overlayLabel = true;
            return binding.Family == InputPromptFamily.Keyboard ? keyboardFallback : controllerFallback;
        }

        private static string Key(InputPromptFamily family, string control) => family + ":" + control;
        private static string ControlName(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            if (path.StartsWith("<", StringComparison.Ordinal))
            {
                int separator = path.IndexOf("/", StringComparison.Ordinal);
                return separator >= 0 ? path.Substring(separator + 1) : path;
            }
            if (path.StartsWith("/", StringComparison.Ordinal))
            {
                int separator = path.IndexOf('/', 1);
                return separator >= 0 ? path.Substring(separator + 1) : path;
            }
            return path;
        }

        private void OnValidate() => lookup = null;
    }
}
