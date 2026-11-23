using System;
using System.Linq;
using SquishyBump.Core;
using Unity.Collections;
using Unity.Netcode;

namespace SquishyBump.Net.Session
{
    /// <summary>NGO lobby: state reliable host→clients; client chỉ gửi ready, không gửi fruit hay settings.</summary>
    public sealed class LobbySync : ILobbyChannel, IDisposable
    {
        public const string StateMessage = "SB.Lobby.State", ReadyMessage = "SB.Lobby.Ready";
        private readonly NetworkManager manager;
        private CustomMessagingManager registered;
        private uint lastRevision;
        public LobbySync(NetworkManager owner) => manager = owner;
        public event Action<LobbyState> StateReceived;
        public event Action<ulong, bool> ReadyReceived;
        public int RejectedPackets { get; private set; }
        public void Reset() => lastRevision = 0;
        public void Poll()
        {
            var next = manager != null && manager.IsListening ? manager.CustomMessagingManager : null;
            if (next == registered) return;
            Unregister(); lastRevision = 0;
            if (next == null) return;
            registered = next;
            registered.RegisterNamedMessageHandler(StateMessage, OnState);
            registered.RegisterNamedMessageHandler(ReadyMessage, OnReady);
        }
        public void Broadcast(LobbyState state)
        {
            if (!manager.IsServer || !manager.IsListening) return;
            var peers = manager.ConnectedClientsIds.Where(id => id != NetworkManager.ServerClientId).ToArray();
            if (peers.Length == 0) return;
            var data = LobbyCodec.Write(state);
            using var writer = new FastBufferWriter(data.Length, Allocator.Temp);
            writer.WriteBytesSafe(data);
            manager.CustomMessagingManager.SendNamedMessage(StateMessage, peers, writer, NetworkDelivery.ReliableSequenced);
        }
        public void SendReady(bool ready)
        {
            if (manager.IsServer || !manager.IsConnectedClient) return;
            using var writer = new FastBufferWriter(1, Allocator.Temp); writer.WriteValueSafe((byte)(ready ? 1 : 0));
            manager.CustomMessagingManager.SendNamedMessage(ReadyMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
        }
        private void OnState(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != NetworkManager.ServerClientId) return;
            // NGO đã đọc hash tên message; Length vẫn gồm phần header, Position là đầu payload.
            var length = reader.Length - reader.Position;
            if (length < LobbyCodec.MinBytes || length > LobbyCodec.MaxBytes || !reader.TryBeginRead(length))
            { RejectedPackets++; return; }
            var data = new byte[length]; reader.ReadBytes(ref data, data.Length);
            if (!LobbyCodec.TryRead(data, out var state)) { RejectedPackets++; return; }
            if (state.Revision <= lastRevision) return;
            lastRevision = state.Revision; StateReceived?.Invoke(state);
        }
        private void OnReady(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || sender == NetworkManager.ServerClientId || !manager.ConnectedClients.ContainsKey(sender)) return;
            if (reader.Length - reader.Position != 1 || !reader.TryBeginRead(1)) { RejectedPackets++; return; }
            reader.ReadValue(out byte ready);
            if (ready > 1) { RejectedPackets++; return; }
            ReadyReceived?.Invoke(sender, ready == 1);
        }
        private void Unregister()
        {
            if (registered == null) return;
            registered.UnregisterNamedMessageHandler(StateMessage); registered.UnregisterNamedMessageHandler(ReadyMessage);
            registered = null;
        }
        public void Dispose() => Unregister();
    }
}
