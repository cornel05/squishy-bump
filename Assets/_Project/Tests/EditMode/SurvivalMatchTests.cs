using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;

namespace SquishyBump.Tests.EditMode
{
    public sealed class SurvivalMatchTests
    {
        private const int TickRate = 60;
        private const int Countdown = 3 * TickRate;

        [Test]
        public void Disconnect_CommitsAtReportedSimulationTickOnceAndKeepsFinalPlacement()
        {
            var hub = new GameEventHub(() => true); var events = 0;
            hub.MatchRaised += e =>
            {
                if (e.Kind != MatchEventKind.Elimination) return;
                events++;
                Assert.That(e.FighterId, Is.EqualTo(4));
                Assert.That(e.HostTick, Is.EqualTo(181));
                Assert.That(e.Value, Is.EqualTo(4));
            };
            using var match = Create(eventSink: hub);
            match.FinishIntro(0); Tick(match, 1, Countdown);
            Assert.That(match.ReportDisconnect(4, 181), Is.True);
            Assert.That(match.ReportDisconnect(4, 181), Is.False);
            Assert.That(match.AliveFighters, Has.Member(4UL));
            match.CompleteTick(181);
            Assert.That(match.AliveFighters, Has.No.Member(4UL));
            Assert.That(match.DisconnectedAt(4), Is.EqualTo(181)); Assert.That(events, Is.EqualTo(1));
            match.CompleteTick(181); Assert.That(events, Is.EqualTo(1));
            Tick(match, 182, Countdown + 9000);
            Assert.That(match.Placements.Single(p => p.FighterId == 4).Rank, Is.EqualTo(4));
            Assert.That(match.ReportDisconnect(1, Countdown + 9001), Is.False);
        }
        [Test]
        public void LastDisconnectAndFallInSameTick_ShareRankOne()
        {
            var hub = new GameEventHub(() => true);
            var disconnectedRank = 0;
            hub.MatchRaised += e => { if (e.Kind == MatchEventKind.Elimination && e.FighterId == 1) disconnectedRank = e.Value; };
            using var match = new MatchDirector(() => true, new[] { 1UL, 2UL }, 2, 18, 60, 3, 0.5f, 150, eventSink: hub);
            match.FinishIntro(0); Tick(match, 1, Countdown);
            match.ReportDisconnect(1, 181); match.ReportRingOut(2, 181); match.CompleteTick(181);
            Tick(match, 182, 211);
            Assert.That(disconnectedRank, Is.EqualTo(1));
            Assert.That(match.Placements.Select(p => p.Rank), Is.EqualTo(new[] { 1, 1 }));
        }
        [TestCase(true)] [TestCase(false)]
        public void DisconnectBeforeGo_IsNotLostAtIntroToCountdownBoundary(bool atBoundary)
        {
            using var match = Create();
            Assert.That(match.ReportDisconnect(4, 10), Is.True);
            if (atBoundary) match.FinishIntro(10);
            else { match.CompleteTick(10); match.FinishIntro(11); }
            Assert.That(match.AliveFighters, Has.No.Member(4UL));
            Assert.That(match.DisconnectedAt(4), Is.EqualTo(10));
        }
        [Test]
        public void Disconnect_RejectsUnknownDeadWrongTickAndClientAuthority()
        {
            using var match = Create(); match.FinishIntro(0); Tick(match, 1, Countdown);
            Assert.That(match.ReportDisconnect(99, 181), Is.False);
            Assert.That(match.ReportDisconnect(4, 180), Is.False);
            Assert.That(match.ReportDisconnect(4, 182), Is.False);
            match.ReportRingOut(4, 181); match.CompleteTick(181);
            Assert.That(match.ReportDisconnect(4, 182), Is.False);
            using var client = new MatchDirector(() => false, new[] { 1UL, 2UL }, 2, 18, 60, 3, 0.5f, 150);
            Assert.That(client.ReportDisconnect(1, 1), Is.False);
        }

        private static MatchDirector Create(int level = 2, IGameEventSink eventSink = null) =>
            new(() => true, new[] { 1UL, 2UL, 3UL, 4UL }, level, 123, TickRate, 3, 0.5f, 150, eventSink: eventSink);

        private static void Tick(MatchDirector match, int first, int last)
        {
            for (var tick = first; tick <= last; tick++)
                match.CompleteTick((uint)tick);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void IdleSurvival_EndsAt150SecondsAndRanksByWobble(int level)
        {
            using var match = Create(level);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Intro));
            match.FinishIntro(0);
            Tick(match, 1, Countdown - 1);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Countdown));
            Tick(match, Countdown, Countdown);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            match.SetWobble(1, 5);
            match.SetWobble(2, 1);
            match.SetWobble(3, 1);
            match.SetWobble(4, 3);
            var end = Countdown + 150 * TickRate;
            Tick(match, Countdown + 1, end - 1);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            Tick(match, end, end);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Result));
            Assert.That(match.ElapsedTicks, Is.EqualTo(9000));
            Assert.That(match.Placements.Select(p => p.FighterId), Is.EqualTo(new[] { 2UL, 3UL, 4UL, 1UL }));
            Assert.That(match.Placements.Select(p => p.Rank), Is.EqualTo(new[] { 1, 1, 3, 4 }));
            Assert.That(match.GetSinglePlayerOutcome(2), Is.EqualTo(SurvivalOutcome.Retry));
        }

        [TestCase(2, SurvivalOutcome.Advance)]
        [TestCase(5, SurvivalOutcome.SessionWon)]
        public void SoleWinner_ConfirmsHalfASecondAfterLastElimination(int level, SurvivalOutcome expected)
        {
            using var match = Create(level);
            match.FinishIntro(0);
            Tick(match, 1, Countdown);
            match.ReportRingOut(2, Countdown + 1);
            match.ReportRingOut(4, Countdown + 1);
            Tick(match, Countdown + 1, Countdown + 1);
            match.ReportRingOut(3, Countdown + 2);
            Tick(match, Countdown + 2, Countdown + 31);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            Tick(match, Countdown + 32, Countdown + 32);
            Assert.That(match.GetSinglePlayerOutcome(1), Is.EqualTo(expected));
            Assert.That(match.GetSinglePlayerOutcome(3), Is.EqualTo(SurvivalOutcome.Retry));
            Assert.That(match.Placements.Select(p => p.Rank), Is.EqualTo(new[] { 1, 2, 3, 3 }));
        }

        [Test]
        public void ClientCannotStartOrAdvanceTheMatch()
        {
            var isHost = false;
            using var match = new MatchDirector(() => isHost, new[] { 1UL, 2UL }, 1, 123, TickRate, 3, 0.5f, 150);
            match.FinishIntro(0);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Intro));
            isHost = true;
            match.FinishIntro(0);
            isHost = false;
            match.CompleteTick(1);
            Assert.That(match.CountdownTicksRemaining, Is.EqualTo(Countdown));
        }
    }
}
