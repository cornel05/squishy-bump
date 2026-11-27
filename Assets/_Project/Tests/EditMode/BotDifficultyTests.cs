using System;
using System.Collections.Generic;
using NUnit.Framework;
using SquishyBump.AI;
using SquishyBump.Core;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    public sealed class BotDifficultyTests
    {
        [TestCase(1, 4)] [TestCase(2, 5)] [TestCase(3, 6)] [TestCase(4, 8)] [TestCase(5, 10)]
        public void Fixed60HzClock_MaintainsConfiguredAverageDecisionRate(int tier, int rate)
        {
            var profile = AssetDatabase.LoadAssetAtPath<BotProfile>($"Assets/_Project/Data/Bots/D{tier}.asset");
            var bot = new BotBrain(profile, 1000, 18);
            void Tick() => bot.Step(true, true, true, false, false, Vector3.zero, 0,
                Vector3.zero, 9, Array.Empty<BotFighter>(), 1f / 60);
            for (var i = 0; i < 60; i++) Tick();
            var before = bot.Decisions;
            for (var i = 0; i < 600; i++) Tick();
            Assert.That(bot.Decisions - before, Is.InRange(rate * 10 - 1, rate * 10 + 1),
                $"D{tier}: phải giữ trung bình {rate} Hz, không bỏ phần dư mỗi lần quyết định.");
        }

        private sealed class Floor : IBotNavigationSnapshot
        {
            public int Width => 17;
            public int Height => 17;
            public Vector2 Origin => new(-8, -8);
            public bool Ice;
            public BotTerrainCell Cell(int index) => new(true, Ice, 10);
            public IReadOnlyList<BotHazardWarning> Warnings => Array.Empty<BotHazardWarning>();
        }

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>($"Assets/_Project/Data/{path}.asset");

        [TestCase(2, true)] [TestCase(3, false)] [TestCase(4, false)] [TestCase(5, false)]
        public void HeavierTargetFacingBot_D3AndAboveRefuseHeadOnCharge(int tier, bool expectedDash)
        {
            var profile = Load<BotProfile>($"Bots/D{tier}");
            var own = Load<ClassProfile>("Classes/Normal");
            var brain = new BotBrain(profile, 1000, 18, own, Load<BalanceProfile>("Balance/BalanceProfile"));
            var fighters = new[] {
                new BotFighter(1000, Vector3.zero, Vector3.zero, true, own.Mass),
                new BotFighter(1001, Vector3.forward * 2, Vector3.zero, true, 15, 10, 180) };
            var dashed = false;
            for (var i = 0; i < 90; i++)
                dashed |= brain.Step(true, true, true, false, true, Vector3.zero, 0,
                    Vector3.zero, 9, fighters, 1f / 60, new Floor()).Dash;
            Assert.That(brain.Target, Is.EqualTo(1001), "Vẫn chọn đối thủ, không giả vờ loại khỏi cảm nhận.");
            Assert.That(dashed, Is.EqualTo(expectedDash));
        }

        [TestCase(4, BotState.Chase)] [TestCase(5, BotState.Evade)]
        public void IceOutwardMomentum_D5PlansStoppingPointBeforeLeavingGrid(int tier, BotState expected)
        {
            var own = Load<ClassProfile>("Classes/Normal");
            var brain = new BotBrain(Load<BotProfile>($"Bots/D{tier}"), 1000, 18, own,
                Load<BalanceProfile>("Balance/BalanceProfile"));
            var position = new Vector3(6, 0, 0);
            var fighters = new[] { new BotFighter(1000, position, Vector3.right * 6, true, own.Mass),
                new BotFighter(1001, new Vector3(5, 0, 0), Vector3.zero, true, own.Mass, 10) };
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, position, 0,
                Vector3.zero, 9, fighters, 1f / 60, new Floor { Ice = true });
            Assert.That(brain.State, Is.EqualTo(expected));
        }

        [TestCase("Normal", FruitWeightClass.Normal)]
        [TestCase("Heavy", FruitWeightClass.Heavy)]
        [TestCase("Light", FruitWeightClass.Light)]
        public void ClassAssets_DeclareCategoryWithoutMassThresholds(string name, FruitWeightClass category) =>
            Assert.That(Load<ClassProfile>($"Classes/{name}").Category, Is.EqualTo(category));

        [Test]
        public void ReachedMappedGoal_GridQuantizationDoesNotTriggerFalseStuckRecovery()
        {
            var brain = new BotBrain(Load<BotProfile>("Bots/D1"), 1000, 18);
            // Host grid của sàn đường kính chẵn có tâm ô lệch 0,5 m so với tâm sàn.
            var position = new Vector3(-0.5f, 0, -0.5f);
            var floor = new HalfCellFloor();
            for (var i = 0; i < 360; i++) brain.Step(true, true, true, false, false, position, 0,
                Vector3.zero, 9, Array.Empty<BotFighter>(), 1f / 60, floor);
            Assert.That(brain.StuckRecoveries, Is.Zero,
                "Đứng đúng tâm ô đích A* phải được tính đã tới nơi, không rút mục tiêu ngẫu nhiên ra mép sàn.");
        }

        private sealed class HalfCellFloor : IBotNavigationSnapshot
        {
            public int Width => 18;
            public int Height => 18;
            public Vector2 Origin => new(-8.5f, -8.5f);
            public BotTerrainCell Cell(int index) => new(true, false, 10);
            public IReadOnlyList<BotHazardWarning> Warnings => Array.Empty<BotHazardWarning>();
        }

        [Test]
        public void NoCellBelowSafeThreshold_EvadeStillLeavesImminentHazardAlongReachablePath()
        {
            var position = Vector3.right;
            var brain = new BotBrain(Load<BotProfile>("Bots/D3"), 1000, 18);
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, position, 0,
                position, 9, Array.Empty<BotFighter>(), 1f / 60, new NarrowIceFloor());
            Assert.That(brain.State, Is.EqualTo(BotState.Evade));
            Assert.That(brain.PlannedPath.Found, Is.True);
            var end = brain.PlannedPath.Cells[brain.PlannedPath.Cells.Count - 1];
            Assert.That(brain.Navigation[end], Is.LessThan(brain.Navigation[brain.Navigation.CellAt(position)]),
                "Không có D<0.2 không có nghĩa là quay về tâm sàn đang bị cảnh báo.");
        }

        private sealed class NarrowIceFloor : IBotNavigationSnapshot
        {
            public int Width => 7;
            public int Height => 1;
            public Vector2 Origin => Vector2.zero;
            public BotTerrainCell Cell(int index) => new(true, true, 1.5f);
            public IReadOnlyList<BotHazardWarning> Warnings { get; } = new[] {
                new BotHazardWarning(BotAwareness.IceTiles, BotHazardShape.Circle,
                    Vector2.right, new Vector2(0.5f, 0), 0, 0.1f) };
        }

        [Test]
        public void DelayedKnownWarning_ImpactClockKeepsAdvancingWhilePerceptionIsDelayed()
        {
            var floor = new TimedFloor();
            var brain = new BotBrain(Load<BotProfile>("Bots/D3"), 1000, 18);
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, Vector3.right, 0,
                Vector3.right, 9, Array.Empty<BotFighter>(), 1f / 60, floor, playingSeconds: 1);
            Assert.That(brain.Navigation[1], Is.EqualTo(0.75f).Within(0.0001f),
                "Cảnh báo đã biết từ t=0, còn 1,5 s khi chụp: tới t=1 chỉ còn 0,5 s.");
            Assert.That(brain.State, Is.EqualTo(BotState.Evade));
        }

        [Test]
        public void Roam_KnownNonUrgentWarningAtCenter_SelectsLowerDangerGoalBeforeEvadeThreshold()
        {
            var brain = new BotBrain(Load<BotProfile>("Bots/D3"), 1000, 18);
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, Vector3.right, 0,
                Vector3.right, 9, Array.Empty<BotFighter>(), 1f / 60, new TimedFloor(), playingSeconds: 0);
            Assert.That(brain.State, Is.EqualTo(BotState.Roam), "D=0.25 chưa cần đổi ngưỡng Evade 0.5.");
            Assert.That(brain.PlannedPath.Found, Is.True);
            var end = brain.PlannedPath.Cells[brain.PlannedPath.Cells.Count - 1];
            Assert.That(brain.Navigation[end], Is.LessThan(0.2f),
                "A.2 yêu cầu Roam tới ô ít nguy hiểm gần tâm, không cố giữ tâm sàn đang được cảnh báo.");
        }

        [TestCase(4, BotState.Roam)] [TestCase(5, BotState.Evade)]
        public void IceDeadlineShorterThanEscapeTime_D5AnticipatesImpactBeforeRawDangerThreshold(int tier, BotState expected)
        {
            var own = Load<ClassProfile>("Classes/Heavy");
            var brain = new BotBrain(Load<BotProfile>($"Bots/D{tier}"), 1000, 18, own,
                Load<BalanceProfile>("Balance/BalanceProfile"));
            var self = new[] { new BotFighter(1000, Vector3.right, Vector3.zero, true, own.Mass) };
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, Vector3.right, 0,
                Vector3.right, 9, self, 1f / 60, new TimedFloor(1.1f, true), playingSeconds: 0);
            Assert.That(brain.Navigation[1], Is.EqualTo(0.45f).Within(0.0001f), "Không thay công thức danger A.4.");
            Assert.That(brain.State, Is.EqualTo(expected),
                "Quả nặng trên băng cần hơn 1,1 s để thoát; D5 phải dùng TTI, D4 giữ phản ứng theo danger.");
        }

        [Test]
        public void D5_EdgeDangerPlateau_DoesNotUseCurrentWarnedCellAsZeroLengthEscape()
        {
            var own = Load<ClassProfile>("Classes/Heavy");
            var brain = new BotBrain(Load<BotProfile>("Bots/D5"), 1000, 18, own,
                Load<BalanceProfile>("Balance/BalanceProfile"));
            var self = new[] { new BotFighter(1000, Vector3.right, Vector3.zero, true, own.Mass) };
            var floor = new TimedFloor(1.1f, true, 1.5f);
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, Vector3.right, 0,
                Vector3.right, 9, self, 1f / 60, floor, playingSeconds: 0);
            Assert.That(brain.PlannedPath.Found, Is.True);
            var end = brain.Navigation.Position(brain.PlannedPath.Cells[brain.PlannedPath.Cells.Count - 1]);
            Assert.That(DangerMap.ContainsExpanded(floor.Warnings[0], end, own.Radius), Is.False,
                "Edge danger masks a new warning: a zero-length plan must not count as an escape for TTI.");
        }

        [TestCase(3, false)] [TestCase(4, true)]
        public void Heavy_D4HoldsCenterInsteadOfFollowingDistantTarget(int tier, bool holds)
        {
            var own = Load<ClassProfile>("Classes/Heavy");
            var brain = new BotBrain(Load<BotProfile>($"Bots/D{tier}"), 1000, 18, own, Load<BalanceProfile>("Balance/BalanceProfile"));
            var fighters = new[] { new BotFighter(1000, Vector3.zero, Vector3.zero, true, own.Mass),
                new BotFighter(1001, Vector3.right * 6, Vector3.zero, true, 9, 10) };
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, Vector3.zero, 0,
                Vector3.zero, 9, fighters, 1f / 60, new Floor());
            var end = brain.Navigation.Position(brain.PlannedPath.Cells[brain.PlannedPath.Cells.Count - 1]);
            Assert.That(end.magnitude < 1, Is.EqualTo(holds));
        }

        [TestCase(3, false)] [TestCase(4, true)]
        public void Light_D4ApproachesFromFlankAndKeepsDistance(int tier, bool flanks)
        {
            var own = Load<ClassProfile>("Classes/Light");
            var brain = new BotBrain(Load<BotProfile>($"Bots/D{tier}"), 1000, 18, own, Load<BalanceProfile>("Balance/BalanceProfile"));
            var fighters = new[] { new BotFighter(1000, Vector3.zero, Vector3.zero, true, own.Mass),
                new BotFighter(1001, Vector3.right * 2, Vector3.zero, true, 9, 10) };
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, Vector3.zero, 0,
                Vector3.zero, 9, fighters, 1f / 60, new Floor());
            var end = brain.Navigation.Position(brain.PlannedPath.Cells[brain.PlannedPath.Cells.Count - 1]);
            Assert.That(Mathf.Abs(end.z) >= 2, Is.EqualTo(flanks));
        }

        [Test]
        public void D5_BaitsHeavierTargetAcrossKnownHazardWhileStandingOutsideIt()
        {
            var own = Load<ClassProfile>("Classes/Normal");
            var brain = new BotBrain(Load<BotProfile>("Bots/D5"), 1000, 18, own, Load<BalanceProfile>("Balance/BalanceProfile"));
            var floor = new BaitFloor();
            var fighters = new[] { new BotFighter(1000, Vector3.zero, Vector3.zero, true, own.Mass),
                new BotFighter(1001, Vector3.right * 2, Vector3.zero, true, 15, 10) };
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, Vector3.zero, 0,
                Vector3.zero, 9, fighters, 1f / 60, floor);
            var end = brain.Navigation.Position(brain.PlannedPath.Cells[brain.PlannedPath.Cells.Count - 1]);
            Assert.That(end.x, Is.LessThan(0));
            Assert.That(DangerMap.Contains(floor.Warnings[0], end), Is.False);
            Assert.That(DangerMap.Contains(floor.Warnings[0], end + (end - fighters[1].Position).normalized * 3.5f), Is.True);
        }

        private sealed class BaitFloor : IBotNavigationSnapshot
        {
            public int Width => 17;
            public int Height => 17;
            public Vector2 Origin => new(-8, -8);
            public BotTerrainCell Cell(int index) => new(true, false, 10);
            public IReadOnlyList<BotHazardWarning> Warnings { get; } = new[] {
                new BotHazardWarning(BotAwareness.Cannons, BotHazardShape.Box, new Vector2(-4.5f, 0),
                    new Vector2(0.5f, 0.5f), 0, 1) };
        }

        [TestCase(4, true)] [TestCase(5, false)]
        public void NearTimeLimit_D5StopsChargingToPreserveWobble(int tier, bool attacks)
        {
            var own = Load<ClassProfile>("Classes/Normal");
            var brain = new BotBrain(Load<BotProfile>($"Bots/D{tier}"), 1000, 18, own, Load<BalanceProfile>("Balance/BalanceProfile"));
            var fighters = new[] { new BotFighter(1000, Vector3.zero, Vector3.zero, true, own.Mass),
                new BotFighter(1001, Vector3.right * 2, Vector3.zero, true, own.Mass, 10) };
            var dashed = false;
            for (var i = 0; i < 90; i++) dashed |= brain.Step(true, true, true, false, true, Vector3.zero, 0,
                Vector3.zero, 9, fighters, 1f / 60, new Floor(), 5, 10).Dash;
            Assert.That(dashed, Is.EqualTo(attacks));
        }

        [TestCase(4, true)] [TestCase(5, false)]
        public void DashNearRim_D5RefusesImpulseWhoseStoppingPathLeavesKnownFloor(int tier, bool attacks)
        {
            var own = Load<ClassProfile>("Classes/Normal");
            var brain = new BotBrain(Load<BotProfile>($"Bots/D{tier}"), 1000, 18, own, Load<BalanceProfile>("Balance/BalanceProfile"));
            var position = Vector3.right * 5;
            var fighters = new[] { new BotFighter(1000, position, Vector3.right * 4, true, own.Mass),
                new BotFighter(1001, Vector3.right * 6.5f, Vector3.zero, true, own.Mass, 10) };
            var dashed = false;
            for (var i = 0; i < 90; i++) dashed |= brain.Step(true, true, true, false, true, position, 90,
                Vector3.zero, 9, fighters, 1f / 60, new Floor()).Dash;
            Assert.That(dashed, Is.EqualTo(attacks));
        }

        [TestCase(3, false)] [TestCase(4, true)] [TestCase(5, true)]
        public void ObservedApproachingBox_D4AndD5JumpBeforeContact_EliminatedBotSendsNoJump(int tier, bool jumps)
        {
            var own = Load<ClassProfile>("Classes/Normal");
            var brain = new BotBrain(Load<BotProfile>($"Bots/D{tier}"), 1000, 18, own, Load<BalanceProfile>("Balance/BalanceProfile"));
            var fighters = new[] { new BotFighter(1000, Vector3.zero, Vector3.zero, true, own.Mass) };
            var jumpedBeforeContact = false;
            for (var i = 0; i < 30; i++)
            {
                var command = brain.Step(true, true, true, false, false, Vector3.zero, 0,
                    Vector3.zero, 9, fighters, 1f / 60, new ApproachingBoxFloor(i / 60f), playingSeconds: i / 60f);
                jumpedBeforeContact |= command.Jump;
                if (command.Jump)
                {
                    Assert.That(brain.Step(true, true, false, false, false, Vector3.zero, 0,
                        Vector3.zero, 9, fighters, 1f / 60).Jump, Is.False);
                    break;
                }
            }
            Assert.That(jumpedBeforeContact, Is.EqualTo(jumps), "Box starts 2m away and moves at 2m/s; physical contact starts at 0.5s.");
        }

        private sealed class ApproachingBoxFloor : ITimedBotNavigationSnapshot
        {
            public ApproachingBoxFloor(float time)
            {
                CapturedAtSeconds = time;
                Warnings = new[] { new BotHazardWarning(BotAwareness.ObstacleBoxes, BotHazardShape.Box,
                    new Vector2(2 - 2 * time, 0), new Vector2(0.5f, 0.5f), 0, 0) };
            }
            public int Width => 17;
            public int Height => 17;
            public Vector2 Origin => new(-8, -8);
            public float CapturedAtSeconds { get; }
            public BotTerrainCell Cell(int index) => new(true, false, 10);
            public IReadOnlyList<BotHazardWarning> Warnings { get; }
        }

        [Test]
        public void D5_FrontalPredictionCrossesSelf_SidestepPreservesExistingLateralMomentum()
        {
            var own = Load<ClassProfile>("Classes/Normal");
            var position = Vector3.forward;
            var brain = new BotBrain(Load<BotProfile>("Bots/D5"), 1000, 18, own, Load<BalanceProfile>("Balance/BalanceProfile"));
            var fighters = new[] { new BotFighter(1000, position, Vector3.forward * 3, true, own.Mass),
                new BotFighter(1001, position + Vector3.right * 2, Vector3.left * 6, true, own.Mass, 10) };
            var sawDash = false;
            for (var i = 0; i < 90; i++)
            {
                var command = brain.Step(true, true, true, false, true, position, 90,
                    Vector3.zero, 9, fighters, 1f / 60, new Floor());
                if (!command.Dash) continue;
                sawDash = true;
                Assert.That(Vector3.Dot(Quaternion.Euler(0, command.YawDegrees, 0) * Vector3.forward, Vector3.forward), Is.GreaterThan(0.9f));
                break;
            }
            Assert.That(sawDash, Is.True);
        }

        [TestCase(1)] [TestCase(5)]
        public void MovingFloorAtGoal_RealMotorHoldsWorldPositionByCompensatingSurfaceMotion(int tier)
        {
            var own = Load<ClassProfile>("Classes/Normal");
            var balance = Load<BalanceProfile>("Balance/BalanceProfile");
            var brain = new BotBrain(Load<BotProfile>($"Bots/D{tier}"), 1000, 18, own, balance);
            var command = default(InputCommand);
            for (var i = 0; i < 60; i++) command = brain.Step(true, true, true, false, false,
                Vector3.zero, 0, Vector3.zero, 9, Array.Empty<BotFighter>(), 1f / 60, new MovingFloor());
            var state = default(MotorState);
            PlayerMotor.Tick(ref state, command, new GroundInfo(balance.FloorFriction, Vector3.right * 2), balance, own, 1f / 60);
            Assert.That(state.Velocity.x, Is.EqualTo(0).Within(0.0001f),
                "Reached a world-space safe cell: empty relative input would let the turntable carry the bot back into a pusher lane.");
        }

        private sealed class MovingFloor : IBotNavigationSnapshot
        {
            public int Width => 1;
            public int Height => 1;
            public Vector2 Origin => Vector2.zero;
            public BotTerrainCell Cell(int index) => new(true, false, 10, Vector2.right * 2);
            public IReadOnlyList<BotHazardWarning> Warnings => Array.Empty<BotHazardWarning>();
        }

        [TestCase(3, BotState.Chase)] [TestCase(4, BotState.Evade)] [TestCase(5, BotState.Evade)]
        public void ApproachingLivePusherLane_D4AndD5BrakeBeforeTheirFootprintEnters(int tier, BotState expected)
        {
            var own = Load<ClassProfile>("Classes/Normal");
            var brain = new BotBrain(Load<BotProfile>($"Bots/D{tier}"), 1000, 18, own, Load<BalanceProfile>("Balance/BalanceProfile"));
            var fighters = new[] { new BotFighter(1000, Vector3.zero, Vector3.right * 4, true, own.Mass),
                new BotFighter(1001, Vector3.right * 4, Vector3.zero, true, own.Mass, 10) };
            var floor = new PusherFloor();
            Assert.That(DangerMap.ContainsExpanded(floor.Warnings[0], Vector3.zero, own.Radius), Is.False);
            for (var i = 0; i < 60; i++) brain.Step(true, true, true, false, false, Vector3.zero, 0,
                Vector3.zero, 9, fighters, 1f / 60, floor);
            Assert.That(brain.State, Is.EqualTo(expected));
        }

        private sealed class PusherFloor : IBotNavigationSnapshot
        {
            public int Width => 17;
            public int Height => 17;
            public Vector2 Origin => new(-8, -8);
            public BotTerrainCell Cell(int index) => new(true, false, 10);
            public IReadOnlyList<BotHazardWarning> Warnings { get; } = new[] {
                new BotHazardWarning(BotAwareness.PusherLanes, BotHazardShape.Box, new Vector2(2, 0),
                    new Vector2(0.5f, 9), 0, 0) };
        }

        private sealed class TimedFloor : ITimedBotNavigationSnapshot
        {
            private readonly bool ice;
            private readonly float edge;
            public TimedFloor(float tau = 1.5f, bool ice = false, float edge = 10)
            {
                this.ice = ice; this.edge = edge;
                Warnings = new[] { new BotHazardWarning(BotAwareness.IceTiles, BotHazardShape.Circle,
                    Vector2.right, new Vector2(0.5f, 0), 0, tau) };
            }
            public int Width => 7;
            public int Height => 1;
            public Vector2 Origin => Vector2.zero;
            public float CapturedAtSeconds => 0;
            public BotTerrainCell Cell(int index) => new(true, ice, edge);
            public IReadOnlyList<BotHazardWarning> Warnings { get; }
        }

        [TestCase(BotHazardShape.Circle, 2, 0, 2.4f, 0)]
        [TestCase(BotHazardShape.Annulus, 2, 3, 1.6f, 0)]
        [TestCase(BotHazardShape.Box, 2, 1, 2.4f, 0)]
        [TestCase(BotHazardShape.Hex, 2, 0, 1.4f, 0)]
        [TestCase(BotHazardShape.Cone, 3, 10, 0.7f, 2)]
        public void GoalFootprint_DoesNotTreatCenterOutsideWarningAsEntireFruitSafe(
            BotHazardShape shape, float a, float b, float x, float z)
        {
            var warning = new BotHazardWarning(BotAwareness.Cannons, shape, Vector2.zero,
                new Vector2(a, b), 0, 1);
            var point = new Vector3(x, 0, z);
            Assert.That(DangerMap.Contains(warning, point), Is.False);
            Assert.That(DangerMap.ContainsExpanded(warning, point, 0.5f), Is.True,
                "Tâm quả ngoài vùng nhưng bán kính 0,5 m vẫn cắt biên cảnh báo.");
        }
    }
}
