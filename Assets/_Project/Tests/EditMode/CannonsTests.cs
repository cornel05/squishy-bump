using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-42 / SB-43: cannon schedule, gust, blast and ballistics of GDD 7.3 / 6.5.</summary>
    public sealed class CannonsTests
    {
        const float Dt = 1f / 60f;
        static HazardProfile H => AssetDatabase.LoadAssetAtPath<HazardProfile>("Assets/_Project/Data/Hazards/HazardProfile.asset");
        static BalanceProfile B => AssetDatabase.LoadAssetAtPath<BalanceProfile>("Assets/_Project/Data/Balance/BalanceProfile.asset");

        [Test]
        public void Profile_HoldsGddValues()
        {
            Assert.That((H.CannonRimRadius, H.CannonSweepDegrees, H.CannonSweepDegreesPerSecond, H.CannonPeriodSeconds,
                H.CannonChaosPeriodSeconds, H.CannonStaggerSeconds, H.CannonTelegraphSeconds),
                Is.EqualTo((9.9f, 60f, 30f, 5f, 4f, 1.25f, 1f)));
            Assert.That((H.AirConeHalfAngle, H.AirRange, H.AirGustSeconds, H.AirForce), Is.EqualTo((15f, 10f, 0.6f, 150f)));
            Assert.That((H.FireLaunchAngle, H.FireMinRange, H.FireMaxRange, H.FireFallbackRange), Is.EqualTo((45f, 10f, 16f, 13f)));
            Assert.That((H.BlastRadius, H.BlastImpulse, H.BlastUpShare, H.BlastWobble, H.PatchRadius, H.PatchSeconds),
                Is.EqualTo((3f, 90f, 0.3f, 2, 2f, 3f)));
        }

        [Test]
        public void Sweep_IsPlusMinusSixtyAtThirtyDegreesPerSecond()
        {
            Assert.That(Cannons.SweepOffset(0f, H), Is.EqualTo(0f));
            Assert.That(Cannons.SweepOffset(2f, H), Is.EqualTo(60f).Within(1e-3f));
            Assert.That(Cannons.SweepOffset(4f, H), Is.EqualTo(0f).Within(1e-3f));
            Assert.That(Cannons.SweepOffset(6f, H), Is.EqualTo(-60f).Within(1e-3f));
            Assert.That(Cannons.SweepOffset(1f, H) - Cannons.SweepOffset(0.9f, H), Is.EqualTo(3f).Within(1e-3f), "30°/s");
        }

        [Test]
        public void FireTicks_EveryFiveSecondsStaggeredByOneAndAQuarter()
        {
            Assert.That(Cannons.FireTick(0, 0, 5f, 1.25f, Dt), Is.EqualTo(300));
            Assert.That(Cannons.FireTick(1, 0, 5f, 1.25f, Dt), Is.EqualTo(375));
            Assert.That(Cannons.FireTick(3, 0, 5f, 1.25f, Dt), Is.EqualTo(525));
            Assert.That(Cannons.FireTick(0, 1, 5f, 1.25f, Dt), Is.EqualTo(600));
            Assert.That(Cannons.FireTick(1, 2, 4f, 1.25f, Dt), Is.EqualTo(795), "Chaos Core period");
        }

        [Test]
        public void Gust_FallsOffLinearlyAndScalesWithWobble()
        {
            Assert.That(Cannons.GustForce(0f, 0, B, H), Is.EqualTo(150f));
            Assert.That(Cannons.GustForce(5f, 0, B, H), Is.EqualTo(75f));
            Assert.That(Cannons.GustForce(10f, 0, B, H), Is.EqualTo(0f));
            Assert.That(Cannons.GustForce(5f, 5, B, H), Is.EqualTo(75f * B.Knockback(5)).Within(1e-3f));
            Assert.That(Cannons.GustForce(0f, 0, B, H) * H.AirGustSeconds, Is.EqualTo(90f).Within(1e-3f), "J = F·t_g");
        }

        [Test]
        public void Blast_IsOutwardWithThirtyPercentUp()
        {
            var j = Cannons.BlastImpulse(new Vector3(1.5f, 0f, 0f), 0, B, H);
            Assert.That(j.magnitude, Is.EqualTo(45f).Within(1e-3f));
            Assert.That(j.y / j.x, Is.EqualTo(0.3f).Within(1e-4f));
            Assert.That(Cannons.BlastImpulse(new Vector3(0f, 0f, 3.1f), 0, B, H).magnitude, Is.EqualTo(0f));
        }

        [Test]
        public void Launch_AtFortyFiveDegrees_MatchesTheWorkedExample()
        {
            var v = Cannons.LaunchVelocity(Vector3.zero, new Vector3(12f, 0f, 0f), 45f, B.Gravity);
            Assert.That(v.magnitude, Is.EqualTo(15.5f).Within(0.05f), "v₀ = √(g·d)");
            Assert.That(Cannons.FlightSeconds(Vector3.zero, new Vector3(12f, 0f, 0f), 45f, B.Gravity), Is.EqualTo(1.10f).Within(0.01f));
            // Fixed-step integration with the same gravity lands within a few centimetres.
            var p = Vector3.zero;
            var vel = v;
            while (true)
            {
                vel += Vector3.down * (B.Gravity * Dt);
                p += vel * Dt;
                if (p.y <= 0f) break;
            }
            Assert.That(p.x, Is.EqualTo(12f).Within(0.3f));
        }

        [Test]
        public void Compass_ZeroIsNorthNinetyIsEast()
        {
            Assert.That(Cannons.Compass(0f), Is.EqualTo(Vector3.forward));
            Assert.That(Vector3.Distance(Cannons.Compass(90f), Vector3.right), Is.LessThan(1e-5f));
        }
    }
}
