using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SquishyBump.Core;

namespace SquishyBump.Net.Session
{
    /// <summary>
    /// LAN room discovery: a public host broadcasts a tiny UDP beacon about once a second, Find Server listens.
    /// Only public rooms announce; the beacon has no password, only a flag. Unreliable and unauthenticated by design:
    /// it is a hint, the real join still goes through connection approval (version, password, capacity).
    /// </summary>
    public sealed class LanDiscovery : IRoomBrowser, IDisposable
    {
        public const ushort Port = 7778;
        public const int MaxNameBytes = 24;
        /// <summary>A room not heard for this long drops off the list.</summary>
        public const long ExpireMilliseconds = 3500;
        private static readonly byte[] Magic = { (byte)'S', (byte)'B', (byte)'R', (byte)'1' };
        private const int Header = 9; // magic 4, players, capacity, rule, password flag, name length

        private readonly object gate = new();
        private readonly Dictionary<string, (RoomInfo Info, long Seen)> rooms = new();
        private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        private UdpClient listener, sender;

        public IReadOnlyList<RoomInfo> Rooms { get { lock (gate) return Snapshot(clock.ElapsedMilliseconds); } }

        public static byte[] Encode(string name, int players, int capacity, MatchRule rule, bool hasPassword)
        {
            var text = Encoding.UTF8.GetBytes(name ?? "");
            if (text.Length > MaxNameBytes) text = text.Take(MaxNameBytes).ToArray();
            var data = new byte[Header + text.Length];
            Magic.CopyTo(data, 0);
            data[4] = (byte)players; data[5] = (byte)capacity; data[6] = (byte)rule; data[7] = (byte)(hasPassword ? 1 : 0);
            data[8] = (byte)text.Length; text.CopyTo(data, Header);
            return data;
        }

        /// <summary>Strict parser: any malformed packet is dropped. The address is the socket's, not the packet's.</summary>
        public static bool TryDecode(byte[] data, string address, out RoomInfo info)
        {
            info = default;
            if (data == null || data.Length < Header || data.Length > Header + MaxNameBytes || string.IsNullOrEmpty(address)) return false;
            for (var i = 0; i < Magic.Length; i++) if (data[i] != Magic[i]) return false;
            int players = data[4], capacity = data[5], rule = data[6], flag = data[7], length = data[8];
            if (capacity < LobbyState.MinCapacity || capacity > LobbyState.MaxCapacity || players < 1 || players > capacity ||
                (rule != (int)MatchRule.Survival && rule != (int)MatchRule.Rank) || flag > 1 ||
                length > MaxNameBytes || data.Length != Header + length) return false;
            var name = new string(Encoding.UTF8.GetString(data, Header, length).Where(c => !char.IsControl(c)).ToArray()).Trim();
            info = new RoomInfo(name.Length == 0 ? address : name, address, players, capacity, (MatchRule)rule, flag == 1);
            return true;
        }

        /// <summary>Records a beacon heard at <paramref name="now"/> ms (clock injected for tests).</summary>
        public void Hear(RoomInfo info, long now)
        { lock (gate) rooms[info.Address] = (info, now); }

        public IReadOnlyList<RoomInfo> Snapshot(long now)
        {
            foreach (var key in rooms.Where(r => now - r.Value.Seen > ExpireMilliseconds).Select(r => r.Key).ToArray()) rooms.Remove(key);
            return rooms.Values.Select(r => r.Info).OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Address).ToArray();
        }

        /// <summary>Host side: call about once a second while the room is public and open.</summary>
        public void Announce(string name, int players, int capacity, MatchRule rule, bool hasPassword)
        {
            try
            {
                sender ??= new UdpClient { EnableBroadcast = true };
                var data = Encode(name, players, capacity, rule, hasPassword);
                sender.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, Port));
            }
            catch (SocketException) { /* no network or firewall: the manual IP join still works */ }
            catch (ObjectDisposedException) { }
        }

        public void Listen(bool on)
        {
            if (!on) { CloseListener(); return; }
            if (listener != null) return;
            try
            {
                listener = new UdpClient(AddressFamily.InterNetwork);
                listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
                Receive(listener);
            }
            catch (SocketException) { CloseListener(); }
        }

        private void Receive(UdpClient client)
        {
            try { client.BeginReceive(ar => OnReceive(client, ar), null); }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
        }

        private void OnReceive(UdpClient client, IAsyncResult result)
        {
            try
            {
                var from = new IPEndPoint(IPAddress.Any, 0);
                var data = client.EndReceive(result, ref from);
                if (from.AddressFamily == AddressFamily.InterNetwork && TryDecode(data, from.Address.ToString(), out var info))
                    Hear(info, clock.ElapsedMilliseconds);
            }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { }
            if (ReferenceEquals(client, listener)) Receive(client);
        }

        private void CloseListener()
        {
            var old = listener; listener = null;
            try { old?.Close(); } catch (SocketException) { }
            lock (gate) rooms.Clear();
        }

        public void Dispose() { CloseListener(); sender?.Close(); sender = null; }
    }
}
