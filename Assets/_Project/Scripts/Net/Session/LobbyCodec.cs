using System;
using System.Collections.Generic;
using System.IO;
using SquishyBump.Core;

namespace SquishyBump.Net.Session
{
    /// <summary>Lobby reliable state: version + revision + settings + ready/fruit par peer, tối đa 6 người.</summary>
    public static class LobbyCodec
    {
        public const int MaxBytes = Header + StubSessionService.MaxPlayers * Row;
        private const byte Version = 2;
        private const int Header = 15, Row = 10;
        public const int MinBytes = Header;
        public static byte[] Write(LobbyState state)
        {
            if (!Valid(state)) throw new ArgumentException("Lobby state không hợp lệ.", nameof(state));
            using var stream = new MemoryStream(); using var w = new BinaryWriter(stream);
            w.Write(Version); w.Write(state.Revision); w.Write((byte)state.Rule); w.Write((byte)state.FinalLevel);
            w.Write(state.RemainingTicks); w.Write(state.Started); w.Write((byte)state.Players.Count);
            w.Write((byte)state.Capacity); w.Write(state.Public);
            foreach (var p in state.Players) { w.Write(p.Id); w.Write((sbyte)p.Fruit); w.Write(p.Ready); }
            return stream.ToArray();
        }
        public static bool TryRead(byte[] data, out LobbyState state)
        {
            state = null;
            if (data == null || data.Length < Header || data.Length > MaxBytes || data[0] != Version ||
                data[11] > 1 || data[12] < 1 || data[12] > StubSessionService.MaxPlayers ||
                data[13] < LobbyState.MinCapacity || data[13] > StubSessionService.MaxPlayers || data[13] < data[12] || data[14] > 1 ||
                data.Length != Header + data[12] * Row) return false;
            using var r = new BinaryReader(new MemoryStream(data)); r.ReadByte();
            var revision = r.ReadUInt32(); var rule = (MatchRule)r.ReadByte(); var last = r.ReadByte();
            var ticks = r.ReadInt32(); var started = r.ReadBoolean(); var count = r.ReadByte();
            var capacity = r.ReadByte(); var isPublic = r.ReadBoolean();
            var rows = new LobbyPlayer[count];
            for (var i = 0; i < count; i++)
            {
                var id = r.ReadUInt64(); var fruit = r.ReadSByte(); var readyByte = r.ReadByte();
                if (readyByte > 1) return false;
                rows[i] = new LobbyPlayer(id, fruit, readyByte == 1);
            }
            var parsed = new LobbyState(revision, rule, last, ticks, started, rows, capacity, isPublic);
            if (!Valid(parsed)) return false;
            state = parsed; return true;
        }
        private static bool Valid(LobbyState s)
        {
            if (s == null || s.Revision == 0 || (s.Rule != MatchRule.Survival && s.Rule != MatchRule.Rank) ||
                s.FinalLevel < 1 || s.FinalLevel > 5 || s.RemainingTicks < 0 ||
                s.Players.Count < 1 || s.Players.Count > s.Capacity || s.Capacity < LobbyState.MinCapacity ||
                s.Capacity > StubSessionService.MaxPlayers) return false;
            var ids = new HashSet<ulong>(); var fruits = new HashSet<int>(); var allReady = true;
            foreach (var p in s.Players)
            {
                if (!ids.Add(p.Id) || p.Id == MatchEvent.NoFighter || p.Fruit < -1 || p.Fruit > 5 ||
                    (p.Fruit >= 0 && !fruits.Add(p.Fruit)) || (p.Ready && p.Fruit < 0)) return false;
                allReady &= p.Ready;
            }
            // Protocol LAN không cho client nhìn thấy countdown/start khi có người chưa ready/chưa chọn.
            return (s.RemainingTicks == 0 && !s.Started) || (s.Players.Count >= 2 && allReady);
        }
    }
}
