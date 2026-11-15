using System.Collections.Generic;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-12 / T02: the pure pair resolution of CollisionResolver against GDD 6.3.</summary>
    public sealed class CollisionResolverTests
    {
        const string Data = "Assets/_Project/Data";
        const float Tolerance = 0.02f; // T02: within 2%

        static readonly BalanceProfile Balance =
            AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Data}/Balance/BalanceProfile.asset");

        static FighterBody Body(string fighter, float x, float velocityX, int wobble = 0, bool dashing = false)
        {
            var c = AssetDatabase.LoadAssetAtPath<ClassProfile>($"{Data}/Classes/{fighter}.asset");
            return new FighterBody
            {
                Position = new Vector3(x, c.Radius, 0f),
                Velocity = new Vector3(velocityX, 0f, 0f),
                Mass = c.Mass,
                Radius = c.Radius,
                Wobble = wobble,
                Dashing = dashing,
            };
        }

        static void AssertWithinPercent(float actual, float expected) =>
            Assert.That(actual, Is.EqualTo(expected).Within(Mathf.Abs(expected) * Tolerance));

        [Test]
        public void HeadOn_KnownSpeeds_MatchesImpulseFormula()
        {
            // Two Normal fruits (10 kg), +5 and −3 m/s: j = 1.6 × 8 / 0.2 = 64 N·s.
            var a = Body("Normal", 0f, 5f);
            var b = Body("Normal", 0.9f, -3f);

            var touching = CollisionResolver.Resolve(ref a, ref b, Balance, out var impulseA, out var impulseB);

            Assert.That(touching, Is.True);
            AssertWithinPercent(impulseA, 64f);
            AssertWithinPercent(impulseB, 64f);
            AssertWithinPercent(a.Velocity.x, -1.4f);
            AssertWithinPercent(b.Velocity.x, 3.4f);
        }

        // GDD 6.3 worked example: a dashing Normal fruit closes at 10 m/s on a stationary target.
        [TestCase("Heavy", 0, 120.0f, 8.0f, 9.6f)]
        [TestCase("Normal", 0, 100.0f, 10.0f, 8.0f)]
        [TestCase("Light", 0, 82.4f, 11.8f, 6.6f)]
        [TestCase("Heavy", 5, 192.0f, 12.8f, 9.6f)]
        [TestCase("Normal", 5, 160.0f, 16.0f, 8.0f)]
        [TestCase("Light", 5, 131.8f, 18.8f, 6.6f)]
        public void DashHit_OnStationaryTarget_MatchesWorkedExample(string target, int wobble,
            float receivedImpulse, float targetDeltaV, float attackerRecoil)
        {
            var attacker = Body("Normal", 0f, 10f, dashing: true);
            var victim = Body(target, 0.9f, 0f, wobble);

            CollisionResolver.Resolve(ref attacker, ref victim, Balance, out _, out var impulse);

            AssertWithinPercent(impulse, receivedImpulse);
            AssertWithinPercent(victim.Velocity.x, targetDeltaV);
            AssertWithinPercent(10f - attacker.Velocity.x, attackerRecoil);
        }

        [Test]
        public void Knockback_AtHighWobble_IsCapped()
        {
            var attacker = Body("Normal", 0f, 10f);
            var victim = Body("Normal", 0.9f, 0f, wobble: Balance.WobbleMax);

            CollisionResolver.Resolve(ref attacker, ref victim, Balance, out _, out var impulse);

            AssertWithinPercent(impulse, 80f * Balance.KnockbackCap);
        }

        [Test]
        public void Apart_NoContact_ChangesNothing()
        {
            var a = Body("Normal", 0f, 5f);
            var b = Body("Normal", 1.01f, -5f);

            var touching = CollisionResolver.Resolve(ref a, ref b, Balance, out _, out _);

            Assert.That(touching, Is.False);
            Assert.That(a.Velocity.x, Is.EqualTo(5f));
            Assert.That(b.Position.x, Is.EqualTo(1.01f));
        }

        [Test]
        public void Separating_Overlap_GetsNoImpulseButIsPushedApart()
        {
            var a = Body("Normal", 0f, -1f);
            var b = Body("Normal", 0.9f, 1f);

            CollisionResolver.Resolve(ref a, ref b, Balance, out var impulseA, out var impulseB);

            Assert.That(impulseA, Is.Zero);
            Assert.That(impulseB, Is.Zero);
            Assert.That(a.Velocity.x, Is.EqualTo(-1f));
            Assert.That(b.Position.x - a.Position.x, Is.GreaterThan(0.9f));
        }

        [Test]
        public void Overlap_IsCorrectedEightyPercentBeyondSlopByInverseMass()
        {
            // Heavy (r 0.60, 15 kg) and Light (r 0.45, 7 kg), centres 0.95 m apart: overlap 0.10 m.
            var heavy = Body("Heavy", 0f, 0f);
            var light = Body("Light", 0.95f, 0f);
            light.Position.y = heavy.Position.y;

            CollisionResolver.Resolve(ref heavy, ref light, Balance, out _, out _);

            var total = (0.10f - 0.01f) * 0.8f;
            Assert.That(light.Position.x - heavy.Position.x, Is.EqualTo(0.95f + total).Within(1e-4f));
            // The lighter fruit moves more: shares are 7/22 and 15/22.
            Assert.That(-heavy.Position.x, Is.EqualTo(total * 7f / 22f).Within(1e-4f));
            Assert.That(light.Position.x - 0.95f, Is.EqualTo(total * 15f / 22f).Within(1e-4f));
        }

        [Test]
        public void Overlap_InsideSlop_IsLeftAlone()
        {
            var a = Body("Normal", 0f, 0f);
            var b = Body("Normal", 0.995f, 0f);

            CollisionResolver.Resolve(ref a, ref b, Balance, out _, out _);

            Assert.That(a.Position.x, Is.Zero);
            Assert.That(b.Position.x, Is.EqualTo(0.995f));
        }

        [Test]
        public void LightTouch_BelowThreshold_IsNotAHit()
        {
            // Closing at 1 m/s: j = 1.6 × 1 / 0.2 = 8 N·s < 30 N·s.
            var bodies = new[] { Body("Normal", 0f, 1f), Body("Normal", 0.9f, 0f) };
            var hits = new List<BodyHit>();

            CollisionResolver.ResolveAll(bodies, 2, Balance, hits);

            Assert.That(hits, Is.Empty);
            Assert.That(bodies[1].Velocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void SixFighters_EveryPairIsTested()
        {
            // All six in one spot and at rest: each of the 15 pairs overlaps, so every body must move,
            // and no impulse (hence no hit) may appear.
            var bodies = new FighterBody[6];
            for (var i = 0; i < 6; i++) bodies[i] = Body("Normal", i * 0.5f, 0f);
            var hits = new List<BodyHit>();

            CollisionResolver.ResolveAll(bodies, 6, Balance, hits);

            Assert.That(hits, Is.Empty);
            for (var i = 0; i < 5; i++)
                Assert.That(bodies[i + 1].Position.x - bodies[i].Position.x, Is.GreaterThan(0.5f), $"pair {i}");

            // Three separate head-on pairs in one pass: both sides of each pair are hit.
            for (var i = 0; i < 6; i++)
                bodies[i] = Body("Normal", (i / 2) * 10f + (i % 2) * 0.9f, i % 2 == 0 ? 5f : -5f);
            CollisionResolver.ResolveAll(bodies, 6, Balance, hits);

            Assert.That(hits, Has.Count.EqualTo(6));
        }
    }
}
