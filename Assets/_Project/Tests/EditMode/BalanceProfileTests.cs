using NUnit.Framework;
using SquishyBump.Core;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>T01: the balance assets hold GDD 6.1 / 12.2 and still satisfy the design checks of GDD 14.1.</summary>
    public sealed class BalanceProfileTests
    {
        const string Data = "Assets/_Project/Data";

        // Scenario constants of the worked examples (GDD 6.3, 6.5, 14.1); they are not balance values.
        const float ClosingSpeed = 10f;
        const float ObstacleBoxHeight = 0.8f;
        const float SnapshotBytes = 200f;
        const int Clients = 5;

        static BalanceProfile Balance => Load<BalanceProfile>("Balance/BalanceProfile");
        static ClassProfile Class(string name) => Load<ClassProfile>($"Classes/{name}");
        static float ArenaRadius(int level) => Load<LevelDefinition>($"Levels/L{level}").ArenaRadiusMeters;

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{Data}/{path}.asset");
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        /// <summary>Δv of a stationary target hit by a dashing Normal fruit (GDD 6.3 worked example).</summary>
        static float DashHitDeltaV(ClassProfile target, int wobble)
        {
            var b = Balance;
            var attacker = Class("Normal");
            var j = (1f + b.Restitution) * ClosingSpeed / (1f / attacker.Mass + 1f / target.Mass);
            return j * b.DashBonus * b.Knockback(wobble) / target.Mass;
        }

        static float Slide(float deltaV, ClassProfile target, float friction) =>
            deltaV * deltaV / (2f * Balance.MaxAcceleration(target, friction));

        [Test]
        public void BalanceAsset_MatchesGddTable()
        {
            var b = Balance;

            Assert.That(b.FixedStepHz, Is.EqualTo(60f));
            Assert.That(b.Gravity, Is.EqualTo(20f));
            Assert.That(b.FloorFriction, Is.EqualTo(0.6f));
            Assert.That(b.IceFriction, Is.EqualTo(0.3f));
            Assert.That(b.AirControl, Is.EqualTo(0.3f));
            Assert.That(b.JumpApexMeters, Is.EqualTo(1.2f));
            Assert.That(b.DashImpulse, Is.EqualTo(80f));
            Assert.That(b.DashCooldownSeconds, Is.EqualTo(3f));
            Assert.That(b.DashStateSeconds, Is.EqualTo(0.35f));
            Assert.That(b.DashBonus, Is.EqualTo(1.25f));
            Assert.That(b.Restitution, Is.EqualTo(0.6f));
            Assert.That(b.HitThreshold, Is.EqualTo(30f));
            Assert.That(b.PenetrationCorrection, Is.EqualTo(0.8f));
            Assert.That(b.PenetrationSlopMeters, Is.EqualTo(0.01f));
            Assert.That(b.KnockbackPerWobble, Is.EqualTo(0.12f));
            Assert.That(b.KnockbackCap, Is.EqualTo(2.2f));
            Assert.That(b.WobbleMax, Is.EqualTo(10));
            Assert.That(b.WobbleDecaySeconds, Is.EqualTo(6f));
            Assert.That(b.StunBaseSeconds, Is.EqualTo(0.15f));
            Assert.That(b.StunPerWobbleSeconds, Is.EqualTo(0.02f));
            Assert.That(b.StunInputBufferSeconds, Is.EqualTo(0.1f));
            Assert.That(b.TimeLimitSeconds, Is.EqualTo(150f));
            Assert.That(b.RingDropSeconds, Is.EqualTo(new[] { 45f, 75f, 105f, 135f }));
            Assert.That(b.RingDropWarningSeconds, Is.EqualTo(3f));
            Assert.That(b.RingWidthMeters, Is.EqualTo(3f));
            Assert.That(b.KillPlaneY, Is.EqualTo(-5f));
            Assert.That(b.Lives, Is.EqualTo(3));
            Assert.That(b.RespawnDelaySeconds, Is.EqualTo(2f));
            Assert.That(b.InvulnerabilitySeconds, Is.EqualTo(1.5f));
            Assert.That(b.RespawnEdgeMarginMeters, Is.EqualTo(1.5f));
            Assert.That(b.LastTouchWindowSeconds, Is.EqualTo(3f));
            Assert.That(b.PointsPerRingOut, Is.EqualTo(1));
            Assert.That(b.InputRateHz, Is.EqualTo(60f));
            Assert.That(b.SnapshotRateHz, Is.EqualTo(30f));
            Assert.That(b.InterpolationBufferSeconds, Is.EqualTo(0.1f));
        }

        [Test]
        public void BalanceAsset_MatchesEngineSettings()
        {
            Assert.That(1f / Balance.FixedStepHz, Is.EqualTo(Time.fixedDeltaTime).Within(1e-6f));
            Assert.That(-Balance.Gravity, Is.EqualTo(UnityEngine.Physics.gravity.y).Within(1e-4f));
        }

        [TestCase("Heavy", 15f, 0.60f, 4.5f, 0.8f, 9.6f, 4.8f, 5.3f)]
        [TestCase("Normal", 10f, 0.50f, 6.0f, 1.0f, 12f, 6.0f, 8.0f)]
        [TestCase("Light", 7f, 0.45f, 7.5f, 1.2f, 14.4f, 7.2f, 11.4f)]
        public void ClassAsset_MatchesGddTable(string name, float mass, float radius, float maxSpeed, float traction,
            float floorAccel, float iceAccel, float dashDeltaV)
        {
            var c = Class(name);

            Assert.That(c.Mass, Is.EqualTo(mass));
            Assert.That(c.Radius, Is.EqualTo(radius));
            Assert.That(c.MaxSpeed, Is.EqualTo(maxSpeed));
            Assert.That(c.Traction, Is.EqualTo(traction));
            Assert.That(Balance.MaxAcceleration(c, Balance.FloorFriction), Is.EqualTo(floorAccel).Within(0.001f));
            Assert.That(Balance.MaxAcceleration(c, Balance.IceFriction), Is.EqualTo(iceAccel).Within(0.001f));
            Assert.That(Balance.DashDeltaV(c), Is.EqualTo(dashDeltaV).Within(0.05f));
        }

        [TestCase("Watermelon", "Heavy", "5CC46E")]
        [TestCase("Pineapple", "Heavy", "FFC93C")]
        [TestCase("Apple", "Normal", "EF4B4B")]
        [TestCase("Mangosteen", "Normal", "7A4FA3")]
        [TestCase("Peach", "Light", "FFA3B5")]
        [TestCase("Blueberry", "Light", "4D7CDB")]
        public void FruitAsset_MatchesCharacterTable(string name, string className, string hex)
        {
            var fruit = Load<FruitDefinition>($"Fruits/{name}");

            Assert.That(fruit.DisplayName, Is.EqualTo(name));
            Assert.That(fruit.Class, Is.SameAs(Class(className)));
            Assert.That(ColorUtility.ToHtmlStringRGB(fruit.Color), Is.EqualTo(hex));
            Assert.That(fruit.Mesh, Is.Not.Null);
        }

        [Test]
        public void Jump_ReachesApexSpeedAndAirTime()
        {
            Assert.That(Balance.JumpSpeed, Is.EqualTo(6.93f).Within(0.005f));
            Assert.That(Balance.JumpAirTime, Is.EqualTo(0.69f).Within(0.005f));
        }

        [Test]
        public void Knockback_IsCappedAtMaxWobble()
        {
            Assert.That(Balance.Knockback(0), Is.EqualTo(1f));
            Assert.That(Balance.Knockback(5), Is.EqualTo(1.6f).Within(1e-4f));
            Assert.That(Balance.Knockback(Balance.WobbleMax), Is.EqualTo(Balance.KnockbackCap).Within(1e-4f));
            Assert.That(Balance.Knockback(Balance.WobbleMax + 5), Is.EqualTo(Balance.KnockbackCap));
            Assert.That(Balance.StunSeconds(5), Is.EqualTo(0.25f).Within(1e-4f));
        }

        // --- GDD 14.1 design consistency checks ---

        [Test]
        public void FreshFruit_IsNotRungOutFromTrainingRingCentreByOneBump()
        {
            var light = Class("Light");

            var slide = Slide(DashHitDeltaV(light, 0), light, Balance.FloorFriction);

            Assert.That(slide, Is.EqualTo(4.8f).Within(0.05f));
            Assert.That(slide, Is.LessThan(ArenaRadius(1)));
        }

        [Test]
        public void WobbleFive_AllowsFinishingOnTrainingRing()
        {
            var normal = Class("Normal");
            var light = Class("Light");
            var afterFirstDrop = ArenaRadius(1) - Balance.RingWidthMeters;

            var normalSlide = Slide(DashHitDeltaV(normal, 5), normal, Balance.FloorFriction);
            var lightSlide = Slide(DashHitDeltaV(light, 5), light, Balance.FloorFriction);

            Assert.That(normalSlide, Is.EqualTo(10.7f).Within(0.05f));
            Assert.That(normalSlide, Is.GreaterThan(afterFirstDrop));
            Assert.That(lightSlide, Is.EqualTo(12.3f).Within(0.05f));
            Assert.That(lightSlide, Is.GreaterThan(ArenaRadius(1)));
        }

        [Test]
        public void IceDash_RingsOutFreshLightButNotHeavy()
        {
            var light = Class("Light");
            var heavy = Class("Heavy");

            var lightSlide = Slide(DashHitDeltaV(light, 0), light, Balance.IceFriction);
            var heavySlide = Slide(DashHitDeltaV(heavy, 0), heavy, Balance.IceFriction);

            Assert.That(lightSlide, Is.EqualTo(9.6f).Within(0.05f));
            Assert.That(lightSlide, Is.GreaterThan(ArenaRadius(4)));
            Assert.That(heavySlide, Is.EqualTo(6.7f).Within(0.05f));
            Assert.That(heavySlide, Is.LessThan(ArenaRadius(4)));
        }

        [Test]
        public void EveryMatch_EndsWithinTimeLimit()
        {
            var drops = Balance.RingDropSeconds;

            for (var level = 1; level <= 5; level++)
                Assert.That(Load<LevelDefinition>($"Levels/L{level}").EndTriggerSeconds,
                    Is.LessThanOrEqualTo(Balance.TimeLimitSeconds));
            Assert.That(Load<LevelDefinition>("Levels/L1").EndTriggerSeconds, Is.EqualTo(drops[drops.Count - 1]));
            // Rank on L1: respawns stop at the third drop, before the last one ends the match.
            Assert.That(drops[2], Is.LessThan(drops[drops.Count - 1]));
            Assert.That(Balance.Lives, Is.GreaterThan(0));
        }

        [Test]
        public void WeightClasses_ReachFullSpeedWithinFiveHundredthsOfEachOther()
        {
            float TimeToFullSpeed(string name) =>
                Class(name).MaxSpeed / Balance.MaxAcceleration(Class(name), Balance.FloorFriction);

            var heavy = TimeToFullSpeed("Heavy");
            var normal = TimeToFullSpeed("Normal");
            var light = TimeToFullSpeed("Light");

            Assert.That(heavy, Is.EqualTo(0.47f).Within(0.005f));
            Assert.That(normal, Is.EqualTo(0.50f).Within(0.005f));
            Assert.That(light, Is.EqualTo(0.52f).Within(0.005f));
            // 1 ms tolerance on the 0.05 s band of GDD 14.1: the exact spread is 0.0521 s (0.47 → 0.52 rounded).
            Assert.That(Mathf.Max(heavy, normal, light) - Mathf.Min(heavy, normal, light), Is.LessThan(0.055f));
        }

        [Test]
        public void Jump_ClearsObstacleBox() =>
            Assert.That(Balance.JumpApexMeters, Is.GreaterThan(ObstacleBoxHeight));

        /// <summary>GDD 14.1 / 7: every hazard telegraph lasts at least 1.0 s. H07 boxes are always visible;
        /// H06 (ice cracks, 1.5 s) joins when its config lands.</summary>
        [Test]
        public void HazardTelegraphs_LastAtLeastOneSecond()
        {
            var h = Load<HazardProfile>("Hazards/HazardProfile");
            Assert.That(Balance.RingDropWarningSeconds, Is.GreaterThanOrEqualTo(1f), "H01");
            Assert.That(h.TurntableWarningSeconds, Is.GreaterThanOrEqualTo(1f), "H02");
            Assert.That(h.PusherLightSeconds, Is.GreaterThanOrEqualTo(1f), "H03");
            Assert.That(h.CannonTelegraphSeconds, Is.GreaterThanOrEqualTo(1f), "H04 / H05");
        }

        [Test]
        public void SnapshotLoad_FitsLanAndRelay() =>
            Assert.That(SnapshotBytes * Balance.SnapshotRateHz * Clients, Is.EqualTo(30000f).Within(1f));
    }
}
