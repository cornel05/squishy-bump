using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.Hazards;

namespace SquishyBump.Tests.EditMode
{
    public sealed class IceFieldTests
    {
        // Fixture trực tiếp từ GDD 7.4, 60 Hz. Mapping SO được test riêng trong Unity.
        private static IceRules Rules => new(4, 37, 180, 90, 600, 60, 1800, 3600, 2, 3, 4, 1.5);
        [Test, Category("UnityAsset")]
        public void IceProfileAsset_MatchesGddAndFeedsRuntimeRules()
        {
            var p = UnityEditor.AssetDatabase.LoadAssetAtPath<SquishyBump.Core.IceProfile>(
                "Assets/_Project/Data/Hazards/IceProfile.asset");
            Assert.That(p, Is.Not.Null, "Chạy IceSceneBuilder.BuildL4 trong Editor trước khi nghiệm thu.");
            var r = IceRules.FromProfile(p, 1f / 60);
            Assert.That(p.FlatToFlat, Is.EqualTo(2f));
            Assert.That(r.Rings, Is.EqualTo(4)); Assert.That(r.MinimumSolid, Is.EqualTo(37));
            Assert.That(r.WaveTicks, Is.EqualTo(180)); Assert.That(r.CrackTicks, Is.EqualTo(90));
            Assert.That(r.ReturnTicks, Is.EqualTo(600)); Assert.That(r.FadeTicks, Is.EqualTo(60));
            Assert.That(r.PhaseTwoTicks, Is.EqualTo(1800)); Assert.That(r.PhaseThreeTicks, Is.EqualTo(3600));
            Assert.That(r.FighterWeight, Is.EqualTo(1.5));
            Assert.That(r.FirstCount, Is.EqualTo(2)); Assert.That(r.SecondCount, Is.EqualTo(3)); Assert.That(r.ThirdCount, Is.EqualTo(4));
            var chaos = IceRules.FromProfile(p, 1f / 60, true);
            Assert.That(chaos.WaveTicks, Is.EqualTo(210)); Assert.That(chaos.PhaseTwoTicks, Is.EqualTo(2700));
        }
        [Test]
        public void FourRings_Has61UniqueHexagonsAndWaveBoundaries()
        {
            var f = new IceField(Rules, 18);
            Assert.That(f.Cells.Select(h => (h.Q, h.R)).Distinct().Count(), Is.EqualTo(61));
            Assert.That(f.SolidConnected(), Is.True);
            Assert.That(Rules.WaveCount(1799), Is.EqualTo(2));
            Assert.That(Rules.WaveCount(1800), Is.EqualTo(3));
            Assert.That(Rules.WaveCount(3600), Is.EqualTo(4));
        }
        [TestCase(0u)] [TestCase(1u)] [TestCase(18u)] [TestCase(314159u)]
        [TestCase(42u)] [TestCase(99u)] [TestCase(65535u)] [TestCase(uint.MaxValue)]
        public void NoFighters120Seconds_EveryTickPreservesMinimumAndConnectivity(uint seed)
        {
            var f = new IceField(Rules, seed);
            for (var tick = 1; tick <= 7200; tick++)
            {
                f.Step(tick, _ => 0, _ => false);
                Assert.That(f.SolidCount, Is.GreaterThanOrEqualTo(37), $"seed {seed}, tick {tick}");
                Assert.That(f.ReservedSolidCount, Is.GreaterThanOrEqualTo(37));
                Assert.That(f.SolidConnected(), Is.True, $"seed {seed}, tick {tick}");
                Assert.That(f.SolidConnected(true), Is.True);
            }
        }
        [Test]
        public void CrackFallFadeOverlap_OnlyRestoresColliderStateAfterClear()
        {
            var f = new IceField(Rules, 18);
            for (var t = 1; t <= 180; t++) f.Step(t, _ => 0, _ => true);
            var tile = Enumerable.Range(0, 61).First(i => f.Stage(i) == IceStage.Cracking);
            for (var t = 181; t <= 269; t++) f.Step(t, _ => 0, _ => true);
            Assert.That(f.Stage(tile), Is.EqualTo(IceStage.Cracking));
            f.Step(270, _ => 0, _ => true);
            Assert.That(f.Stage(tile), Is.EqualTo(IceStage.Fallen));
            for (var t = 271; t <= 870; t++) f.Step(t, _ => 0, _ => true);
            Assert.That(f.Stage(tile), Is.EqualTo(IceStage.Fading));
            for (var t = 871; t <= 1200; t++) f.Step(t, _ => 0, _ => true);
            Assert.That(f.Stage(tile), Is.EqualTo(IceStage.Fading));
            f.Step(1201, _ => 0, _ => false);
            Assert.That(f.Stage(tile), Is.EqualTo(IceStage.Solid));
        }
        [Test]
        public void SameSeedAndOccupancy_ProducesIdenticalTransitions()
        {
            var a = new IceField(Rules, 18); var b = new IceField(Rules, 18);
            var first = new List<(int, IceStage, int)>(); var second = new List<(int, IceStage, int)>();
            a.Changed += (i, s, t) => first.Add((i, s, t)); b.Changed += (i, s, t) => second.Add((i, s, t));
            for (var t = 1; t <= 7200; t++)
            { a.Step(t, i => i % 7, _ => false); b.Step(t, i => i % 7, _ => false); }
            CollectionAssert.AreEqual(first, second);
            Assert.That(first.Count, Is.GreaterThan(100));
        }
        [Test]
        public void OccupiedTile_HasHigherSelectionFrequencyAcrossFixedSeeds()
        {
            int Count(int occupants)
            {
                var selected = 0;
                for (uint seed = 0; seed < 512; seed++)
                {
                    var f = new IceField(Rules, seed);
                    f.Step(180, i => i == 30 ? occupants : 0, _ => false);
                    if (f.Stage(30) == IceStage.Cracking) selected++;
                }
                return selected;
            }
            Assert.That(Count(6), Is.GreaterThan(Count(0) * 3));
        }
        [Test]
        public void DuplicateTick_DoesNotAdvanceOrRepeatEvents()
        {
            var f = new IceField(Rules, 18); var count = 0; f.Changed += (_, _, _) => count++;
            f.Step(180, _ => 0, _ => false); var before = count;
            f.Step(180, _ => 0, _ => false); f.Step(179, _ => 0, _ => false);
            Assert.That(count, Is.EqualTo(before));
        }
    }
}
