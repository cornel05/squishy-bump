using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.Match;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class SurvivalArenaTests
    {
        [UnityTest]
        public IEnumerator T08_LastTwoFallOnSameTick_DrawAndRetrySameSeed()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Levels/L1.unity");
            var bounds = Object.FindAnyObjectByType<TrainingRingBounds>();
            Assert.That(bounds, Is.Not.Null);
            using var match = new MatchDirector(() => true, new[] { 11UL, 22UL }, 1, 494, 60, 3, 0.5f, 150, bounds);
            match.FinishIntro(0);
            for (uint tick = 1; tick <= 180; tick++)
                match.CompleteTick(tick);

            // SB-15 sẽ bỏ collider sàn. Mẫu này đại diện cho hai quả idle
            // đã rơi qua kill plane một giây sau khi vành cuối rụng.
            const uint fallTick = 180 + 136 * 60;
            for (uint tick = 181; tick < fallTick; tick++)
                match.CompleteTick(tick);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            match.SampleArena(22, fallTick, new Vector3(0, -5.1f, 0));
            match.SampleArena(11, fallTick, new Vector3(0, -5.1f, 0));
            for (uint tick = fallTick; tick < fallTick + 30; tick++)
                match.CompleteTick(tick);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            match.CompleteTick(fallTick + 30);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Result));
            Assert.That(match.Placements.Select(p => p.Rank), Is.EqualTo(new[] { 1, 1 }));
            Assert.That(match.GetSinglePlayerOutcome(11), Is.EqualTo(SurvivalOutcome.Retry));
            Assert.That(match.RetrySinglePlayer(11), Is.True);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Intro));
            Assert.That(match.Seed, Is.EqualTo(494u));
            Assert.That(match.Level, Is.EqualTo(1));
            Assert.That(match.AliveFighters, Is.EquivalentTo(new[] { 11UL, 22UL }));
        }
    }
}
