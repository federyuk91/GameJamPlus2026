using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameJamPlus.UI
{
    internal sealed class ShockGamepadFeedback : IDisposable
    {
        private Gamepad controller;
        private float lowFrequency;
        private float highFrequency;

        public void Refresh(Gamepad activeController, bool active, float low, float high)
        {
            Gamepad next = active && activeController != null && activeController.added && activeController.enabled
                ? activeController : null;
            if (controller != next) Stop();
            if (next == null) return;

            low = Mathf.Clamp01(low);
            high = Mathf.Clamp01(high);
            if (controller == next && Mathf.Approximately(lowFrequency, low) && Mathf.Approximately(highFrequency, high)) return;
            controller = next;
            lowFrequency = low;
            highFrequency = high;
            controller.SetMotorSpeeds(low, high);
        }

        public void Stop()
        {
            if (controller != null && controller.added && controller.enabled) controller.SetMotorSpeeds(0f, 0f);
            controller = null;
            lowFrequency = highFrequency = 0f;
        }

        public void Dispose() => Stop();
    }
}
