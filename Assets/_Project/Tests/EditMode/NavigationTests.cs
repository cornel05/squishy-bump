using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.AI;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    public sealed class NavigationTests
    {
        private sealed class World : IBotNavigationSnapshot
        {
            public World(int width, int height)
            {
                Width = width; Height = height;
                Cells = Enumerable.Repeat(new BotTerrainCell(true, false, 10), width * height).ToArray();
            }
            public int Width { get; }
            public int Height { get; }
            public Vector2 Origin => Vector2.zero;
            public BotTerrainCell[] Cells;
            public readonly List<BotHazardWarning> Threats = new();
            public BotTerrainCell Cell(int index) => Cells[index];
            public IReadOnlyList<BotHazardWarning> Warnings => Threats;
            public DangerMap Map(float margin = 2, BotAwareness mask = (BotAwareness)int.MaxValue,
                float ice = 1) => new(this, margin, mask, ice);
        }

        [Test]
        public void Grid_OneMeterAndMaximum26By26()
        {
            var map = new World(26, 26).Map();
            Assert.That(map.Count, Is.EqualTo(676));
            Assert.That(map.Position(1) - map.Position(0), Is.EqualTo(Vector3.right));
            Assert.That(map.CellAt(new Vector3(25, 0, 25)), Is.EqualTo(675));
            Assert.That(map.CellAt(new Vector3(26, 0, 0)), Is.EqualTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new World(27, 26).Map());
        }

        [Test]
        public void Danger_MaxOfEdgeAndTelegraph_FilteredByTierAwareness()
        {
            var w = new World(1, 1); w.Cells[0] = new BotTerrainCell(true, false, 0.5f);
            w.Threats.Add(new BotHazardWarning(BotAwareness.Cannons, BotHazardShape.Circle,
                Vector2.zero, new Vector2(2, 0), 0, 0.2f));
            Assert.That(w.Map(mask: BotAwareness.ArenaEdge)[0], Is.EqualTo(0.75f));
            Assert.That(w.Map(mask: BotAwareness.Cannons)[0], Is.EqualTo(0.9f).Within(0.0001f));
            w.Threats[0] = new BotHazardWarning(BotAwareness.Cannons, BotHazardShape.Circle,
                Vector2.zero, new Vector2(2, 0), 0, 1f);
            Assert.That(w.Map()[0], Is.EqualTo(0.75f), "Lấy max, không cộng các nguy hiểm.");
        }

        [Test]
        public void FallenCellsBlocked_IceWidensEdgeMargin()
        {
            var w = new World(3, 1);
            w.Cells[0] = new BotTerrainCell(false, true, 0);
            w.Cells[1] = new BotTerrainCell(true, true, 1);
            w.Cells[2] = new BotTerrainCell(true, false, 1);
            var map = w.Map(ice: 1.5f);
            Assert.That(map.Walkable(0), Is.False); Assert.That(map[0], Is.EqualTo(1));
            Assert.That(map[1], Is.EqualTo(2f / 3).Within(0.0001f));
            Assert.That(map[2], Is.EqualTo(0.5f));
        }

        [TestCase(BotHazardShape.Circle, 2, 0, 0, 1, 0, true)]
        [TestCase(BotHazardShape.Annulus, 1, 2, 0, 0, 0, false)]
        [TestCase(BotHazardShape.Annulus, 1, 2, 0, 1.5f, 0, true)]
        [TestCase(BotHazardShape.Box, 2, 0.25f, 90, 0, 1, true)]
        [TestCase(BotHazardShape.Box, 2, 0.25f, 90, 1, 0, false)]
        [TestCase(BotHazardShape.Cone, 10, 15, 90, 5, 0, true)]
        [TestCase(BotHazardShape.Cone, 10, 15, 90, 0, 5, false)]
        [TestCase(BotHazardShape.Hex, 2, 0, 0, 0.9f, 0.5f, true)]
        [TestCase(BotHazardShape.Hex, 2, 0, 0, 0.9f, 0.9f, false)]
        public void WarningShapes_MatchRotatedFootprints(BotHazardShape shape, float a, float b,
            float yaw, float x, float z, bool expected)
        {
            var warning = new BotHazardWarning(BotAwareness.TimeToImpact, shape, Vector2.zero,
                new Vector2(a, b), yaw, 1);
            Assert.That(DangerMap.Contains(warning, new Vector3(x, 0, z)), Is.EqualTo(expected));
        }

        [Test]
        public void DiagonalSteps_CostOneAndNeverCutBlockedCorner()
        {
            var finder = new GridPathfinder();
            var path = finder.Find(new World(4, 4).Map(), 0, 15);
            Assert.That(path.Found, Is.True); Assert.That(path.Cost, Is.EqualTo(3));
            Assert.That(path.Cells.Count, Is.EqualTo(4));
            var corner = new World(2, 2);
            corner.Cells[1] = corner.Cells[2] = new BotTerrainCell(false, false, 0);
            Assert.That(finder.Find(corner.Map(), 0, 3).Found, Is.False);
        }

        [Test]
        public void DangerCost_PrefersSafeDetour_ThenReplansWhenFloorFalls()
        {
            var w = new World(5, 3);
            for (var x = 1; x <= 3; x++) w.Cells[5 + x] = new BotTerrainCell(true, false, 0);
            var finder = new GridPathfinder(); var map = w.Map();
            var path = finder.Find(map, 5, 9);
            Assert.That(path.Cost, Is.EqualTo(4));
            Assert.That(path.MeanDanger, Is.EqualTo(0));
            foreach (var index in path.Cells.Skip(1).SkipLast(1)) w.Cells[index] = new BotTerrainCell(false, false, 0);
            var updated = w.Map(); var replanned = finder.Find(updated, 5, 9);
            Assert.That(replanned.Found, Is.True);
            Assert.That(replanned.Cells.All(updated.Walkable), Is.True);
        }

        [Test]
        public void FiftySeededWeightedGrids_AStarEqualsIndependentDijkstra()
        {
            var finder = new GridPathfinder(); var reachable = 0;
            for (var seed = 0; seed < 50; seed++)
            {
                var random = new System.Random(seed); var w = new World(8, 8);
                for (var i = 0; i < 64; i++)
                    w.Cells[i] = new BotTerrainCell(i == 0 || i == 63 || random.NextDouble() > 0.2,
                        false, (float)random.NextDouble() * 2);
                var map = w.Map(); var expected = Dijkstra(map, 0, 63);
                var actual = finder.Find(map, 0, 63);
                Assert.That(actual.Found, Is.EqualTo(!float.IsPositiveInfinity(expected)), $"seed {seed}");
                if (!actual.Found) continue;
                reachable++;
                Assert.That(actual.Cost, Is.EqualTo(expected).Within(0.0001f), $"seed {seed}");
                var independentlySummed = actual.Cells.Skip(1).Sum(i => 1f + 8f * map[i]);
                Assert.That(actual.Cost, Is.EqualTo(independentlySummed).Within(0.0001f));
            }
            Assert.That(reachable, Is.GreaterThan(10));
        }

        private static float Dijkstra(DangerMap map, int start, int goal)
        {
            // Oracle O(V²), không dùng heap/heuristic hoặc code relax của GridPathfinder.
            var distance = Enumerable.Repeat(float.PositiveInfinity, map.Count).ToArray();
            var settled = new bool[map.Count]; distance[start] = 0;
            for (var iteration = 0; iteration < map.Count; iteration++)
            {
                var from = -1;
                for (var i = 0; i < map.Count; i++)
                    if (!settled[i] && map.Walkable(i) && (from < 0 || distance[i] < distance[from])) from = i;
                if (from < 0 || float.IsPositiveInfinity(distance[from])) break;
                if (from == goal) return distance[from];
                settled[from] = true;
                for (var to = 0; to < map.Count; to++)
                {
                    var dx = Math.Abs(from % map.Width - to % map.Width);
                    var dz = Math.Abs(from / map.Width - to / map.Width);
                    if (!map.Walkable(to) || settled[to] || Math.Max(dx, dz) != 1) continue;
                    if (dx == 1 && dz == 1 && (!map.Walkable(map.Index(from % map.Width, to / map.Width)) ||
                        !map.Walkable(map.Index(to % map.Width, from / map.Width)))) continue;
                    distance[to] = Math.Min(distance[to], distance[from] + 1 + 8 * map[to]);
                }
            }
            return distance[goal];
        }

        [Test]
        public void Evade_SafeCellAcrossGapIsIgnored_ChoosesReachableSafeCellByPathCost()
        {
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<BotProfile>("Assets/_Project/Data/Bots/D2.asset");
            var brain = new BotBrain(profile, 1000, 18);
            var world = new World(5, 5);
            for (var i = 0; i < 25; i++) world.Cells[i] = new BotTerrainCell(false, false, 0);
            world.Cells[12] = world.Cells[17] = new BotTerrainCell(true, false, 0);
            world.Cells[10] = world.Cells[22] = new BotTerrainCell(true, false, 10);
            for (var tick = 0; tick < 60; tick++) brain.Step(true, true, true, false, true,
                new Vector3(2, 0, 2), 0, Vector3.zero, 9, Array.Empty<BotFighter>(), 1f / 60, world);
            Assert.That(brain.State, Is.EqualTo(BotState.Evade));
            Assert.That(brain.PlannedPath.Found, Is.True, "Ô gần qua khe không có đường tới không phải mục tiêu né hợp lệ.");
            Assert.That(brain.PlannedPath.Cells.Last(), Is.EqualTo(22));
        }

        [Test]
        public void ApproachPointReached_BotFacesVictimAndChargesInsteadOfWaitingAtWaypoint()
        {
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<BotProfile>("Assets/_Project/Data/Bots/D2.asset");
            var brain = new BotBrain(profile, 1000, 18);
            var world = new World(9, 9);
            var position = new Vector3(3, 0, 3); var yaw = 0f; var charged = false;
            for (var tick = 0; tick < 120; tick++)
            {
                var fighters = new[] { new BotFighter(1000, position, Vector3.zero, true, 2),
                    new BotFighter(1, new Vector3(0, 0, 5), Vector3.zero, true, 1, 10) };
                var command = brain.Step(true, true, true, false, true, position, yaw,
                    Vector3.zero, 9, fighters, 1f / 60, world);
                if (command.Dash) { charged = true; break; }
                // Fixture chỉ tích phân input để kiểm tra hướng nhìn/ý định, không thay cho kiểm thử PlayerMotor.
                position += Quaternion.Euler(0, command.YawDegrees, 0) *
                    new Vector3(command.Move.x, 0, command.Move.y) * (6f / 60);
                yaw = command.YawDegrees;
            }
            Assert.That(brain.Replans, Is.GreaterThan(0));
            Assert.That(charged, Is.True, "Đã đến phía sau mục tiêu thì phải quay nhìn mục tiêu, không nhìn mãi waypoint.");
        }

        [Test]
        public void Utility_ExactGddTermsAndClassVulnerability()
        {
            Assert.That(UtilityTargeting.Score(6, 3, 5, 2, 4, 0.4f), Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(UtilityTargeting.Score(6, 3, 5, 4, 2, 0.4f), Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(UtilityTargeting.Score(6, 3, 5, 2, 2, 0.4f), Is.EqualTo(0.30f).Within(0.0001f));
        }
        [Test]
        public void Utility_Hysteresis015AndEngagementThreshold04()
        {
            var random = new System.Random(18);
            var candidates = new[] { new UtilityTargeting.Candidate(1, 0.6f), new UtilityTargeting.Candidate(2, 0.7499f) };
            Assert.That(UtilityTargeting.Select(candidates, 1, random), Is.EqualTo(1));
            candidates[1] = new UtilityTargeting.Candidate(2, 0.75f);
            Assert.That(UtilityTargeting.Select(candidates, 1, random), Is.EqualTo(2));
            Assert.That(UtilityTargeting.Select(new[] { new UtilityTargeting.Candidate(1, 0.3999f) }, 1, random), Is.Null);
            Assert.That(UtilityTargeting.Select(new[] { new UtilityTargeting.Candidate(2, 0.4f) }, 1, random), Is.EqualTo(2));
        }
    }
}
