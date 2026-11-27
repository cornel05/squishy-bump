using System;
using SquishyBump.Core;

namespace SquishyBump.Match
{
    /// <summary>Chính sách tutorial GDD 9.3. Bằng chứng do adapter vật lý host cung cấp.</summary>
    public sealed class TutorialFlow
    {
        private readonly Func<bool> canSimulate;
        private bool jumpStarted, leftFloor;
        public TutorialStep Step { get; private set; } = TutorialStep.Move;
        public bool Finished => Step == TutorialStep.Completed || Step == TutorialStep.Skipped;
        public event Action<TutorialStep> Changed;
        public TutorialFlow(Func<bool> authority) => canSimulate = authority ?? throw new ArgumentNullException(nameof(authority));
        public void Observe(bool markerReached, bool tookOff, bool grounded, bool dashStarted, bool toyPushedOut)
        {
            if (!canSimulate() || Finished) return;
            switch (Step)
            {
                case TutorialStep.Move: if (markerReached && grounded) Set(TutorialStep.Jump); break;
                case TutorialStep.Jump:
                    jumpStarted |= tookOff;
                    leftFloor |= jumpStarted && !grounded;
                    if (leftFloor && grounded) Set(TutorialStep.Dash);
                    break;
                case TutorialStep.Dash: if (dashStarted) Set(TutorialStep.ClassCard); break;
                case TutorialStep.Push: if (toyPushedOut) Set(TutorialStep.Completed); break;
            }
        }
        public void DismissClassCard()
        { if (canSimulate() && Step == TutorialStep.ClassCard) Set(TutorialStep.Push); }
        public void Skip()
        { if (canSimulate() && !Finished) Set(TutorialStep.Skipped); }
        public void PlayerReset() { jumpStarted = false; leftFloor = false; }
        public static bool ShouldRun(bool singlePlayer, int level, bool completed, bool skipped) =>
            singlePlayer && level == 1 && !completed && !skipped;
        private void Set(TutorialStep next) { Step = next; Changed?.Invoke(next); }
    }
}
