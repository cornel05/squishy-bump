using System.Linq;
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

        public MainMenu Menu { get; private set; }
        public MatchHost Match { get; private set; }
        public PauseMenu Pause { get; private set; }

        public void Init(GameplayBootstrap owner, MatchSetup matchSetup)
        {
            bootstrap = owner;
            setup = matchSetup;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        public void StartSinglePlayer()
        {
            singlePlayer = true;
            if (bootstrap.Session.State == SessionState.Idle && !bootstrap.Session.Host(SessionMode.SinglePlayer)) return;
            LoadLevel(1);
        }

        public bool HostLan()
        {
            singlePlayer = false;
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
            var level = setup.Levels.First(l => l.LevelNumber == number);
            var sceneManager = NetworkManager.Singleton != null ? NetworkManager.Singleton.SceneManager : null;
            if (bootstrap.Session.CanSimulateGameplay && sceneManager != null)
                sceneManager.LoadScene(level.ScenePath, LoadSceneMode.Single);
            else
                SceneManager.LoadScene(level.ScenePath, LoadSceneMode.Single);
        }

        public void LeaveToMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            bootstrap.Session.Leave();
            SceneManager.LoadScene(setup.MainMenuScene, LoadSceneMode.Single);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == setup.MainMenuScene) BuildMenu(scene);
            var level = setup.Levels.FirstOrDefault(l => l.ScenePath == scene.path);
            if (level != null) BuildMatch(scene, level);
        }

        private void BuildMenu(Scene scene)
        {
            var go = new GameObject("Main Menu");
            SceneManager.MoveGameObjectToScene(go, scene);
            Menu = go.AddComponent<MainMenu>();
            Menu.Init(bootstrap.Session, bootstrap.Settings, $"v{Application.version}");
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
                Debug.LogWarning($"[LevelLauncher] {scene.path} has no TrainingRingBounds yet; no match started.");
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
            Pause = hostObject.AddComponent<PauseMenu>();
            Pause.Init(bootstrap.Settings, singlePlayer);
            Match.AttachPause(Pause, singlePlayer, LeaveToMenu);
            hostObject.SetActive(true);
            // Host starts on its first frame (after Start); clients only show snapshots.
            Match.StartWhenReady = bootstrap.Session.CanSimulateGameplay;
        }

        private static T FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }
}
