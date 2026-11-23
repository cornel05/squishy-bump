using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Net.Session;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-77: host-enforced room options over real loopback sessions, and the discovery listener.</summary>
    public sealed class RoomOptionsPlayTests
    {
        private GameplayBootstrap host;
        private readonly System.Collections.Generic.List<GameObject> extras = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Boot.unity", LoadSceneMode.Single);
            host = Object.FindAnyObjectByType<GameplayBootstrap>();
            host.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
            host.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            Assert.That(host.Session.Host(SessionMode.Lan), Is.True);
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            foreach (var go in extras.Where(go => go != null)) Object.Destroy(go);
            if (host != null) Object.Destroy(host.gameObject);
            yield return null;
        }

        private GameplayBootstrap NewClient()
        {
            var go = new GameObject("Room options client");
            extras.Add(go);
            var client = go.AddComponent<GameplayBootstrap>();
            client.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
            client.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            return client;
        }

        private static IEnumerator Until(System.Func<bool> done, string what)
        {
            var deadline = Time.realtimeSinceStartup + 6f;
            while (!done() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(done(), Is.True, what);
        }

        [UnityTest]
        public IEnumerator Password_WrongIsRefused_RightJoins()
        {
            host.Session.Password = "star";
            var wrong = NewClient();
            Assert.That(wrong.Session.Join("127.0.0.1", 7777, "moon"), Is.True);
            yield return Until(() => wrong.Session.State == SessionState.Idle, "wrong password ends the attempt");
            Assert.That(wrong.Session.LastEndReason, Is.EqualTo(SessionEndReason.WrongPassword));
            Assert.That(host.Session.Peers, Is.Empty);

            var right = NewClient();
            Assert.That(right.Session.Join("127.0.0.1", 7777, "star"), Is.True);
            yield return Until(() => right.Session.State == SessionState.Joined, "right password joins");
            Assert.That(host.Session.Peers, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Capacity_TwoLetsOneGuestIn_AndRefusesTheNext()
        {
            host.Session.Capacity = 2;
            var first = NewClient();
            Assert.That(first.Session.Join("127.0.0.1"), Is.True);
            yield return Until(() => first.Session.State == SessionState.Joined, "first guest joins");

            var second = NewClient();
            Assert.That(second.Session.Join("127.0.0.1"), Is.True);
            yield return Until(() => second.Session.State == SessionState.Idle, "second guest refused");
            Assert.That(second.Session.LastEndReason, Is.EqualTo(SessionEndReason.RoomFull));
            Assert.That(host.Session.Peers, Has.Count.EqualTo(1));
        }

        [Test]
        public void Capacity_IsClampedToTwoThroughSix()
        {
            host.Session.Capacity = 99;
            Assert.That(host.Session.Capacity, Is.EqualTo(6));
            host.Session.Capacity = 0;
            Assert.That(host.Session.Capacity, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Discovery_ListenerListsABeaconAndIgnoresGarbage()
        {
            var listener = new LanDiscovery();
            extras.Add(new GameObject("discovery owner"));
            try
            {
                listener.Listen(true);
                using var sender = new UdpClient();
                var beacon = LanDiscovery.Encode("Loopback room", 2, 4, MatchRule.Survival, true);
                var garbage = new byte[] { 1, 2, 3 };
                var target = new IPEndPoint(IPAddress.Loopback, LanDiscovery.Port);
                sender.Send(garbage, garbage.Length, target);
                sender.Send(beacon, beacon.Length, target);

                var deadline = Time.realtimeSinceStartup + 3f;
                while (listener.Rooms.Count == 0 && Time.realtimeSinceStartup < deadline) yield return null;

                if (listener.Rooms.Count == 0) Assert.Inconclusive("UDP 7778 is busy or blocked on this machine.");
                Assert.That(listener.Rooms.Count, Is.EqualTo(1));
                Assert.That(listener.Rooms[0].Name, Is.EqualTo("Loopback room"));
                Assert.That(listener.Rooms[0].Address, Is.EqualTo("127.0.0.1"));
                Assert.That(listener.Rooms[0].HasPassword, Is.True);

                listener.Listen(false);
                Assert.That(listener.Rooms, Is.Empty, "stopping clears the list");
            }
            finally { listener.Dispose(); }
        }
    }
}
