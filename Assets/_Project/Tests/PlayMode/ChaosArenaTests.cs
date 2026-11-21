using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class ChaosArenaTests
    {
        private sealed class EmptyArena : IFighterPhysics, IGameEventSink
        {
            private uint id;
            public uint Tick;
            public readonly List<(uint sentAt, HazardEvent e)> Events = new();
            public IReadOnlyCollection<ulong> FighterIds { get; } = new ulong[0];
            public Vector3 GetPosition(ulong fighterId) => Vector3.zero;
            public bool TryGetFighter(Collider collider, out ulong fighterId) { fighterId = 0; return false; }
            public int GetWobble(ulong fighterId) => 0;
            public void AddImpulse(ulong fighterId, Vector3 impulse) { }
            public void AddWobble(ulong fighterId, int amount) { }
            public uint NextEventId() => ++id;
            public void Publish(HazardEvent e) => Events.Add((Tick, e));
            public void Publish(MatchEvent e) { }
        }

        [UnityTest]
        public IEnumerator L5_RealSceneReuses61IceTilesAndWestAirEastFire_AllSpawnsHaveFloor()
        {
            yield return SceneManager.LoadSceneAsync("L5", LoadSceneMode.Single);
            var ice = Object.FindFirstObjectByType<IceTiles>();
            var cannons = Object.FindFirstObjectByType<Cannons>();
            var bounds = Object.FindFirstObjectByType<TrainingRingBounds>();
            Assert.That(ice, Is.Not.Null, "L5 phải được tạo bằng IceSceneBuilder.BuildL5.");
            Assert.That(ice.TileCount, Is.EqualTo(61));
            Assert.That(cannons, Is.Not.Null);
            Assert.That(cannons.Count, Is.EqualTo(2));
            Assert.That(cannons.Kind(0), Is.EqualTo(CannonKind.Air));
            Assert.That(cannons.Kind(1), Is.EqualTo(CannonKind.Fire));
            Assert.That(cannons.Muzzle(0).x, Is.EqualTo(-9.9f).Within(0.001f));
            Assert.That(cannons.Muzzle(1).x, Is.EqualTo(9.9f).Within(0.001f));
            Assert.That(cannons.Muzzle(0).z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(cannons.Muzzle(1).z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(Object.FindObjectsByType<MovingGround>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<RingDrop>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(bounds.SpawnPoints.Count, Is.EqualTo(6));
            UnityEngine.Physics.SyncTransforms();
            foreach (var spawn in bounds.SpawnPoints)
            {
                Assert.That(new Vector2(spawn.position.x, spawn.position.z).magnitude,
                    Is.EqualTo(6f).Within(0.001f));
                Assert.That(UnityEngine.Physics.Raycast(spawn.position + Vector3.up, Vector3.down,
                    out var hit, 2f), Is.True);
                Assert.That(hit.collider.GetComponentInParent<IceTiles>(), Is.SameAs(ice));
                Assert.That(hit.collider.sharedMaterial.dynamicFriction, Is.EqualTo(0.3f));
            }
        }

        [UnityTest]
        public IEnumerator L5_150Seconds_IceWavesEvery210TicksAndCannonsEvery240Ticks()
        {
            yield return SceneManager.LoadSceneAsync("L5", LoadSceneMode.Single);
            var ice = Object.FindFirstObjectByType<IceTiles>();
            var cannons = Object.FindFirstObjectByType<Cannons>();
            Assert.That(ice, Is.Not.Null); Assert.That(cannons, Is.Not.Null);
            var arena = new EmptyArena();
            ice.SetSeed(18); ice.Bind(arena, arena); cannons.Bind(arena, arena);
            for (var tick = 1; tick <= 9000; tick++)
            {
                arena.Tick = (uint)tick;
                ice.StepOnHost(tick, (uint)tick, 1f / 60);
                cannons.StepOnHost(tick, (uint)tick, 1f / 60);
                Assert.That(ice.Field.ReservedSolidCount, Is.GreaterThanOrEqualTo(37), $"tick {tick}");
                Assert.That(ice.Field.SolidConnected(true), Is.True, $"tick {tick}");
                for (var i = 0; i < ice.TileCount; i++)
                    Assert.That(ice.TileCollider(i).enabled, Is.EqualTo(
                        ice.Field.Stage(i) == IceStage.Solid || ice.Field.Stage(i) == IceStage.Cracking));
                if (tick % 60 == 0) yield return null;
            }
            var waves = arena.Events.Where(x => x.e.Kind == HazardEventKind.IceTileWarning)
                .GroupBy(x => x.sentAt).OrderBy(x => x.Key).ToArray();
            Assert.That(waves.Length, Is.EqualTo(42));
            for (var i = 0; i < waves.Length; i++)
            {
                Assert.That(waves[i].Key, Is.EqualTo(210u * (i + 1)));
                Assert.That(waves[i].Count(), Is.EqualTo(waves[i].Key < 2700 ? 2 : 3));
                Assert.That(waves[i].All(x => x.e.HostTick - x.sentAt == 90), Is.True);
            }
            for (var index = 0; index < 2; index++)
            {
                var shots = arena.Events.Where(x => x.e.Kind == HazardEventKind.CannonFire && x.e.Index == index)
                    .Select(x => x.e.HostTick).ToArray();
                Assert.That(shots.Length, Is.EqualTo(37));
                for (var i = 1; i < shots.Length; i++) Assert.That(shots[i] - shots[i - 1], Is.EqualTo(240));
                foreach (var shot in shots)
                    Assert.That(arena.Events.Any(x => x.e.Kind == HazardEventKind.CannonAim &&
                        x.e.Index == index && x.e.HostTick == shot && shot - x.sentAt == 60), Is.True,
                        $"cannon {index}, shot {shot} phải có cảnh báo 1 giây");
            }
        }
    }
}
