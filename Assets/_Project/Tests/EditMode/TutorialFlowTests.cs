using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Match;

namespace SquishyBump.Tests.EditMode
{
    public sealed class TutorialFlowTests
    {
        private static void ToDash(TutorialFlow f)
        {
            f.Observe(true, false, true, false, false);
            f.Observe(false, true, true, false, false);
            f.Observe(false, false, false, false, false);
            f.Observe(false, false, true, false, false);
        }
        [Test]
        public void FiveSteps_OnlyAdvanceOnObservedActionAndClassDismissal()
        {
            var f = new TutorialFlow(() => true); var transitions = 0; f.Changed += _ => transitions++;
            f.Observe(false, true, false, true, true); Assert.That(f.Step, Is.EqualTo(TutorialStep.Move));
            f.DismissClassCard(); Assert.That(f.Step, Is.EqualTo(TutorialStep.Move));
            ToDash(f); Assert.That(f.Step, Is.EqualTo(TutorialStep.Dash));
            f.Observe(false, false, true, false, true); Assert.That(f.Step, Is.EqualTo(TutorialStep.Dash));
            f.Observe(false, false, true, true, false); Assert.That(f.Step, Is.EqualTo(TutorialStep.ClassCard));
            f.Observe(true, true, true, true, true); Assert.That(f.Step, Is.EqualTo(TutorialStep.ClassCard));
            f.DismissClassCard(); Assert.That(f.Step, Is.EqualTo(TutorialStep.Push));
            f.Observe(false, false, true, false, false); Assert.That(f.Finished, Is.False);
            f.Observe(false, false, true, false, true); Assert.That(f.Step, Is.EqualTo(TutorialStep.Completed));
            f.Skip(); Assert.That(transitions, Is.EqualTo(5));
        }
        [Test]
        public void JumpRequiresTakeoffAirborneAndLanding_FallResetKeepsStep()
        {
            var f = new TutorialFlow(() => true); f.Observe(true, false, true, false, false);
            f.Observe(false, false, false, false, false); f.Observe(false, false, true, false, false);
            Assert.That(f.Step, Is.EqualTo(TutorialStep.Jump), "Rơi khỏi mép rồi đặt lại không phải nhảy");
            f.Observe(false, true, true, false, false); f.Observe(false, false, false, false, false);
            f.PlayerReset(); f.Observe(false, false, true, false, false);
            Assert.That(f.Step, Is.EqualTo(TutorialStep.Jump));
            f.Observe(false, true, true, false, false); f.Observe(false, false, false, false, false);
            f.Observe(false, false, true, false, false); Assert.That(f.Step, Is.EqualTo(TutorialStep.Dash));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void SkipAtEveryStep_FinishesExactlyOnce(int target)
        {
            var f = new TutorialFlow(() => true);
            if (target >= 1) f.Observe(true, false, true, false, false);
            if (target >= 2) { f.Observe(false, true, false, false, false); f.Observe(false, false, true, false, false); }
            if (target >= 3) f.Observe(false, false, true, true, false);
            if (target >= 4) f.DismissClassCard();
            var changes = 0; f.Changed += _ => changes++;
            f.Skip(); f.Skip(); f.Observe(true, true, true, true, true);
            Assert.That(f.Step, Is.EqualTo(TutorialStep.Skipped)); Assert.That(changes, Is.EqualTo(1));
        }
        [Test]
        public void ClientCannotAdvanceDismissOrSkip()
        {
            var f = new TutorialFlow(() => false); f.Observe(true, true, true, true, true);
            f.DismissClassCard(); f.Skip(); Assert.That(f.Step, Is.EqualTo(TutorialStep.Move));
        }
        [TestCase(true, 1, false, false, true)] [TestCase(false, 1, false, false, false)]
        [TestCase(true, 2, false, false, false)] [TestCase(true, 1, true, false, false)]
        [TestCase(true, 1, false, true, false)]
        public void Eligibility_OnlyFirstSingleL1(bool single, int level, bool completed, bool skipped, bool expected) =>
            Assert.That(TutorialFlow.ShouldRun(single, level, completed, skipped), Is.EqualTo(expected));
    }
}
