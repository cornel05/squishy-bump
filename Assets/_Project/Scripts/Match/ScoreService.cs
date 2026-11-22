using System;
using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;

namespace SquishyBump.Match
{
    public readonly struct SessionScore
    {
        public SessionScore(ulong fighterId, int rank, int points, int lives, long survivalTicks)
        { FighterId = fighterId; Rank = rank; Points = points; Lives = lives; SurvivalTicks = survivalTicks; }
        public ulong FighterId { get; }
        public int Rank { get; }
        public int Points { get; }
        public int Lives { get; }
        public long SurvivalTicks { get; }
    }

    /// <summary>Điểm Rank cộng dồn của một session, chỉ host ghi; chạm cuối theo tick (GDD 3.6).</summary>
    public sealed class ScoreService
    {
        private sealed class Total { public int Points, Lives; public long SurvivalTicks; }
        private readonly Func<bool> hasAuthority;
        private readonly int touchWindowTicks, pointsPerFall;
        private readonly Dictionary<ulong, Total> totals = new();
        private readonly Dictionary<ulong, (ulong other, uint tick)> touches = new();
        private readonly HashSet<(uint tick, ulong victim)> scoredFalls = new();
        private readonly HashSet<ulong> activeRoster = new();
        private readonly HashSet<uint> completedMatches = new();
        private uint currentMatch;
        private bool open;

        public ScoreService(Func<bool> hasAuthority, int touchWindowTicks, int pointsPerFall)
        {
            this.hasAuthority = hasAuthority ?? throw new ArgumentNullException(nameof(hasAuthority));
            if (touchWindowTicks < 0 || pointsPerFall < 0) throw new ArgumentOutOfRangeException(nameof(touchWindowTicks));
            this.touchWindowTicks = touchWindowTicks;
            this.pointsPerFall = pointsPerFall;
        }

        public void BeginMatch(uint matchId, IEnumerable<ulong> roster)
        {
            if (!hasAuthority()) return;
            if (open || completedMatches.Contains(matchId)) throw new InvalidOperationException("Match ID phải mới và trận trước đã chốt.");
            currentMatch = matchId;
            activeRoster.Clear();
            foreach (var id in roster)
            {
                if (!activeRoster.Add(id)) throw new ArgumentException("Roster trùng ID.", nameof(roster));
                if (!totals.ContainsKey(id)) totals.Add(id, new Total());
            }
            touches.Clear(); scoredFalls.Clear(); open = true;
        }

        public void RecordTouch(ulong victim, ulong other, uint tick)
        {
            if (hasAuthority() && open && victim != other && activeRoster.Contains(victim) && activeRoster.Contains(other))
                touches[victim] = (other, tick);
        }

        public void ClearTouch(ulong fighterId)
        {
            if (hasAuthority()) touches.Remove(fighterId);
        }

        public ulong RecordRingOut(ulong victim, uint tick)
        {
            if (!hasAuthority() || !open || !activeRoster.Contains(victim) || !scoredFalls.Add((tick, victim)))
                return MatchEvent.NoFighter;
            var credit = MatchEvent.NoFighter;
            if (touches.TryGetValue(victim, out var last) && unchecked(tick - last.tick) <= touchWindowTicks)
            {
                totals[last.other].Points += pointsPerFall;
                credit = last.other;
            }
            touches.Remove(victim);
            return credit;
        }

        public int Points(ulong fighterId) => totals.TryGetValue(fighterId, out var total) ? total.Points : 0;

        /// <summary>Gọi đúng một lần khi host xác nhận kết quả; không cộng lại khi phát event/result trùng.</summary>
        public void CompleteMatch(uint matchId, IReadOnlyDictionary<ulong, int> lives, IReadOnlyDictionary<ulong, long> survivalTicks)
        {
            if (!hasAuthority() || !open || matchId != currentMatch || completedMatches.Contains(matchId)) return;
            // Kiểm tra đủ dữ liệu trước khi thay tổng, tránh chốt một nửa roster.
            foreach (var id in activeRoster)
                if (!lives.ContainsKey(id) || !survivalTicks.ContainsKey(id) || lives[id] < 0 || survivalTicks[id] < 0)
                    throw new ArgumentException("Thiếu hoặc sai dữ liệu tổng kết Rank.");
            foreach (var id in activeRoster)
            {
                totals[id].Lives += lives[id];
                totals[id].SurvivalTicks += survivalTicks[id];
            }
            completedMatches.Add(matchId); open = false; touches.Clear();
        }

        public IReadOnlyList<SessionScore> Standings()
        {
            var result = new List<SessionScore>();
            (int points, int lives, long ticks)? previous = null;
            var rank = 0;
            foreach (var pair in totals.OrderByDescending(p => p.Value.Points).ThenByDescending(p => p.Value.Lives)
                .ThenByDescending(p => p.Value.SurvivalTicks).ThenBy(p => p.Key))
            {
                var value = pair.Value;
                var key = (value.Points, value.Lives, value.SurvivalTicks);
                if (previous == null || previous.Value != key) rank = result.Count + 1;
                result.Add(new SessionScore(pair.Key, rank, value.Points, value.Lives, value.SurvivalTicks));
                previous = key;
            }
            return result.AsReadOnly();
        }
    }
}
