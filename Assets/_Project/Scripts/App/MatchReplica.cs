using System;
using System.Collections.Generic;
using SquishyBump.Core;

namespace SquishyBump.App
{
    /// <summary>Bản đọc cho HUD/kết quả trên mỗi peer. Không tính luật hoặc thứ hạng phía client.</summary>
    public sealed class MatchReplica
    {
        private readonly HashSet<ulong> outFighters = new();
        private readonly Dictionary<ulong, int> eliminated = new();
        public int CountdownSeconds { get; private set; } = -1;
        public bool HasResult { get; private set; }
        public IReadOnlyCollection<ulong> OutFighters => outFighters;
        public IReadOnlyDictionary<ulong, int> Eliminated => eliminated;
        public IReadOnlyList<MatchStanding> Result { get; private set; } = Array.Empty<MatchStanding>();
        public IReadOnlyList<MatchStanding> Scoreboard { get; private set; } = Array.Empty<MatchStanding>();
        public IReadOnlyList<MatchStanding> SessionResult { get; private set; } = Array.Empty<MatchStanding>();
        public event Action<MatchEvent> Changed;
        public void Apply(MatchEvent e)
        {
            switch (e.Kind)
            {
                case MatchEventKind.Countdown:
                    if (CountdownSeconds < 0 || HasResult) Reset();
                    CountdownSeconds = e.Value; break;
                case MatchEventKind.RingOut: outFighters.Add(e.FighterId); break;
                case MatchEventKind.Respawn: outFighters.Remove(e.FighterId); break;
                case MatchEventKind.Elimination: eliminated[e.FighterId] = e.Value; break;
                case MatchEventKind.MatchResult: Result = e.Standings; HasResult = true; break;
                case MatchEventKind.Scoreboard: Scoreboard = e.Standings; break;
                case MatchEventKind.SessionResult: SessionResult = e.Standings; break;
            }
            Changed?.Invoke(e);
        }
        public void Reset()
        {
            CountdownSeconds = -1; HasResult = false; outFighters.Clear(); eliminated.Clear();
            Result = Scoreboard = SessionResult = Array.Empty<MatchStanding>();
        }
    }
}
