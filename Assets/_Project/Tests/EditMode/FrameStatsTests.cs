using NUnit.Framework;
using SquishyBump.Presentation;
using UnityEditor;

namespace SquishyBump.Tests.EditMode
{
    public sealed class FrameStatsTests
    {
        [Test]
        public void KnownSamples_MedianAndP95_Expected()
        {
            var stats = new FrameStats(20);
            stats.Add(0.5f);
            stats.Add(0.25f);
            for (int i = 0; i < 9; i++) stats.Add(0.125f);
            for (int i = 0; i < 9; i++) stats.Add(0.0625f);

            var (medianFps, p95Ms) = stats.Summarize();

            Assert.That(medianFps, Is.EqualTo(8d));
            Assert.That(p95Ms, Is.EqualTo(250d));
        }

        [Test]
        public void BufferWrap_KeepsLatestSamplesInOrder()
        {
            var stats = new FrameStats(2);
            stats.Add(0.5f);
            stats.Add(0.25f);
            stats.Add(0.125f);

            Assert.That(stats.CopySamples(), Is.EqualTo(new[] { 0.25f, 0.125f }));
            Assert.That(stats.Summarize().MedianFps, Is.EqualTo(6d));
        }

        [Test]
        public void PerformanceTargets_MatchGdd()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PresentationProfile>(
                "Assets/_Project/Data/Presentation/PresentationProfile.asset");

            Assert.That(profile.PerfMedianFps, Is.EqualTo(60f));
            Assert.That(profile.PerfP95Milliseconds, Is.EqualTo(25f));
        }
    }
}
