using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;
using SquishyBump.Hazards;
using SquishyBump.UI;
using SquishyBump.UI.Menus;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class ResultsSpectatorTests
    {
        private GameplayBootstrap app;
        private GameplayBootstrap client;
        private GameObject root;
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior backgroundBehavior;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior;
#endif
        [SetUp] public void Setup()
        {
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
            editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }
        private static readonly MatchStanding[] Rows = {
            new(1, 1, 17, 2), new(2, 2, 13, 1), new(3, 3, 9, 0),
            new(4, 4, 7, 0), new(5, 5, 3, 0), new(0, 6, 1, 0) };

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (client != null) { client.Session.Leave(); Object.Destroy(client.gameObject); }
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior;
#endif
            if (app != null) { app.Session.Leave(); Object.Destroy(app.gameObject); }
            if (root != null) Object.Destroy(root);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MainMenu.unity");
        }

        [UnityTest]
        public IEnumerator ResultButton_WaitsThreeRealSecondsEvenWhenPaused()
        {
            root = new GameObject("SB32 overlay");
            var overlay = root.AddComponent<MatchOverlay>(); overlay.Init();
            overlay.ShowResult(Rows, id => "Animal " + id);
            var clicked = 0;
            overlay.OfferResultAction("Tiếp tục", () => clicked++);
            Time.timeScale = 0;
            var since = Time.unscaledTime;
            while (Time.unscaledTime - since < 2.9f)
            {
                Assert.That(root.GetComponentsInChildren<Button>(), Is.Empty);
                yield return null;
            }
            yield return WaitFor(() => root.GetComponentsInChildren<Button>().Length == 1);
            Assert.That(Time.unscaledTime - since, Is.GreaterThanOrEqualTo(3f));
            root.GetComponentInChildren<Button>().onClick.Invoke();
            Assert.That(clicked, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RoundAndSession_PodiumKeepsHostRanksPointsAndLives()
        {
            root = new GameObject("SB32 podium");
            var overlay = root.AddComponent<MatchOverlay>(); overlay.Init();
            overlay.ShowResult(Rows, id => "Animal " + id, round: 2);
            yield return null;
            AssertPodium();
            overlay.ShowSessionResult(Rows, id => "Animal " + id);
            yield return null;
            AssertPodium();
            Assert.That(overlay.ResultText, Does.Contain("17 điểm · 2 mạng").And.Contain("6. Animal 0"));
        }

        private void AssertPodium()
        {
            Assert.That(root.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Podium ")).Select(i => i.name),
                Is.EquivalentTo(new[] { "Podium 1", "Podium 2", "Podium 3" }));
            Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.enabled && t.text.Contains("17 điểm · 2 mạng")), Is.True);
        }

        [UnityTest]
        public IEnumerator Scoreboard_HoldTabUsesConfirmedRowsAndReleaseCloses()
        {
            yield return Boot(MatchRule.Rank);
            var match = app.Launcher.Match;
            // Reliable event deliberately disagrees with lives sorting: client/display must retain host ranks.
            app.EventSink.Publish(MatchEvent.Scoreboard(app.EventSink.NextEventId(), app.Tick, Rows));
            yield return null;
            Assert.That(match.Hud.BoardText.Split('\n')[0], Does.StartWith("1 ").And.Contain("17 điểm · 2 mạng"));
            Assert.That(match.Hud.ScoreboardVisible, Is.False);
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            yield return null; yield return null;
            Assert.That(keyboard.tabKey.isPressed, Is.True, $"Synthetic Tab: device enabled={keyboard.enabled}; updateMode={InputSystem.settings.updateMode}");
            Assert.That(match.Hud.ScoreboardVisible, Is.True);
            var tick = app.Tick;
            yield return WaitFor(() => app.Tick > tick + 6);
            Assert.That(match.Hud.ScoreboardVisible, Is.True, "Giữ Tab không dừng tick gameplay.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null; yield return null;
            Assert.That(match.Hud.ScoreboardVisible, Is.False);
        }

        [UnityTest] public IEnumerator Survival_EliminatedCameraCyclesOnlyLivingFighters() => Spectator(MatchRule.Survival);
        [UnityTest] public IEnumerator Rank_RespawnKeepsOwnCamera_EliminationEnablesCycle() => Spectator(MatchRule.Rank);

        [UnityTest]
        public IEnumerator ClientRankBoard_ReliablePointsAndRanksSurviveNewSnapshots()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Boot.unity");
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None).Single(b => b.Launcher != null);
            app.Store = null; app.Saved.Bindings = ""; app.Settings.Rule = MatchRule.Rank;
            app.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
            app.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            Assert.That(app.Launcher.HostLan(), Is.True);
            yield return WaitFor(() => app.Launcher.Lobby != null);
            client = new GameObject("SB32 loopback client").AddComponent<GameplayBootstrap>();
            Object.DontDestroyOnLoad(client.gameObject);
            client.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
            client.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            Assert.That(client.Session.Join("127.0.0.1"), Is.True);
            yield return WaitFor(() => client.Session.State == SessionState.Joined);
            client.Session.Leave();
            yield return WaitFor(() => app.Session.Peers.Count == 0);
            yield return null;
            Assert.That(client.Session.Join("127.0.0.1"), Is.True);
            yield return WaitFor(() => client.Session.State == SessionState.Joined);
            var peer = app.Session.Peers.Single();
            Assert.That(peer, Is.GreaterThan(1UL), "ID NGO sau khi vào lại khác slot roster.");
            app.SetLobbySelection(new LobbyState(1, MatchRule.Rank, 5, 0, true,
                new[] { new LobbyPlayer(0, 1, true), new LobbyPlayer(peer, 0, true) }));
            app.Launcher.LoadLevel(1);
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Started);
            var rows = new[] { new MatchStanding(peer, 1, 17, 1), new MatchStanding(0, 2, 13, 3) };
            app.EventSink.Publish(MatchEvent.Scoreboard(app.EventSink.NextEventId(), app.Tick, rows));
            yield return WaitFor(() => client.MatchState.Scoreboard.Count == 2 && client.MatchState.Scoreboard[0].Points == 17);
            var setup = Resources.FindObjectsOfTypeAll<MatchSetup>().First(s => s.name == "MatchSetup");
            root = new GameObject("SB32 client match"); root.SetActive(false);
            var remoteMatch = root.AddComponent<MatchHost>();
            var remoteOverlay = root.AddComponent<MatchOverlay>(); remoteOverlay.Init();
            remoteMatch.AttachOverlay(remoteOverlay, 0, 2, 30);
            remoteMatch.Configure(client, setup.Balance, Object.FindAnyObjectByType<TrainingRingBounds>(), null, null,
                new[] { setup.Roster[1], setup.Roster[0] }, 1, false);
            root.SetActive(true);
            yield return WaitFor(() => remoteMatch.ClientView != null && remoteMatch.ClientView.Views.Count == 2);
            yield return WaitFor(() => remoteMatch.Hud.BoardText.Contains("17 điểm · 1 mạng"));
            var tick = remoteMatch.ClientView.Buffer.Newest.HostTick;
            yield return WaitFor(() => remoteMatch.ClientView.Buffer.Newest.HostTick > tick + 6);
            Assert.That(remoteMatch.Hud.BoardText, Is.EqualTo(app.Launcher.Match.Hud.BoardText));
            Assert.That(remoteMatch.Hud.BoardText.Split('\n')[0], Does.StartWith("1 Bear").And.Contain("17 điểm · 1 mạng"));
            Assert.That(remoteMatch.GetComponentsInChildren<Text>().Single(t => t.name == "Board").text,
                Is.EqualTo(app.Launcher.Match.GetComponentsInChildren<Text>().Single(t => t.name == "Board").text),
                "Màu nhân vật cũng phải khớp khi ID peer khác slot prefab.");
            app.EventSink.Publish(MatchEvent.SessionResult(app.EventSink.NextEventId(), app.Tick, rows));
            yield return WaitFor(() => remoteOverlay.ResultText.Contains("17 điểm · 1 mạng"));
            Assert.That(remoteMatch.Hud.GetComponentsInChildren<Canvas>().Single(c => c.name == "Match HUD").enabled, Is.False,
                "SessionResult Rank nhận sớm cũng phải ẩn HUD gameplay.");
        }

        private IEnumerator Spectator(MatchRule rule)
        {
            yield return Boot(rule);
            var match = app.Launcher.Match;
            Time.timeScale = 4;
            yield return WaitFor(() => match.Director.Phase == MatchPhase.Playing);
            var own = match.Fighters.Single(f => f.Id == 0);
            var camera = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).Single(c => c.name == "FollowCamera");
            var falls = rule == MatchRule.Rank ? 3 : 1;
            for (var i = 0; i < falls; i++)
            {
                yield return WaitFor(() => !own.Out && !match.Director.IsInvulnerable(0));
                own.Motor.transform.position = Vector3.down * 20;
                own.Motor.GetComponent<Rigidbody>().position = Vector3.down * 20;
                yield return WaitFor(() => own.Out);
                yield return null;
                if (rule == MatchRule.Rank && i < 2)
                {
                    Assert.That(match.Director.Lives(0), Is.GreaterThan(0));
                    Assert.That(camera.Follow, Is.SameAs(own.Motor.transform), "Đợi hồi sinh giữ camera mình.");
                    Assert.That(app.Launcher.Overlay.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith("ĐANG QUAN SÁT")), Is.False);
                }
            }
            Time.timeScale = 0;
            yield return null;
            var living = match.Fighters.Where(f => !f.Out).OrderBy(f => f.Id).ToArray();
            Assert.That(living.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(camera.Follow, Is.SameAs(living[0].Motor.transform));
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
            yield return null; yield return null;
            Assert.That(keyboard.rightArrowKey.isPressed, Is.True, $"Synthetic Right: device enabled={keyboard.enabled}; updateMode={InputSystem.settings.updateMode}");
            Assert.That(camera.Follow, Is.SameAs(living[1].Motor.transform));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftArrow)); yield return null; yield return null;
            Assert.That(camera.Follow, Is.SameAs(living[0].Motor.transform));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftArrow)); yield return null; yield return null;
            Assert.That(camera.Follow, Is.SameAs(living.Last().Motor.transform));
        }

        private IEnumerator Boot(MatchRule rule)
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Boot.unity");
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None).Single(b => b.Launcher != null);
            app.Store = null; app.Saved.Bindings = ""; app.Settings.Rule = rule;
            app.Launcher.StartSinglePlayer(1, 913);
            yield return WaitFor(() => app.Launcher.Match != null && app.Launcher.Match.Started);
        }

        [UnityTest]
        public IEnumerator WindowsPlayer_ResultAndScoreboardLayoutsAtFourResolutions()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Ca hình ảnh cần Windows Player có renderer thật.");
            root = new GameObject("SB32 visual fixture");
            var background = new GameObject("Background camera"); background.transform.SetParent(root.transform);
            var camera = background.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.06f, 0.14f); camera.cullingMask = 0;
            var overlayRoot = new GameObject("Visual overlay"); overlayRoot.transform.SetParent(root.transform);
            var overlay = overlayRoot.AddComponent<MatchOverlay>(); overlay.Init();
            var hudRoot = new GameObject("Visual HUD"); hudRoot.transform.SetParent(root.transform);
            var balance = ScriptableObject.CreateInstance<BalanceProfile>();
            var hud = hudRoot.AddComponent<MatchHud>(); hud.Init(balance, 2);
            var evidence = new DirectoryInfo(Application.dataPath);
            while (evidence != null && !Directory.Exists(Path.Combine(evidence.FullName, "ProjectSettings"))) evidence = evidence.Parent;
            Assert.That(evidence, Is.Not.Null);
            var directory = Path.Combine(evidence.FullName, "TestResults", "SB32-visual"); Directory.CreateDirectory(directory);
            var log = new System.Collections.Generic.List<string>();
            foreach (var size in new[] { (1280, 720), (1920, 1080), (1680, 720), (1280, 960) })
            {
                Screen.SetResolution(size.Item1, size.Item2, FullScreenMode.Windowed);
                for (var frame = 0; frame < 8; frame++) yield return null;
                Assert.That((Screen.width, Screen.height), Is.EqualTo(size), "OS không nhận độ phân giải yêu cầu.");
                for (var state = 0; state < 3; state++)
                {
                    overlayRoot.SetActive(state < 2); hudRoot.SetActive(state == 2);
                    if (state == 0) overlay.ShowResult(Rows, id => "Animal " + id, round: 2);
                    else if (state == 1) overlay.ShowSessionResult(Rows, id => "Animal " + id);
                    else { hud.SetStandings(Rows, id => "Animal " + id, _ => Color.cyan, MatchRule.Rank, 2, 5); hud.ShowScoreboard(true); }
                    if (state < 2)
                    {
                        overlay.OfferResultAction(state == 0 ? "Tiếp tục" : "Về menu", () => { });
                        yield return WaitFor(() => overlay.GetComponentsInChildren<Button>().Length == 1);
                    }
                    yield return null; yield return null;
                    Canvas.ForceUpdateCanvases();
                    if (state == 0)
                    {
                        var ribbon = root.GetComponentsInChildren<Image>().Single(i => i.sprite != null && i.sprite.name == "ribbon_score");
                        foreach (var plate in root.GetComponentsInChildren<Image>().Where(i => i.sprite != null && i.sprite.name == "plate"))
                            Assert.That(ScreenRect(ribbon.rectTransform).Overlaps(ScreenRect(plate.rectTransform)), Is.False,
                                "Dải điểm không được che bảng tên podium.");
                    }
                    foreach (var text in root.GetComponentsInChildren<Text>().Where(t => t.enabled))
                    {
                        var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            var p = RectTransformUtility.WorldToScreenPoint(null, corner);
                            Assert.That(p.x, Is.InRange(-1f, Screen.width + 1f), text.name);
                            Assert.That(p.y, Is.InRange(-1f, Screen.height + 1f), text.name);
                        }
                        Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 2), text.text);
                    }
                    var filename = $"{size.Item1}x{size.Item2}-{state}.png";
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, filename));
                    yield return null; yield return null;
                    log.Add($"{filename}: {SystemInfo.graphicsDeviceType}; text bounds/height PASS");
                }
            }
            File.WriteAllLines(Path.Combine(directory, "layouts.txt"), log);
            Object.Destroy(balance);
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds = 20)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(condition(), Is.True, "Hết thời gian chờ trạng thái SB-32.");
        }
    }
}
