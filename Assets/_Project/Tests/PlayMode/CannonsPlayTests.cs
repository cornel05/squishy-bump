#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-42 / SB-43: the L3 cannon layout on host ticks with two fruits.</summary>
    public sealed class CannonsPlayTests
    {
        const string Root = "Assets/_Project";
        GameObject root;

        sealed class Sink : IGameEventSink
        {
            uint id;
            public readonly List<HazardEvent> Events = new();
            public uint NextEventId() => ++id;
            public void Publish(HazardEvent e) => Events.Add(e);
            public void Publish(MatchEvent e) { }
        }

        sealed class Fighters : IFighterPhysics
        {
            public readonly Dictionary<ulong, Vector3> Positions = new();
            public readonly Dictionary<ulong, int> Wobble = new();
            public readonly Dictionary<ulong, List<Vector3>> Impulses = new();
            public IReadOnlyCollection<ulong> FighterIds => Positions.Keys;
            public Vector3 GetPosition(ulong id) => Positions[id];
            public bool TryGetFighter(Collider c, out ulong id) { id = 0; return false; }
            public int GetWobble(ulong id) => Wobble.TryGetValue(id, out var w) ? w : 0;
            public void AddImpulse(ulong id, Vector3 j)
            {
                if (!Impulses.TryGetValue(id, out var list)) Impulses[id] = list = new List<Vector3>();
                list.Add(j);
            }
            public void AddWobble(ulong id, int n) => Wobble[id] = GetWobble(id) + n;
            public float Total(ulong id) => Impulses.TryGetValue(id, out var l) ? l.ConvertAll(v => v.magnitude).Sum() : 0f;
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CourtLayout_AirGustsAndFireballLandsOnTarget()
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");
            var h = AssetDatabase.LoadAssetAtPath<HazardProfile>($"{Root}/Data/Hazards/HazardProfile.asset");
            root = new GameObject("Cannons");
            root.SetActive(false);
            var cannons = root.AddComponent<Cannons>();
            cannons.Configure(balance, h, new[]
            {
                new Cannons.Mount { Kind = CannonKind.Fire, AngleDegrees = 45f },
                new Cannons.Mount { Kind = CannonKind.Air, AngleDegrees = 135f },
                new Cannons.Mount { Kind = CannonKind.Fire, AngleDegrees = 225f },
                new Cannons.Mount { Kind = CannonKind.Air, AngleDegrees = 315f },
            }, false);
            root.SetActive(true);
            yield return null;

            // A: on fire cannon 0's home line, 12 m out (its sweep is at 0° when it locks at 4 s).
            var muzzle0 = cannons.Muzzle(0);
            var a = muzzle0 - Cannons.Compass(45f) * 12f;
            a.y = 0.5f;
            // B: 5 m into air cannon 1's cone as it locks at 5.25 s.
            var aimDir = Quaternion.Euler(0f, Cannons.SweepOffset(5.25f, h), 0f) * -Cannons.Compass(135f);
            var b = cannons.Muzzle(1) + aimDir * 5f;
            b.y = 0.5f;
            var fighters = new Fighters();
            fighters.Positions[1] = a;
            fighters.Positions[2] = b;
            var sink = new Sink();
            cannons.Bind(sink, fighters);

            var dt = Time.fixedDeltaTime;
            Vector3? landing = null;
            for (var tick = 1; tick <= 480; tick++)
            {
                var wasFlying = cannons.BallFlying(0);
                var ball = cannons.BallPosition(0);
                cannons.StepOnHost(tick, (uint)(1000 + tick), dt);
                if (wasFlying && !cannons.BallFlying(0)) landing = ball;
            }

            var aim0 = sink.Events.Find(e => e.Kind == HazardEventKind.CannonAim && e.Index == 0);
            Assert.That(aim0.HostTick, Is.EqualTo(1300u), "aim event carries the fire tick (5 s)");
            Assert.That(sink.Events.FindIndex(e => e.Kind == HazardEventKind.CannonAim && e.Index == 0),
                Is.LessThan(sink.Events.FindIndex(e => e.Kind == HazardEventKind.CannonFire && e.Index == 0)));
            Assert.That(Vector3.Distance(new Vector3(aim0.Aim.x, 0f, aim0.Aim.z), new Vector3(a.x, 0f, a.z)), Is.LessThan(0.01f),
                "fire cannon locked on the fruit in its 10–16 m band");
            var aimFire = sink.Events.FindAll(e => e.Kind == HazardEventKind.CannonFire).ConvertAll(e => e.HostTick - 1000);
            Assert.That(aimFire, Is.EquivalentTo(new uint[] { 300, 375, 450 }), "staggered by 1.25 s");

            Assert.That(landing.HasValue, "fireball landed");
            Assert.That(Vector2.Distance(new Vector2(landing.Value.x, landing.Value.z), new Vector2(a.x, a.z)), Is.LessThan(0.4f));
            Assert.That(fighters.Total(1), Is.GreaterThan(0f).And.LessThanOrEqualTo(90f), "blast J ≤ J₀");
            // +2 from the blast, +1 after one full second in the burning patch.
            Assert.That(fighters.Wobble[1], Is.EqualTo(3));

            Assert.That(fighters.Total(2), Is.InRange(44f, 52f), "0.6 s at 75 N ≈ 45 N·s");
            Assert.That(fighters.Wobble[2], Is.EqualTo(1), "+1 once J ≥ 30 N·s");
        }
    }
}
#endif
