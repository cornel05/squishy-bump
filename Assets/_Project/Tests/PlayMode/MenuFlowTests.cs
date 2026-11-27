#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Match;
using SquishyBump.Presentation;
using SquishyBump.UI.Menus;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CoreSession = SquishyBump.Core.SessionState;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-31: Boot → main menu → Single Player on L1 → Esc pause → leave to the menu.</summary>
    public sealed class MenuFlowTests
    {
        const string Root = "Assets/_Project";
        GameplayBootstrap app;
        GameplayBootstrap host;

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1f;
            if (host != null)
                Object.Destroy(host.gameObject);
            if (app != null)
            {
                app.Session.Leave();
                Object.Destroy(app.gameObject);
            }
            // Never leave a level floor loaded under later tests.
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/MainMenu.unity", LoadSceneMode.Single);
        }

        static IEnumerator WaitFor(System.Func<bool> condition, float seconds = 10f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_SinglePlayer_PauseAndLeave()
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            // Bootstraps of earlier tests may survive (NGO keeps NetworkManager across loads): take Boot's own.
            app = System.Linq.Enumerable.First(Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None),
                b => b.gameObject.scene.name == "DontDestroyOnLoad" && b.Launcher != null);
            // Never write the player's real save from a test.
            app.Store = new LocalSave(System.IO.Path.Combine(Application.temporaryCachePath, "sb-tests"));
            // Bài này kiểm tra menu/pause; tutorial có bộ kiểm thử riêng.
            app.Saved.TutorialCompleted = true;
            Assert.That(app.Launcher, Is.Not.Null, "Boot carries the match setup");

            SceneManager.LoadScene($"{Root}/Scenes/MainMenu.unity", LoadSceneMode.Single);
            yield return WaitFor(() => app.Launcher.Menu != null);
            var menu = app.Launcher.Menu;
            Assert.That(menu.CurrentPage, Is.EqualTo("Main"));
            foreach (var page in new[] { "Single Player", "Multiplayer", "Settings", "Credits", "Main" })
            {
                menu.ShowPage(page);
                Assert.That(menu.CurrentPage, Is.EqualTo(page));
            }

            // Single đi qua lobby rồi bắt đầu L1, không có đường chọn tắt L2–L5.
            menu.ShowPage("Single Player");
            var picks = menu.GetComponentsInChildren<UnityEngine.UI.Button>()
                .Select(b => b.GetComponentInChildren<UnityEngine.UI.Text>().text).ToArray();
            Assert.That(picks, Does.Contain("Enter Lobby"));
            Assert.That(picks.Any(t => t.StartsWith("L2")), Is.False);

            app.Launcher.EnterSinglePlayerLobby();
            yield return WaitFor(() => app.Launcher.Lobby != null && app.Launcher.Lobby.Coordinator != null && app.Launcher.Lobby.Motors.Count == 1);
            var lobby = app.Launcher.Lobby;
            var layout = SceneManager.GetActiveScene().GetRootGameObjects().Single(g => g.name == "Store aisle").transform;
            lobby.Motors[0].GetComponent<Rigidbody>().position = layout.Find("Fruit stands/Stand 1/Pick volume").GetComponent<BoxCollider>().bounds.center;
            yield return WaitFor(() => lobby.Coordinator.State.Players[0].Fruit == 0);
            lobby.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.GetComponentInChildren<UnityEngine.UI.Text>().text == "Start").onClick.Invoke();
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Started);
            var match = app.Launcher.Match;
            Assert.That(app.Launcher.Overlay.CardVisible, Is.True, "arena card (B02) before the countdown");
            Assert.That(match.Director.Phase, Is.EqualTo(MatchPhase.Intro), "countdown waits for the card");
            yield return WaitFor(() => match.Director.Phase != MatchPhase.Intro, 6f);
            Assert.That(match.Started, Is.True, "L1 match started from the menu");
            Assert.That(app.Session.State, Is.EqualTo(CoreSession.Hosting));
            Assert.That(match.Director.Phase, Is.Not.EqualTo(MatchPhase.Intro));

            var rig = Object.FindAnyObjectByType<CameraRig>();
            app.Settings.ScreenShake = false;
            app.Settings.CameraDistance = 1.5f;
            app.Settings.NotifyChanged();
            Assert.That(rig.ShakeEnabled, Is.False);
            Assert.That(rig.Distance, Is.EqualTo(13.5f).Within(1e-4f));

            var pause = app.Launcher.Pause;
            pause.Toggle();
            Assert.That(pause.IsOpen && Time.timeScale == 0f, "Esc pauses Single Player");
            pause.Toggle();
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            app.Launcher.LeaveToMenu();
            yield return WaitFor(() => SceneManager.GetActiveScene().path.EndsWith("MainMenu.unity") && app.Launcher.Menu != null
                && app.Launcher.Menu != menu);
            Assert.That(SceneManager.GetActiveScene().path, Does.EndWith("MainMenu.unity"));
            yield return WaitFor(() => app.Session.State == CoreSession.Idle, 3f);
            Assert.That(app.Session.State, Is.EqualTo(CoreSession.Idle));
        }

        [UnityTest]
        public IEnumerator LanHostLeaves_ClientInLevelReturnsToMenuWithHostLostMessage()
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None).First(b => b.Launcher != null);
            app.Store = null;
            host = new GameObject("Test LAN host").AddComponent<GameplayBootstrap>();
            // The loopback peers share Unity's scene list in this process.
            foreach (var peer in new[] { app, host })
            {
                var config = peer.GetComponent<NetworkManager>().NetworkConfig;
                config.EnableSceneManagement = false;
                config.ForceSamePrefabs = false;
            }
            Assert.That(host.Session.Host(Core.SessionMode.Lan), Is.True);
            Assert.That(app.Launcher.Join("127.0.0.1"), Is.True);
            yield return WaitFor(() => app.Session.State == CoreSession.Joined, 7f);
            Assert.That(app.Session.State, Is.EqualTo(CoreSession.Joined));

            app.Launcher.LoadLevel(1);
            yield return WaitFor(() => app.Launcher.Match != null);
            Assert.That(SceneManager.GetActiveScene().path, Does.EndWith("/L1.unity"));

            host.Session.Leave();
            yield return WaitFor(() => app.Launcher.Menu != null, 3f);
            Assert.That(app.Session.State, Is.EqualTo(CoreSession.Idle));
            Assert.That(app.Session.LastEndReason, Is.EqualTo(Core.SessionEndReason.HostLost));
            Assert.That(SceneManager.GetActiveScene().path, Does.EndWith("/MainMenu.unity"));
            Assert.That(app.Launcher.Menu.CurrentPage, Is.EqualTo("Multiplayer"));
            yield return null;
            Assert.That(app.Launcher.Menu.GetComponentsInChildren<UnityEngine.UI.Text>().Select(label => label.text),
                Does.Contain(MainMenu.ErrorText(Core.SessionEndReason.HostLost)));
        }

        [UnityTest]
        public IEnumerator T11_ClientLeaves_RemoteInputStopsAndLobbyReopensAdmission()
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None).First(b => b.Launcher != null);
            app.Store = null;
            host = new GameObject("SB30 test client").AddComponent<GameplayBootstrap>();
            foreach (var peer in new[] { app, host })
            {
                peer.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
                peer.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            }
            Assert.That(app.Session.Host(Core.SessionMode.Lan), Is.True);
            Assert.That(host.Session.Join("127.0.0.1"), Is.True);
            yield return WaitFor(() => host.Session.State == CoreSession.Joined, 7);
            Assert.That(host.Session.State, Is.EqualTo(CoreSession.Joined));
            var remoteId = app.Session.Peers.Single();
            app.Launcher.LoadLevel(1);
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Started);
            var match = app.Launcher.Match;
            Assert.That(app.AcceptingPlayers, Is.False);
            yield return WaitFor(() => match.Director.Phase == MatchPhase.Playing, 8);
            var remote = match.Fighters.Single(f => f.Id == remoteId);
            var initialZ = remote.Motor.transform.position.z;
            host.Sync.SendInput(new[] {
                new Core.InputCommand(1, Vector2.up, false, false, 0),
                new Core.InputCommand(2, Vector2.up, false, false, 0),
                new Core.InputCommand(3, Vector2.up, false, false, 0) });
            yield return WaitFor(() => remote.Motor.transform.position.z > initialZ + 0.01f);
            uint? eventTick = null;
            app.Events.MatchRaised += e =>
            { if (e.Kind == Core.MatchEventKind.Elimination && e.FighterId == remoteId) eventTick = e.HostTick; };
            host.Session.Leave();
            yield return WaitFor(() => match.Director.DisconnectedAt(remoteId).HasValue);
            Assert.That(eventTick, Is.EqualTo(match.Director.DisconnectedAt(remoteId)));
            Assert.That(match.Director.AliveFighters, Has.No.Member(remoteId));
            Assert.That(remote.Out, Is.True); Assert.That(remote.Motor.enabled, Is.False);
            Assert.That(remote.Motor.GetComponent<Rigidbody>().isKinematic, Is.True);
            var stopped = remote.Motor.transform.position;
            for (var i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Distance(stopped, remote.Motor.transform.position), Is.LessThan(0.001f));
            Assert.That(match.Director.Phase, Is.EqualTo(MatchPhase.Playing), "Các slot còn lại tiếp tục trận.");
            // Luồng kết quả→lobby tự động còn thuộc session flow; kiểm tra điểm vào scene mở lại admission.
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Lobby.unity", LoadSceneMode.Single);
            Assert.That(app.AcceptingPlayers, Is.True);
            yield return WaitFor(() => host.Session.State == CoreSession.Idle);
            Assert.That(host.Session.Join("127.0.0.1"), Is.True);
            yield return WaitFor(() => host.Session.State == CoreSession.Joined, 7);
            Assert.That(host.Session.State, Is.EqualTo(CoreSession.Joined));
        }

        [UnityTest]
        public IEnumerator Level_StartsWithItsHazard([Values(2, 3)] int level)
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            app = System.Linq.Enumerable.First(Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None),
                b => b.gameObject.scene.name == "DontDestroyOnLoad" && b.Launcher != null);
            // Never write the player's real save from a test.
            app.Store = new LocalSave(System.IO.Path.Combine(Application.temporaryCachePath, "sb-tests"));
            Assert.That(app.Session.Host(Core.SessionMode.SinglePlayer), Is.True);
            app.Launcher.LoadLevel(level);
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Started);

            var match = app.Launcher.Match;
            Assert.That(match.Fighters, Has.Count.EqualTo(6));
            // SB-49: a strong hit squishes with a higher pitch, a light one bumps.
            var sfx = app.Launcher.Audio;
            sfx.PlayHit(10f, Vector3.zero);
            Assert.That(sfx.LastPlayed, Is.EqualTo(SquishyBump.Presentation.Sfx.Bump));
            sfx.PlayHit(150f, Vector3.zero);
            Assert.That(sfx.LastPlayed, Is.EqualTo(SquishyBump.Presentation.Sfx.SquishBump));
            Assert.That(sfx.LastPitch, Is.GreaterThan(1f));
            Assert.That(level == 2 ? System.Linq.Enumerable.OfType<SquishyBump.Hazards.MovingGround>(match.Hazards).Count()
                : System.Linq.Enumerable.OfType<SquishyBump.Hazards.Cannons>(match.Hazards).Count(), Is.EqualTo(1));
            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.That(System.Linq.Enumerable.All(match.Fighters, f => f.Motor.transform.position.y > 0f),
                "all six spawns stand on the floor");
        }

        [Test]
        public void StatusText_ShowsHostAndPeers() =>
            Assert.That(MainMenu.StatusText(CoreSession.Hosting, 2), Does.Contain("2 joined"));
    }
}
#endif
