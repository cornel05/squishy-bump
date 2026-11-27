#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class TutorialPlayTests
    {
        private GameplayBootstrap app;
        private TutorialSession session;
        private const string Root = "Assets/_Project";
        private static IEnumerator WaitFor(System.Func<bool> ready, float seconds = 10)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (!ready() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(ready(), Is.True, "Không hoàn thành bước tập bằng motor thật trong thời gian chờ.");
        }
        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1; session?.Dispose();
            if (app != null) { app.Session.Leave(); Object.Destroy(app.gameObject); }
            yield return SceneManager.LoadSceneAsync(Root + "/Scenes/MainMenu.unity");
        }
        [UnityTest]
        public IEnumerator FiveSteps_UseRealMotorAndToyWithoutStartingHazards()
        {
            yield return SceneManager.LoadSceneAsync(Root + "/Scenes/Levels/L1.unity");
            app = new GameObject("Tutorial test host").AddComponent<GameplayBootstrap>(); app.Store = null;
            Assert.That(app.Session.Host(SessionMode.SinglePlayer), Is.True);
            var profile = AssetDatabase.LoadAssetAtPath<TutorialProfile>(Root + "/Data/TutorialProfile.asset");
            Assert.That(profile, Is.Not.Null, "Chạy ProjectScaffold.Run qua Editor để tạo profile.");
            var balance = AssetDatabase.LoadAssetAtPath<BalanceProfile>(Root + "/Data/Balance/BalanceProfile.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Fighters/Cat.prefab");
            var bounds = Object.FindFirstObjectByType<TrainingRingBounds>();
            var command = new InputCommand(1, Vector2.up, false, false, 0);
            var finished = false; var skipped = false;
            session = new TutorialSession(app, profile, balance, bounds, prefab, null, app.transform,
                () => command, wasSkipped => { finished = true; skipped = wasSkipped; session.Dispose(); });
            Time.timeScale = 4;
            yield return WaitFor(() => session.Flow.Step == TutorialStep.Jump);
            command = new InputCommand(2, Vector2.zero, false, false, 0);
            session.Player.GetComponent<Rigidbody>().position = Vector3.down * 10;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(session.Flow.Step, Is.EqualTo(TutorialStep.Jump));
            Assert.That(session.Player.transform.position.y, Is.GreaterThan(0));
            command = new InputCommand(3, Vector2.zero, true, false, 0);
            yield return WaitFor(() => session.Flow.Step == TutorialStep.Dash);
            command = new InputCommand(4, Vector2.up, false, true, 0);
            yield return WaitFor(() => session.Flow.Step == TutorialStep.ClassCard);
            Assert.That(session.Player.DashReady, Is.False);
            command = new InputCommand(5, Vector2.zero, false, false, 0);
            session.Flow.DismissClassCard();
            Assert.That(session.Toy.gameObject.activeSelf, Is.True);
            Assert.That(session.Player.DashReady, Is.True, "Chuẩn bị bài đẩy không giữ cooldown của bài dash.");
            command = new InputCommand(6, Vector2.up, false, true, 0);
            yield return WaitFor(() => finished);
            Assert.That(skipped, Is.False);
            foreach (var ring in Object.FindObjectsByType<RingDrop>(FindObjectsSortMode.None))
                for (var i = 0; i < ring.BandCount; i++) Assert.That(ring.IsDropped(i), Is.False);
            Assert.That(Object.FindObjectsByType<MatchHost>(FindObjectsSortMode.None), Is.Empty);
        }
        [Test]
        public void TutorialFlags_RoundTripAndOldV1SaveStillShowsTutorial()
        {
            var old = JsonUtility.FromJson<SaveData>("{\"Version\":1,\"BestSurvivalLevel\":4}");
            Assert.That(old.TutorialCompleted || old.TutorialSkipped, Is.False);
            Assert.That(old.BestSurvivalLevel, Is.EqualTo(4));
            old.TutorialCompleted = true;
            var restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(old));
            Assert.That(TutorialFlow.ShouldRun(true, 1, restored.TutorialCompleted, restored.TutorialSkipped), Is.False);
            Assert.That(restored.BestSurvivalLevel, Is.EqualTo(4));
        }
        [UnityTest]
        public IEnumerator SkipThroughLauncher_PersistsChoiceAndStartsSixFreshFruits()
        {
            yield return SceneManager.LoadSceneAsync(Root + "/Scenes/Boot.unity");
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None).First(b => b.Launcher != null);
            app.Store = null; app.Saved.TutorialCompleted = app.Saved.TutorialSkipped = false;
            app.Launcher.StartSinglePlayer();
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Tutorial != null);
            var match = app.Launcher.Match;
            Assert.That(match.Started, Is.False); Assert.That(match.Fighters, Is.Empty);
            Assert.That(app.Launcher.Overlay.CardVisible, Is.False);
            match.Tutorial.Flow.Skip();
            yield return WaitFor(() => match.Started);
            Assert.That(app.Saved.TutorialSkipped, Is.True); Assert.That(app.Saved.TutorialCompleted, Is.False);
            Assert.That(match.Tutorial, Is.Null); Assert.That(match.Fighters, Has.Count.EqualTo(6));
            Assert.That(match.Director.ElapsedTicks, Is.EqualTo(0));
            foreach (var f in match.Fighters)
            { Assert.That(f.Motor.DashReady, Is.True); Assert.That(f.Motor.IsStunned, Is.False); }
            Assert.That(match.Snapshot(app.Tick).Fighters.All(f => f.Wobble == 0), Is.True);
            app.Launcher.LoadLevel(1);
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match != match && app.Launcher.Match.Started);
            Assert.That(app.Launcher.Match.Tutorial, Is.Null, "Lựa chọn bỏ qua không hiện lại khi retry.");
        }
    }
}
#endif
