using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-41: H03 pusher block numbers and pass profile (GDD 7.2, 6.5).</summary>
    public sealed class PusherBlocksTests
    {
        static HazardProfile H => AssetDatabase.LoadAssetAtPath<HazardProfile>("Assets/_Project/Data/Hazards/HazardProfile.asset");
        static BalanceProfile B => AssetDatabase.LoadAssetAtPath<BalanceProfile>("Assets/_Project/Data/Balance/BalanceProfile.asset");

        [Test]
        public void Profile_HoldsGddValues()
        {
            Assert.That(H.PusherSize, Is.EqualTo(new Vector3(1.5f, 0.8f, 1f)));
            Assert.That((H.PusherLaneZ, H.PusherDockX, H.PusherSpeed, H.PusherEaseSeconds), Is.EqualTo((1.2f, 9.5f, 3f, 0.3f)));
            Assert.That((H.PusherDockWaitSeconds, H.PusherFirstDepartureSeconds, H.PusherLightSeconds), Is.EqualTo((2f, 5f, 1f)));
        }

        [Test]
        public void Pass_RunsDockToDockAtThreeMetresPerSecond()
        {
            var total = PusherBlocks.PassSeconds(H);
            Assert.That(total, Is.EqualTo(6.63f).Within(0.02f), "≈ 6.3 s cruise plus two 0.3 s eases");
            Assert.That(PusherBlocks.PassX(0f, -1, H), Is.EqualTo(-9.5f));
            Assert.That(PusherBlocks.PassX(total, -1, H), Is.EqualTo(9.5f).Within(1e-4f));
            Assert.That(PusherBlocks.PassX(total, 1, H), Is.EqualTo(-9.5f).Within(1e-4f));
            var speed = (PusherBlocks.PassX(3.1f, -1, H) - PusherBlocks.PassX(3f, -1, H)) / 0.1f;
            Assert.That(speed, Is.EqualTo(3f).Within(1e-3f));
            var easeIn = (PusherBlocks.PassX(0.1f, -1, H) - PusherBlocks.PassX(0f, -1, H)) / 0.1f;
            Assert.That(easeIn, Is.LessThan(1f), "eases out of the dock");
        }

        [Test]
        public void BlockImpulse_IsFiftyAlongTheLane()
        {
            Assert.That(PusherBlocks.BlockImpulse(1, 0, B, H), Is.EqualTo(new Vector3(50f, 0f, 0f)));
            Assert.That(PusherBlocks.BlockImpulse(-1, 5, B, H).x, Is.EqualTo(-50f * B.Knockback(5)).Within(1e-3f));
        }
    }
}
