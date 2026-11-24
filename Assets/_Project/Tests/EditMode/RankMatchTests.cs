using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.App;
using SquishyBump.Match;
using SquishyBump.Net.Sync;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    public sealed class RankMatchTests
    {
        private const int Hz = 60, Countdown = 180;
        private static BalanceProfile Profile => AssetDatabase.LoadAssetAtPath<BalanceProfile>("Assets/_Project/Data/Balance/BalanceProfile.asset");
        private static MatchDirector Create(int level = 2, System.Func<bool> authority = null) =>
            new(authority ?? (() => true), new[] { 1UL, 2UL, 3UL }, level, 7, Hz, 3, 0.5f, 150,
                rule: MatchRule.Rank, rankRules: new RankRules(3, 120, 90, 180, 1, 105 * Hz));

        [Test, Category("UnityAsset")]
        public void RankRules_FromBalanceAsset_MatchesTheGddFixture()
        {
            var rules = RankRules.FromProfile(Profile, Hz);
            Assert.That((rules.Lives, rules.RespawnTicks, rules.InvulnerabilityTicks, rules.TouchTicks, rules.Points, rules.L1CloseTick),
                Is.EqualTo((3, 120, 90, 180, 1, 6300)));
        }
        private static void Start(MatchDirector match)
        {
            match.FinishIntro(0);
            To(match, 1, Countdown);
        }
        private static void To(MatchDirector match, int first, int last)
        {
            for (var tick = first; tick <= last; tick++) match.CompleteTick((uint)tick);
        }

        [Test]
        public void Rank_RespawnsAtTwoSecondsAndProtectsForOnePointFiveSeconds()
        {
            using var match = Create(); Start(match);
            match.SetWobble(2, 7);
            match.RecordTouch(2, 1, Countdown + 1);
            match.ReportRingOut(2, Countdown + 1);
            match.ReportRingOut(2, Countdown + 1); // cùng tick chỉ mất một mạng
            match.CompleteTick(Countdown + 1);
            Assert.That(match.Lives(2), Is.EqualTo(2));
            Assert.That(match.IsRespawning(2), Is.True);
            Assert.That(match.Scores.Points(1), Is.EqualTo(1));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            To(match, Countdown + 2, Countdown + 120);
            Assert.That(match.AliveFighters, Has.No.Member(2UL));
            match.CompleteTick(Countdown + 121);
            Assert.That(match.AliveFighters, Has.Member(2UL));
            Assert.That(match.Wobble(2), Is.Zero);
            Assert.That(match.IsInvulnerable(2), Is.True);
            To(match, Countdown + 122, Countdown + 210);
            Assert.That(match.IsInvulnerable(2), Is.True);
            match.CompleteTick(Countdown + 211);
            Assert.That(match.IsInvulnerable(2), Is.False);
        }

        [Test]
        public void WaitingFruits_DoNotTriggerAnEarlyWinner()
        {
            using var match = Create(); Start(match);
            match.ReportRingOut(2, Countdown + 1); match.ReportRingOut(3, Countdown + 1);
            To(match, Countdown + 1, Countdown + 121);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(match.AliveFighters, Has.Count.EqualTo(3));
        }

        [Test]
        public void SoleEligibleFruitWaitingToRespawn_RemainsTheWinnerInTheReplicatedResult()
        {
            var host = new GameEventHub(() => true);
            var resultEvents = new List<MatchEvent>();
            host.MatchRaised += e =>
            {
                Assert.That(SyncCodec.TryReadMatch(SyncCodec.WriteMatch(e), out var decoded), Is.True);
                if (decoded.Kind == MatchEventKind.MatchResult) resultEvents.Add(decoded);
            };
            using var match = new MatchDirector(() => true, new[] { 1UL, 2UL }, 2, 7, Hz, 3, 0.5f, 150,
                rule: MatchRule.Rank, rankRules: new RankRules(3, 120, 90, 180, 1, 6300), eventSink: host);
            Start(match);
            var tick = Countdown;
            for (var loss = 0; loss < 2; loss++)
            {
                match.ReportRingOut(2, (uint)++tick);
                match.CompleteTick((uint)tick);
                To(match, tick + 1, tick + 120); tick += 120;
            }
            tick++;
            match.ReportRingOut(1, (uint)tick); match.ReportRingOut(2, (uint)tick);
            match.CompleteTick((uint)tick);
            Assert.That(match.IsRespawning(1), Is.True);
            Assert.That(match.Lives(2), Is.Zero);
            To(match, tick + 1, tick + 30);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Result));
            Assert.That(match.Placements, Has.Count.EqualTo(2));
            Assert.That(match.Placements[0].FighterId, Is.EqualTo(1));
            Assert.That(resultEvents, Has.Count.EqualTo(1));
            Assert.That(resultEvents[0].Standings.Select(s => (s.FighterId, s.Rank, s.Lives)),
                Is.EqualTo(new[] { (1UL, 1, 2), (2UL, 2, 0) }));
        }

        [Test]
        public void NoSafeFloor_KeepsRespawnPendingUntilWindowCloses()
        {
            using var match = Create(1); Start(match);
            match.TryPlaceRespawn = _ => false;
            match.ReportRingOut(2, Countdown + 1);
            To(match, Countdown + 1, Countdown + 105 * Hz - 1);
            Assert.That(match.IsRespawning(2), Is.True);
            match.CompleteTick(Countdown + 105 * Hz);
            Assert.That(match.RespawnsOpen, Is.False);
            Assert.That(match.IsRespawning(2), Is.False);
            Assert.That(match.AliveFighters, Has.No.Member(2UL));
        }

        [TestCase(1, 105)]
        [TestCase(2, 150)]
        [TestCase(5, 150)]
        public void FallAtWindowBoundary_DoesNotRespawn(int level, int closeAt)
        {
            using var match = Create(level); Start(match);
            var tick = Countdown + closeAt * Hz;
            To(match, Countdown + 1, tick - 1);
            match.ReportRingOut(2, (uint)tick); match.CompleteTick((uint)tick);
            Assert.That(match.Lives(2), Is.EqualTo(2));
            Assert.That(match.IsRespawning(2), Is.False);
            Assert.That(match.AliveFighters, Has.No.Member(2UL));
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void Timeout_RanksLivesBeforeWobble_AndKeepsPoints(int level)
        {
            using var match = Create(level); Start(match);
            match.RecordTouch(2, 1, Countdown + 1); match.ReportRingOut(2, Countdown + 1);
            To(match, Countdown + 1, Countdown + 121);
            match.SetWobble(1, 5); match.SetWobble(2, 0); match.SetWobble(3, 2);
            To(match, Countdown + 122, Countdown + 150 * Hz);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Result));
            Assert.That(match.Placements.Select(p => p.FighterId), Is.EqualTo(new[] { 3UL, 1UL, 2UL }));
            Assert.That(match.Scores.Points(1), Is.EqualTo(1));
        }

        [Test]
        public void LastLivesLostSameTick_ShareFirstAfterConfirmation()
        {
            using var match = Create(); Start(match);
            var tick = Countdown;
            for (var life = 0; life < 3; life++)
            {
                tick++;
                foreach (var id in new[] { 1UL, 2UL, 3UL }) match.ReportRingOut(id, (uint)tick);
                match.CompleteTick((uint)tick);
                if (life < 2) { To(match, tick + 1, tick + 120); tick += 120; }
            }
            To(match, tick + 1, tick + 29);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            match.CompleteTick((uint)(tick + 30));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Result));
            Assert.That(match.Placements.Select(p => p.Rank), Is.EqualTo(new[] { 1, 1, 1 }));
        }

        [Test]
        public void ClientCannotLoseLivesScoreOrRespawn()
        {
            var host = true;
            using var match = Create(authority: () => host); Start(match);
            host = false;
            match.RecordTouch(2, 1, Countdown + 1); match.ReportRingOut(2, Countdown + 1);
            match.CompleteTick(Countdown + 1);
            Assert.That(match.Lives(2), Is.EqualTo(3));
            Assert.That(match.Scores.Points(1), Is.Zero);
            Assert.That(match.ElapsedTicks, Is.Zero);
        }

        [Test]
        public void RespawnPicker_RejectsUnsupportedFloorCandidates()
        {
            Assert.That(RespawnPicker.TryPick(Vector3.zero, 2, null, null, out _, _ => false), Is.False);
            Assert.That(RespawnPicker.TryPick(Vector3.zero, 2, null, null, out var position, p => p.x >= 1), Is.True);
            Assert.That(position.x, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Disconnect_EliminatesWithoutRespawnOrCreditAndKeepsScore()
        {
            var hub = new GameEventHub(() => true);
            var events = new List<MatchEvent>();
            hub.MatchRaised += events.Add;
            using var match = new MatchDirector(() => true, new[] { 1UL, 2UL, 3UL }, 2, 7, Hz, 3, 0.5f, 150,
                rule: MatchRule.Rank, rankRules: new RankRules(3, 120, 90, 180, 1, 105 * Hz), eventSink: hub);
            Start(match);

            // Fighter 2 scores 1 point before disconnecting later.
            match.RecordTouch(1, 2, Countdown + 1);
            match.ReportRingOut(1, Countdown + 1);
            To(match, Countdown + 1, Countdown + 121);
            Assert.That(match.Scores.Points(2), Is.EqualTo(1));

            // Fighter 1 touches Fighter 2, then Fighter 2 disconnects cleanly: no RingOut credit to 1, no respawn, keeps score.
            var dropTick = (uint)(Countdown + 122);
            match.RecordTouch(2, 1, dropTick);
            Assert.That(match.ReportDisconnect(2, dropTick), Is.True);
            Assert.That(match.ReportDisconnect(2, dropTick), Is.False);
            match.CompleteTick(dropTick);
            Assert.That(match.Lives(2), Is.Zero);
            Assert.That(match.IsRespawning(2), Is.False);
            Assert.That(match.DisconnectedAt(2), Is.EqualTo(dropTick));
            Assert.That(match.Scores.Points(1), Is.Zero);
            Assert.That(match.Scores.Points(2), Is.EqualTo(1));
            Assert.That(events.Count(e => e.HostTick == dropTick && e.Kind == MatchEventKind.Elimination && e.FighterId == 2 && e.Value == 3), Is.EqualTo(1));
            Assert.That(events.Count(e => e.HostTick == dropTick && e.Kind == MatchEventKind.RingOut), Is.Zero);

            To(match, (int)dropTick + 1, Countdown + 250);
            Assert.That(match.Lives(2), Is.Zero);
            Assert.That(match.AliveFighters, Has.No.Member(2UL));

            // Respawning fighter 3 disconnecting skips remaining lives cleanly and finishes match.
            var fallTick = (uint)(Countdown + 251);
            match.ReportRingOut(3, fallTick);
            match.CompleteTick(fallTick);
            Assert.That(match.IsRespawning(3), Is.True);
            Assert.That(match.ReportDisconnect(3, fallTick + 1), Is.True);
            match.CompleteTick(fallTick + 1);
            Assert.That(match.Lives(3), Is.Zero);
            Assert.That(match.IsRespawning(3), Is.False);
            To(match, (int)fallTick + 2, (int)fallTick + 31);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Result));
            Assert.That(match.Placements.Select(p => (p.FighterId, p.Rank)), Is.EqualTo(new[] { (1UL, 1), (3UL, 2), (2UL, 3) }));
            Assert.That(match.Scores.Points(2), Is.EqualTo(1));
        }
    }
}
