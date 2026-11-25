using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using UnityEditor;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-15: H01 schedule and telegraph of GDD 7.1, counted in 60 Hz host ticks.</summary>
    public sealed class RingDropTests
    {
        const float Dt = 1f / 60f;

        static BalanceProfile Balance =>
            AssetDatabase.LoadAssetAtPath<BalanceProfile>("Assets/_Project/Data/Balance/BalanceProfile.asset");
        static HazardProfile Hazards =>
            AssetDatabase.LoadAssetAtPath<HazardProfile>("Assets/_Project/Data/Hazards/HazardProfile.asset");

        [Test]
        public void HazardProfile_HoldsGddValues()
        {
            Assert.That(Hazards, Is.Not.Null);
            Assert.That(Hazards.RingDropFinalWarningSeconds, Is.EqualTo(1f));
            Assert.That(Hazards.TelegraphBlinkHz, Is.EqualTo(2f));
        }

        [Test]
        public void DropTicks_Are45_75_105_135Seconds()
        {
            var ticks = new int[4];
            for (var i = 0; i < 4; i++) ticks[i] = RingDrop.DropTick(i, Balance, Dt);
            Assert.That(ticks, Is.EqualTo(new[] { 2700, 4500, 6300, 8100 }));
        }

        [Test]
        public void StageAt_AmberThenRedInLastSecond()
        {
            Assert.That(RingDrop.StageAt(-1f, Balance, Hazards), Is.EqualTo(BandStage.Solid));
            Assert.That(RingDrop.StageAt(0f, Balance, Hazards), Is.EqualTo(BandStage.Warning));
            Assert.That(RingDrop.StageAt(1.99f, Balance, Hazards), Is.EqualTo(BandStage.Warning));
            Assert.That(RingDrop.StageAt(2f, Balance, Hazards), Is.EqualTo(BandStage.FinalWarning));
            Assert.That(RingDrop.StageAt(2.99f, Balance, Hazards), Is.EqualTo(BandStage.FinalWarning));
            Assert.That(RingDrop.StageAt(3f, Balance, Hazards), Is.EqualTo(BandStage.Dropped));
        }

        [Test]
        public void BlinkOn_TogglesAtTwoHertz()
        {
            Assert.That(RingDrop.BlinkOn(0f, 2f), Is.True);
            Assert.That(RingDrop.BlinkOn(0.24f, 2f), Is.True);
            Assert.That(RingDrop.BlinkOn(0.26f, 2f), Is.False);
            Assert.That(RingDrop.BlinkOn(0.51f, 2f), Is.True);
        }

        [Test]
        public void SeamRadius_IsInnerEdgeOfEachBand()
        {
            Assert.That(RingDrop.SeamRadius(0, 4, 3f), Is.EqualTo(9f));
            Assert.That(RingDrop.SeamRadius(1, 4, 3f), Is.EqualTo(6f));
            Assert.That(RingDrop.SeamRadius(2, 4, 3f), Is.EqualTo(3f));
            Assert.That(RingDrop.SeamRadius(3, 4, 3f), Is.EqualTo(3f), "the centre disc flashes its rim");
        }

        [Test]
        public void RingDropTelegraph_LastsAtLeastOneSecond() =>
            Assert.That(Balance.RingDropWarningSeconds, Is.GreaterThanOrEqualTo(1f));

        [Test]
        public void TargetWaterY_RisesWithEachDroppedBandAndSubmergesRingAtEnd()
        {
            Assert.That(RingDrop.TargetWaterY(0, 4), Is.EqualTo(RingDrop.InitialWaterY).Within(0.001f));
            Assert.That(RingDrop.InitialWaterY, Is.GreaterThan(-0.5f), "water starts close to the ring surface");
            for (var i = 1; i <= 4; i++)
                Assert.That(RingDrop.TargetWaterY(i, 4), Is.GreaterThan(RingDrop.TargetWaterY(i - 1, 4)));
            Assert.That(RingDrop.TargetWaterY(4, 4), Is.GreaterThan(0.2f), "final ring drop submerges the ring");
        }
    }
}
