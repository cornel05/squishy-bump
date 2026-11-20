using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class HostGameplayTests
    {
        private GameObject root;
        private GameplayBootstrap app;

        [UnityTest]
        public IEnumerator SinglePlayerSession_OnlySimulatesDuringNgoHost()
        {
            yield return LoadBoot();
            var manager = root.GetComponent<NetworkManager>();
            var motor = new StubMotor();
            var states = new List<SessionState>();
            app.Session.StateChanged += states.Add;
            var command = new InputCommand(1, Vector2.up, true, true, 90f);
            var invalid = new InputCommand(2, new Vector2(float.NaN, 0f), false, false, 0f);

            Assert.That(app.TrySimulate(motor, command), Is.False);
            Assert.That(app.Session.Host(SessionMode.SinglePlayer), Is.True);
            Assert.That(manager.IsHost, Is.True);
            Assert.That(manager.ConnectedClientsIds, Is.EqualTo(new[] { NetworkManager.ServerClientId }));
            Assert.That(app.Session.Peers, Is.Empty);
            Assert.That(app.Session.Host(SessionMode.Lan), Is.False);
            Assert.That(app.TrySimulate(motor, invalid), Is.False);
            Assert.That(app.TrySimulate(motor, command), Is.True);
            Assert.That(motor.LastCommand.Sequence, Is.EqualTo(1u));
            Assert.That(motor.LastCommand.Jump && motor.LastCommand.Dash, Is.True);
            Assert.That(motor.Position.x, Is.EqualTo(Time.fixedDeltaTime).Within(0.0001f));

            app.Session.Leave();
            Assert.That(app.TrySimulate(motor, command), Is.False);
            yield return WaitForIdle();
            Assert.That(manager.IsListening, Is.False);
            Assert.That(app.Session.LastEndReason, Is.EqualTo(SessionEndReason.LocalLeave));
            CollectionAssert.AreEqual(new[] { SessionState.Hosting, SessionState.Leaving, SessionState.Idle }, states);
        }

        [UnityTest]
        public IEnumerator HostClock_TicksOnlyWhileHosting()
        {
            yield return LoadBoot();
            var ticks = new List<uint>();
            app.Clock.Ticked += ticks.Add;

            yield return new WaitForFixedUpdate();
            Assert.That(app.Clock.Tick, Is.Zero);

            Assert.That(app.Session.Host(SessionMode.SinglePlayer), Is.True);
            for (var i = 0; i < 3; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(app.Clock.Tick, Is.EqualTo(3u));
            CollectionAssert.AreEqual(new uint[] { 1, 2, 3 }, ticks);

            app.Session.Leave();
            yield return new WaitForFixedUpdate();
            Assert.That(ticks, Has.Count.EqualTo(3));
            yield return WaitForIdle();
            Assert.That(app.Clock.Tick, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Join_InvalidArguments_ReturnsFalse()
        {
            yield return LoadBoot();

            Assert.That(app.Session.Join("not-an-ip", 7777), Is.False);
            Assert.That(app.Session.LastEndReason, Is.EqualTo(SessionEndReason.InvalidAddress));
            Assert.That(app.Session.Join("127.0.0.1", 0), Is.False);
            Assert.That(app.Session.Join("::1", 7777), Is.False, "LAN flow nhận IPv4 theo hướng dẫn demo");
            Assert.That(app.Session.State, Is.EqualTo(SessionState.Idle));
            Assert.That(root.GetComponent<NetworkManager>().IsListening, Is.False);
        }

        [Test]
        public void EventHub_DropsDuplicateEventIds()
        {
            var isHost = false;
            var hub = new GameEventHub(() => isHost);
            var received = new List<uint>();
            hub.MatchRaised += gameEvent => received.Add(gameEvent.EventId);
            var first = MatchEvent.RingOut(hub.NextEventId(), 10, 2, 5);

            hub.Publish(first);
            Assert.That(received, Is.Empty, "client không được publish");

            isHost = true;
            hub.Publish(first);
            hub.Deliver(first);
            hub.Publish(MatchEvent.Respawn(hub.NextEventId(), 11, 2));

            CollectionAssert.AreEqual(new uint[] { 1, 2 }, received);
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (root != null)
                Object.Destroy(root);

            yield return null;
        }

        private IEnumerator LoadBoot()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Boot.unity", LoadSceneMode.Single);
            app = Object.FindAnyObjectByType<GameplayBootstrap>();
            Assert.That(app, Is.Not.Null);
            root = app.gameObject;
        }

        private IEnumerator WaitForIdle()
        {
            var deadline = Time.realtimeSinceStartup + 3f;
            while (app.Session.State != SessionState.Idle && Time.realtimeSinceStartup < deadline)
                yield return null;
        }
    }
}
