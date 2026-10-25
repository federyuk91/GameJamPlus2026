using System;
using UnityEngine;

namespace MousePlatformer
{
    public sealed class WaterBasinModel
    {
        private readonly Rect[] sections;
        public double Volume { get; private set; }
        public double Capacity { get; }
        public float Bottom { get; }
        public float Top { get; }
        public float Surface => SurfaceAtVolume(Volume);

        public WaterBasinModel(Rect[] sections, float initialSurface)
        {
            this.sections = sections;
            Bottom = float.PositiveInfinity;
            Top = float.NegativeInfinity;
            foreach (Rect section in sections)
            {
                Bottom = Mathf.Min(Bottom, section.yMin);
                Top = Mathf.Max(Top, section.yMax);
                Capacity += section.width * section.height;
            }
            Volume = VolumeAtSurface(initialSurface);
        }

        public double VolumeAtSurface(float surface)
        {
            double volume = 0d;
            foreach (Rect section in sections)
                volume += section.width * Mathf.Clamp(surface - section.yMin, 0f, section.height);
            return volume;
        }

        public float SurfaceAtVolume(double volume)
        {
            if (volume <= 0d) return Bottom;
            if (volume >= Capacity) return Top;
            float low = Bottom;
            float high = Top;
            for (int iteration = 0; iteration < 28; iteration++)
            {
                float middle = (low + high) * 0.5f;
                if (VolumeAtSurface(middle) < volume) low = middle;
                else high = middle;
            }
            return (low + high) * 0.5f;
        }

        public static double CalculateTransfer(WaterBasinModel source, WaterBasinModel target, float sillHeight, float flowRate, float deltaTime)
        {
            float head = source.Surface - Mathf.Max(sillHeight, target.Surface);
            if (head <= 0.0001f) return 0d;
            double available = source.Volume - source.VolumeAtSurface(sillHeight);
            double maximum = Math.Min(available, target.Capacity - target.Volume);
            if (maximum <= 0d) return 0d;

            // Bound the transfer by hydrostatic equilibrium, so a large timestep cannot swap the levels.
            double low = 0d;
            double high = maximum;
            for (int iteration = 0; iteration < 24; iteration++)
            {
                double middle = (low + high) * 0.5d;
                float sourceLevel = source.SurfaceAtVolume(source.Volume - middle);
                float targetLevel = target.SurfaceAtVolume(target.Volume + middle);
                if (sourceLevel > Mathf.Max(sillHeight, targetLevel)) low = middle;
                else high = middle;
            }
            return Math.Min(low, Mathf.Max(0f, flowRate) * Mathf.Sqrt(head) * deltaTime);
        }

        public void SetVolume(double volume) => Volume = Math.Max(0d, Math.Min(Capacity, volume));
    }
}
