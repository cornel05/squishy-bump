using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using SquishyBump.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SquishyBump.Net.Session
{
    /// <summary>Phiên LAN NGO; giữ tên stub để tương thích composition root đã có từ SB-06.</summary>
    public sealed class StubSessionService : ISessionService, ISessionAdmission, IDisposable
    {
        private const string Loopback = "127.0.0.1";
        private const ushort LanPort = 7777;
        public const int MaxPlayers = 6;
        // Phiên bản hợp đồng mạng, độc lập Application.version; đổi khi wire format thay đổi.
        public const string Protocol = "SquishyBump-LAN-4";
        private readonly NetworkManager manager;
        private readonly UnityTransport transport;
        private readonly List<ulong> peers = new();
        private SessionMode hostMode;
        private readonly string buildVersion;
        private SessionEndReason pendingEndReason;

        public StubSessionService(NetworkManager manager, UnityTransport transport, string version = null)
        {
            this.manager = manager;
            this.transport = transport;
            buildVersion = $"{Protocol}:{version ?? Application.version}";
            manager.ConnectionApprovalCallback = ApproveConnection;
            manager.OnClientConnectedCallback += OnClientConnected;
            manager.OnClientDisconnectCallback += OnClientDisconnected;
            manager.OnClientStopped += OnClientStopped;
        }

        public SessionState State { get; private set; }
        public bool CanSimulateGameplay => State == SessionState.Hosting && manager.IsHost;
        public bool AcceptingJoins { get; set; } = true;
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
            AcceptingJoins = true;
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(buildVersion);
            pendingEndReason = SessionEndReason.None;
            if (mode == SessionMode.SinglePlayer)
                transport.SetConnectionData(Loopback, 0, Loopback);
            else
                transport.SetConnectionData(Loopback, LanPort, "0.0.0.0");

            if (!manager.StartHost())
            {
                LastEndReason = SessionEndReason.StartFailed;
                return false;
            }

            LastEndReason = SessionEndReason.None;
            SetState(SessionState.Hosting);
            return true;
        }

        public bool Join(string address, ushort port = LanPort)
        {
            if (State != SessionState.Idle || manager.IsListening)
                return false;
            if (port == 0 || !IPAddress.TryParse(address?.Trim(), out var ip) ||
                ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                LastEndReason = SessionEndReason.InvalidAddress;
                return false;
            }

            transport.SetConnectionData(ip.ToString(), port);
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(buildVersion);
            pendingEndReason = SessionEndReason.None;
            LastEndReason = SessionEndReason.None;
            SetState(SessionState.Joining);
            if (!manager.StartClient())
            {
                LastEndReason = SessionEndReason.StartFailed;
                SetState(SessionState.Idle);
                return false;
            }
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
            var reason = request.ClientNetworkId == NetworkManager.ServerClientId ? SessionEndReason.None :
                CheckJoin(request.Payload, buildVersion, manager.ConnectedClientsIds.Count, hostMode, AcceptingJoins);
            response.Approved = reason == SessionEndReason.None;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = reason == SessionEndReason.None ? "" : $"SB:{reason}";
        }

        /// <summary>Host kiểm tra payload có giới hạn trước khi cấp slot; không nhận gameplay state từ client.</summary>
        public static SessionEndReason CheckJoin(byte[] payload, string expectedVersion, int connected, SessionMode mode)
            => CheckJoin(payload, expectedVersion, connected, mode, true);

        public static SessionEndReason CheckJoin(byte[] payload, string expectedVersion, int connected, SessionMode mode, bool acceptingJoins)
        {
            if (payload == null || payload.Length == 0 || payload.Length > 128 ||
                !string.Equals(Encoding.UTF8.GetString(payload), expectedVersion, StringComparison.Ordinal))
                return SessionEndReason.VersionMismatch;
            if (mode != SessionMode.Lan || connected >= MaxPlayers) return SessionEndReason.RoomFull;
            if (!acceptingJoins) return SessionEndReason.MatchInProgress;
            return SessionEndReason.None;
        }

        public static SessionEndReason ReadRejection(string reason) => reason switch
        {
            "SB:VersionMismatch" => SessionEndReason.VersionMismatch,
            "SB:RoomFull" => SessionEndReason.RoomFull,
            "SB:MatchInProgress" => SessionEndReason.MatchInProgress,
            _ => SessionEndReason.ConnectFailed,
        };

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
            if (State == SessionState.Joining) pendingEndReason = ReadRejection(manager.DisconnectReason);
            if (peers.Remove(clientId))
                PeerLeft?.Invoke(clientId);
        }

        private void OnClientStopped(bool wasHost)
        {
            // Leave() đặt Leaving trước khi tắt; mọi trạng thái khác nghĩa là phiên tự đứt.
            LastEndReason = State switch
            {
                SessionState.Leaving => SessionEndReason.LocalLeave,
                SessionState.Joining => pendingEndReason == SessionEndReason.None ? SessionEndReason.ConnectFailed : pendingEndReason,
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
