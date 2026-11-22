using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;
using SquishyBump.Net.Sync;

namespace SquishyBump.Tests.EditMode
{
    public sealed class MatchEventTests
    {
        private static MatchStanding[] Rows => new[] { new MatchStanding(1, 1, 7, 2, 9000), new MatchStanding(2, 1, 7, 2, 9000) };
        private static MatchEvent Event(MatchEventKind kind)
        {
            switch (kind)
            {
                case MatchEventKind.Countdown: return MatchEvent.Countdown(1, 60, 3);
                case MatchEventKind.RingOut: return MatchEvent.RingOut(1, 60, 2, 1);
                case MatchEventKind.Respawn: return MatchEvent.Respawn(1, 60, 2);
                case MatchEventKind.Elimination: return MatchEvent.Elimination(1, 60, 2, 2);
                case MatchEventKind.MatchResult: return MatchEvent.MatchResult(1, 60, Rows);
                default: return MatchEvent.Scoreboard(1, 60, Rows);
            }
        }
        [TestCase(MatchEventKind.Countdown)] [TestCase(MatchEventKind.RingOut)]
        [TestCase(MatchEventKind.Respawn)] [TestCase(MatchEventKind.Elimination)]
        [TestCase(MatchEventKind.MatchResult)] [TestCase(MatchEventKind.Scoreboard)]
        public void SixKinds_RoundTripWithoutChangingTiesLivesOrPoints(MatchEventKind kind)
        {
            var original = Event(kind);
            Assert.That(SyncCodec.TryReadMatch(SyncCodec.WriteMatch(original), out var actual), Is.True);
            Assert.That(actual.EventId, Is.EqualTo(original.EventId)); Assert.That(actual.HostTick, Is.EqualTo(original.HostTick));
            Assert.That(actual.Kind, Is.EqualTo(kind)); Assert.That(actual.FighterId, Is.EqualTo(original.FighterId));
            Assert.That(actual.CreditedFighterId, Is.EqualTo(original.CreditedFighterId)); Assert.That(actual.Value, Is.EqualTo(original.Value));
            CollectionAssert.AreEqual(original.Standings.Select(s => (s.FighterId, s.Rank, s.Points, s.Lives, s.SurvivalTicks)),
                actual.Standings.Select(s => (s.FighterId, s.Rank, s.Points, s.Lives, s.SurvivalTicks)));
        }
        [Test]
        public void MalformedPacket_IsRejectedBeforeApplyingState()
        {
            Assert.That(SyncCodec.TryReadMatch(null, out _), Is.False);
            var bytes = SyncCodec.WriteMatch(Event(MatchEventKind.MatchResult));
            for (var size = 0; size < bytes.Length; size++)
                Assert.That(SyncCodec.TryReadMatch(bytes.Take(size).ToArray(), out _), Is.False);
            Assert.That(SyncCodec.TryReadMatch(bytes.Concat(new byte[1]).ToArray(), out _), Is.False);
            bytes[8] = 255; Assert.That(SyncCodec.TryReadMatch(bytes, out _), Is.False);
            bytes = SyncCodec.WriteMatch(Event(MatchEventKind.Countdown)); bytes[29] = 255;
            Assert.That(SyncCodec.TryReadMatch(bytes, out _), Is.False);
            bytes = SyncCodec.WriteMatch(Event(MatchEventKind.Countdown)); bytes[9] = 1; // trường fighter không dùng
            Assert.That(SyncCodec.TryReadMatch(bytes, out _), Is.False);
        }
        [Test]
        public void InvalidTableAndSelfCredit_CannotBeSent()
        {
            foreach (var rows in new[] {
                new[] { new MatchStanding(1, 0, 0), new MatchStanding(2, 2, 0) },
                new[] { new MatchStanding(1, 1, -1), new MatchStanding(2, 2, 0) },
                new[] { new MatchStanding(1, 1, 0, 4), new MatchStanding(2, 2, 0) },
                new[] { new MatchStanding(1, 1, 0), new MatchStanding(1, 2, 0) },
                new[] { new MatchStanding(1, 1, 0, 0, -1), new MatchStanding(2, 2, 0) } })
                Assert.Throws<ArgumentException>(() => SyncCodec.WriteMatch(MatchEvent.Scoreboard(1, 10, rows)));
            Assert.Throws<ArgumentException>(() => SyncCodec.WriteMatch(MatchEvent.RingOut(1, 10, 2, 2)));
            Assert.Throws<ArgumentException>(() => SyncCodec.WriteMatch(MatchEvent.Countdown(0, 10, 3)));
            Assert.Throws<ArgumentException>(() => SyncCodec.WriteMatch(MatchEvent.Countdown(1, 10, -1)));
            Assert.Throws<ArgumentException>(() => SyncCodec.WriteMatch(MatchEvent.MatchResult(1, 10, new MatchStanding[7])));
        }
        [Test]
        public void CallerReusesTable_PublishedResultRemainsImmutable()
        {
            var rows = Rows; var e = MatchEvent.MatchResult(1, 10, rows);
            rows[0] = new MatchStanding(9, 2, 0);
            Assert.That(e.Standings[0].FighterId, Is.EqualTo(1));
        }
        [Test]
        public void HostDirector_WireAndDuplicateDelivery_ProduceSameConfirmedResult()
        {
            var host = new GameEventHub(() => true); var client = new GameEventHub(() => false);
            var hostView = new MatchReplica(); var clientView = new MatchReplica(); var received = new List<MatchEvent>();
            host.MatchRaised += hostView.Apply; client.MatchRaised += clientView.Apply;
            host.MatchRaised += e =>
            {
                Assert.That(SyncCodec.TryReadMatch(SyncCodec.WriteMatch(e), out var wire), Is.True);
                client.Deliver(wire); client.Deliver(wire); received.Add(e);
            };
            using var director = new MatchDirector(() => true, new[] { 1UL, 2UL, 3UL }, 2, 18, 60, 3, 0.5f, 150, eventSink: host);
            director.FinishIntro(0);
            for (uint t = 1; t <= 180; t++) director.CompleteTick(t);
            director.ReportRingOut(2, 181); director.ReportRingOut(3, 181); director.CompleteTick(181);
            for (uint t = 182; t <= 211; t++) director.CompleteTick(t);
            Assert.That(clientView.HasResult && hostView.HasResult, Is.True);
            Assert.That(received.Where(e => e.Kind == MatchEventKind.Countdown).Select(e => e.Value), Is.EqualTo(new[] { 3, 2, 1, 0 }));
            CollectionAssert.AreEqual(hostView.Result.Select(s => (s.FighterId, s.Rank)), clientView.Result.Select(s => (s.FighterId, s.Rank)));
            Assert.That(clientView.Eliminated[2], Is.EqualTo(2)); Assert.That(clientView.Eliminated[3], Is.EqualTo(2));
            var old = received.First(); client.Deliver(old);
            Assert.That(clientView.CountdownSeconds, Is.EqualTo(0), "ID cũ không được ghi đè GO");
            var results = received.Count(e => e.Kind == MatchEventKind.MatchResult);
            director.CompleteTick(212); Assert.That(received.Count(e => e.Kind == MatchEventKind.MatchResult), Is.EqualTo(results));
        }
        [Test]
        public void RespawnAndNewMatch_UpdateReplicaWithoutClientRules()
        {
            var view = new MatchReplica(); var hub = new GameEventHub(() => false); hub.MatchRaised += view.Apply;
            hub.Deliver(MatchEvent.Countdown(1, 1, 3)); hub.Deliver(MatchEvent.RingOut(2, 2, 1));
            Assert.That(view.OutFighters, Does.Contain(1UL));
            hub.Deliver(MatchEvent.Respawn(3, 122, 1)); Assert.That(view.OutFighters, Is.Empty);
            hub.Deliver(MatchEvent.MatchResult(4, 150, Rows)); Assert.That(view.HasResult, Is.True);
            hub.Deliver(MatchEvent.Countdown(5, 160, 3));
            Assert.That(view.HasResult, Is.False); Assert.That(view.Result, Is.Empty);
            Assert.That(view.CountdownSeconds, Is.EqualTo(3));
            hub.Publish(MatchEvent.MatchResult(6, 200, Rows)); Assert.That(view.HasResult, Is.False, "client không publish kết quả");
        }
    }
}
