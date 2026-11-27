using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class SinglePlayerRunTests
    {
        private GameplayBootstrap app;

        [UnityTest]
        public IEnumerator LobbyPick_StartsL1WithFiveOtherCharacters()
        {
            yield return Boot();
            app.Saved.TutorialCompleted = true;
            app.Launcher.EnterSinglePlayerLobby();
            yield return WaitFor(() => app.Launcher.Lobby != null && app.Launcher.Lobby.Coordinator != null);
            var lobby = app.Launcher.Lobby;
            Assert.That(lobby.Coordinator.ClaimFromHostPosition(0, 4), Is.True);
            lobby.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "Start").onClick.Invoke();
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Started);
            var match = app.Launcher.Match;
            Assert.That(match.Director.Level, Is.EqualTo(1));
            Assert.That(match.Fighters.Single(f => f.Id == 0).Fruit, Is.EqualTo(4));
            Assert.That(match.Fighters.Select(f => (int)f.Fruit).OrderBy(x => x), Is.EqualTo(Enumerable.Range(0, 6)));
            Assert.That(match.Bots, Has.Count.EqualTo(5));
            Assert.That(match.Fighters.Where(f => f.Id != 0).Select(f => f.BotTier), Is.All.EqualTo(1));
        }

        [UnityTest] public IEnumerator SingleSurvival_FiveArenaFlow_RetryTieAndSummary() => RunFiveLevels(MatchRule.Survival);
        [UnityTest] public IEnumerator SingleRank_FiveArenaFlow_AdvancesAfterLossAndKeepsScores() => RunFiveLevels(MatchRule.Rank);

        private IEnumerator RunFiveLevels(MatchRule rule)
        {
            yield return Boot();
            app.Settings.Rule = rule;
            Assert.That(app.Session.Host(SessionMode.SinglePlayer), Is.True);
            app.SetLobbySelection(new LobbyState(1, rule, 5, 0, true, new[] { new LobbyPlayer(0, 3, true) }));
            app.Saved.TutorialCompleted = true;
            app.Launcher.StartSinglePlayer(1, 913);
            var log = new List<string>();
            Dictionary<ushort, byte> characters = null;
            ScoreService scores = null;
            for (var n = 1; n <= 5; n++)
            {
                yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Started && app.Launcher.Match.Director.Level == n);
                var match = app.Launcher.Match;
                Time.timeScale = 8;
                yield return WaitFor(() => match.Director.Phase == MatchPhase.Playing);
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("L" + n));
                Assert.That(app.Session.CanSimulateGameplay, Is.True);
                Assert.That(match.Fighters, Has.Count.EqualTo(6));
                var current = match.Fighters.ToDictionary(f => f.Id, f => f.Fruit);
                characters ??= current;
                Assert.That(current, Is.EquivalentTo(characters));
                Assert.That(match.Fighters.Single(f => f.Id == 0).Fruit, Is.EqualTo(3));
                Assert.That(match.Fighters.Where(f => f.Id != 0).OrderBy(f => f.Id).Select(f => f.BotTier),
                    Is.EqualTo(app.Launcher.SingleRun.Current.Tiers));
                Assert.That(match.Director.Seed, Is.EqualTo(app.Launcher.SingleRun.Current.Seed));
                if (rule == MatchRule.Rank)
                { scores ??= match.Director.Scores; Assert.That(match.Director.Scores, Is.SameAs(scores)); }
                var starts = match.Fighters.Where(f => f.Id != 0).ToDictionary(f => f.Id, f => f.Motor.transform.position);
                yield return new WaitForSeconds(2);
                Assert.That(match.Bots.Values.All(b => b.Replans > 0), Is.True, "Bot thật phải nhận clock/navigation trước khi điều khiển outcome.");
                Assert.That(match.Fighters.Where(f => f.Id != 0).Any(f => Vector3.Distance(starts[f.Id], f.Motor.transform.position) > 0.1f), Is.True);
                log.Add($"L{n}: seed={match.Director.Seed}; characters={string.Join(",", current.Select(p => p.Key + ":" + p.Value))}; tiers={string.Join(",", app.Launcher.SingleRun.Current.Tiers)}; botsMoved=true");

                if (rule == MatchRule.Survival && n == 2)
                {
                    var seed = match.Director.Seed;
                    var tiers = match.Fighters.Select(f => f.BotTier).ToArray();
                    foreach (var tie in new[] { false, true })
                    {
                        Drop(match, tie ? null : match.Fighters[1].Id);
                        yield return WaitFor(() => match.Director.Phase == MatchPhase.Result);
                        Assert.That(match.Director.GetSinglePlayerOutcome(0), Is.EqualTo(SurvivalOutcome.Retry));
                        yield return ClickResult("Retry");
                        var previous = match;
                        yield return WaitFor(() => app.Launcher.Match != previous && app.Launcher.Match.Started);
                        match = app.Launcher.Match;
                        Assert.That(match.Director.Level, Is.EqualTo(2));
                        Assert.That(match.Director.Seed, Is.EqualTo(seed));
                        Assert.That(match.Fighters.ToDictionary(f => f.Id, f => f.Fruit), Is.EquivalentTo(characters));
                        Assert.That(match.Fighters.Select(f => f.BotTier), Is.EqualTo(tiers));
                        Assert.That(match.Hazards.Count, Is.GreaterThan(0));
                        yield return WaitFor(() => match.Director.Phase == MatchPhase.Playing);
                    }
                    Assert.That(app.Launcher.SingleRun.Retries, Is.EqualTo(2));
                    log.Add("L2 loss+tie: reloaded scene, same seed/characters/tiers, retries=2");
                }

                if (rule == MatchRule.Survival) Drop(match, 0);
                else
                {
                    // Fixture điều khiển ring-out qua kill plane; Rank phải đi tiếp dù người chơi thua.
                    var deadline = Time.realtimeSinceStartup + 15;
                    while (match.Director.Phase != MatchPhase.Result && Time.realtimeSinceStartup < deadline)
                    { Drop(match, match.Fighters[1].Id); yield return null; }
                }
                yield return WaitFor(() => match.Director.Phase == MatchPhase.Result);
                if (n < 5) yield return ClickResult("Continue");
                else
                {
                    yield return WaitFor(() => app.Launcher.SingleRun.Finished);
                    Assert.That(app.Launcher.SingleRun.LevelsCleared, Is.EqualTo(5));
                    Assert.That(app.Launcher.Overlay.ResultText, Does.Contain("CLEARED 5/5 LEVELS"));
                    Assert.That(app.Launcher.Overlay.GetComponentsInChildren<Text>().Any(t => t.name == "Single Run Summary" && t.text.Contains("RETRIES:")), Is.True);
                    if (Application.platform == RuntimePlatform.WindowsPlayer)
                    {
                        Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
                        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
                        for (var frame = 0; frame < 12; frame++) yield return null;
                        var capture = Path.Combine(EvidenceRoot(), $"TestResults/SB26-{rule}-Summary.png");
                        Directory.CreateDirectory(Path.GetDirectoryName(capture));
                        if (File.Exists(capture)) File.Delete(capture);
                        ScreenCapture.CaptureScreenshot(capture);
                        yield return new WaitForSecondsRealtime(0.5f);
                        Assert.That(File.Exists(capture), Is.True);
                    }
                    yield return ClickResult("Main Menu");
                    yield return WaitFor(() => app.Session.State == SessionState.Idle);
                    Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
                }
            }
            Directory.CreateDirectory(Path.Combine(EvidenceRoot(), "TestResults"));
            File.WriteAllLines(Path.Combine(EvidenceRoot(), $"TestResults/SB26-{Application.platform}-{rule}-Flow.txt"), log);
        }

        private static string EvidenceRoot()
        {
            var project = new DirectoryInfo(Application.dataPath);
            while (project != null && !File.Exists(Path.Combine(project.FullName, "ProjectSettings/ProjectVersion.txt"))) project = project.Parent;
            Assert.That(project, Is.Not.Null, "Đặt test Player dưới Builds/ của project.");
            return project.FullName;
        }

        private static void Drop(MatchHost match, ushort? survivor)
        {
            foreach (var f in match.Fighters.Where(f => !f.Out && f.Id != survivor))
            {
                var p = new Vector3(0, -20, 0);
                f.Motor.transform.position = p;
                f.Motor.GetComponent<Rigidbody>().position = p;
            }
        }

        private IEnumerator ClickResult(string caption)
        {
            Button button = null;
            yield return WaitFor(() =>
            {
                button = app.Launcher.Overlay.GetComponentsInChildren<Button>()
                    .FirstOrDefault(b => b.GetComponentInChildren<Text>().text == caption);
                return button != null;
            }, 6);
            button.onClick.Invoke();
            if (caption != "Main Menu") button.onClick.Invoke(); // Nhấp lặp không được nhảy hai màn/tăng retry hai lần.
        }

        private IEnumerator Boot()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Boot.unity");
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None).First(b => b.Launcher != null);
            app.Store = null;
            app.Settings.Rule = MatchRule.Survival;
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds = 15)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Hết thời gian chờ trạng thái trận.");
        }

        [UnityTest]
        public IEnumerator SingleSurvival_Loss_OffersRetryInsteadOfLeavingRun()
        {
            yield return Boot();
            app.Saved.TutorialCompleted = true;
            app.Launcher.StartSinglePlayer();
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Started);
            var match = app.Launcher.Match;
            Time.timeScale = 8;
            yield return WaitFor(() => match.Director.Phase == MatchPhase.Playing);
            // Đưa các quả qua kill plane để arena sampling thật chốt kết quả thua.
            foreach (var fighter in match.Fighters.Where(f => f.Id != match.Fighters[1].Id))
            {
                var p = new Vector3(0, -20, 0);
                fighter.Motor.transform.position = p;
                fighter.Motor.GetComponent<Rigidbody>().position = p;
            }
            yield return WaitFor(() => match.Director.Phase == MatchPhase.Result);
            yield return new WaitForSecondsRealtime(3.1f);
            Assert.That(app.Launcher.Overlay.GetComponentsInChildren<Button>()
                .Select(b => b.GetComponentInChildren<Text>().text), Does.Contain("Retry"));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (app != null) { app.Session.Leave(); Object.Destroy(app.gameObject); }
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MainMenu.unity");
        }
    }
}
