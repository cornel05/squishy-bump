using System.Linq;
using System.Collections.Generic;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using SquishyBump.Presentation;
using SquishyBump.UI.Menus;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquishyBump.App
{
    /// <summary>
    /// Scene flow (F01 → match): lives on the persistent bootstrap object. Builds the main menu when the menu
    /// scene loads and a match (camera rig, <see cref="MatchHost"/>, Esc menu) when a level scene loads.
    /// While hosting, levels load through NGO's scene manager so joined clients follow.
    /// </summary>
    public sealed class LevelLauncher : MonoBehaviour
    {
        public const ushort LanPort = 7777;
        public const string LobbyScenePath = "Assets/_Project/Scenes/Lobby.unity";

        private GameplayBootstrap bootstrap;
        private MatchSetup setup;
        private bool singlePlayer;
        private double perfSeconds;
        private NetworkSceneManager loadingThrough;
        private string pendingScene;
        private ScoreService scores;
        private uint matchSequence;

        private void ResetRankSession()
        {
            scores = new ScoreService(() => bootstrap.Session.CanSimulateGameplay,
                Mathf.RoundToInt(setup.Balance.LastTouchWindowSeconds * setup.Balance.FixedStepHz), setup.Balance.PointsPerRingOut);
            matchSequence = 0;
        }
        private string joinedAddress;

        public MainMenu Menu { get; private set; }
        public MatchHost Match { get; private set; }
        public PauseMenu Pause { get; private set; }
        public MatchOverlay Overlay { get; private set; }
        public AudioDirector Audio { get; private set; }
        public LobbyController Lobby { get; private set; }

        public void Init(GameplayBootstrap owner, MatchSetup matchSetup)
        {
            bootstrap = owner;
            setup = matchSetup;
            perfSeconds = FrameStatsRecorder.ReadDuration(System.Environment.GetCommandLineArgs());
            SceneManager.sceneLoaded += OnSceneLoaded;
            bootstrap.Session.StateChanged += OnSessionStateChanged;
        }

        private void OnDestroy()
        {
            StopWaitingForScene();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (bootstrap != null)
                bootstrap.Session.StateChanged -= OnSessionStateChanged;
        }

        /// <summary>Menu path (GDD 3.5): Single Player always goes lobby → L1.</summary>
        public void EnterSinglePlayerLobby()
        {
            if (BeginSinglePlayer()) LoadLobby();
        }

        /// <summary>Direct start of one level, no lobby (tests and tools).</summary>
        public void StartSinglePlayer(int level = 1)
        {
            if (BeginSinglePlayer()) LoadLevel(level);
        }

        private bool BeginSinglePlayer()
        {
            singlePlayer = true;
            ResetRankSession();
            return bootstrap.Session.State != SessionState.Idle || bootstrap.Session.Host(SessionMode.SinglePlayer);
        }

        public bool HostLan()
        {
            singlePlayer = false;
            ResetRankSession();
            var hosted = bootstrap.Session.Host(SessionMode.Lan);
            if (hosted) LoadLobby();
            return hosted;
        }

        public bool Join(string address)
        {
            singlePlayer = false;
            joinedAddress = address;
            return bootstrap.Session.Join(address, LanPort);
        }

        private void LoadLobby()
        {
            var manager = bootstrap.GetComponent<NetworkManager>();
            if (bootstrap.Session.CanSimulateGameplay && manager.NetworkConfig.EnableSceneManagement && manager.SceneManager != null)
                manager.SceneManager.LoadScene(LobbyScenePath, LoadSceneMode.Single);
            else SceneManager.LoadScene(LobbyScenePath, LoadSceneMode.Single);
        }
        private void LaunchLobby(LobbyState state)
        {
            if (!bootstrap.Session.CanSimulateGameplay) return;
            bootstrap.SetLobbySelection(state); LoadLevel(1);
        }
        private string HostAddress()
        {
            if (singlePlayer) return "Single Player";
            if (!bootstrap.Session.CanSimulateGameplay) return $"{joinedAddress}:{LanPort}";
            var addresses = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a.Address))
                .Select(a => a.Address.ToString()).Distinct();
            return string.Join(", ", addresses.Select(a => $"{a}:{LanPort}"));
        }
        private GameObject[] MatchRoster()
        {
            var selection = bootstrap.LobbySelection;
            if (selection == null) return setup.Roster;
            if (singlePlayer)
            {
                var choice = selection.Players.Single(p => p.Id == 0).Fruit;
                return new[] { setup.Roster[choice] }.Concat(setup.Roster.Where((_, i) => i != choice)).ToArray();
            }
            return selection.Players.OrderBy(p => p.Id).Select(p => setup.Roster[p.Fruit]).ToArray();
        }

        /// <summary>Host: every peer loads the level; the match starts when it is loaded here.</summary>
        public void LoadLevel(int number)
        {
            StopWaitingForScene();
            var level = setup.Levels.First(l => l.LevelNumber == number);
            var manager = bootstrap.GetComponent<NetworkManager>();
            var sceneManager = manager.NetworkConfig.EnableSceneManagement ? manager.SceneManager : null;
            if (bootstrap.Session.CanSimulateGameplay && sceneManager != null)
            {
                bootstrap.MatchState.Reset();
                loadingThrough = sceneManager;
                pendingScene = System.IO.Path.GetFileNameWithoutExtension(level.ScenePath);
                loadingThrough.OnLoadEventCompleted += OnLevelReady;
                sceneManager.LoadScene(level.ScenePath, LoadSceneMode.Single);
            }
            else
                SceneManager.LoadScene(level.ScenePath, LoadSceneMode.Single);
        }

        private void OnLevelReady(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            if (sceneName != pendingScene) return;
            StopWaitingForScene();
            // Không mở scene event khác trong callback NGO. Chỉ cho phép vòng Update bắt đầu trận.
            if (Match != null && bootstrap.Session.CanSimulateGameplay) Match.StartWhenReady = true;
        }
        private void StopWaitingForScene()
        {
            if (loadingThrough != null) loadingThrough.OnLoadEventCompleted -= OnLevelReady;
            loadingThrough = null; pendingScene = null;
        }

        public void LeaveToMenu()
        {
            bootstrap.Session.Leave();
            LoadMenu();
        }

        private void LoadMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            SceneManager.LoadScene(setup.MainMenuScene, LoadSceneMode.Single);
        }

        private void OnSessionStateChanged(SessionState state)
        {
            if (state != SessionState.Idle || bootstrap.Session.LastEndReason == SessionEndReason.LocalLeave)
                return;

            var activePath = SceneManager.GetActiveScene().path;
            if (activePath == LobbyScenePath || setup.Levels.Any(level => level.ScenePath == activePath))
                LoadMenu();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == setup.MainMenuScene) BuildMenu(scene);
            if (scene.path == LobbyScenePath)
            {
                if (bootstrap.Session.State == SessionState.Idle && bootstrap.Session.LastEndReason == SessionEndReason.HostLost)
                { LoadMenu(); return; }
                var go = new GameObject("Live 3D Lobby"); go.SetActive(false); SceneManager.MoveGameObjectToScene(go, scene);
                Lobby = go.AddComponent<LobbyController>();
                Lobby.Init(bootstrap, setup, singlePlayer, HostAddress(), LaunchLobby, LeaveToMenu);
                go.SetActive(true); return;
            }
            var level = setup.Levels.FirstOrDefault(l => l.ScenePath == scene.path);
            if (level == null) return;

            // A disconnect can finish before an in-flight network scene load reaches this callback.
            if (bootstrap.Session.State == SessionState.Idle && bootstrap.Session.LastEndReason == SessionEndReason.HostLost)
                LoadMenu();
            else
                BuildMatch(scene, level);
        }

        private void BuildMenu(Scene scene)
        {
            var go = new GameObject("Main Menu");
            SceneManager.MoveGameObjectToScene(go, scene);
            Menu = go.AddComponent<MainMenu>();
            Menu.Init(bootstrap.Session, bootstrap.Settings, $"v{Application.version}", setup.Levels);
            if (bootstrap.Session.LastEndReason == SessionEndReason.HostLost)
                Menu.ShowPage("Multiplayer");
            Menu.SinglePlayerRequested += _ => EnterSinglePlayerLobby();
            Menu.HostRequested += () => HostLan();
            Menu.JoinRequested += address => Join(address);
            Menu.QuitRequested += Application.Quit;
        }

        private void BuildMatch(Scene scene, LevelDefinition level)
        {
            var bounds = FindInScene<TrainingRingBounds>(scene);
            if (bounds == null)
            {
                Debug.LogWarning($"[LevelLauncher] {scene.path} has no TrainingRingBounds yet; back to the menu.");
                // No match means no pause menu: return instead of leaving the player stuck on an empty level.
                LeaveToMenu();
                return;
            }

            var rigObject = new GameObject("Camera Rig");
            rigObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(rigObject, scene);
            var rig = rigObject.AddComponent<CameraRig>();
            rig.Configure(setup.Presentation, FindInScene<Camera>(scene));
            rigObject.SetActive(true);

            var hostObject = new GameObject("Match");
            hostObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(hostObject, scene);
            Match = hostObject.AddComponent<MatchHost>();
            Match.Configure(bootstrap, setup.Balance, bounds, FindInScene<RingDrop>(scene), rig, MatchRoster(),
                level.LevelNumber, autoHost: false);
            Match.ConfigureRank(scores, ++matchSequence);
            Match.ConfigureBots(setup.DefaultBotProfile);
            Pause = hostObject.AddComponent<PauseMenu>();
            Pause.Init(bootstrap.Settings, singlePlayer);
            Match.AttachPause(Pause, singlePlayer, LeaveToMenu);
            Audio = hostObject.AddComponent<AudioDirector>();
            Audio.Configure(setup.Presentation);
            Match.AttachAudio(Audio);
            Match.ConfigureFeedback(setup.Presentation.HitStopSeconds, setup.Presentation.WhooshDistance);
            Overlay = hostObject.AddComponent<MatchOverlay>();
            Overlay.Init();
            Match.AttachOverlay(Overlay, setup.CardSeconds, setup.BannerSeconds, setup.FinalWarningSeconds);
            Overlay.ShowCard(level.ArenaName, setup.CardLine(level.LevelNumber), setup.CardSeconds);
            if (perfSeconds > 0d)
                hostObject.AddComponent<FrameStatsRecorder>().Configure(setup.Presentation, perfSeconds);
            hostObject.SetActive(true);
            // Host starts on its first frame (after Start); clients only show snapshots.
            Match.StartWhenReady = bootstrap.Session.CanSimulateGameplay && loadingThrough == null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }
}
