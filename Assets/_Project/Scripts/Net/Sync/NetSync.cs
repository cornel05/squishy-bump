using System;
using System.Collections.Generic;
using SquishyBump.Core;
using Unity.Collections;
using Unity.Netcode;

namespace SquishyBump.Net.Sync
{
    /// <summary>
    /// The two state streams over NGO named messages (GDD 10.4):
    /// <list type="bullet">
    /// <item>"SB.Input" client → host, <b>unreliable</b>, 60 Hz, last 3 commands per packet. Input only: a client
    /// never sends a position or a result. The sender id comes from the transport, never from the packet.</item>
    /// <item>"SB.Snapshot" host → all clients, <b>unreliable</b>, 30 Hz: tick + per fighter position, velocity,
    /// Wobble, lives, flags, last processed input.</item>
    /// </list>
    /// Hazard and match events are reliable and travel separately (SB-23).
    /// Handlers register once the NetworkManager is listening; call <see cref="Poll"/> every frame.
    /// </summary>
    public sealed class NetSync : IDisposable
    {
        public const string InputMessage = "SB.Input";
        public const string SnapshotMessage = "SB.Snapshot";

        // Largest legal payload is a full snapshot (5 + 8 × 34 bytes); anything bigger is rejected unread.
        private const int MaxPayloadBytes = 512;

        private readonly NetworkManager manager;
        private readonly List<InputCommand> parsed = new();
        private CustomMessagingManager registeredOn;

        public NetSync(NetworkManager manager) => this.manager = manager;

        /// <summary>Host: validated commands of one remote client.</summary>
        public event Action<ulong, IReadOnlyList<InputCommand>> InputReceived;
        /// <summary>Client: a well-formed snapshot from the host.</summary>
        public event Action<WorldSnapshot> SnapshotReceived;
        public int RejectedPackets { get; private set; }

        public void Poll()
        {
            var messaging = manager != null && manager.IsListening ? manager.CustomMessagingManager : null;
            if (messaging == registeredOn) return;
            Unregister();
            if (messaging == null) return;
            messaging.RegisterNamedMessageHandler(InputMessage, OnInput);
            messaging.RegisterNamedMessageHandler(SnapshotMessage, OnSnapshot);
            registeredOn = messaging;
        }

        public void SendInput(IReadOnlyList<InputCommand> lastCommands)
        {
            if (!manager.IsConnectedClient || manager.IsServer) return;
            Send(SyncCodec.WriteInput(lastCommands), w =>
                manager.CustomMessagingManager.SendNamedMessage(InputMessage, NetworkManager.ServerClientId, w,
                    NetworkDelivery.Unreliable));
        }

        public void BroadcastSnapshot(WorldSnapshot snapshot)
        {
            if (!manager.IsServer) return;
            var remote = new List<ulong>();
            foreach (var id in manager.ConnectedClientsIds)
                if (id != NetworkManager.ServerClientId) remote.Add(id);
            if (remote.Count == 0) return;
            Send(SyncCodec.WriteSnapshot(snapshot), w =>
                manager.CustomMessagingManager.SendNamedMessage(SnapshotMessage, remote, w, NetworkDelivery.Unreliable));
        }

        private static void Send(byte[] payload, Action<FastBufferWriter> send)
        {
            using var writer = new FastBufferWriter(payload.Length + 4, Allocator.Temp);
            writer.WriteValueSafe(payload.Length);
            writer.WriteBytesSafe(payload);
            send(writer);
        }

        private static byte[] Receive(FastBufferReader reader)
        {
            if (!reader.TryBeginRead(sizeof(int))) return null;
            reader.ReadValue(out int length);
            if (length < 0 || length > MaxPayloadBytes || !reader.TryBeginRead(length)) return null;
            var data = new byte[length];
            reader.ReadBytes(ref data, length);
            return data;
        }

        private void OnInput(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || sender == NetworkManager.ServerClientId) return;
            if (!SyncCodec.TryReadInput(Receive(reader), parsed)) { RejectedPackets++; return; }
            InputReceived?.Invoke(sender, parsed);
        }

        private void OnSnapshot(ulong sender, FastBufferReader reader)
        {
            // Only the host may describe the world.
            if (manager.IsServer || sender != NetworkManager.ServerClientId) return;
            if (SyncCodec.TryReadSnapshot(Receive(reader), out var snapshot)) SnapshotReceived?.Invoke(snapshot);
            else RejectedPackets++;
        }

        private void Unregister()
        {
            if (registeredOn == null) return;
            registeredOn.UnregisterNamedMessageHandler(InputMessage);
            registeredOn.UnregisterNamedMessageHandler(SnapshotMessage);
            registeredOn = null;
        }

        public void Dispose() => Unregister();
    }
}
