#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Match;
using SquishyBump.Presentation;
using SquishyBump.UI.Menus;
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

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1f;
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

            // The level picker lists every level of the match setup.
            menu.ShowPage("Single Player");
            var picks = menu.GetComponentsInChildren<UnityEngine.UI.Button>()
                .Select(b => b.GetComponentInChildren<UnityEngine.UI.Text>().text).Where(t => t.StartsWith("L")).ToArray();
            Assert.That(picks, Has.Length.EqualTo(5));
            Assert.That(picks[1], Does.StartWith("L2"));

            app.Launcher.StartSinglePlayer();
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
