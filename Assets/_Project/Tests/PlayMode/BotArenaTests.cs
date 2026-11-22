#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.AI;
using SquishyBump.App;
using SquishyBump.Match;
using SquishyBump.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class BotArenaTests
    {
        private const string Root = "Assets/_Project";
        private GameplayBootstrap app;

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1;
            if (app != null)
            {
                app.Session.Leave();
                Object.Destroy(app.gameObject);
            }
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/MainMenu.unity", LoadSceneMode.Single);
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds = 15)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Hết thời gian chờ trạng thái trận");
        }

        [UnityTest]
        public IEnumerator SinglePlayer_D1BotsMoveThroughPlayerMotorAndEnterEliminatedOnFall()
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None)
                .First(b => b.Launcher != null);
            app.Store = new LocalSave(System.IO.Path.Combine(Application.temporaryCachePath, "sb18-tests"));
            app.Launcher.StartSinglePlayer(1);
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Started);
            var match = app.Launcher.Match;
            Assert.That(match.Fighters, Has.Count.EqualTo(6));
            Assert.That(match.Bots, Has.Count.EqualTo(5), "MatchSetup thực sự nối D1 cho năm slot bot");
            Assert.That(match.Bots.Values.All(b => b.State == BotState.Countdown), Is.True);
            yield return WaitFor(() => match.Director.Phase == MatchPhase.Playing);
            var bots = match.Fighters.Where(f => match.Bots.ContainsKey(f.Id)).ToArray();
            var starts = bots.ToDictionary(f => f.Id, f => f.Motor.transform.position);
            yield return new WaitForSeconds(2);
            foreach (var bot in bots)
            {
                Assert.That(bot.Motor, Is.TypeOf<PlayerMotor>());
                Assert.That(bot.Motor.GetComponent<Rigidbody>().isKinematic, Is.False);
                var displacement = bot.Motor.transform.position - starts[bot.Id];
                displacement.y = 0;
                Assert.That(displacement.magnitude, Is.GreaterThan(0.15f), $"Bot {bot.Id} nhận input và di chuyển trên L1");
            }
            var victim = bots.First(f => !f.Out);
            victim.Motor.GetComponent<Rigidbody>().position = new Vector3(0, -100, 0);
            yield return WaitFor(() => victim.Out);
            Assert.That(match.Bots[victim.Id].State, Is.EqualTo(BotState.Eliminated));
        }
    }
}
#endif
