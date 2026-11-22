using System;
using System.Collections.Generic;
using System.Linq;

namespace SquishyBump.Core
{
    public interface ILobbyChannel
    {
        event Action<LobbyState> StateReceived;
        event Action<ulong, bool> ReadyReceived;
        void Broadcast(LobbyState state);
        void SendReady(bool ready);
    }
    public readonly struct LobbyPlayer
    {
        public LobbyPlayer(ulong id, int fruit, bool ready) { Id = id; Fruit = fruit; Ready = ready; }
        public ulong Id { get; }
        /// <summary>-1 là chưa chọn; 0–5 theo MatchSetup.Roster.</summary>
        public int Fruit { get; }
        public bool Ready { get; }
    }

    /// <summary>Ảnh chụp lobby bất biến do host quyết định, chưa phải trạng thái gameplay.</summary>
    public sealed class LobbyState
    {
        public LobbyState(uint revision, MatchRule rule, int finalLevel, int remainingTicks, bool started,
            IEnumerable<LobbyPlayer> players)
        {
            Revision = revision; Rule = rule; FinalLevel = finalLevel; RemainingTicks = remainingTicks; Started = started;
            Players = Array.AsReadOnly(players.ToArray());
        }
        public uint Revision { get; }
        public MatchRule Rule { get; }
        public int FinalLevel { get; }
        public int RemainingTicks { get; }
        public bool Started { get; }
        public IReadOnlyList<LobbyPlayer> Players { get; }
    }
}
