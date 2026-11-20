using System;

namespace SquishyBump.Presentation
{
    /// <summary>Stores the latest frame durations in seconds, oldest first when copied.</summary>
    public sealed class FrameStats
    {
        private readonly float[] samples;
        private int next;

        public int Count { get; private set; }
        public int Capacity => samples.Length;

        public FrameStats(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            samples = new float[capacity];
        }

        public void Add(float seconds)
        {
            if (!(seconds > 0f) || float.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));

            samples[next] = seconds;
            next = (next + 1) % Capacity;
            if (Count < Capacity) Count++;
        }

        public float[] CopySamples()
        {
            var result = new float[Count];
            int oldest = Count == Capacity ? next : 0;
            int firstLength = Math.Min(Count, Capacity - oldest);
            Array.Copy(samples, oldest, result, 0, firstLength);
            Array.Copy(samples, 0, result, firstLength, Count - firstLength);
            return result;
        }

        public (double MedianFps, double P95Milliseconds) Summarize()
        {
            if (Count == 0) throw new InvalidOperationException("No frame durations recorded.");

            var sorted = CopySamples();
            Array.Sort(sorted);
            double medianFps = (1d / sorted[(Count - 1) / 2] + 1d / sorted[Count / 2]) / 2d;
            int p95 = (int)Math.Ceiling(Count * 0.95d) - 1;
            return (medianFps, sorted[p95] * 1000d);
        }
    }
}
