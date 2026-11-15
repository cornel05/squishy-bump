#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-12: CollisionResolver on real fighter prefabs (T02, Wobble, stun).</summary>
    public sealed class CollisionTests
    {
        const string Root = "Assets/_Project";
        readonly List<GameObject> spawned = new List<GameObject>();
        CollisionResolver resolver;

        static BalanceProfile Balance =>
            AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");

        [SetUp]
        public void CreateResolver()
        {
            var host = new GameObject("Resolver");
            spawned.Add(host);
            resolver = host.AddComponent<CollisionResolver>();
            resolver.Configure(Balance);
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            foreach (var go in spawned) Object.Destroy(go);
            spawned.Clear();
            yield return null;
        }

        Rigidbody Spawn(ulong id, string fruit, float x, float velocityX)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/{fruit}.prefab");
            var fighter = Object.Instantiate(prefab, new Vector3(x, 5f, 0f), Quaternion.identity);
            spawned.Add(fighter);
            var body = fighter.GetComponent<Rigidbody>();
            body.useGravity = false;
            body.linearVelocity = new Vector3(velocityX, 0f, 0f);
            resolver.Register(id, fighter.GetComponent<PlayerMotor>());
            return body;
        }

        [UnityTest]
        public IEnumerator HeadOn_KnownSpeeds_MatchesFormulaWithinTwoPercent()
        {
            var a = Spawn(1, "Apple", 0f, 5f);
            var b = Spawn(2, "Mangosteen", 0.9f, -3f);
            var hits = new List<FighterHit>();
            resolver.Hit += hits.Add;
            yield return null;

            resolver.Step(Time.fixedDeltaTime);

            Assert.That(a.linearVelocity.x, Is.EqualTo(-1.4f).Within(1.4f * 0.02f));
            Assert.That(b.linearVelocity.x, Is.EqualTo(3.4f).Within(3.4f * 0.02f));
            Assert.That(hits, Has.Count.EqualTo(2));
            Assert.That(resolver.GetWobble(1), Is.EqualTo(1));
            Assert.That(resolver.GetWobble(2), Is.EqualTo(1));
            Assert.That(a.GetComponent<PlayerMotor>().IsStunned, Is.True);
        }

        [UnityTest]
        public IEnumerator Hit_StunsForBasePlusWobbleAndIgnoresInput()
        {
            var a = Spawn(1, "Apple", 0f, 5f);
            Spawn(2, "Mangosteen", 0.9f, -3f);
            var motor = a.GetComponent<PlayerMotor>();
            yield return null;

            resolver.Step(Time.fixedDeltaTime);

            // h = 1 after the hit: 0.15 + 0.02 = 0.17 s = 10 ticks.
            var stunTicks = Mathf.RoundToInt(Balance.StunSeconds(1) / Time.fixedDeltaTime);
            for (var i = 0; i < stunTicks; i++)
            {
                Assert.That(motor.IsStunned, Is.True, $"tick {i}");
                motor.Step(default, Time.fixedDeltaTime);
            }
            Assert.That(motor.IsStunned, Is.False);
        }

        [UnityTest]
        public IEnumerator OverlappingFighters_AreNotSeparatedByPhysX()
        {
            var a = Spawn(1, "Apple", 0f, 0f);
            var b = Spawn(2, "Mangosteen", 0.5f, 0f);

            for (var i = 0; i < 10; i++) yield return new WaitForFixedUpdate();

            Assert.That(a.position.x, Is.Zero.Within(1e-4f));
            Assert.That(b.position.x, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(a.linearVelocity, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator Wobble_DecaysEverySixSecondsAndIsCapped()
        {
            Spawn(1, "Apple", 0f, 0f);
            var changes = new List<int>();
            resolver.WobbleChanged += (_, wobble) => changes.Add(wobble);
            var decayTicks = Mathf.RoundToInt(Balance.WobbleDecaySeconds / Time.fixedDeltaTime);
            yield return null;

            resolver.AddWobble(1, 2);
            resolver.AddWobble(1, 50);
            Assert.That(resolver.GetWobble(1), Is.EqualTo(Balance.WobbleMax));

            for (var i = 0; i < decayTicks - 1; i++) resolver.Step(Time.fixedDeltaTime);
            Assert.That(resolver.GetWobble(1), Is.EqualTo(Balance.WobbleMax));
            resolver.Step(Time.fixedDeltaTime);
            Assert.That(resolver.GetWobble(1), Is.EqualTo(Balance.WobbleMax - 1));

            // A new hit source restarts the 6 s window.
            for (var i = 0; i < decayTicks - 1; i++) resolver.Step(Time.fixedDeltaTime);
            resolver.AddWobble(1, 1);
            for (var i = 0; i < decayTicks - 1; i++) resolver.Step(Time.fixedDeltaTime);
            Assert.That(resolver.GetWobble(1), Is.EqualTo(Balance.WobbleMax));

            resolver.ResetWobble(1);
            Assert.That(resolver.GetWobble(1), Is.Zero);
            CollectionAssert.AreEqual(new[] { 2, 10, 9, 10, 0 }, changes);
        }
    }
}
#endif
