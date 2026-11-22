#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>
    /// SB-44 player bug: L4 started from the Single Player menu lost its whole ice floor right after GO,
    /// while the scene-only tests (which drive IceTiles directly) kept it. This goes through LevelLauncher.
    /// </summary>
    public sealed class IceLaunchTests
    {
        private const string Root = "Assets/_Project";
        private GameplayBootstrap app;

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1;
            if (app != null) { app.Session.Leave(); Object.Destroy(app.gameObject); }
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/MainMenu.unity", LoadSceneMode.Single);
        }

        [UnityTest]
        public IEnumerator SinglePlayerL4_AfterGo_KeepsAtLeast37FloorColliders()
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None).First(b => b.Launcher != null);
            app.Store = new LocalSave(System.IO.Path.Combine(Application.temporaryCachePath, "sb44-launch-tests"));
            app.Settings.Rule = MatchRule.Survival;

            app.Launcher.StartSinglePlayer(4);
            var deadline = Time.realtimeSinceStartup + 20f;
            while ((app.Launcher.Match == null || app.Launcher.Match.Director == null
                    || app.Launcher.Match.Director.Phase != MatchPhase.Playing) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(app.Launcher.Match.Director.Phase, Is.EqualTo(MatchPhase.Playing));
            yield return new WaitForSeconds(4f);

            var ice = Object.FindFirstObjectByType<IceTiles>();
            Assert.That(ice, Is.Not.Null);
            var solid = Enumerable.Range(0, ice.TileCount).Count(i => ice.TileCollider(i).enabled);
            Assert.That(solid, Is.GreaterThanOrEqualTo(37), "ice floor colliders after GO");
            Assert.That(app.Launcher.Match.Director.Phase, Is.EqualTo(MatchPhase.Playing), "nobody fell through a missing floor");
        }
    }
}
#endif
