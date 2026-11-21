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

        public MainMenu Menu { get; private set; }
        public MatchHost Match { get; private set; }
        public PauseMenu Pause { get; private set; }
        public MatchOverlay Overlay { get; private set; }
        public AudioDirector Audio { get; private set; }

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

        public void StartSinglePlayer(int level = 1)
        {
            singlePlayer = true;
            ResetRankSession();
            if (bootstrap.Session.State == SessionState.Idle && !bootstrap.Session.Host(SessionMode.SinglePlayer)) return;
            LoadLevel(level);
        }

        public bool HostLan()
        {
            singlePlayer = false;
            ResetRankSession();
            return bootstrap.Session.Host(SessionMode.Lan);
        }

        public bool Join(string address)
        {
            singlePlayer = false;
            return bootstrap.Session.Join(address, LanPort);
        }

        /// <summary>Host: every peer loads the level; the match starts when it is loaded here.</summary>
        public void LoadLevel(int number)
        {
            StopWaitingForScene();
            var level = setup.Levels.First(l => l.LevelNumber == number);
            var sceneManager = NetworkManager.Singleton != null ? NetworkManager.Singleton.SceneManager : null;
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
            if (setup.Levels.Any(level => level.ScenePath == activePath))
                LoadMenu();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == setup.MainMenuScene) BuildMenu(scene);
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
            Menu.SinglePlayerRequested += StartSinglePlayer;
            Menu.HostRequested += () => HostLan();
            Menu.JoinRequested += address => Join(address);
            Menu.QuitRequested += Application.Quit;
            Menu.StartMatchRequested += () => LoadLevel(1);
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
            Match.Configure(bootstrap, setup.Balance, bounds, FindInScene<RingDrop>(scene), rig, setup.Roster,
                level.LevelNumber, autoHost: false);
            Match.ConfigureRank(scores, ++matchSequence);
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
