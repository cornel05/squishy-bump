using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.UI.Menus;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class MatchEventNetworkTests
    {
        private GameplayBootstrap host, client;
        private GameObject overlays;
        private static IEnumerator WaitFor(System.Func<bool> ready, float seconds = 7)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(ready(), Is.True, "Hết thời gian chờ loopback.");
        }
        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (client != null) client.Session.Leave();
            if (host != null) host.Session.Leave();
            yield return null;
            if (client != null) Object.Destroy(client.gameObject);
            if (host != null) Object.Destroy(host.gameObject);
            if (overlays != null) Object.Destroy(overlays);
            yield return null;
        }
        [UnityTest]
        public IEnumerator ReliableEvents_LoopbackReplicasAndResultTextAgreeAndDuplicateIsIgnored()
        {
            host = new GameObject("SB24 Host").AddComponent<GameplayBootstrap>();
            client = new GameObject("SB24 Client").AddComponent<GameplayBootstrap>();
            foreach (var peer in new[] { host, client })
            {
                peer.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
                peer.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            }
            Assert.That(host.Session.Host(SessionMode.Lan), Is.True);
            Assert.That(client.Session.Join("127.0.0.1"), Is.True);
            yield return WaitFor(() => client.Session.State == SessionState.Joined);
            yield return null; // NetSync.Poll đăng ký handler trên cả hai peer.
            overlays = new GameObject("SB24 Result overlays");
            var a = new GameObject("Host overlay"); a.transform.SetParent(overlays.transform);
            var b = new GameObject("Client overlay"); b.transform.SetParent(overlays.transform);
            var hostOverlay = a.AddComponent<MatchOverlay>(); hostOverlay.Init();
            var clientOverlay = b.AddComponent<MatchOverlay>(); clientOverlay.Init();
            host.Events.MatchRaised += e => { if (e.Kind == MatchEventKind.MatchResult) hostOverlay.ShowResult(e.Standings, id => $"Fruit {id}"); };
            client.Events.MatchRaised += e => { if (e.Kind == MatchEventKind.MatchResult) clientOverlay.ShowResult(e.Standings, id => $"Fruit {id}"); };
            var delivered = 0; client.Events.MatchRaised += _ => delivered++;
            var sink = host.EventSink;
            sink.Publish(MatchEvent.Countdown(sink.NextEventId(), host.Tick, 3));
            sink.Publish(MatchEvent.RingOut(sink.NextEventId(), host.Tick, 2, 1));
            sink.Publish(MatchEvent.Respawn(sink.NextEventId(), host.Tick, 2));
            sink.Publish(MatchEvent.Elimination(sink.NextEventId(), host.Tick, 2, 2));
            var rows = new[] { new MatchStanding(1, 1, 5, 2, 9000), new MatchStanding(2, 2, 3, 0, 8000) };
            sink.Publish(MatchEvent.Scoreboard(sink.NextEventId(), host.Tick, rows));
            var result = MatchEvent.MatchResult(sink.NextEventId(), host.Tick, rows); sink.Publish(result);
            host.Sync.BroadcastMatch(result); // retransmit cùng EventId không được áp dụng lần hai
            yield return WaitFor(() => client.MatchState.HasResult);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(delivered, Is.EqualTo(6));
            Assert.That(client.MatchState.CountdownSeconds, Is.EqualTo(3));
            Assert.That(client.MatchState.OutFighters, Is.Empty);
            Assert.That(client.MatchState.Eliminated[2], Is.EqualTo(2));
            CollectionAssert.AreEqual(host.MatchState.Result.Select(s => (s.FighterId, s.Rank, s.Points, s.Lives)),
                client.MatchState.Result.Select(s => (s.FighterId, s.Rank, s.Points, s.Lives)));
            Assert.That(clientOverlay.ResultText, Is.EqualTo(hostOverlay.ResultText));
            Assert.That(clientOverlay.ResultText, Does.Contain("5 pts"));
        }
    }
}
