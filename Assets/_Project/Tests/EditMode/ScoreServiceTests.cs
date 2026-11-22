using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Match;

namespace SquishyBump.Tests.EditMode
{
    public sealed class ScoreServiceTests
    {
        [TestCase(179, true)] [TestCase(180, true)] [TestCase(181, false)]
        public void LastTouchWindow_IsThreeSecondsInclusive(int age, bool scores)
        {
            var service = new ScoreService(() => true, 180, 1);
            service.BeginMatch(1, new[] { 1UL, 2UL });
            service.RecordTouch(2, 1, 100);
            Assert.That(service.RecordRingOut(2, (uint)(100 + age)), Is.EqualTo(scores ? 1UL : MatchEvent.NoFighter));
            Assert.That(service.Points(1), Is.EqualTo(scores ? 1 : 0));
            service.RecordRingOut(2, (uint)(100 + age));
            Assert.That(service.Points(1), Is.EqualTo(scores ? 1 : 0), "không cộng trùng một lượt rơi");
        }

        [Test]
        public void HazardOnlyAndSelfTouch_GiveNoPoint_AndRespawnClearsCredit()
        {
            var service = new ScoreService(() => true, 180, 1);
            service.BeginMatch(1, new[] { 1UL, 2UL });
            service.RecordTouch(2, 2, 1);
            Assert.That(service.RecordRingOut(2, 2), Is.EqualTo(MatchEvent.NoFighter));
            service.RecordTouch(2, 1, 3); service.ClearTouch(2);
            Assert.That(service.RecordRingOut(2, 4), Is.EqualTo(MatchEvent.NoFighter));
            Assert.That(service.Points(1), Is.Zero);
        }

        [Test]
        public void SessionTotals_AccumulateOnceAndSortPointsLivesThenSurvivalTime()
        {
            var service = new ScoreService(() => true, 180, 1);
            var roster = new[] { 1UL, 2UL, 3UL, 4UL };
            service.BeginMatch(1, roster);
            service.RecordTouch(4, 1, 1); service.RecordRingOut(4, 2);
            var lives = new Dictionary<ulong, int> { [1]=1, [2]=3, [3]=3, [4]=2 };
            var time = new Dictionary<ulong, long> { [1]=20, [2]=50, [3]=60, [4]=70 };
            service.CompleteMatch(1, lives, time); service.CompleteMatch(1, lives, time);
            service.BeginMatch(2, roster);
            service.CompleteMatch(2, lives, time);
            var standings = service.Standings();
            Assert.That(standings.Select(s => s.FighterId), Is.EqualTo(new[] { 1UL, 3UL, 2UL, 4UL }));
            Assert.That(standings.Single(s => s.FighterId == 2).Lives, Is.EqualTo(6));
            Assert.That(standings.Single(s => s.FighterId == 2).SurvivalTicks, Is.EqualTo(100));
        }

        [Test]
        public void ClientCannotWriteScoresOrTotals()
        {
            var service = new ScoreService(() => false, 180, 1);
            service.BeginMatch(1, new[] { 1UL, 2UL });
            service.RecordTouch(2, 1, 1); service.RecordRingOut(2, 2);
            Assert.That(service.Standings(), Is.Empty);
        }

        [Test]
        public void MultipleContacts_CreditTheLastOtherFighter()
        {
            var service = new ScoreService(() => true, 180, 1);
            service.BeginMatch(1, new[] { 1UL, 2UL, 3UL });
            service.RecordTouch(3, 1, 10); service.RecordTouch(3, 2, 11);
            Assert.That(service.RecordRingOut(3, 12), Is.EqualTo(2UL));
            Assert.That(service.Points(1), Is.Zero);
            Assert.That(service.Points(2), Is.EqualTo(1));
        }

        [Test]
        public void EqualSessionTotals_ShareRankAndSkipTheNextRank()
        {
            var service = new ScoreService(() => true, 180, 1);
            service.BeginMatch(1, new[] { 1UL, 2UL, 3UL });
            service.CompleteMatch(1, new Dictionary<ulong, int> { [1]=3, [2]=3, [3]=2 },
                new Dictionary<ulong, long> { [1]=100, [2]=100, [3]=100 });
            Assert.That(service.Standings().Select(s => s.Rank), Is.EqualTo(new[] { 1, 1, 3 }));
        }
    }
}
