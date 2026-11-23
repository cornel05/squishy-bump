#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
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
        private GameplayBootstrap host, client;
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
            var length3 = hostDesk.GetComponentsInChildren<Button>(true).Single(b => b.GetComponentInChildren<Text>().text == "L1 → L3");
            length3.onClick.Invoke();
            Assert.That(lobby.Coordinator.State.FinalLevel, Is.EqualTo(5), "Không đổi cấu hình từ xa quầy.");
            lobby.Motors[0].GetComponent<Rigidbody>().position = layout.Find("Host desk").position + Vector3.back * 2;
            yield return WaitFor(() => hostDesk.gameObject.activeSelf, "host tới quầy");
            hostDesk.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "Rank").onClick.Invoke();
            Assert.That(lobby.Coordinator.State.Rule, Is.EqualTo(MatchRule.Rank));
            hostDesk.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "Survival").onClick.Invoke();
            length3.onClick.Invoke();
            Assert.That(lobby.Coordinator.State.Rule, Is.EqualTo(MatchRule.Survival));
            Assert.That(lobby.Coordinator.State.FinalLevel, Is.EqualTo(3));
            lobby.Coordinator.SetReady(true); client.LobbySync.SendReady(true);
            yield return WaitFor(() => lobby.Coordinator.State.RemainingTicks > 0, "host countdown");
            yield return WaitFor(() => client.LobbySelection != null && client.LobbySelection.RemainingTicks > 0, "client nhận countdown");
            Assert.That(panel.BoardText, Does.Contain("Watermelon").And.Contain("Pineapple").And.Contain("READY").And.Contain("IP host:"));
            Assert.That(host.LobbySync.RejectedPackets, Is.Zero);
            Assert.That(client.LobbySync.RejectedPackets, Is.Zero);
            Assert.That(host.Launcher.Match, Is.Null, "Chưa được bỏ qua countdown 5 giây.");
            Time.timeScale = 4;
            yield return WaitFor(() => host.Launcher.Match != null && host.Launcher.Match.Started, "bắt đầu match sau countdown");
            Assert.That(host.Launcher.Match.Fighters, Has.Count.EqualTo(2), "LAN không có idle/bot slots phụ.");
            Assert.That(host.LobbySelection.FinalLevel, Is.EqualTo(3));
            Assert.That(host.Launcher.Match.Fighters.Select(f => (ulong)f.Id), Is.EquivalentTo(new[] { 0UL, peer }));
            Assert.That(host.Launcher.Match.Fighters[0].Motor.name, Does.StartWith("Watermelon"));
            Assert.That(host.Launcher.Match.Fighters[1].Motor.name, Does.StartWith("Pineapple"));
        }
    }
}
#endif
