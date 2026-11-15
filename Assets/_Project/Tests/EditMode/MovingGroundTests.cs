using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-40: H02 turntable and H07 obstacle ring numbers and motion of GDD 7.2 / 6.5.</summary>
    public sealed class MovingGroundTests
    {
        static HazardProfile H => AssetDatabase.LoadAssetAtPath<HazardProfile>("Assets/_Project/Data/Hazards/HazardProfile.asset");
        static BalanceProfile B => AssetDatabase.LoadAssetAtPath<BalanceProfile>("Assets/_Project/Data/Balance/BalanceProfile.asset");

        [Test]
        public void Profile_HoldsGddValues()
        {
            Assert.That((H.TurntableRadius, H.TurntableDegreesPerSecond, H.TurntableReversalSeconds, H.TurntableRampSeconds,
                H.TurntableWarningSeconds), Is.EqualTo((4f, 20f, 15f, 2f, 2f)));
            Assert.That((H.ObstacleRingOuterRadius, H.ObstacleRingDegreesPerSecond, H.ObstacleBoxCount, H.ObstacleBoxRadius),
                Is.EqualTo((6.5f, 15f, 3, 5.9f)));
            Assert.That(H.ObstacleBoxSize, Is.EqualTo(new Vector3(1.2f, 0.8f, 1f)));
            Assert.That((H.ObstacleImpulse, H.ObstacleRadialShare, H.ObstacleWobble, H.ObstacleRehitSeconds),
                Is.EqualTo((50f, 0.5f, 1, 0.5f)));
        }

        [Test]
        public void Turntable_ReversesEveryFifteenSecondsWithTwoSecondRamp()
        {
            Assert.That(MovingGround.TurntableDegreesPerSecond(0f, H), Is.EqualTo(20f));
            Assert.That(MovingGround.TurntableDegreesPerSecond(14.9f, H), Is.EqualTo(20f));
            Assert.That(MovingGround.TurntableDegreesPerSecond(16f, H), Is.EqualTo(0f).Within(1e-4f), "mid ramp");
            Assert.That(MovingGround.TurntableDegreesPerSecond(17f, H), Is.EqualTo(-20f));
            Assert.That(MovingGround.TurntableDegreesPerSecond(31f, H), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(MovingGround.TurntableDegreesPerSecond(33f, H), Is.EqualTo(20f));
        }

        [Test]
        public void TurntableAngle_IsTheIntegralOfItsRate()
        {
            const float dt = 1f / 600f;
            var angle = 0f;
            for (var t = 0f; t < 47f; t += dt)
            {
                angle += MovingGround.TurntableDegreesPerSecond(t + dt * 0.5f, H) * dt;
                if (Mathf.Abs(t % 5f) < dt)
                    Assert.That(MovingGround.TurntableAngle(t + dt, H), Is.EqualTo(angle).Within(0.05f), $"t = {t}");
            }
        }

        [Test]
        public void ObstacleRing_AlwaysTurnsAgainstTurntableAtFifteenDegrees()
        {
            foreach (var t in new[] { 0f, 10f, 17f, 40f })
                Assert.That(MovingGround.RingDegreesPerSecond(t, H),
                    Is.EqualTo(-0.75f * MovingGround.TurntableDegreesPerSecond(t, H)).Within(1e-4f));
            Assert.That(MovingGround.RingDegreesPerSecond(0f, H), Is.EqualTo(-15f));
        }

        [Test]
        public void ArrowsWarnTwoSecondsBeforeReversal()
        {
            Assert.That(MovingGround.InReversalWarning(12.9f, H), Is.False);
            Assert.That(MovingGround.InReversalWarning(13f, H), Is.True);
            Assert.That(MovingGround.InReversalWarning(14.99f, H), Is.True);
            Assert.That(MovingGround.InReversalWarning(15.5f, H), Is.False);
        }

        [Test]
        public void SurfaceVelocity_IsOmegaCrossR()
        {
            var v = MovingGround.SurfaceVelocity(20f, new Vector3(0f, 0.5f, 4f));
            Assert.That(v.magnitude, Is.EqualTo(1.396f).Within(0.01f), "edge speed 1.4 m/s");
            Assert.That(Vector3.Dot(v, new Vector3(0f, 0f, 4f)), Is.EqualTo(0f).Within(1e-4f), "tangential");
            Assert.That(MovingGround.SurfaceVelocity(-15f, new Vector3(5.9f, 0f, 0f)).magnitude, Is.EqualTo(1.545f).Within(0.01f));
        }

        [Test]
        public void BoxImpulse_IsFiftyTimesKAlongTravelPlusHalfRadial()
        {
            var r = new Vector3(5.9f, 0f, 0f);
            var j = MovingGround.BoxImpulse(r, -15f, 0, B, H);
            Assert.That(j.magnitude, Is.EqualTo(50f).Within(1e-3f));
            var travel = Vector3.Cross(Vector3.up, r.normalized) * -1f;
            Assert.That(Vector3.Dot(j.normalized, (travel + 0.5f * r.normalized).normalized), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(MovingGround.BoxImpulse(r, -15f, 5, B, H).magnitude, Is.EqualTo(50f * B.Knockback(5)).Within(1e-3f));
            // Δv for Heavy / Normal / Light at h = 0 (GDD 6.5: 3.3 / 5.0 / 7.1 m/s).
            Assert.That(50f / 15f, Is.EqualTo(3.3f).Within(0.05f));
        }
    }
}
