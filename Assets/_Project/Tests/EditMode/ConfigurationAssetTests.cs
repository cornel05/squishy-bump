using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    public sealed class ConfigurationAssetTests
    {
        [TestCase(1, "Training Ring", 12f, 6f, 135f, LevelEndTrigger.FinalRingDrop, "1")]
        [TestCase(2, "Moving Ground", 6.5f, 5.25f, 150f, LevelEndTrigger.TimeLimit, "2,3,7")]
        [TestCase(3, "Cannon Court", 9f, 5.5f, 150f, LevelEndTrigger.TimeLimit, "4,5")]
        [TestCase(4, "Frost Ring", 9f, 6f, 150f, LevelEndTrigger.TimeLimit, "6")]
        [TestCase(5, "Chaos Core", 9f, 6f, 150f, LevelEndTrigger.TimeLimit, "4,5,6")]
        public void LevelAsset_MatchesArenaConfiguration(int number, string arena, float radius,
            float spawnRadius, float seconds, LevelEndTrigger trigger, string hazardIds)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/_Project/Data/Levels/L{number}.asset");
            Assert.That(level, Is.Not.Null);
            Assert.That(level.LevelNumber, Is.EqualTo(number));
            Assert.That(level.ArenaName, Is.EqualTo(arena));
            Assert.That(level.ArenaRadiusMeters, Is.EqualTo(radius));
            Assert.That(level.ScenePath, Is.EqualTo($"Assets/_Project/Scenes/Levels/L{number}.unity"));
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == level.ScenePath), Is.True);
            Assert.That(level.EndTrigger, Is.EqualTo(trigger));
            Assert.That(level.EndTriggerSeconds, Is.EqualTo(seconds));
            Assert.That(string.Join(",", level.Hazards.Select(id => (int)id)), Is.EqualTo(hazardIds));
            Assert.That(level.SpawnPoints.Count, Is.EqualTo(6));

            for (var i = 0; i < level.SpawnPoints.Count; i++)
            {
                var spawn = level.SpawnPoints[i];
                var next = level.SpawnPoints[(i + 1) % level.SpawnPoints.Count];
                Assert.That(spawn.y, Is.Zero);
                Assert.That(spawn.magnitude, Is.EqualTo(spawnRadius).Within(0.001f));
                Assert.That(Vector3.Angle(spawn, next), Is.EqualTo(60f).Within(0.01f));
                if (number == 2)
                    Assert.That(Mathf.Abs(Mathf.Abs(spawn.z) - 1.2f), Is.GreaterThan(0.3f));
            }
        }

        [TestCase(1, 4f, .50f, 25f, 35f, 0f, .5f, .85f, 3, BotIceHandling.None, 1f, BotClassTactics.None)]
        [TestCase(2, 5f, .40f, 18f, 30f, 0f, 1f, .90f, 63, BotIceHandling.None, 1f, BotClassTactics.None)]
        [TestCase(3, 6f, .30f, 12f, 25f, .2f, 1.5f, .95f, 127, BotIceHandling.WiderMargin, 1.5f, BotClassTactics.AvoidHeavier)]
        [TestCase(4, 8f, .20f, 7f, 20f, .35f, 2f, 1f, 1023, BotIceHandling.BrakeEarly, 1.5f, BotClassTactics.PositionByClass)]
        [TestCase(5, 10f, .12f, 3f, 15f, .5f, 2.5f, 1f, 2047, BotIceHandling.PredictSlide, 1.5f, BotClassTactics.Predictive)]
        public void BotAsset_MatchesDifficultyLadder(int tier, float rate, float delay, float noise, float alignment,
            float lead, float edge, float speed, int awareness, BotIceHandling ice, float iceMargin, BotClassTactics tactics)
        {
            var bot = AssetDatabase.LoadAssetAtPath<BotProfile>($"Assets/_Project/Data/Bots/D{tier}.asset");
            Assert.That(bot, Is.Not.Null);
            Assert.That(bot.Tier, Is.EqualTo(tier));
            Assert.That(bot.DecisionRateHz, Is.EqualTo(rate));
            Assert.That(bot.ReactionDelaySeconds, Is.EqualTo(delay));
            Assert.That(bot.AimNoiseDegrees, Is.EqualTo(noise));
            Assert.That(bot.ChargeAlignmentDegrees, Is.EqualTo(alignment));
            Assert.That(bot.TargetLeadSeconds, Is.EqualTo(lead));
            Assert.That(bot.EdgeMarginMeters, Is.EqualTo(edge));
            Assert.That(bot.SpeedFactor, Is.EqualTo(speed));
            Assert.That(bot.Targeting, Is.EqualTo(tier == 1 ? BotTargeting.Random : BotTargeting.Utility));
            Assert.That((int)bot.Awareness, Is.EqualTo(awareness));
            Assert.That(bot.IceHandling, Is.EqualTo(ice));
            Assert.That(bot.IceEdgeMarginMultiplier, Is.EqualTo(iceMargin));
            Assert.That(bot.ClassTactics, Is.EqualTo(tactics));
        }
    }
}
