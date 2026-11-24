using System;
using System.Collections.Generic;
using System.IO;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Net.Sync
{
    [Flags]
    public enum FighterFlags : byte
    {
        None = 0,
        Dashing = 1,
        Stunned = 2,
        Out = 4,
        Invulnerable = 8,
        Respawning = 16,
    }

    /// <summary>One fighter in a world snapshot (GDD 10.4).</summary>
    public readonly struct FighterSnapshot
    {
        public FighterSnapshot(ushort id, byte fruit, Vector3 position, Vector3 velocity, byte wobble, byte lives,
            FighterFlags flags, uint lastInput, ushort dashCooldownTicks = 0)
        {
            Id = id;
            Fruit = fruit;
            Position = position;
            Velocity = velocity;
            Wobble = wobble;
            Lives = lives;
            Flags = flags;
            LastInput = lastInput;
            DashCooldownTicks = dashCooldownTicks;
        }

        /// <summary>Fighter id = owner's NGO client id (host 0) or a bot slot id.</summary>
        public ushort Id { get; }
        /// <summary>Index into the level roster prefabs, so clients spawn the right fruit.</summary>
        public byte Fruit { get; }
        public Vector3 Position { get; }
        public Vector3 Velocity { get; }
        public byte Wobble { get; }
        public byte Lives { get; }
        public FighterFlags Flags { get; }
        /// <summary>Last InputCommand.Sequence the host applied for this fighter.</summary>
        public uint LastInput { get; }
        /// <summary>Host-confirmed cooldown at the snapshot tick; unreliable, 30 Hz.</summary>
        public ushort DashCooldownTicks { get; }
    }

    public sealed class WorldSnapshot
    {
        public WorldSnapshot(uint hostTick, uint goTick, IReadOnlyList<FighterSnapshot> fighters)
        {
            HostTick = hostTick;
            GoTick = goTick;
            Fighters = fighters;
        }

        public uint HostTick { get; }
        /// <summary>Host tick of GO, 0 before the match plays: clients pose time-driven hazards from it.</summary>
        public uint GoTick { get; }
        public IReadOnlyList<FighterSnapshot> Fighters { get; }
    }

    /// <summary>
    /// Wire formats of the two unreliable streams (GDD 10.4). Readers reject anything malformed instead of
    /// throwing past the message handler: the host never trusts a client packet's length or values.
    /// </summary>
    public static class SyncCodec
    {
        /// <summary>Each input packet repeats the last three commands.</summary>
        public const int CommandsPerPacket = 3;
        public const int MaxFighters = 8;
        const int CommandBytes = 4 + 4 + 4 + 1 + 4;
        const int SnapshotHeader = 4 + 4 + 1;
        const int FighterBytes = 2 + 1 + 12 + 12 + 1 + 1 + 1 + 4 + 2;

        public static byte[] WriteInput(IReadOnlyList<InputCommand> commands)
        {
            var count = Math.Min(commands.Count, CommandsPerPacket);
            using var stream = new MemoryStream(1 + count * CommandBytes);
            using var w = new BinaryWriter(stream);
            w.Write((byte)count);
            for (var i = commands.Count - count; i < commands.Count; i++)
            {
                var c = commands[i];
                w.Write(c.Sequence);
                w.Write(c.Move.x);
                w.Write(c.Move.y);
                w.Write((byte)((c.Jump ? 1 : 0) | (c.Dash ? 2 : 0)));
                w.Write(c.YawDegrees);
            }
            return stream.ToArray();
        }

        /// <summary>False for a wrong length, too many commands or unknown button bits.</summary>
        public static bool TryReadInput(byte[] data, List<InputCommand> into)
        {
            into.Clear();
            if (data == null || data.Length < 1) return false;
            var count = data[0];
            if (count < 1 || count > CommandsPerPacket || data.Length != 1 + count * CommandBytes) return false;
            using var r = new BinaryReader(new MemoryStream(data, 1, data.Length - 1));
            for (var i = 0; i < count; i++)
            {
                var sequence = r.ReadUInt32();
                var move = new Vector2(r.ReadSingle(), r.ReadSingle());
                var buttons = r.ReadByte();
                var yaw = r.ReadSingle();
                if ((buttons & ~3) != 0) { into.Clear(); return false; }
                // The constructor clamps move to length 1 and yaw to [0, 360).
                into.Add(new InputCommand(sequence, move, (buttons & 1) != 0, (buttons & 2) != 0, yaw));
            }
            return true;
        }

        public static byte[] WriteSnapshot(WorldSnapshot snapshot)
        {
            var count = Math.Min(snapshot.Fighters.Count, MaxFighters);
            using var stream = new MemoryStream(SnapshotHeader + count * FighterBytes);
            using var w = new BinaryWriter(stream);
            w.Write(snapshot.HostTick);
            w.Write(snapshot.GoTick);
            w.Write((byte)count);
            for (var i = 0; i < count; i++)
            {
                var f = snapshot.Fighters[i];
                w.Write(f.Id);
                w.Write(f.Fruit);
                Write(w, f.Position);
                Write(w, f.Velocity);
                w.Write(f.Wobble);
                w.Write(f.Lives);
                w.Write((byte)f.Flags);
                w.Write(f.LastInput);
                w.Write(f.DashCooldownTicks);
            }
            return stream.ToArray();
        }

        public static bool TryReadSnapshot(byte[] data, out WorldSnapshot snapshot)
        {
            snapshot = null;
            if (data == null || data.Length < SnapshotHeader) return false;
            var count = data[SnapshotHeader - 1];
            if (count > MaxFighters || data.Length != SnapshotHeader + count * FighterBytes) return false;
            using var r = new BinaryReader(new MemoryStream(data));
            var tick = r.ReadUInt32();
            var go = r.ReadUInt32();
            r.ReadByte();
            var fighters = new FighterSnapshot[count];
            for (var i = 0; i < count; i++)
                fighters[i] = new FighterSnapshot(r.ReadUInt16(), r.ReadByte(), Read(r), Read(r), r.ReadByte(),
                    r.ReadByte(), (FighterFlags)r.ReadByte(), r.ReadUInt32(), r.ReadUInt16());
            snapshot = new WorldSnapshot(tick, go, fighters);
            return true;
        }

        const int HazardBytes = 4 + 4 + 1 + 4 + 12 + 1;

        // Header: id/tick/kind/fighter/credit/value/count. Tối đa 6 dòng, mỗi dòng 24 byte.
        private const int MatchHeaderBytes = 30;
        private const int StandingBytes = 24;
        private const int MatchFighterLimit = 6;

        public static byte[] WriteMatch(MatchEvent e)
        {
            if (!ValidMatch(e)) throw new ArgumentException("Payload match không hợp lệ.", nameof(e));
            using var stream = new MemoryStream(MatchHeaderBytes + e.Standings.Count * StandingBytes);
            using var w = new BinaryWriter(stream);
            w.Write(e.EventId); w.Write(e.HostTick); w.Write((byte)e.Kind);
            w.Write(e.FighterId); w.Write(e.CreditedFighterId); w.Write(e.Value); w.Write((byte)e.Standings.Count);
            foreach (var s in e.Standings)
            { w.Write(s.FighterId); w.Write(s.Rank); w.Write(s.Points); w.Write(s.Lives); w.Write(s.SurvivalTicks); }
            return stream.ToArray();
        }

        public static bool TryReadMatch(byte[] data, out MatchEvent gameEvent)
        {
            gameEvent = default;
            if (data == null || data.Length < MatchHeaderBytes) return false;
            var count = data[MatchHeaderBytes - 1];
            if (count > MatchFighterLimit || data.Length != MatchHeaderBytes + count * StandingBytes) return false;
            using var r = new BinaryReader(new MemoryStream(data));
            var id = r.ReadUInt32(); var tick = r.ReadUInt32(); var kind = (MatchEventKind)r.ReadByte();
            var fighter = r.ReadUInt64(); var credit = r.ReadUInt64(); var value = r.ReadInt32(); r.ReadByte();
            var rows = new MatchStanding[count];
            for (var i = 0; i < count; i++) rows[i] = new MatchStanding(r.ReadUInt64(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
            switch (kind)
            {
                case MatchEventKind.Countdown: gameEvent = MatchEvent.Countdown(id, tick, value); break;
                case MatchEventKind.RingOut: gameEvent = MatchEvent.RingOut(id, tick, fighter, credit); break;
                case MatchEventKind.Respawn: gameEvent = MatchEvent.Respawn(id, tick, fighter); break;
                case MatchEventKind.Elimination: gameEvent = MatchEvent.Elimination(id, tick, fighter, value); break;
                case MatchEventKind.MatchResult: gameEvent = MatchEvent.MatchResult(id, tick, rows); break;
                case MatchEventKind.Scoreboard: gameEvent = MatchEvent.Scoreboard(id, tick, rows); break;
                case MatchEventKind.SessionResult: gameEvent = MatchEvent.SessionResult(id, tick, rows); break;
                default: return false;
            }
            // Không cho packet có trường không dùng giả dạng payload hợp lệ sau khi factory bỏ chúng.
            if (gameEvent.FighterId != fighter || gameEvent.CreditedFighterId != credit ||
                gameEvent.Value != value || gameEvent.Standings.Count != count || !ValidMatch(gameEvent))
            { gameEvent = default; return false; }
            return true;
        }

        private static bool ValidMatch(MatchEvent e)
        {
            if (e.EventId == 0 || !Enum.IsDefined(typeof(MatchEventKind), e.Kind)) return false;
            var table = e.Kind == MatchEventKind.Scoreboard || e.Kind == MatchEventKind.MatchResult ||
                e.Kind == MatchEventKind.SessionResult;
            if (table)
            {
                if (e.Standings.Count < 2 || e.Standings.Count > MatchFighterLimit) return false;
                var ids = new HashSet<ulong>();
                foreach (var s in e.Standings)
                    if (s.FighterId == MatchEvent.NoFighter || !ids.Add(s.FighterId) || s.Rank < 1 ||
                        s.Rank > e.Standings.Count || s.Points < 0 || s.Lives < 0 || s.Lives > 3 || s.SurvivalTicks < 0) return false;
                return true;
            }
            if (e.Standings.Count != 0) return false;
            if (e.Kind == MatchEventKind.Countdown) return e.Value >= 0;
            if (e.FighterId == MatchEvent.NoFighter) return false;
            if (e.Kind == MatchEventKind.RingOut) return e.FighterId != e.CreditedFighterId;
            return e.Kind != MatchEventKind.Elimination || (e.Value >= 1 && e.Value <= MatchFighterLimit);
        }

        public static byte[] WriteHazard(HazardEvent e)
        {
            using var stream = new MemoryStream(HazardBytes);
            using var w = new BinaryWriter(stream);
            w.Write(e.EventId);
            w.Write(e.HostTick);
            w.Write((byte)e.Kind);
            w.Write(e.Index);
            Write(w, e.Aim);
            w.Write((sbyte)Math.Sign(e.Direction));
            return stream.ToArray();
        }

        public static bool TryReadHazard(byte[] data, out HazardEvent gameEvent)
        {
            gameEvent = default;
            if (data == null || data.Length != HazardBytes) return false;
            using var r = new BinaryReader(new MemoryStream(data));
            var id = r.ReadUInt32();
            var tick = r.ReadUInt32();
            var kind = (HazardEventKind)r.ReadByte();
            if (!Enum.IsDefined(typeof(HazardEventKind), kind)) return false;
            gameEvent = new HazardEvent(id, tick, kind, r.ReadInt32(), Read(r), r.ReadSByte());
            return true;
        }

        static void Write(BinaryWriter w, Vector3 v)
        {
            w.Write(v.x);
            w.Write(v.y);
            w.Write(v.z);
        }

        static Vector3 Read(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    }
}
