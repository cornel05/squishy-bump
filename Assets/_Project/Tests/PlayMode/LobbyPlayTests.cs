#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;
using SquishyBump.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class LobbyPlayTests
    {
        private GameplayBootstrap host, client, secondClient;
        private static IEnumerator WaitFor(System.Func<bool> done, string stage, float seconds = 10)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(done(), Is.True, $"Lobby không tới trạng thái: {stage}.");
        }
        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1;
            if (secondClient != null) { secondClient.Session.Leave(); Object.Destroy(secondClient.gameObject); }
            if (client != null) { client.Session.Leave(); Object.Destroy(client.gameObject); }
            if (host != null) { host.Session.Leave(); Object.Destroy(host.gameObject); }
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MainMenu.unity");
        }
        [UnityTest]
        public IEnumerator LanLobby_HostVolumesEnforceExclusiveClaims_ReadyStartsTwoHumanRoster()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Boot.unity");
            host = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None).Single(b => b.Launcher != null);
            host.Store = null; host.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
            host.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            Assert.That(host.Launcher.HostLan(), Is.True);
            yield return WaitFor(() => host.Launcher.Lobby != null && host.Launcher.Lobby.Coordinator != null, "tạo lobby/coordinator");
            client = new GameObject("Lobby loopback client").AddComponent<GameplayBootstrap>();
            client.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
            client.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            Assert.That(client.Session.Join("127.0.0.1"), Is.True);
            yield return WaitFor(() => client.Session.State == SessionState.Joined && host.Launcher.Lobby.Motors.Count == 2, "client join và hai avatar");
            yield return WaitFor(() => client.LobbySelection != null && client.LobbySelection.Players.Count == 2,
                "client nhận bảng info lúc join");
            var lobby = host.Launcher.Lobby;
            var peer = host.Session.Peers.Single();
            var beforeWalk = lobby.Motors[peer].GetComponent<Rigidbody>().position;
            for (uint sequence = 1; sequence <= 60; sequence++)
            {
                client.Sync.SendInput(new[] { new InputCommand(sequence, sequence <= 30 ? Vector2.right : Vector2.zero,
                    false, false, 0) });
                yield return new WaitForFixedUpdate();
            }
            Assert.That(lobby.Motors[peer].GetComponent<Rigidbody>().position.x - beforeWalk.x,
                Is.GreaterThan(0.15f), "InputCommand qua NGO thực sự làm avatar lobby đi lại bằng PlayerMotor.");
            var layout = SceneManager.GetActiveScene().GetRootGameObjects().Single(g => g.name == "Store aisle").transform;
            var stands = layout.Find("Fruit stands");
            var pick0 = stands.GetChild(0).Find("Pick volume").GetComponent<BoxCollider>().bounds.center;
            var pick1 = stands.GetChild(1).Find("Pick volume").GetComponent<BoxCollider>().bounds.center;
            lobby.Motors[0].GetComponent<Rigidbody>().position = pick0;
            yield return WaitFor(() => lobby.Coordinator.State.Players.Single(p => p.Id == 0).Fruit == 0, "host chọn kệ 0");
            yield return null;
            var panel = lobby.GetComponent<LobbyPanel>();
            Assert.That(lobby.GetComponentsInChildren<Text>().Single(t => t.name == "Stand 0 status").text,
                Does.Contain("ĐÃ CHỌN").And.Contain("BẠN"));
            lobby.Motors[peer].GetComponent<Rigidbody>().position = pick0;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(lobby.Coordinator.Room.Players[peer].Fruit, Is.Null, "Kệ đã chọn không cấp cho peer khác.");
            lobby.Motors[peer].GetComponent<Rigidbody>().position = pick1;
            yield return WaitFor(() => lobby.Coordinator.Room.Players[peer].Fruit == 1, "client chọn kệ 1");
            var hostDesk = lobby.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "Host desk");
            var length3 = hostDesk.GetComponentsInChildren<Button>(true).Single(b => b.name == "Map 3");
            length3.onClick.Invoke();
            Assert.That(lobby.Coordinator.State.FinalLevel, Is.EqualTo(5), "Không đổi cấu hình từ xa quầy.");
            lobby.Motors[0].GetComponent<Rigidbody>().position = layout.Find("Host desk").position + Vector3.back * 2;
            yield return WaitFor(() => hostDesk.gameObject.activeSelf, "host tới quầy");
            SetRule(lobby, hostDesk, MatchRule.Rank);
            Assert.That(lobby.Coordinator.State.Rule, Is.EqualTo(MatchRule.Rank));
            SetRule(lobby, hostDesk, MatchRule.Survival);
            length3.onClick.Invoke();
            Assert.That(lobby.Coordinator.State.Rule, Is.EqualTo(MatchRule.Survival));
            Assert.That(lobby.Coordinator.State.FinalLevel, Is.EqualTo(3));
            lobby.Coordinator.SetReady(true); client.LobbySync.SendReady(true);
            yield return WaitFor(() => lobby.Coordinator.State.RemainingTicks > 0, "host countdown");
            yield return WaitFor(() => client.LobbySelection != null && client.LobbySelection.RemainingTicks > 0, "client nhận countdown");
            Assert.That(panel.BoardText, Does.Contain("Bear").And.Contain("Pig").And.Contain("READY").And.Contain("IP host:"));
            Assert.That(host.LobbySync.RejectedPackets, Is.Zero);
            Assert.That(client.LobbySync.RejectedPackets, Is.Zero);
            Assert.That(host.Launcher.Match, Is.Null, "Chưa được bỏ qua countdown 5 giây.");
            Time.timeScale = 4;
            yield return WaitFor(() => host.Launcher.Match != null && host.Launcher.Match.Started, "bắt đầu match sau countdown");
            Assert.That(host.Launcher.Match.Fighters, Has.Count.EqualTo(2), "LAN không có idle/bot slots phụ.");
            Assert.That(host.LobbySelection.FinalLevel, Is.EqualTo(3));
            Assert.That(host.Launcher.Match.Fighters.Select(f => (ulong)f.Id), Is.EquivalentTo(new[] { 0UL, peer }));
            Assert.That(host.Launcher.Match.Fighters[0].Motor.name, Does.StartWith("Bear"));
            Assert.That(host.Launcher.Match.Fighters[1].Motor.name, Does.StartWith("Pig"));
        }

        [UnityTest]
        public IEnumerator LanSurvival_TwoPlayersContinueThroughL5_AndClientReceivesChampion()
        {
            yield return StartSurvivalLobby(5);
            var peer = host.Session.Peers.Single();
            for (var level = 1; level <= 5; level++)
            {
                yield return WaitFor(() => IsPlaying(level), $"L{level} bắt đầu");
                var match = host.Launcher.Match;
                Assert.That(match.Fighters.Select(f => (ulong)f.Id), Is.EquivalentTo(new[] { 0UL, peer }));
                Assert.That(match.Fighters.Single(f => f.Id == 0).Motor.name, Does.StartWith("Pig"));
                Assert.That(match.Fighters.Single(f => f.Id == peer).Motor.name, Does.StartWith("Bear"));
                var roomResultCount = 0;
                System.Action<MatchEvent> onResult = e => { if (e.Kind == MatchEventKind.MatchResult) roomResultCount++; };
                client.Events.MatchRaised += onResult;
                RingOut(peer);
                yield return WaitFor(() => roomResultCount == 1, $"client nhận kết quả L{level}");
                client.Events.MatchRaised -= onResult;
                Assert.That(client.MatchState.Result[0].FighterId, Is.Zero);
                if (level < 5)
                {
                    Assert.That(ResultButton("Tiếp tục"), Is.Null, "Nút phải đợi bảng kết quả.");
                    yield return ClickResultAction("Tiếp tục");
                }
            }
            yield return WaitFor(() => client.MatchState.SessionResult.Count == 2, "client nhận bảng xếp hạng phiên");
            Assert.That(client.MatchState.SessionResult.Select(row => (row.FighterId, row.Rank)),
                Is.EqualTo(new[] { (0UL, 1), (peer, 2) }));
            Assert.That(host.Launcher.Overlay.ResultText, Does.Contain("VÔ ĐỊCH · Pig"));
        }

        [UnityTest]
        public IEnumerator LanSurvival_HostKnockedOut_SpectatesWhileClientsPlayFinal()
        {
            yield return StartSurvivalLobby(2, remotePlayers: 2);
            yield return WaitFor(() => IsPlaying(1), "L1 bắt đầu");
            var peers = host.Session.Peers.OrderBy(id => id).ToArray();
            RingOut(0);
            yield return WaitFor(() => host.Launcher.Match.Director.AliveFighters.Count == 2, "host bị loại khỏi trận");
            RingOut(peers[1]);
            yield return ClickResultAction("Tiếp tục");
            yield return WaitFor(() => IsPlaying(2), "L2 bắt đầu sau khi loại host");
            Assert.That(host.Launcher.Match.Fighters.Select(f => (ulong)f.Id), Is.EquivalentTo(peers));
            Assert.That(host.Launcher.Overlay.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith("ĐANG QUAN SÁT")), Is.True);
            secondClient.Session.Leave();
            yield return WaitFor(() => client.MatchState.SessionResult.Count == 3, "client nhận kết quả bỏ cuộc");
            Assert.That(client.MatchState.SessionResult.Select(row => row.FighterId), Is.EqualTo(new[] { peers[0], peers[1], 0UL }));
        }

        [UnityTest]
        public IEnumerator LanSurvival_DrawAtFinal_ReplaysLevelUntilThereIsOneChampion()
        {
            yield return StartSurvivalLobby(1);
            var peer = host.Session.Peers.Single();
            yield return WaitFor(() => IsPlaying(1), "L1 bắt đầu");
            var first = host.Launcher.Match;
            var seed = first.Director.Seed;
            RingOut(0);
            RingOut(peer);
            yield return ClickResultAction("Đấu lại");
            yield return WaitFor(() => host.Launcher.Match != first && IsPlaying(1), "L1 bắt đầu lại");
            Assert.That(host.Launcher.Match.Director.Seed, Is.EqualTo(seed));
            Assert.That(host.Launcher.Match.Fighters.Select(f => (ulong)f.Id), Is.EquivalentTo(new[] { 0UL, peer }));
            RingOut(0);
            yield return WaitFor(() => client.MatchState.SessionResult.Count == 2, "client nhận kết quả chung cuộc");
            Assert.That(client.MatchState.SessionResult[0].FighterId, Is.EqualTo(peer));
        }

        private IEnumerator StartSurvivalLobby(int finalLevel, int remotePlayers = 1)
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Boot.unity");
            host = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None).Single(b => b.Launcher != null);
            host.Store = null;
            host.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
            host.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            Assert.That(host.Launcher.HostLan(), Is.True);
            yield return WaitFor(() => host.Launcher.Lobby != null && host.Launcher.Lobby.Coordinator != null, "tạo lobby");
            client = JoinSurvivalLobby();
            if (remotePlayers == 2) secondClient = JoinSurvivalLobby();
            var lobby = host.Launcher.Lobby;
            yield return WaitFor(() => client.Session.State == SessionState.Joined && lobby.Motors.Count == remotePlayers + 1,
                "client vào lobby");
            var ids = host.Session.Peers.OrderBy(id => id).Prepend(0UL).ToArray();
            var choices = new[] { 1, 0, 2 };
            var layout = SceneManager.GetActiveScene().GetRootGameObjects().Single(g => g.name == "Store aisle").transform;
            var stands = layout.Find("Fruit stands");
            for (var i = 0; i < ids.Length; i++)
                lobby.Motors[ids[i]].GetComponent<Rigidbody>().position = PickPosition(stands, choices[i]);
            yield return WaitFor(() => ids.Select(id => lobby.Coordinator.Room.Players[id].Fruit)
                .SequenceEqual(choices.Take(ids.Length).Select(choice => (int?)choice)), "mọi người chọn trái");
            lobby.Motors[0].GetComponent<Rigidbody>().position = layout.Find("Host desk").position + Vector3.back * 2;
            var hostDesk = lobby.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "Host desk");
            yield return WaitFor(() => hostDesk.gameObject.activeSelf, "host tới quầy");
            SetRule(lobby, hostDesk, MatchRule.Survival);
            hostDesk.GetComponentsInChildren<Button>(true).Single(b => b.name == $"Map {finalLevel}").onClick.Invoke();
            Assert.That(lobby.Coordinator.State.FinalLevel, Is.EqualTo(finalLevel));
            lobby.Coordinator.SetReady(true);
            client.LobbySync.SendReady(true);
            if (secondClient != null) secondClient.LobbySync.SendReady(true);
            Time.timeScale = 8;
        }

        // The console's arrows flip between the two rules; click one only when the rule is not already the wanted one.
        private static void SetRule(LobbyController lobby, RectTransform desk, MatchRule rule)
        {
            if (lobby.Coordinator.State.Rule != rule)
                desk.GetComponentsInChildren<Button>().Single(b => b.name == "Rule next").onClick.Invoke();
        }

        private static GameplayBootstrap JoinSurvivalLobby()
        {
            var peer = new GameObject("Survival loopback client").AddComponent<GameplayBootstrap>();
            peer.GetComponent<NetworkManager>().NetworkConfig.EnableSceneManagement = false;
            peer.GetComponent<NetworkManager>().NetworkConfig.ForceSamePrefabs = false;
            Assert.That(peer.Session.Join("127.0.0.1"), Is.True);
            return peer;
        }

        private bool IsPlaying(int level) => host.Launcher.Match != null &&
            host.Launcher.Match.Director?.Level == level && host.Launcher.Match.Director.Phase == MatchPhase.Playing;

        private void RingOut(ulong id) => host.Launcher.Match.Fighters.Single(f => f.Id == id)
            .Motor.GetComponent<Rigidbody>().position = Vector3.down * 30;

        private IEnumerator ClickResultAction(string caption)
        {
            yield return WaitFor(() => ResultButton(caption) != null, $"host có nút {caption}");
            var button = ResultButton(caption);
            button.onClick.Invoke();
            button.onClick.Invoke();
        }

        private static Vector3 PickPosition(Transform stands, int fruit) =>
            stands.GetChild(fruit).Find("Pick volume").GetComponent<BoxCollider>().bounds.center;

        private Button ResultButton(string label) => host.Launcher.Overlay.GetComponentsInChildren<Button>()
            .SingleOrDefault(button => button.GetComponentInChildren<Text>().text == label);
    }
}
#endif
