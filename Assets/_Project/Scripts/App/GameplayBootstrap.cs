using System;
using SquishyBump.Core;
using SquishyBump.Net.Session;
using SquishyBump.Net.Sync;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SquishyBump.App
{
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class GameplayBootstrap : MonoBehaviour, IHostClock
    {
        private StubSessionService session;
        private GameEventHub events;
        private NetSync sync;

        public ISessionService Session => session;
        public IHostClock Clock => this;
        public IGameEvents Events => events;
        public IGameEventSink EventSink => events;
        public GameEventHub EventHub => events;
        /// <summary>Input up / snapshot down streams of this peer.</summary>
        public NetSync Sync => sync;

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
            events = new GameEventHub(() => session.CanSimulateGameplay);
            session.StateChanged += OnSessionStateChanged;
            // Hazard events: the host's hub forwards each one reliably; a client's hub receives them (dedup by EventId).
            events.HazardRaised += ForwardHazard;
            sync.HazardReceived += events.Deliver;
        }

        private void Update() => sync.Poll();

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
                events.Reset();

            // Hosting mở phiên mới, Idle đóng phiên: cả hai đều bắt đầu lại tick và EventId.
            if (state != SessionState.Hosting && state != SessionState.Idle)
                return;

            Tick = 0;
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
            session.Dispose();
        }
    }
}
