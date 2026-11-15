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

        [UnityTest]
        public IEnumerator SinglePlayerSession_OnlySimulatesDuringNgoHost()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Boot.unity", LoadSceneMode.Single);
            var app = Object.FindAnyObjectByType<GameplayBootstrap>();
            Assert.That(app, Is.Not.Null);
            root = app.gameObject;
            var manager = root.GetComponent<NetworkManager>();
            var motor = new StubMotor();
            var states = new List<SessionState>();
            app.Session.StateChanged += states.Add;
            var command = new InputCommand(1, Vector2.up, true, true, 90f);
            var invalid = new InputCommand(2, Vector2.up * 2f, false, false, 0f);

            Assert.That(app.TrySimulate(motor, command), Is.False);
            Assert.That(app.Session.Host(SessionMode.SinglePlayer), Is.True);
            Assert.That(manager.IsHost, Is.True);
            Assert.That(manager.ConnectedClientsIds, Is.EqualTo(new[] { NetworkManager.ServerClientId }));
            Assert.That(app.Session.Host(SessionMode.Lan), Is.False);
            Assert.That(app.TrySimulate(motor, invalid), Is.False);
            Assert.That(app.TrySimulate(motor, command), Is.True);
            Assert.That(motor.LastCommand.Sequence, Is.EqualTo(1u));
            Assert.That(motor.LastCommand.Jump && motor.LastCommand.Dash, Is.True);
            Assert.That(motor.Position.x, Is.EqualTo(Time.fixedDeltaTime).Within(0.0001f));

            app.Session.Leave();
            Assert.That(app.TrySimulate(motor, command), Is.False);
            var deadline = Time.realtimeSinceStartup + 3f;
            while (app.Session.State != SessionState.Idle && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(manager.IsListening, Is.False);
            CollectionAssert.AreEqual(new[] { SessionState.Hosting, SessionState.Leaving, SessionState.Idle }, states);
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (root != null)
                Object.Destroy(root);

            yield return null;
        }
    }
}
