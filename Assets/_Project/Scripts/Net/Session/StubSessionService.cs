using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using SquishyBump.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SquishyBump.Net.Session
{
    /// <summary>Phiên LAN NGO; giữ tên stub để tương thích composition root đã có từ SB-06.</summary>
    public sealed class StubSessionService : ISessionService, IDisposable
    {
        private const string Loopback = "127.0.0.1";
        private const ushort LanPort = 7777;
        public const int MaxPlayers = 6;
        // Phiên bản hợp đồng mạng, độc lập Application.version; đổi khi wire format thay đổi.
        public const string Protocol = "SquishyBump-LAN-11";
        private readonly NetworkManager manager;
        private readonly UnityTransport transport;
        private readonly List<ulong> peers = new();
        private SessionMode hostMode;
        private readonly string buildVersion;
        private SessionEndReason pendingEndReason;
        public bool AcceptingPlayers { get; set; } = true;
        private int capacity = MaxPlayers;
        /// <summary>Host: số người tối đa, 2–6. Giá trị ngoài khoảng bị chặn.</summary>
        public int Capacity { get => capacity; set => capacity = Mathf.Clamp(value, LobbyState.MinCapacity, MaxPlayers); }
        /// <summary>Host: mật khẩu phòng; null hoặc rỗng là không có. Chỉ băm đi qua mạng.</summary>
        public string Password { get; set; }

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
            AcceptingPlayers = true;
            Capacity = MaxPlayers;
            Password = null;
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.ConnectionData = JoinPayload(buildVersion, null);
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

        public bool Join(string address, ushort port = LanPort, string password = null)
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
            manager.NetworkConfig.ConnectionData = JoinPayload(buildVersion, password);
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
                CheckJoin(request.Payload, buildVersion, manager.ConnectedClientsIds.Count, hostMode, Capacity, Password);
            if (request.ClientNetworkId != NetworkManager.ServerClientId && reason == SessionEndReason.None && !AcceptingPlayers)
                reason = SessionEndReason.MatchInProgress;
            response.Approved = reason == SessionEndReason.None;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = reason == SessionEndReason.None ? "" : $"SB:{reason}";
        }

        /// <summary>Payload = phiên bản, rồi (nếu có mật khẩu) LF + băm. Mật khẩu không đi qua mạng dưới dạng chữ.</summary>
        public static byte[] JoinPayload(string version, string password) =>
            Encoding.UTF8.GetBytes(string.IsNullOrEmpty(password) ? version : version + "\n" + PasswordHash(password));

        public static string PasswordHash(string password)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""))).Replace("-", "").Substring(0, 16);
        }

        /// <summary>Host kiểm tra payload có giới hạn trước khi cấp slot; không nhận gameplay state từ client.</summary>
        public static SessionEndReason CheckJoin(byte[] payload, string expectedVersion, int connected, SessionMode mode) =>
            CheckJoin(payload, expectedVersion, connected, mode, MaxPlayers, null);

        public static SessionEndReason CheckJoin(byte[] payload, string expectedVersion, int connected, SessionMode mode,
            int capacity, string password)
        {
            if (payload == null || payload.Length == 0 || payload.Length > 128) return SessionEndReason.VersionMismatch;
            var text = Encoding.UTF8.GetString(payload);
            var split = text.IndexOf('\n');
            var version = split < 0 ? text : text.Substring(0, split);
            if (!string.Equals(version, expectedVersion, StringComparison.Ordinal)) return SessionEndReason.VersionMismatch;
            if (!string.IsNullOrEmpty(password) &&
                !string.Equals(split < 0 ? "" : text.Substring(split + 1), PasswordHash(password), StringComparison.Ordinal))
                return SessionEndReason.WrongPassword;
            if (mode != SessionMode.Lan || connected >= Math.Min(capacity, MaxPlayers)) return SessionEndReason.RoomFull;
            return SessionEndReason.None;
        }

        public static SessionEndReason ReadRejection(string reason) => reason switch
        {
            "SB:VersionMismatch" => SessionEndReason.VersionMismatch,
            "SB:RoomFull" => SessionEndReason.RoomFull,
            "SB:MatchInProgress" => SessionEndReason.MatchInProgress,
            "SB:WrongPassword" => SessionEndReason.WrongPassword,
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
