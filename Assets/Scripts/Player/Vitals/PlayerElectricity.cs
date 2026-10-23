using System;
using System.Collections.Generic;
using UnityEngine;
using GameJamPlus.Gameplay;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class PlayerElectricity : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Accumulated lightning charges that kill this animal.")]
        private int lethalCharges = 3;
        [SerializeField, Min(0.1f)] private float decayInterval = 60f;
        private float decayElapsed;
        private readonly HashSet<UnityEngine.Object> activeShocks = new HashSet<UnityEngine.Object>();

        public int Current { get; private set; }
        public int Maximum => Mathf.Max(1, lethalCharges);
        public bool IsLethal => Current >= Maximum;
        public bool IsParalyzed => activeShocks.Count > 0;
        public event Action<int> Changed;

        public void BeginShock(UnityEngine.Object source)
        {
            if (isActiveAndEnabled && source != null) activeShocks.Add(source);
        }

        public void EndShock(UnityEngine.Object source)
        {
            if (source != null) activeShocks.Remove(source);
        }

        private void OnDisable() => activeShocks.Clear();

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (Current <= 0 || IsLethal || manager == null || manager.State != GameManager.LoopState.Playing) return;
            decayElapsed += Time.fixedDeltaTime;
            if (decayElapsed < Mathf.Max(0.1f, decayInterval)) return;
            decayElapsed -= Mathf.Max(0.1f, decayInterval);
            Current--;
            if (Current == 0) decayElapsed = 0f;
            Changed?.Invoke(Current);
        }

        public bool ReceiveShock(int amount)
        {
            if (!isActiveAndEnabled || amount <= 0 || IsLethal) return false;
            if (Current == 0) decayElapsed = 0f;
            Current += Mathf.Min(amount, Maximum - Current);
            GameSession.RecordObservation(TestObservation.Electrician);
            Changed?.Invoke(Current);
            return true;
        }
    }
}
