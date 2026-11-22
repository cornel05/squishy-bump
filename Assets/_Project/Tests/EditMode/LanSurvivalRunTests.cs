using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Match;

namespace SquishyBump.Tests.EditMode
{
    public sealed class LanSurvivalRunTests
    {
        [TestCase(2, 5, new[] { 2, 2, 2, 2 })]
        [TestCase(6, 5, new[] { 5, 4, 3, 2 })]
        [TestCase(4, 3, new[] { 3, 2 })]
        [TestCase(2, 1, new int[0])]
        public void ConfirmedResults_KeepTwoUntilFinal_ThenRankEveryone(int count, int finalLevel, int[] expected)
        {
            var entrants = Enumerable.Range(0, count).Select(id => (ulong)id).ToArray();
            var peers = entrants.Skip(1).ToArray();
            var run = new LanSurvivalRun(finalLevel, entrants);

            foreach (var nextCount in expected)
            {
                // Put the host last. It may spectate while its peers continue playing.
                var decision = run.Evaluate(peers, Placements(run.Active.Reverse().ToArray()));
                Assert.That(decision.NextLevel, Is.EqualTo(run.Level + 1));
                Assert.That(decision.Active.Count, Is.EqualTo(nextCount));
                run.Advance(decision);
            }

            var final = run.Evaluate(peers, Placements(run.Active.Reverse().ToArray()));
            Assert.That(final.NextLevel, Is.Null);
            Assert.That(final.Standings.Select(row => row.FighterId), Is.EqualTo(entrants.Reverse()));
            Assert.That(final.Standings.Select(row => row.Rank), Is.EqualTo(Enumerable.Range(1, count)));
        }

        [TestCase(1, 2, 1)]
        [TestCase(3, 4, 3)]
        public void TieAtKnockoutCutoff_ReplaysSameLevelAndRoster(int finalLevel, int count, int tieRank)
        {
            var entrants = Enumerable.Range(0, count).Select(id => (ulong)id).ToArray();
            var peers = entrants.Skip(1).ToArray();
            var run = new LanSurvivalRun(finalLevel, entrants);
            var rows = entrants.Select((id, i) => new SurvivalPlacement(id, System.Math.Min(i + 1, tieRank))).ToArray();

            var retry = run.Evaluate(peers, rows);

            Assert.That(retry.NextLevel, Is.EqualTo(1));
            Assert.That(retry.Active, Is.EqualTo(entrants));
            run.Advance(retry);
            var resolved = run.Evaluate(peers, Placements(entrants));
            if (finalLevel == 1) Assert.That(resolved.Standings.Select(row => row.FighterId), Is.EqualTo(entrants));
            else Assert.That(resolved.Active, Is.EqualTo(entrants.Take(count - 1)));
        }

        [Test]
        public void LastOpponentLeaves_FinishesByForfeitWithoutAnotherMatch()
        {
            var run = new LanSurvivalRun(5, new[] { 0UL, 1UL, 2UL });
            var first = run.Evaluate(new[] { 1UL, 2UL }, Placements(0, 1, 2));
            run.Advance(first);

            var final = run.Evaluate(System.Array.Empty<ulong>());

            Assert.That(final.NextLevel, Is.Null);
            Assert.That(final.Standings.Select(row => (row.FighterId, row.Rank)),
                Is.EqualTo(new[] { (0UL, 1), (1UL, 2), (2UL, 3) }));
        }

        private static SurvivalPlacement[] Placements(params ulong[] ids) =>
            ids.Select((id, i) => new SurvivalPlacement(id, i + 1)).ToArray();
    }
}
