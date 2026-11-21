using System;
using SquishyBump.Core;
using SquishyBump.Net.Session;
using SquishyBump.Net.Sync;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquishyBump.App
{
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class GameplayBootstrap : MonoBehaviour, IHostClock
    {
        private StubSessionService session;
        private GameEventHub events;
        private NetSync sync;
        private LobbySync lobbySync;
        [Tooltip("Boot only: profiles, levels and roster for the scene flow. Empty in test rigs.")]
        [SerializeField] private MatchSetup setup;

        public ISessionService Session => session;
        public IHostClock Clock => this;
        public IGameEvents Events => events;
        public IGameEventSink EventSink => events;
        public GameEventHub EventHub => events;
        /// <summary>Input up / snapshot down streams of this peer.</summary>
        public NetSync Sync => sync;
        public LobbySync LobbySync => lobbySync;
        public LobbyState LobbySelection { get; private set; }
        public void SetLobbySelection(LobbyState state)
        {
            LobbySelection = state;
            if (state != null) Settings.Rule = state.Rule;
        }
        /// <summary>Local options (not game state); persisted by SB-51.</summary>
        public GameSettings Settings => Saved.Settings;
        /// <summary>This player's save (GDD 10.5); loaded on Boot, written whenever settings or records change.</summary>
        public SaveData Saved { get; private set; } = new();
        /// <summary>Where saves go; null (test rigs without a match setup) keeps everything in memory.</summary>
        public LocalSave Store { get; set; }

        public void WriteSave() => Store?.Save(Saved);

        /// <summary>Single Player Survival level won: keeps the best run.</summary>
        public void RecordSurvival(int levelWon)
        {
            if (Saved.RecordSurvival(levelWon)) WriteSave();
        }

        public void RecordRank(int points)
        {
            if (Saved.RecordRank(points)) WriteSave();
        }
        public LevelLauncher Launcher { get; private set; }

        public uint Tick { get; private set; }
        public event Action<uint> Ticked;

        private void Awake()
        {
            var manager = GetComponent<NetworkManager>();
            var transport = GetComponent<UnityTransport>();
            manager.NetworkConfig ??= new NetworkConfig();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.TickRate = 60;
            session = new StubSessionService(manager, transport);
            sync = new NetSync(manager);
            lobbySync = new LobbySync(manager);
            lobbySync.StateReceived += SetLobbySelection;
            events = new GameEventHub(() => session.CanSimulateGameplay);
            session.StateChanged += OnSessionStateChanged;
            if (setup != null)
            {
                Store = new LocalSave(Application.persistentDataPath);
                Saved = Store.Load();
                Settings.Changed += _ => WriteSave();
                Launcher = gameObject.AddComponent<LevelLauncher>();
                Launcher.Init(this, setup);
            }
            // Hazard events: the host's hub forwards each one reliably; a client's hub receives them (dedup by EventId).
            events.HazardRaised += ForwardHazard;
            sync.HazardReceived += events.Deliver;
        }

        private void Start()
        {
            // shortcut: only a fresh start (player launch or Play on Boot, frame ≤ 2) goes to the menu, so PlayMode
            // tests that load Boot mid-run keep their own scenes. Replace with an explicit boot flag if it bites.
            if (Launcher != null && Time.frameCount <= 2)
                SceneManager.LoadScene(setup.MainMenuScene, LoadSceneMode.Single);
        }

        private void Update() { sync.Poll(); lobbySync.Poll(); }

        // Vòng mô phỏng 60 Hz của host (GDD 10.4). Client không mở tick.
        private void FixedUpdate()
        {
            if (!session.CanSimulateGameplay)
                return;

            Tick++;
            Ticked?.Invoke(Tick);
        }

        // Nguồn input và vòng lặp trận đấu sẽ gọi điểm vào này khi được tích hợp.
        public bool TrySimulate(IFighterMotor motor, InputCommand command)
        {
            if (!session.CanSimulateGameplay || !command.HasValidValues)
                return false;

            motor.Step(command, Time.fixedDeltaTime);
            return true;
        }

        private void ForwardHazard(HazardEvent gameEvent)
        {
            if (session.CanSimulateGameplay) sync.BroadcastHazard(gameEvent);
        }

        private void OnSessionStateChanged(SessionState state)
        {
            // A client starts a fresh EventId stream when it joins.
            if (state == SessionState.Joined)
            {
                events.Reset();
                LobbySelection = null; lobbySync.Reset();
            }

            // Hosting mở phiên mới, Idle đóng phiên: cả hai đều bắt đầu lại tick và EventId.
            if (state != SessionState.Hosting && state != SessionState.Idle)
                return;

            Tick = 0;
            LobbySelection = null; lobbySync.Reset();
            events.Reset();
        }

        private void OnDestroy()
        {
            if (session == null)
                return;

            session.StateChanged -= OnSessionStateChanged;
            events.HazardRaised -= ForwardHazard;
            sync.HazardReceived -= events.Deliver;
            sync.Dispose();
            lobbySync.Dispose();
            lobbySync.StateReceived -= SetLobbySelection;
            session.Dispose();
        }
    }
}
