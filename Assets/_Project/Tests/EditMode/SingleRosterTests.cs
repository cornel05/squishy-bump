using System;
using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Match;

namespace SquishyBump.Tests.EditMode
{
    public sealed class SingleRosterTests
    {
        [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        public void EachPlayerPick_LeavesFiveDistinctBotsAndStableCharacters(int pick)
        {
            var run = new SinglePlayerRun(pick, 123, MatchRule.Survival);
            var fruits = run.Current.Fruits.ToArray();
            Assert.That(fruits[0], Is.EqualTo(pick));
            Assert.That(fruits.OrderBy(x => x), Is.EqualTo(Enumerable.Range(0, 6)));
            for (var level = 1; level <= 5; level++)
            {
                Assert.That(run.Current.Fruits, Is.EqualTo(fruits));
                Assert.That(run.Current.Level, Is.EqualTo(level));
                run.Continue(true);
            }
            Assert.That(run.Finished && run.LevelsCleared == 5, Is.True);
        }

        [Test]
        public void EveryLevelAndSeed_HasOneGuaranteedTierAndFourDrawsWithinA6Range()
        {
            for (uint seed = 0; seed < 256; seed++)
            {
                var a = new SinglePlayerRun(2, seed, MatchRule.Survival);
                var b = new SinglePlayerRun(2, seed, MatchRule.Survival);
                for (var n = 1; n <= 5; n++)
                {
                    Assert.That(a.Current.Tiers, Has.Count.EqualTo(5));
                    Assert.That(a.Current.Tiers[0], Is.EqualTo(n));
                    Assert.That(a.Current.Tiers.Skip(1), Is.All.InRange(Math.Max(1, n - 2), n));
                    Assert.That(a.Current.Fruits, Is.EqualTo(b.Current.Fruits));
                    Assert.That(a.Current.Tiers, Is.EqualTo(b.Current.Tiers));
                    Assert.That(a.Current.Seed, Is.EqualTo(b.Current.Seed));
                    a.Continue(true); b.Continue(true);
                }
            }
        }

        [Test]
        public void SurvivalLossOrTie_RetainsExactRoundAndCountsUnlimitedRetries()
        {
            var run = new SinglePlayerRun(5, 456, MatchRule.Survival);
            var round = run.Current;
            for (var i = 0; i < 100; i++) run.Continue(false);
            Assert.That(run.Current, Is.SameAs(round));
            Assert.That(run.LevelsCleared, Is.Zero);
            Assert.That(run.Retries, Is.EqualTo(100));
            Assert.That(run.Finished, Is.False);
        }

        [Test]
        public void RankLoss_AdvancesThroughFiveLevelsAndFinishCannotAdvanceAgain()
        {
            var run = new SinglePlayerRun(1, 0, MatchRule.Rank);
            for (var i = 0; i < 5; i++) run.Continue(false);
            run.Continue(false);
            Assert.That(run.Level, Is.EqualTo(5));
            Assert.That(run.LevelsCleared, Is.EqualTo(5));
            Assert.That(run.Retries, Is.Zero);
            Assert.That(run.Finished, Is.True);
        }
    }
}
