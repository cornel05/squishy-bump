using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SquishyBump.Core;

namespace SquishyBump.Net.Session
{
    /// <summary>Host-owned state for the SB-25 LAN lobby. Advance once per host tick.</summary>
    public sealed class LobbyRoom
    {
        private readonly ulong hostId;
        private readonly int lastLevel;
        private readonly int countdownTicks;
        private readonly Dictionary<ulong, (int? Fruit, bool Ready)> players = new();

        public IReadOnlyDictionary<ulong, (int? Fruit, bool Ready)> Players { get; }
        public MatchRule Rule { get; private set; }
        public int FinalLevel { get; private set; } = 1;
        public int RemainingTicks { get; private set; }
        public bool Started { get; private set; }

        public LobbyRoom(ulong hostId, int lastLevel, int countdownTicks)
        {
            if (lastLevel < 1) throw new ArgumentOutOfRangeException(nameof(lastLevel));
            if (countdownTicks < 1) throw new ArgumentOutOfRangeException(nameof(countdownTicks));

            this.hostId = hostId;
            this.lastLevel = lastLevel;
            this.countdownTicks = countdownTicks;
            players.Add(hostId, (null, false));
            Players = new ReadOnlyDictionary<ulong, (int? Fruit, bool Ready)>(players);
        }

        public bool TryAdd(ulong sender)
        {
            if (Started || players.Count >= StubSessionService.MaxPlayers || !players.TryAdd(sender, (null, false)))
                return false;
            RemainingTicks = 0;
            return true;
        }

        public bool TryRemove(ulong sender)
        {
            if (Started || sender == hostId || !players.Remove(sender)) return false;
            RemainingTicks = 0;
            return true;
        }

        public bool TryClaim(ulong sender, int fruit)
        {
            if (Started || fruit < 0 || fruit >= StubSessionService.MaxPlayers ||
                !players.TryGetValue(sender, out var player)) return false;
            if (player.Fruit == fruit) return true;
            if (players.Values.Any(other => other.Fruit == fruit)) return false;

            players[sender] = (fruit, false);
            RemainingTicks = 0;
            return true;
        }

        public bool TrySetReady(ulong sender, bool ready)
        {
            if (Started || !players.TryGetValue(sender, out var player) || (ready && !player.Fruit.HasValue))
                return false;
            if (player.Ready == ready) return true;

            players[sender] = (player.Fruit, ready);
            if (!ready) RemainingTicks = 0;
            return true;
        }

        public bool TryConfigure(ulong sender, MatchRule rule, int finalLevel)
        {
            if (Started || sender != hostId || finalLevel < 1 || finalLevel > lastLevel ||
                (rule != MatchRule.Survival && rule != MatchRule.Rank)) return false;
            if (Rule == rule && FinalLevel == finalLevel) return true;

            Rule = rule;
            FinalLevel = finalLevel;
            RemainingTicks = 0;
            return true;
        }

        public bool Advance()
        {
            if (Started) return false;
            if (RemainingTicks == 0)
            {
                if (players.Count < 2 || players.Values.Any(player => !player.Ready)) return false;
                RemainingTicks = countdownTicks;
                return false;
            }

            if (--RemainingTicks != 0) return false;
            Started = true;
            return true;
        }
    }
}
