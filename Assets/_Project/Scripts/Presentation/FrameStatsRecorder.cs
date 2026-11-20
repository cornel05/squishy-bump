using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SquishyBump.Presentation
{
    public sealed class FrameStatsRecorder : MonoBehaviour
    {
        // About 1 MiB per match. Record overwritten samples in the summary so truncated runs are identifiable.
        private const int SampleCapacity = 262144;

        private FrameStats stats;
        private PresentationProfile profile;
        private double requestedSeconds;
        private double elapsedSeconds;
        private long totalFrames;
        private int firstFrame;
        private bool finished;

        public static double ReadDuration(string[] args)
        {
            int index = Array.IndexOf(args, "-perf");
            if (index < 0) return 0d;
            if (index + 1 < args.Length &&
                double.TryParse(args[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) &&
                seconds > 0d && !double.IsInfinity(seconds))
                return seconds;

            Debug.LogWarning("[FrameStats] Expected -perf followed by a positive duration in seconds.");
            return 0d;
        }

        public void Configure(PresentationProfile presentation, double seconds)
        {
            profile = presentation;
            requestedSeconds = seconds;
            stats = new FrameStats(SampleCapacity);
            firstFrame = Time.frameCount;
        }

        private void Update()
        {
            if (stats == null || Time.frameCount == firstFrame) return;
            float delta = Time.unscaledDeltaTime;
            if (delta <= 0f) return;

            stats.Add(delta);
            totalFrames++;
            elapsedSeconds += delta;
            if (elapsedSeconds >= requestedSeconds)
            {
                Save(complete: true);
                enabled = false;
            }
        }

        private void OnDestroy() => Save(complete: false);

        private void Save(bool complete)
        {
            if (finished || stats == null || stats.Count == 0) return;
            finished = true;
            int width = Screen.width;
            int height = Screen.height;
            var (medianFps, p95Ms) = stats.Summarize();
            bool meetsTargets = complete && totalFrames == stats.Count &&
                medianFps >= profile.PerfMedianFps && p95Ms <= profile.PerfP95Milliseconds;

            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "perf");
                Directory.CreateDirectory(directory);
                string prefix = Path.Combine(directory, FormattableString.Invariant(
                    $"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{gameObject.scene.name}-{Guid.NewGuid():N}"));

                using (var csv = File.CreateText(prefix + ".csv"))
                {
                    csv.WriteLine("frame_index,frame_time_ms");
                    long frame = totalFrames - stats.Count;
                    foreach (float seconds in stats.CopySamples())
                        csv.WriteLine(FormattableString.Invariant($"{++frame},{seconds * 1000d:R}"));
                }

                string summary = FormattableString.Invariant(
                    $"scene={gameObject.scene.path}\nversion={Application.version}\nresolution={width}x{height}\n") +
                    ($"quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}\ncpu={SystemInfo.processorType}\ngpu={SystemInfo.graphicsDeviceName}\n") +
                    FormattableString.Invariant(
                    $"requested_seconds={requestedSeconds:R}\nrecorded_seconds={elapsedSeconds:R}\nframes={stats.Count}\noverwritten_frames={totalFrames - stats.Count}\n") +
                    FormattableString.Invariant($"complete={complete}\nmedian_fps={medianFps:R}\np95_ms={p95Ms:R}\n") +
                    FormattableString.Invariant($"target_median_fps={profile.PerfMedianFps:R}\ntarget_p95_ms={profile.PerfP95Milliseconds:R}\n") +
                    $"meets_frame_targets={meetsTargets}\n";
                File.WriteAllText(prefix + ".txt", summary);
                Debug.Log($"[FrameStats] Saved {stats.Count} frames to {prefix}");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"[FrameStats] Could not write capture: {exception.Message}");
            }
        }
    }
}
