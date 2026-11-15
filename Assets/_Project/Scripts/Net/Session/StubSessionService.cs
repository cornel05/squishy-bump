using System;
using System.Collections.Generic;
using System.Net;
using SquishyBump.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace SquishyBump.Net.Session
{
    public sealed class StubSessionService : ISessionService, IDisposable
    {
        private const string Loopback = "127.0.0.1";
        private const ushort LanPort = 7777;
        private readonly NetworkManager manager;
        private readonly UnityTransport transport;
        private readonly List<ulong> peers = new();
        private SessionMode hostMode;

        public StubSessionService(NetworkManager manager, UnityTransport transport)
        {
            this.manager = manager;
            this.transport = transport;
            manager.ConnectionApprovalCallback = ApproveConnection;
            manager.OnClientConnectedCallback += OnClientConnected;
            manager.OnClientDisconnectCallback += OnClientDisconnected;
            manager.OnClientStopped += OnClientStopped;
        }

        public SessionState State { get; private set; }
        public bool CanSimulateGameplay => State == SessionState.Hosting && manager.IsHost;
        public event Action<SessionState> StateChanged;
        public SessionEndReason LastEndReason { get; private set; }
        public IReadOnlyList<ulong> Peers => peers;
        public event Action<ulong> PeerJoined;
        public event Action<ulong> PeerLeft;

        public bool Host(SessionMode mode)
        {
            if (State != SessionState.Idle || manager.IsListening ||
                (mode != SessionMode.SinglePlayer && mode != SessionMode.Lan))
                return false;

            hostMode = mode;
            manager.NetworkConfig.ConnectionApproval = true;
            if (mode == SessionMode.SinglePlayer)
                transport.SetConnectionData(Loopback, 0, Loopback);
            else
                transport.SetConnectionData(Loopback, LanPort, "0.0.0.0");

            if (!manager.StartHost())
                return false;

            LastEndReason = SessionEndReason.None;
            SetState(SessionState.Hosting);
            return true;
        }

        public bool Join(string address, ushort port)
        {
            if (State != SessionState.Idle || manager.IsListening || port == 0 ||
                !IPAddress.TryParse(address, out var ip))
                return false;

            transport.SetConnectionData(ip.ToString(), port);
            if (!manager.StartClient())
                return false;

            LastEndReason = SessionEndReason.None;
            SetState(SessionState.Joining);
            return true;
        }

        public void Leave()
        {
            if (State == SessionState.Idle || State == SessionState.Leaving)
                return;

            SetState(SessionState.Leaving);
            manager.Shutdown();
        }

        public void Dispose()
        {
            if (manager == null)
                return;

            manager.OnClientConnectedCallback -= OnClientConnected;
            manager.OnClientDisconnectCallback -= OnClientDisconnected;
            manager.OnClientStopped -= OnClientStopped;
            manager.ConnectionApprovalCallback = null;
            if (manager.IsListening)
                manager.Shutdown();
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = request.ClientNetworkId == NetworkManager.ServerClientId ||
                (hostMode == SessionMode.Lan && manager.ConnectedClientsIds.Count < 6);
            response.CreatePlayerObject = false;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (State == SessionState.Joining && clientId == manager.LocalClientId)
                SetState(SessionState.Joined);
            else if (State == SessionState.Hosting && clientId != manager.LocalClientId && !peers.Contains(clientId))
            {
                peers.Add(clientId);
                PeerJoined?.Invoke(clientId);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (peers.Remove(clientId))
                PeerLeft?.Invoke(clientId);
        }

        private void OnClientStopped(bool wasHost)
        {
            // Leave() đặt Leaving trước khi tắt; mọi trạng thái khác nghĩa là phiên tự đứt.
            LastEndReason = State switch
            {
                SessionState.Leaving => SessionEndReason.LocalLeave,
                SessionState.Joining => SessionEndReason.ConnectFailed,
                _ => SessionEndReason.HostLost,
            };
            peers.Clear();
            SetState(SessionState.Idle);
        }

        private void SetState(SessionState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
