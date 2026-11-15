using SquishyBump.Core;
using SquishyBump.Net.Session;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SquishyBump.App
{
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class GameplayBootstrap : MonoBehaviour
    {
        private StubSessionService session;
        public ISessionService Session => session;

        private void Awake()
        {
            var manager = GetComponent<NetworkManager>();
            var transport = GetComponent<UnityTransport>();
            manager.NetworkConfig ??= new NetworkConfig();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.TickRate = 60;
            session = new StubSessionService(manager, transport);
        }

        // Nguồn input và vòng lặp trận đấu sẽ gọi điểm vào này khi được tích hợp.
        public bool TrySimulate(IFighterMotor motor, InputCommand command)
        {
            if (!session.CanSimulateGameplay || !command.HasValidValues)
                return false;

            motor.Step(command, Time.fixedDeltaTime);
            return true;
        }

        private void OnDestroy() => session?.Dispose();
    }
}
