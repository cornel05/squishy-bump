using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Net.Sync;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class IceArenaTests
    {
        private sealed class Events : IGameEventSink
        {
            private uint next;
            public readonly List<HazardEvent> Raised = new();
            public uint NextEventId() => ++next;
            public void Publish(HazardEvent e) => Raised.Add(e);
            public void Publish(MatchEvent e) { }
        }
        private sealed class EmptyFighters : IFighterPhysics
        {
            public IReadOnlyCollection<ulong> FighterIds { get; } = new ulong[0];
            public Vector3 GetPosition(ulong id) => Vector3.zero;
            public bool TryGetFighter(Collider c, out ulong id) { id = 0; return false; }
            public int GetWobble(ulong id) => 0;
            public void AddImpulse(ulong id, Vector3 impulse) { }
            public void AddWobble(ulong id, int amount) { }
        }
        [UnityTest]
        public IEnumerator T10_L4WithoutFruits120SimulatedSeconds_PreservesRealFloor()
        {
            yield return SceneManager.LoadSceneAsync("L4", LoadSceneMode.Single);
            var ice = Object.FindFirstObjectByType<IceTiles>();
            Assert.That(ice, Is.Not.Null, "L4 phải được build bằng IceSceneBuilder, không phải scene placeholder.");
            Assert.That(ice.TileCount, Is.EqualTo(61));
            ice.SetSeed(18); ice.Bind(null, new EmptyFighters());
            try
            {
                for (var tick = 1; tick <= 7200; tick++)
                {
                    ice.StepOnHost(tick, (uint)tick, 1f / 60);
                    var colliderCount = 0;
                    for (var i = 0; i < ice.TileCount; i++)
                    {
                        var c = ice.TileCollider(i);
                        Assert.That(c.sharedMaterial.dynamicFriction, Is.EqualTo(0.3f));
                        Assert.That(((MeshCollider)c).sharedMesh.bounds.size.x, Is.EqualTo(2f).Within(0.0001f));
                        var expected = ice.Field.Stage(i) == IceStage.Solid || ice.Field.Stage(i) == IceStage.Cracking;
                        Assert.That(c.enabled, Is.EqualTo(expected), $"tick {tick}, tile {i}");
                        if (c.enabled) colliderCount++;
                    }
                    Assert.That(colliderCount, Is.GreaterThanOrEqualTo(37), $"tick {tick}");
                    Assert.That(ice.Field.SolidConnected(), Is.True);
                    if (tick % 60 == 0) yield return null;
                }
            }
            finally { Object.Destroy(ice.gameObject); }
        }

        [UnityTest]
        public IEnumerator ReturningTile_RealFighterColliderDelaysSolidUntilClear()
        {
            yield return SceneManager.LoadSceneAsync("L4", LoadSceneMode.Single);
            var ice = Object.FindFirstObjectByType<IceTiles>();
            ice.SetSeed(18); ice.Bind(null, new EmptyFighters());
            GameObject blocker = null;
            try
            {
                for (var t = 1; t <= 180; t++) ice.StepOnHost(t, (uint)t, 1f / 60);
                var tile = Enumerable.Range(0, ice.TileCount).First(i => ice.Field.Stage(i) == IceStage.Cracking);
                blocker = new GameObject("Fighter overlapping returning ice");
                blocker.layer = LayerMask.NameToLayer("Fighter");
                blocker.transform.position = ice.TileCenter(tile) + Vector3.up * 0.1f;
                blocker.AddComponent<SphereCollider>().radius = 0.5f;
                UnityEngine.Physics.SyncTransforms();
                for (var t = 181; t <= 1200; t++) ice.StepOnHost(t, (uint)t, 1f / 60);
                Assert.That(ice.Field.Stage(tile), Is.EqualTo(IceStage.Fading));
                Assert.That(ice.TileCollider(tile).enabled, Is.False);
                blocker.transform.position = Vector3.up * 20;
                UnityEngine.Physics.SyncTransforms();
                ice.StepOnHost(1201, 1201, 1f / 60);
                Assert.That(ice.Field.Stage(tile), Is.EqualTo(IceStage.Solid));
                Assert.That(ice.TileCollider(tile).enabled, Is.True);
                Assert.That(UnityEngine.Physics.Raycast(ice.TileCenter(tile) + Vector3.up,
                    Vector3.down, out var hit, 2f), Is.True);
                Assert.That(hit.collider, Is.SameAs(ice.TileCollider(tile)));
            }
            finally { Object.Destroy(blocker); Object.Destroy(ice.gameObject); }
        }

        [UnityTest]
        public IEnumerator HostEventsThroughCodec_ClientFloorMatchesEveryTransitionAndWaitsForSolidEvent()
        {
            yield return SceneManager.LoadSceneAsync("L4", LoadSceneMode.Single);
            var host = Object.FindFirstObjectByType<IceTiles>();
            var clientObject = new GameObject("Client ice replica");
            clientObject.SetActive(false);
            var client = clientObject.AddComponent<IceTiles>();
            client.Configure(Resources.FindObjectsOfTypeAll<IceProfile>().Single(),
                Resources.FindObjectsOfTypeAll<BalanceProfile>().Single(),
                Resources.FindObjectsOfTypeAll<HazardProfile>().Single(), null);
            clientObject.SetActive(true);
            var sink = new Events();
            host.SetSeed(18); host.Bind(sink, new EmptyFighters());
            try
            {
                var delivered = 0;
                for (var tick = 1; tick <= 7200; tick++)
                {
                    host.StepOnHost(tick, (uint)tick, 1f / 60);
                    while (delivered < sink.Raised.Count)
                    {
                        var sent = sink.Raised[delivered++];
                        Assert.That(SyncCodec.TryReadHazard(SyncCodec.WriteHazard(sent), out var received), Is.True);
                        client.ApplyEvent(received, received.HostTick / 60f);
                    }
                    client.ShowAt(tick / 60f);
                    for (var i = 0; i < host.TileCount; i++)
                        Assert.That(client.TileCollider(i).enabled, Is.EqualTo(host.TileCollider(i).enabled),
                            $"tick {tick}, tile {i}");
                    if (tick % 60 == 0) yield return null;
                }
                Assert.That(sink.Raised.Any(e => e.Kind == HazardEventKind.IceTileSolid), Is.True);
                client.ApplyEvent(new HazardEvent(99999, 9000, HazardEventKind.IceTileWarning, 0), 150);
                client.ShowAt(170);
                Assert.That(client.TileCollider(0).enabled, Is.False,
                    "Client không tự bật collider sau thời gian hồi dự kiến khi chưa có Solid từ host.");
            }
            finally { Object.Destroy(client.gameObject); Object.Destroy(host.gameObject); }
        }
    }
}
