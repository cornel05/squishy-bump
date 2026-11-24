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
        private LanSurvivalRun survivalRun;
        private LanSurvivalRun.Decision survivalDecision;
        private bool survivalLaunchPending, survivalLoadPending, survivalFinished;
        private int evaluatedPeers = -1;
        private SinglePlayerRun singleRun;
        private uint singleSeed;
        private bool singleResultHandled, singleLoadPending;
        private bool singleWon;
        public SinglePlayerRun SingleRun => singleRun;

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
        public void StartSinglePlayer(int level = 1, uint? seed = null)
        {
            if (BeginSinglePlayer())
            {
                if (seed.HasValue) singleSeed = seed.Value;
                LoadLevel(level);
            }
        }

        private bool BeginSinglePlayer()
        {
            singlePlayer = true;
            singleRun = null;
            singleSeed = unchecked((uint)System.Environment.TickCount);
            singleResultHandled = singleLoadPending = false;
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

        public bool Join(string address, string password = null)
        {
            singlePlayer = false;
            joinedAddress = address;
            return bootstrap.Session.Join(address, LanPort, password);
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
            bootstrap.SetLobbySelection(state);
            if (!singlePlayer && state.Rule == MatchRule.Survival)
            {
                bootstrap.LockLanRoster();
                survivalRun = new LanSurvivalRun(state.FinalLevel, state.Players.Select(player => player.Id));
                survivalLaunchPending = true;
                return;
            }
            LoadLevel(1);
        }

        private void Update()
        {
            if (singlePlayer && singleRun != null && bootstrap.Session.CanSimulateGameplay)
            { UpdateSinglePlayer(); return; }
            if (survivalRun == null || survivalFinished || !bootstrap.Session.CanSimulateGameplay) return;
            if (survivalLaunchPending)
            {
                survivalLaunchPending = !TryLoadLevel(1);
                return;
            }
            if (survivalLoadPending || loadingThrough != null || Match == null || !Match.Initialized) return;

            var peers = bootstrap.Session.Peers;
            var result = Match.Director?.Phase == MatchPhase.Result ? Match.Director.Placements : null;
            if (evaluatedPeers == peers.Count && (result == null || survivalDecision != null)) return;
            evaluatedPeers = peers.Count;
            survivalDecision = survivalRun.Evaluate(peers, result);
            if (survivalDecision == null) return;
            if (survivalDecision.NextLevel == null)
            {
                survivalFinished = true;
                var sink = bootstrap.EventSink;
                sink.Publish(MatchEvent.SessionResult(sink.NextEventId(), bootstrap.Tick, survivalDecision.Standings));
                return;
            }
            Overlay.OfferResultAction(survivalDecision.NextLevel == survivalRun.Level ? "Đấu lại" : "Tiếp tục", ContinueSurvival);
        }

        private void UpdateSinglePlayer()
        {
            if (singleLoadPending || singleResultHandled || Match == null || !Match.Started || Match.Director.Phase != MatchPhase.Result) return;
            singleResultHandled = true;
            singleWon = singleRun.Rule == MatchRule.Rank || Match.Director.GetSinglePlayerOutcome(0) != SurvivalOutcome.Retry;
            if (singleWon && singleRun.Level == SinglePlayerRun.FinalLevel)
            {
                singleRun.Continue(true);
                var rows = singleRun.Rule == MatchRule.Rank
                    ? scores.Standings().Select(s => new MatchStanding(s.FighterId, s.Rank, s.Points, s.Lives, checked((int)s.SurvivalTicks))).ToArray()
                    : Match.Director.Placements.Select(p => new MatchStanding(p.FighterId, p.Rank, 0)).ToArray();
                var characters = Match.Fighters.ToDictionary(f => (ulong)f.Id, f => (int)f.Fruit);
                Overlay.ShowSessionResult(rows, id => setup.Roster[characters[id]].name, id => characters[id]);
                Overlay.ShowSinglePlayerSummary(singleRun.LevelsCleared, singleRun.Retries);
                Overlay.OfferResultAction("Về menu", LeaveToMenu);
            }
            else Overlay.OfferResultAction(singleWon ? "Tiếp tục" : "Chơi lại", ContinueSinglePlayer);
        }

        private void ContinueSinglePlayer()
        {
            if (singleLoadPending || !singleResultHandled || singleRun.Finished || !bootstrap.Session.CanSimulateGameplay) return;
            var next = singleWon ? singleRun.Level + 1 : singleRun.Level;
            singleLoadPending = true;
            if (!TryLoadLevel(next)) { singleLoadPending = false; return; }
            singleRun.Continue(singleWon);
        }

        private void ContinueSurvival()
        {
            if (survivalLoadPending || survivalFinished || !bootstrap.Session.CanSimulateGameplay ||
                survivalDecision?.NextLevel == null || evaluatedPeers != bootstrap.Session.Peers.Count) return;
            if (!TryLoadLevel(survivalDecision.NextLevel.Value)) return;
            survivalRun.Advance(survivalDecision);
            survivalLoadPending = true;
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
            if (singlePlayer) return setup.Roster;
            return selection.Players.OrderBy(p => p.Id).Select(p => setup.Roster[p.Fruit]).ToArray();
        }

        /// <summary>Host: every peer loads the level; the match starts when it is loaded here.</summary>
        public void LoadLevel(int number)
        {
            // Lối tắt công khai dành cho test/công cụ dev; UI production chỉ gọi từ lobby ở L1.
            if (singlePlayer && singleRun != null && singleRun.Level != number)
                singleRun = new SinglePlayerRun(singleRun.Current.Fruits[0], singleRun.Seed, singleRun.Rule, number);
            TryLoadLevel(number);
        }

        private bool TryLoadLevel(int number)
        {
            if (loadingThrough != null) return false;
            var level = setup.Levels.First(l => l.LevelNumber == number);
            var manager = bootstrap.GetComponent<NetworkManager>();
            var sceneManager = manager.NetworkConfig.EnableSceneManagement ? manager.SceneManager : null;
            if (bootstrap.Session.CanSimulateGameplay && sceneManager != null)
            {
                loadingThrough = sceneManager;
                pendingScene = System.IO.Path.GetFileNameWithoutExtension(level.ScenePath);
                loadingThrough.OnLoadEventCompleted += OnLevelReady;
                var status = sceneManager.LoadScene(level.ScenePath, LoadSceneMode.Single);
                if (status != SceneEventProgressStatus.Started)
                {
                    StopWaitingForScene();
                    if (status != SceneEventProgressStatus.SceneEventInProgress)
                        Debug.LogError($"[LevelLauncher] Could not load {level.ScenePath}: {status}");
                    return false;
                }
            }
            else
                SceneManager.LoadScene(level.ScenePath, LoadSceneMode.Single);
            return true;
        }

        private void OnLevelReady(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            if (sceneName != pendingScene) return;
            StopWaitingForScene();
            if (survivalRun != null && bootstrap.Session.CanSimulateGameplay)
            {
                var manager = bootstrap.GetComponent<NetworkManager>();
                foreach (var id in timedOut)
                    if (bootstrap.Session.Peers.Contains(id)) manager.DisconnectClient(id);
            }
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
            if (state == SessionState.Idle)
            {
                StopWaitingForScene();
                survivalRun = null;
                survivalDecision = null;
                survivalLaunchPending = survivalLoadPending = survivalFinished = false;
                evaluatedPeers = -1;
                singleRun = null;
                singleLoadPending = singleResultHandled = false;
            }
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
            Menu.Init(bootstrap.Session, bootstrap.Settings, $"v{Application.version}", setup.Levels, bootstrap.Discovery);
            if (bootstrap.Session.LastEndReason == SessionEndReason.HostLost)
                Menu.ShowPage("Multiplayer");
            Menu.SinglePlayerRequested += _ => EnterSinglePlayerLobby();
            Menu.HostRequested += () => HostLan();
            Menu.JoinRequested += (address, password) => Join(address, password);
            Menu.QuitRequested += Application.Quit;
        }

        private void BuildMatch(Scene scene, LevelDefinition level)
        {
            bootstrap.MatchState.Reset();
            survivalLoadPending = false;
            survivalDecision = null;
            evaluatedPeers = -1;
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
            var lanSurvival = !singlePlayer && bootstrap.LobbySelection?.Rule == MatchRule.Survival;
            var roster = lanSurvival ? setup.Roster : MatchRoster();
            Match.Configure(bootstrap, setup.Balance, bounds, FindInScene<RingDrop>(scene), rig, roster,
                level.LevelNumber, autoHost: false);
            if (lanSurvival)
            {
                var contestants = survivalRun == null ? null : bootstrap.LobbySelection.Players
                    .Where(player => survivalRun.Active.Contains(player.Id)).OrderBy(player => player.Id).ToArray();
                Match.ConfigureLanSurvival(contestants, LeaveToMenu);
            }
            Match.ConfigureRank(scores, ++matchSequence);
            Match.ConfigureBots(setup.DefaultBotProfile);
            if (singlePlayer)
            {
                singleRun ??= new SinglePlayerRun(bootstrap.LobbySelection?.Players.Single(p => p.Id == 0).Fruit ?? 0,
                    singleSeed, bootstrap.Settings.Rule, level.LevelNumber);
                Match.ConfigureSinglePlayer(singleRun.Current, setup);
                singleResultHandled = singleLoadPending = false;
            }
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
