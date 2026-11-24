using System;
using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;

namespace SquishyBump.Match
{
    public enum MatchPhase { Intro, Countdown, Playing, Result }
    public enum SurvivalOutcome { Retry, Advance, SessionWon }

    /// <summary>Bản chụp cấu hình SO ở đầu match; mô phỏng chỉ đọc số tick bất biến.</summary>
    public readonly struct RankRules
    {
        public RankRules(int lives, int respawnTicks, int invulnerabilityTicks, int touchTicks, int points, int l1CloseTick)
        {
            if (lives <= 0 || respawnTicks <= 0 || invulnerabilityTicks < 0 || touchTicks < 0 || points < 0 || l1CloseTick <= 0)
                throw new ArgumentException("Cấu hình Rank không hợp lệ.");
            Lives = lives; RespawnTicks = respawnTicks; InvulnerabilityTicks = invulnerabilityTicks;
            TouchTicks = touchTicks; Points = points; L1CloseTick = l1CloseTick;
        }
        public int Lives { get; }
        public int RespawnTicks { get; }
        public int InvulnerabilityTicks { get; }
        public int TouchTicks { get; }
        public int Points { get; }
        public int L1CloseTick { get; }
        public static RankRules FromProfile(BalanceProfile profile, int hz)
        {
            if (profile == null || hz <= 0 || profile.RingDropSeconds.Count < 3)
                throw new ArgumentException("Cần BalanceProfile và tick rate hợp lệ.");
            return new RankRules(profile.Lives, (int)Math.Round(profile.RespawnDelaySeconds * hz),
                (int)Math.Round(profile.InvulnerabilitySeconds * hz), (int)Math.Round(profile.LastTouchWindowSeconds * hz),
                profile.PointsPerRingOut, (int)Math.Round(profile.RingDropSeconds[2] * hz));
        }
    }

    public readonly struct SurvivalPlacement
    {
        public SurvivalPlacement(ulong fighterId, int rank)
        {
            FighterId = fighterId;
            Rank = rank;
        }

        public ulong FighterId { get; }
        public int Rank { get; }
    }

    /// <summary>Điều phối Survival/Rank. Host gom ring-out rồi chốt một tick theo thứ tự.</summary>
    public sealed class MatchDirector : IDisposable
    {
        private readonly Func<bool> hasHostAuthority;
        private readonly TrainingRingBounds bounds;
        private readonly ulong[] roster;
        private readonly int countdownTicks;
        private readonly int confirmationTicks;
        private readonly int limitTicks;
        private readonly Dictionary<ulong, int> wobble = new();
        private readonly List<ulong[]> eliminations = new();
        private readonly HashSet<ulong> pendingFalls = new();
        private readonly Dictionary<ulong, uint> pendingDisconnects = new();
        private readonly Dictionary<ulong, uint> disconnectedAt = new();
        private uint lastTick;
        private int countdownRemaining;
        private int elapsedTicks;
        private int lastEliminationAt;
        private bool disposed;
        private sealed class RankEntry
        {
            public int Lives, RespawnAt = -1, InvulnerableUntil;
            public long SurvivalTicks;
            public bool Eliminated;
        }
        private readonly Dictionary<ulong, RankEntry> rankEntries = new();
        private readonly RankRules rankRules;
        private readonly int tickRate, respawnCloseTick;
        private readonly uint matchId;
        private readonly IGameEventSink eventSink;

        public MatchDirector(Func<bool> hasHostAuthority, IEnumerable<ulong> roster, int level, uint seed,
            int tickRate, int countdownSeconds, float confirmationSeconds, int limitSeconds,
            TrainingRingBounds bounds = null, MatchRule rule = MatchRule.Survival, BalanceProfile rankProfile = null,
            ScoreService scores = null, uint matchId = 0, IGameEventSink eventSink = null, RankRules? rankRules = null)
        {
            this.hasHostAuthority = hasHostAuthority ?? throw new ArgumentNullException(nameof(hasHostAuthority));
            this.roster = roster.ToArray();
            if (this.roster.Length < 2 || this.roster.Distinct().Count() != this.roster.Length)
                throw new ArgumentException("Cần ít nhất hai fighter với ID riêng.", nameof(roster));
            if (level < 1 || level > 5 || tickRate <= 0 || countdownSeconds <= 0 ||
                (rule != MatchRule.Survival && rule != MatchRule.Rank) ||
                !(confirmationSeconds > 0) || float.IsInfinity(confirmationSeconds) || limitSeconds <= 0)
                throw new ArgumentException("Cấu hình thời gian hoặc level không hợp lệ.");

            Level = level;
            Seed = seed;
            Rule = rule;
            this.tickRate = tickRate;
            this.matchId = matchId;
            this.eventSink = eventSink;
            countdownTicks = checked(countdownSeconds * tickRate);
            confirmationTicks = checked((int)Math.Ceiling(confirmationSeconds * tickRate));
            limitTicks = checked(limitSeconds * tickRate);
            if (rule == MatchRule.Rank)
            {
                this.rankRules = rankRules ?? RankRules.FromProfile(rankProfile, tickRate);
                respawnCloseTick = level == 1 ? this.rankRules.L1CloseTick : limitTicks;
                Scores = scores ?? new ScoreService(hasHostAuthority, this.rankRules.TouchTicks, this.rankRules.Points);
                Scores.BeginMatch(matchId, this.roster);
            }
            this.bounds = bounds;
            if (bounds != null)
                bounds.RingOut += OnArenaRingOut;
            ResetRoster();
        }

        public int Level { get; }
        public MatchRule Rule { get; }
        public ScoreService Scores { get; }
        /// <summary>App đặt vị trí bằng respawn picker; false thì giữ chờ, tuyệt đối không spawn ở điểm nguy hiểm.</summary>
        public Func<ulong, bool> TryPlaceRespawn { private get; set; }
        public bool RespawnsOpen => Rule == MatchRule.Rank && Phase == MatchPhase.Playing && elapsedTicks < respawnCloseTick;
        public int Lives(ulong id) => rankEntries.TryGetValue(id, out var state) ? state.Lives : wobble.ContainsKey(id) ? 1 : 0;
        public bool IsRespawning(ulong id) => rankEntries.TryGetValue(id, out var state) && state.RespawnAt >= 0 && !state.Eliminated;
        public bool IsInvulnerable(ulong id, int ticksAhead = 0) => rankEntries.TryGetValue(id, out var state) &&
            wobble.ContainsKey(id) && elapsedTicks + ticksAhead < state.InvulnerableUntil;
        public int Wobble(ulong id) => wobble.TryGetValue(id, out var value) ? value : 0;
        public long SurvivalTicks(ulong id) => rankEntries.TryGetValue(id, out var state) ? state.SurvivalTicks : 0;
        public uint Seed { get; }
        public MatchPhase Phase { get; private set; }
        public IReadOnlyCollection<ulong> AliveFighters => wobble.Keys;
        public IReadOnlyList<SurvivalPlacement> Placements { get; private set; } = Array.Empty<SurvivalPlacement>();
        public int CountdownTicksRemaining => countdownRemaining;
        public int ElapsedTicks => elapsedTicks;
        public event Action<MatchPhase> PhaseChanged;
        public event Action<IReadOnlyList<SurvivalPlacement>> ResultConfirmed;
        public uint? DisconnectedAt(ulong id) => disconnectedAt.TryGetValue(id, out var tick) ? tick : null;

        private bool CanSimulate => !disposed && hasHostAuthority();

        public void FinishIntro(uint hostTick)
        {
            if (!CanSimulate || Phase != MatchPhase.Intro)
                return;
            CommitNonPlayingDisconnects(hostTick);
            lastTick = hostTick;
            countdownRemaining = countdownTicks;
            SetPhase(MatchPhase.Countdown);
            Publish(MatchEvent.Countdown(NextEventId(), hostTick, countdownRemaining / tickRate));
        }

        public void RecordTouch(ulong victim, ulong other, uint hostTick)
        {
            if (CanSimulate && Rule == MatchRule.Rank && Phase == MatchPhase.Playing && IsNextTick(hostTick) &&
                wobble.ContainsKey(victim) && wobble.ContainsKey(other) && !IsInvulnerable(victim, 1) && !IsInvulnerable(other, 1))
                Scores.RecordTouch(victim, other, hostTick);
        }

        public void SetWobble(ulong fighterId, int value)
        {
            if (CanSimulate && Phase == MatchPhase.Playing && value >= 0 && wobble.ContainsKey(fighterId))
                wobble[fighterId] = value;
        }

        public void ReportRingOut(ulong fighterId, uint hostTick)
        {
            if (CanSimulate && Phase == MatchPhase.Playing && IsNextTick(hostTick) && wobble.ContainsKey(fighterId))
                pendingFalls.Add(fighterId);
        }

        /// <summary>Host áp dụng mất kết nối ở tick mô phỏng nhận sự kiện. Không phải ring-out được tính điểm.</summary>
        public bool ReportDisconnect(ulong fighterId, uint hostTick)
        {
            var active = Rule == MatchRule.Rank
                ? rankEntries.TryGetValue(fighterId, out var entry) && !entry.Eliminated
                : wobble.ContainsKey(fighterId);
            if (!CanSimulate || Phase == MatchPhase.Result || !active ||
                (Phase != MatchPhase.Intro && !IsNextTick(hostTick)) || hostTick == 0 ||
                pendingDisconnects.ContainsKey(fighterId)) return false;
            pendingDisconnects.Add(fighterId, hostTick); return true;
        }

        public void SampleArena(ulong fighterId, uint hostTick, UnityEngine.Vector3 worldCenter)
        {
            if (bounds != null && CanSimulate && Phase == MatchPhase.Playing && IsNextTick(hostTick)
                && wobble.ContainsKey(fighterId))
                bounds.SampleOnHost(fighterId, hostTick, worldCenter);
        }

        // Host gọi sau khi đã báo mọi ring-out của tick này, kể cả tick cuối.
        public void CompleteTick(uint hostTick)
        {
            if (!CanSimulate || Phase == MatchPhase.Result)
                return;
            if (Phase == MatchPhase.Intro) { CommitNonPlayingDisconnects(hostTick); return; }
            if (!IsNextTick(hostTick)) return;
            lastTick = hostTick;

            if (Phase == MatchPhase.Countdown)
            {
                CommitNonPlayingDisconnects(hostTick);
                if (--countdownRemaining % tickRate == 0)
                    Publish(MatchEvent.Countdown(NextEventId(), hostTick, countdownRemaining / tickRate));
                if (countdownRemaining == 0)
                {
                    SetPhase(MatchPhase.Playing);
                    PublishScoreboard();
                }
                return;
            }

            elapsedTicks++;
            if (Rule == MatchRule.Rank)
            {
                foreach (var id in wobble.Keys) rankEntries[id].SurvivalTicks++;
                CompleteRankTick(hostTick);
                return;
            }
            var disconnects = TakeDisconnects(hostTick);
            if (pendingFalls.Count != 0 || disconnects.Length != 0)
            {
                var disconnectSet = disconnects.Length == 0 ? null : disconnects.ToHashSet();
                var group = roster.Where(id => wobble.ContainsKey(id) &&
                    (pendingFalls.Contains(id) || (disconnectSet != null && disconnectSet.Contains(id)))).ToArray();
                foreach (var fighter in group)
                {
                    wobble.Remove(fighter);
                    if (disconnectSet != null && disconnectSet.Contains(fighter)) disconnectedAt[fighter] = hostTick;
                    else Publish(MatchEvent.RingOut(NextEventId(), hostTick, fighter));
                }
                if (group.Length != 0)
                {
                    eliminations.Add(group);
                    foreach (var fighter in group)
                        Publish(MatchEvent.Elimination(NextEventId(), hostTick, fighter, wobble.Count + 1));
                    lastEliminationAt = elapsedTicks;
                    PublishScoreboard();
                }
                pendingFalls.Clear();
            }

            if (wobble.Count <= 1)
            {
                if (elapsedTicks - lastEliminationAt >= confirmationTicks)
                    ConfirmResult();
            }
            else if (elapsedTicks >= limitTicks)
                ConfirmResult();
        }

        public SurvivalOutcome GetSinglePlayerOutcome(ulong playerId)
        {
            if (Rule != MatchRule.Survival || Phase != MatchPhase.Result || !roster.Contains(playerId))
                throw new InvalidOperationException("Cần kết quả đã xác nhận và ID người chơi thuộc roster.");

            if (Placements[0].FighterId != playerId || Placements[1].Rank == 1)
                return SurvivalOutcome.Retry;
            return Level == 5 ? SurvivalOutcome.SessionWon : SurvivalOutcome.Advance;
        }

        public bool RetrySinglePlayer(ulong playerId)
        {
            if (!CanSimulate || Phase != MatchPhase.Result || GetSinglePlayerOutcome(playerId) != SurvivalOutcome.Retry)
                return false;
            ResetRoster();
            SetPhase(MatchPhase.Intro);
            return true;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (bounds != null)
                bounds.RingOut -= OnArenaRingOut;
        }

        private bool IsNextTick(uint tick) => tick == unchecked(lastTick + 1);

        private void OnArenaRingOut(ulong fighterId, uint tick, RingOutReason reason) => ReportRingOut(fighterId, tick);

        private void ResetRoster()
        {
            elapsedTicks = 0;
            countdownRemaining = 0;
            lastEliminationAt = 0;
            wobble.Clear();
            eliminations.Clear();
            pendingFalls.Clear();
            pendingDisconnects.Clear();
            disconnectedAt.Clear();
            rankEntries.Clear();
            Placements = Array.Empty<SurvivalPlacement>();
            foreach (var fighter in roster)
            {
                wobble.Add(fighter, 0);
                if (Rule == MatchRule.Rank) rankEntries.Add(fighter, new RankEntry { Lives = rankRules.Lives });
                if (bounds != null)
                    bounds.ResetFighterOnHost(fighter);
            }
        }

        private void ConfirmResult()
        {
            var result = new List<SurvivalPlacement>();
            foreach (var group in RemainingGroups())
                AppendGroup(result, group);
            for (var i = eliminations.Count - 1; i >= 0; i--)
                AppendGroup(result, eliminations[i]);
            Placements = result.AsReadOnly();
            if (Rule == MatchRule.Rank)
            {
                Scores.CompleteMatch(matchId, rankEntries.ToDictionary(p => p.Key, p => p.Value.Lives),
                    rankEntries.ToDictionary(p => p.Key, p => p.Value.SurvivalTicks));
                foreach (var state in rankEntries.Values) state.RespawnAt = -1;
            }
            SetPhase(MatchPhase.Result);
            var rows = Standings(result);
            Publish(MatchEvent.Scoreboard(NextEventId(), lastTick, rows));
            Publish(MatchEvent.MatchResult(NextEventId(), lastTick, rows));
            ResultConfirmed?.Invoke(Placements);
        }

        private ulong[] TakeDisconnects(uint tick)
        {
            var ids = roster.Where(id => pendingDisconnects.TryGetValue(id, out var at) && at == tick).ToArray();
            foreach (var id in ids) pendingDisconnects.Remove(id);
            return ids;
        }

        private void CommitNonPlayingDisconnects(uint tick)
        {
            var disconnects = TakeDisconnects(tick);
            if (disconnects.Length == 0) return;
            foreach (var id in disconnects)
            {
                wobble.Remove(id);
                disconnectedAt[id] = tick;
                if (Rule == MatchRule.Rank && rankEntries.TryGetValue(id, out var state))
                { state.Lives = 0; state.RespawnAt = -1; state.Eliminated = true; }
            }
            eliminations.Add(disconnects);
            lastEliminationAt = elapsedTicks;
            var rank = (Rule == MatchRule.Rank ? rankEntries.Values.Count(s => !s.Eliminated) : wobble.Count) + 1;
            foreach (var id in disconnects) Publish(MatchEvent.Elimination(NextEventId(), tick, id, rank));
        }

        private MatchStanding[] Standings(IEnumerable<SurvivalPlacement> placements) => placements.Select(p =>
            new MatchStanding(p.FighterId, p.Rank, Scores?.Points(p.FighterId) ?? 0,
                Rule == MatchRule.Rank ? Lives(p.FighterId) : wobble.ContainsKey(p.FighterId) ? 1 : 0,
                Rule == MatchRule.Rank ? checked((int)SurvivalTicks(p.FighterId)) : 0)).ToArray();

        // Quả chờ hồi sinh vẫn còn quyền chơi, không được biến mất khỏi bảng xếp hạng.
        private IEnumerable<IEnumerable<ulong>> RemainingGroups() => Rule == MatchRule.Rank
            ? rankEntries.Where(p => !p.Value.Eliminated).OrderByDescending(p => p.Value.Lives).ThenBy(p => Wobble(p.Key))
                .GroupBy(p => (p.Value.Lives, Wobble(p.Key))).Select(g => g.Select(p => p.Key))
            : wobble.OrderBy(p => p.Value).GroupBy(p => p.Value).Select(g => g.Select(p => p.Key));

        private void PublishScoreboard()
        {
            if (eventSink == null) return;
            var result = new List<SurvivalPlacement>();
            foreach (var group in RemainingGroups()) AppendGroup(result, group);
            for (var i = eliminations.Count - 1; i >= 0; i--) AppendGroup(result, eliminations[i]);
            Publish(MatchEvent.Scoreboard(NextEventId(), lastTick, Standings(result)));
        }

        private void CompleteRankTick(uint hostTick)
        {
            var disconnects = TakeDisconnects(hostTick);
            var disconnectSet = disconnects.Length == 0 ? null : disconnects.ToHashSet();
            if (disconnectSet != null) pendingFalls.ExceptWith(disconnectSet);
            var changed = pendingFalls.Count > 0 || disconnects.Length > 0;
            var eliminated = new List<ulong>(disconnects);
            foreach (var id in disconnects)
            {
                var state = rankEntries[id];
                state.Lives = 0; state.RespawnAt = -1; state.Eliminated = true;
                wobble.Remove(id);
                disconnectedAt[id] = hostTick;
            }
            foreach (var id in roster.Where(pendingFalls.Contains))
            {
                var state = rankEntries[id];
                state.Lives--;
                wobble.Remove(id);
                var credit = Scores.RecordRingOut(id, hostTick);
                Publish(MatchEvent.RingOut(NextEventId(), hostTick, id, credit));
                if (state.Lives > 0 && elapsedTicks < respawnCloseTick)
                    state.RespawnAt = elapsedTicks + rankRules.RespawnTicks;
                else { state.Eliminated = true; eliminated.Add(id); }
            }
            pendingFalls.Clear();
            foreach (var id in roster)
            {
                var state = rankEntries[id];
                if (state.RespawnAt < 0 || state.Eliminated) continue;
                if (elapsedTicks >= respawnCloseTick)
                {
                    state.RespawnAt = -1; state.Eliminated = true; eliminated.Add(id); changed = true;
                }
                else if (elapsedTicks >= state.RespawnAt && (TryPlaceRespawn == null || TryPlaceRespawn(id)))
                {
                    state.RespawnAt = -1;
                    state.InvulnerableUntil = elapsedTicks + rankRules.InvulnerabilityTicks;
                    wobble[id] = 0;
                    Scores.ClearTouch(id);
                    if (bounds != null) bounds.ResetFighterOnHost(id);
                    Publish(MatchEvent.Respawn(NextEventId(), hostTick, id));
                    changed = true;
                }
            }
            if (eliminated.Count > 0)
            {
                var group = roster.Where(eliminated.Contains).ToArray();
                eliminations.Add(group); lastEliminationAt = elapsedTicks;
                var rank = rankEntries.Values.Count(s => !s.Eliminated) + 1;
                foreach (var id in group) Publish(MatchEvent.Elimination(NextEventId(), hostTick, id, rank));
            }
            if (changed) PublishScoreboard();
            var eligible = rankEntries.Values.Count(s => !s.Eliminated);
            if (elapsedTicks >= limitTicks || eligible <= 1 && elapsedTicks - lastEliminationAt >= confirmationTicks)
                ConfirmResult();
        }

        private uint NextEventId() => eventSink?.NextEventId() ?? 0;
        private void Publish(MatchEvent gameEvent) => eventSink?.Publish(gameEvent);
        private static void AppendGroup(List<SurvivalPlacement> results, IEnumerable<ulong> fighters)
        {
            var rank = results.Count + 1;
            foreach (var fighter in fighters)
                results.Add(new SurvivalPlacement(fighter, rank));
        }

        private void SetPhase(MatchPhase next)
        {
            Phase = next;
            PhaseChanged?.Invoke(next);
        }
    }
}
