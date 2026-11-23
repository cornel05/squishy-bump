using System.Linq;
using NUnit.Framework;
using SquishyBump.AI;
using SquishyBump.Core;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    public sealed class BotBrainTests
    {
        private const float Dt = 1f / 60;
        private static BotProfile D1 => AssetDatabase.LoadAssetAtPath<BotProfile>("Assets/_Project/Data/Bots/D1.asset");
        private static readonly BotFighter[] Targets = { new(2, Vector3.forward * 8, Vector3.zero, true),
            new(3, Vector3.right * 8, Vector3.zero, true) };

        private static InputCommand Tick(BotBrain brain, Vector3 position, bool host = true, bool playing = true,
            bool alive = true, bool dashing = false, BotFighter[] targets = null) =>
            brain.Step(host, playing, alive, dashing, true, position, 0, Vector3.zero, 12, targets ?? Targets, Dt);

        [Test]
        public void AuthorityCountdownAndElimination_ProduceNoInput()
        {
            var bot = new BotBrain(D1, 1001, 19);
            Assert.That(Tick(bot, Vector3.zero, host: false).Move, Is.EqualTo(Vector2.zero));
            Assert.That(bot.State, Is.EqualTo(BotState.Countdown));
            Assert.That(Tick(bot, Vector3.zero, playing: false).Dash, Is.False);
            Tick(bot, Vector3.zero, alive: false);
            Assert.That(bot.State, Is.EqualTo(BotState.Eliminated));
            Tick(bot, Vector3.zero);
            Assert.That(bot.State, Is.EqualTo(BotState.Roam));
        }

        [Test]
        public void D1_ReactionDelayRetargetCadenceAndSeed_AreRespected()
        {
            var a = new BotBrain(D1, 1001, 19);
            var b = new BotBrain(D1, 1001, 19);
            for (var i = 0; i < 30; i++)
            {
                Assert.That(Tick(a, Vector3.zero).Move, Is.EqualTo(Tick(b, Vector3.zero).Move));
                Assert.That(a.Target, Is.Null, "0.5 s cảm nhận cũ trước khi chọn mục tiêu");
            }
            Tick(a, Vector3.zero); Tick(b, Vector3.zero);
            var first = a.Target;
            Assert.That(first, Is.Not.Null);
            var direction = Targets.Single(f => f.Id == first).Position.normalized;
            for (var i = 0; i < 230; i++)
            {
                var position = direction * (i * 0.01f);
                var ca = Tick(a, position); var cb = Tick(b, position);
                Assert.That((ca.Move, ca.Dash, ca.YawDegrees), Is.EqualTo((cb.Move, cb.Dash, cb.YawDegrees)));
                Assert.That(a.Target, Is.EqualTo(first));
            }
            Assert.That(a.TargetSelections, Is.EqualTo(1), "Chưa đủ 4 s từ lần chọn đầu");
            for (var i = 230; i < 260; i++)
            {
                var position = direction * (i * 0.01f);
                var ca = Tick(a, position); var cb = Tick(b, position);
                Assert.That((ca.Move, ca.Dash, ca.YawDegrees), Is.EqualTo((cb.Move, cb.Dash, cb.YawDegrees)));
            }
            Assert.That(a.TargetSelections, Is.EqualTo(2), "Chọn lại sau chu kỳ 4 s ở nhịp quyết định D1");
        }

        [Test]
        public void StationaryBot_RecoversToRandomSafeRoamGoal()
        {
            var bot = new BotBrain(D1, 1001, 19);
            for (var i = 0; i < 210; i++) Tick(bot, Vector3.zero);
            Assert.That(bot.StuckRecoveries, Is.GreaterThan(0));
            Assert.That(bot.State, Is.EqualTo(BotState.Roam));
        }

        [Test]
        public void BotMakingProgress_DoesNotTriggerAntiStuckFromTheOldRoamGoal()
        {
            var bot = new BotBrain(D1, 1001, 19);
            for (var i = 0; i < 40; i++) Tick(bot, Vector3.zero);
            var target = Targets.Single(f => f.Id == bot.Target);
            for (var i = 1; i <= 180; i++) Tick(bot, target.Position.normalized * (i * 0.015f));
            Assert.That(bot.StuckRecoveries, Is.Zero);
        }

        [Test]
        public void EdgeDanger_OverridesChaseAndSteersInward()
        {
            var bot = new BotBrain(D1, 1001, 19);
            InputCommand input = default;
            for (var i = 0; i < 50; i++) input = Tick(bot, new Vector3(11.9f, 0, 0));
            Assert.That(bot.State, Is.EqualTo(BotState.Evade));
            var world = Quaternion.Euler(0, input.YawDegrees, 0) * new Vector3(input.Move.x, 0, input.Move.y);
            Assert.That(world.x, Is.LessThan(0));
            Assert.That(input.Move.magnitude, Is.EqualTo(D1.SpeedFactor).Within(0.001f));
        }

        [Test]
        public void NearbyAlignedTarget_DashesThenBrakesToRecover()
        {
            var bot = new BotBrain(D1, 1001, 19);
            var nearby = new[] { new BotFighter(2, Vector3.forward * 2, Vector3.zero, true) };
            InputCommand input = default;
            for (var i = 0; i < 50 && !input.Dash; i++) input = Tick(bot, Vector3.zero, targets: nearby);
            Assert.That(input.Dash, Is.True);
            Assert.That(bot.State, Is.EqualTo(BotState.Charge));
            Tick(bot, Vector3.zero, dashing: true, targets: nearby);
            Tick(bot, Vector3.zero, targets: nearby);
            Assert.That(bot.State, Is.EqualTo(BotState.Recover));
        }
    }
}
