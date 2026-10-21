using UnityEngine;

namespace GameJamPlus.UI
{
    public sealed class ScientistSpeechAnimator
    {
        private readonly ScientistSpeechSettings settings;
        private Sprite[] currentFrames;
        private Sprite[] requestedFrames;
        private int frame;
        private float elapsed;
        private float idleElapsed;
        private bool restRequested;

        public ScientistSpeechAnimator(ScientistSpeechSettings settings) => this.settings = settings;

        public void Speak(char character)
        {
            Sprite[] frames = settings != null ? settings.FramesFor(character) : null;
            if (frames == null || frames.Length == 0)
            {
                RequestRest();
                return;
            }
            restRequested = false;
            requestedFrames = frames;
            if (currentFrames != null) return;
            currentFrames = frames;
            idleElapsed = 0f;
            frame = 0;
            elapsed = 0f;
        }

        public void RequestRest()
        {
            restRequested = true;
            requestedFrames = null;
        }

        public Sprite Tick(float dt)
        {
            if (settings == null) return null;
            if (currentFrames == null) return TickIdle(dt);
            elapsed += Mathf.Max(0f, dt);
            while (elapsed >= settings.FrameDuration && currentFrames != null)
            {
                elapsed -= settings.FrameDuration;
                frame++;
                if (frame < currentFrames.Length) continue;
                // Complete each mouth cycle before switching to the latest revealed letter group.
                currentFrames = restRequested ? null : requestedFrames;
                frame = 0;
                if (currentFrames == null) idleElapsed = 0f;
            }
            return currentFrames != null ? currentFrames[frame] ?? settings.IdlePortrait : TickIdle(elapsed);
        }

        private Sprite TickIdle(float dt)
        {
            idleElapsed += Mathf.Max(0f, dt);
            int idleFrame = Mathf.FloorToInt(idleElapsed / settings.IdleFrameDuration);
            return settings.IdleFrame(idleFrame);
        }

        public void Reset()
        {
            currentFrames = requestedFrames = null;
            frame = 0;
            elapsed = idleElapsed = 0f;
            restRequested = false;
        }
    }
}
