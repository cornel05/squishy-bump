using System;
using SquishyBump.Core;
using SquishyBump.Net.Session;
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

        public ISessionService Session => session;
        public IHostClock Clock => this;
        public IGameEvents Events => events;
        public IGameEventSink EventSink => events;
        public GameEventHub EventHub => events;

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
            events = new GameEventHub(() => session.CanSimulateGameplay);
            session.StateChanged += OnSessionStateChanged;
        }

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

        private void OnSessionStateChanged(SessionState state)
        {
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
            session.Dispose();
        }
    }
}
