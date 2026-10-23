using System;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        public const int Maximum = 5;
        [SerializeField, Range(0, Maximum), Tooltip("Health of this animal, displayed as up to five hearts.")]
        private int current = Maximum;
        public int Current => current;
        public bool IsDead => current <= 0;
        public event Action Changed;
        public event Action Damaged;

        public bool ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDead) return false;
            SetHealth(current - amount);
            Damaged?.Invoke();
            return true;
        }

        public void SetHealth(int value)
        {
            int next = Mathf.Clamp(value, 0, Maximum);
            if (current == next) return;
            current = next;
            Changed?.Invoke();
        }

        private void OnValidate()
        {
            current = Mathf.Clamp(current, 0, Maximum);
            Changed?.Invoke();
        }
    }
}
