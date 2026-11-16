using NUnit.Framework;
using SquishyBump.App;
using UnityEditor;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-48: arena card lines and the B07 last-seconds warning (GDD B.3, B.6).</summary>
    public sealed class MatchOverlayTests
    {
        static MatchSetup Setup => AssetDatabase.LoadAssetAtPath<MatchSetup>("Assets/_Project/Data/MatchSetup.asset");

        [Test]
        public void Setup_HoldsCardLinesAndTimings()
        {
            Assert.That(Setup.CardLine(1), Is.EqualTo("Every Cup starts on the play mat. Watch the rings!"));
            Assert.That(Setup.CardLine(4), Is.EqualTo("Careful in the freezer: the ice is cracking."));
            Assert.That(Setup.CardLine(5), Does.StartWith("The top shelf"));
            Assert.That(Setup.CardSeconds, Is.InRange(2f, 3f));
            Assert.That(Setup.FinalWarningSeconds, Is.EqualTo(10f));
            Assert.That(Setup.BannerSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void FinalWarning_At140SecondsOnL2ToL5Only()
        {
            Assert.That(MatchHost.FinalWarningDue(2, 139.9f, 150f, 10f), Is.False);
            Assert.That(MatchHost.FinalWarningDue(2, 140f, 150f, 10f), Is.True);
            Assert.That(MatchHost.FinalWarningDue(5, 145f, 150f, 10f), Is.True);
            Assert.That(MatchHost.FinalWarningDue(1, 145f, 150f, 10f), Is.False, "L1 warns on ring drops instead");
        }
    }
}
