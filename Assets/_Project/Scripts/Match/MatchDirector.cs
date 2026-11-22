using System;
using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;

namespace SquishyBump.Match
{
    public enum MatchPhase { Intro, Countdown, Playing, Result }
    public enum SurvivalOutcome { Retry, Advance, SessionWon }

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

    /// <summary>Điều phối một trận Survival. Host mở và đóng từng tick theo thứ tự.</summary>
    public sealed class MatchDirector : IDisposable
    {
        private readonly Func<bool> hasHostAuthority;
        private readonly TrainingRingBounds bounds;
        private readonly ulong[] roster;
        private readonly int countdownTicks;
        private readonly int confirmationTicks;
        private readonly int limitTicks;
        private readonly int tickRate;
        private readonly IGameEventSink eventSink;
        private readonly Dictionary<ulong, int> wobble = new();
        private readonly List<ulong[]> eliminations = new();
        private readonly HashSet<ulong> pendingFalls = new();
        private uint lastTick;
        private int countdownRemaining;
        private int elapsedTicks;
        private int lastEliminationAt;
        private bool disposed;

        public MatchDirector(Func<bool> hasHostAuthority, IEnumerable<ulong> roster, int level, uint seed,
            int tickRate, int countdownSeconds, float confirmationSeconds, int limitSeconds,
            TrainingRingBounds bounds = null, IGameEventSink eventSink = null)
        {
            this.hasHostAuthority = hasHostAuthority ?? throw new ArgumentNullException(nameof(hasHostAuthority));
            this.roster = roster.ToArray();
            if (this.roster.Length < 2 || this.roster.Distinct().Count() != this.roster.Length)
                throw new ArgumentException("Cần ít nhất hai fighter với ID riêng.", nameof(roster));
            if (level < 1 || level > 5 || tickRate <= 0 || countdownSeconds <= 0 ||
                !(confirmationSeconds > 0) || float.IsInfinity(confirmationSeconds) || limitSeconds <= 0)
                throw new ArgumentException("Cấu hình thời gian hoặc level không hợp lệ.");

            Level = level;
            this.eventSink = eventSink;
            this.tickRate = tickRate;
            Seed = seed;
            countdownTicks = checked(countdownSeconds * tickRate);
            confirmationTicks = checked((int)Math.Ceiling(confirmationSeconds * tickRate));
            limitTicks = checked(limitSeconds * tickRate);
            this.bounds = bounds;
            if (bounds != null)
                bounds.RingOut += OnArenaRingOut;
            ResetRoster();
        }

        public int Level { get; }
        public uint Seed { get; }
        public MatchPhase Phase { get; private set; }
        public IReadOnlyCollection<ulong> AliveFighters => wobble.Keys;
        public IReadOnlyList<SurvivalPlacement> Placements { get; private set; } = Array.Empty<SurvivalPlacement>();
        public int CountdownTicksRemaining => countdownRemaining;
        public int ElapsedTicks => elapsedTicks;
        public event Action<MatchPhase> PhaseChanged;
        public event Action<IReadOnlyList<SurvivalPlacement>> ResultConfirmed;

        private bool CanSimulate => !disposed && hasHostAuthority();

        public void FinishIntro(uint hostTick)
        {
            if (!CanSimulate || Phase != MatchPhase.Intro)
                return;
            lastTick = hostTick;
            countdownRemaining = countdownTicks;
            SetPhase(MatchPhase.Countdown);
            PublishCountdown();
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

        public void SampleArena(ulong fighterId, uint hostTick, UnityEngine.Vector3 worldCenter)
        {
            if (bounds != null && CanSimulate && Phase == MatchPhase.Playing && IsNextTick(hostTick)
                && wobble.ContainsKey(fighterId))
                bounds.SampleOnHost(fighterId, hostTick, worldCenter);
        }

        // Host gọi sau khi đã báo mọi ring-out của tick này, kể cả tick cuối.
        public void CompleteTick(uint hostTick)
        {
            if (!CanSimulate || Phase == MatchPhase.Intro || Phase == MatchPhase.Result || !IsNextTick(hostTick))
                return;
            lastTick = hostTick;

            if (Phase == MatchPhase.Countdown)
            {
                --countdownRemaining;
                if (countdownRemaining % tickRate == 0) PublishCountdown();
                if (countdownRemaining == 0)
                {
                    SetPhase(MatchPhase.Playing);
                    PublishScoreboard();
                }
                return;
            }

            elapsedTicks++;
            if (pendingFalls.Count != 0)
            {
                var group = roster.Where(pendingFalls.Contains).ToArray();
                foreach (var fighter in group)
                {
                    wobble.Remove(fighter);
                    if (eventSink != null) eventSink.Publish(MatchEvent.RingOut(eventSink.NextEventId(), lastTick, fighter));
                }
                foreach (var fighter in group)
                    if (eventSink != null) eventSink.Publish(MatchEvent.Elimination(eventSink.NextEventId(), lastTick,
                        fighter, wobble.Count + 1));
                eliminations.Add(group);
                pendingFalls.Clear();
                lastEliminationAt = elapsedTicks;
                PublishScoreboard();
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
            if (Phase != MatchPhase.Result || !roster.Contains(playerId))
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
            Placements = Array.Empty<SurvivalPlacement>();
            foreach (var fighter in roster)
            {
                wobble.Add(fighter, 0);
                if (bounds != null)
                    bounds.ResetFighterOnHost(fighter);
            }
        }

        private void ConfirmResult()
        {
            var result = new List<SurvivalPlacement>();
            foreach (var group in wobble.OrderBy(fighter => fighter.Value).GroupBy(fighter => fighter.Value))
                AppendGroup(result, group.Select(fighter => fighter.Key));
            for (var i = eliminations.Count - 1; i >= 0; i--)
                AppendGroup(result, eliminations[i]);
            Placements = result.AsReadOnly();
            SetPhase(MatchPhase.Result);
            if (eventSink != null)
            {
                var rows = Standings(result);
                eventSink.Publish(MatchEvent.Scoreboard(eventSink.NextEventId(), lastTick, rows));
                eventSink.Publish(MatchEvent.MatchResult(eventSink.NextEventId(), lastTick, rows));
            }
            ResultConfirmed?.Invoke(Placements);
        }

        private MatchStanding[] Standings(IEnumerable<SurvivalPlacement> placements) => placements.Select(p =>
            new MatchStanding(p.FighterId, p.Rank, 0, wobble.ContainsKey(p.FighterId) ? 1 : 0)).ToArray();

        private void PublishCountdown()
        {
            if (eventSink != null) eventSink.Publish(MatchEvent.Countdown(eventSink.NextEventId(), lastTick,
                (countdownRemaining + tickRate - 1) / tickRate));
        }
        private void PublishScoreboard()
        {
            if (eventSink == null) return;
            var result = new List<SurvivalPlacement>();
            foreach (var group in wobble.OrderBy(f => f.Value).GroupBy(f => f.Value))
                AppendGroup(result, group.Select(f => f.Key));
            for (var i = eliminations.Count - 1; i >= 0; i--) AppendGroup(result, eliminations[i]);
            eventSink.Publish(MatchEvent.Scoreboard(eventSink.NextEventId(), lastTick, Standings(result)));
        }

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
