#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.AI;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Hazards;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class NavigationArenaTests
    {
        private sealed class EmptyFighters : IFighterPhysics
        {
            public IReadOnlyCollection<ulong> FighterIds { get; } = new ulong[0];
            public Vector3 GetPosition(ulong id) => Vector3.zero;
            public bool TryGetFighter(Collider c, out ulong id) { id = 0; return false; }
            public int GetWobble(ulong id) => 0;
            public void AddImpulse(ulong id, Vector3 impulse) { }
            public void AddWobble(ulong id, int amount) { }
        }

        [UnityTest]
        public IEnumerator L4_OnlyCrackingTilesGiveHints_ActualFallenFloorIsBlocked()
        {
            yield return SceneManager.LoadSceneAsync("L4", LoadSceneMode.Single);
            var ice = Object.FindFirstObjectByType<IceTiles>();
            var balance = AssetDatabase.LoadAssetAtPath<BalanceProfile>("Assets/_Project/Data/Balance/BalanceProfile.asset");
            var world = new BotNavigationWorld(Vector3.zero, balance, new IHazard[] { ice });
            var hints = new List<BotHazardWarning>();
            ice.SetSeed(18); ice.Bind(null, new EmptyFighters());
            ice.CollectBotWarnings(hints, 179, 1f / 60);
            Assert.That(hints, Is.Empty, "Không đọc các ô chưa được host chọn/cảnh báo.");
            for (var t = 1; t <= 180; t++) ice.StepOnHost(t, (uint)t, 1f / 60);
            UnityEngine.Physics.SyncTransforms();
            var before = world.Capture(9, 180, 1f / 60);
            Assert.That(before.Warnings.Count, Is.EqualTo(2));
            Assert.That(before.Warnings.All(h => h.Shape == BotHazardShape.Hex &&
                Mathf.Abs(h.SecondsUntilImpact - 1.5f) < 0.0001f), Is.True);
            var selected = Enumerable.Range(0, 61).Where(i => ice.Field.Stage(i) == IceStage.Cracking).ToArray();
            var initialMap = new DangerMap(before, 1.5f, BotAwareness.IceTiles, 1.5f);
            foreach (var index in selected) Assert.That(initialMap.Walkable(initialMap.CellAt(ice.TileCenter(index))), Is.True);
            for (var t = 181; t <= 270; t++) ice.StepOnHost(t, (uint)t, 1f / 60);
            var fighter = new GameObject("Fighter collider above a real ice hole");
            fighter.layer = LayerMask.NameToLayer("Fighter");
            fighter.transform.position = ice.TileCenter(selected[0]) + Vector3.up * 3;
            fighter.AddComponent<BoxCollider>().size = Vector3.one * 2;
            try
            {
                UnityEngine.Physics.SyncTransforms();
                var after = world.Capture(9, 270, 1f / 60);
                var map = new DangerMap(after, 1.5f, BotAwareness.IceTiles, 1.5f);
                foreach (var index in selected)
                {
                    var cell = map.CellAt(ice.TileCenter(index));
                    Assert.That(map.Walkable(cell), Is.False,
                        $"tile {index}: collider đã tắt thì navigation phải chặn, không nhận fighter làm sàn");
                }
            }
            finally { Object.Destroy(fighter); }
        }

        [UnityTest]
        public IEnumerator L2_ReversalBoxesAndPusherLanes_ExposeVisibleWarningsWithinGridBudget()
        {
            yield return SceneManager.LoadSceneAsync("L2", LoadSceneMode.Single);
            var ground = Object.FindFirstObjectByType<MovingGround>();
            var pushers = Object.FindFirstObjectByType<PusherBlocks>();
            var balance = AssetDatabase.LoadAssetAtPath<BalanceProfile>("Assets/_Project/Data/Balance/BalanceProfile.asset");
            var profile = AssetDatabase.LoadAssetAtPath<HazardProfile>("Assets/_Project/Data/Hazards/HazardProfile.asset");
            var bounds = Object.FindFirstObjectByType<SquishyBump.Match.TrainingRingBounds>();
            var hints = new List<BotHazardWarning>(); var sawLane = false;
            for (var t = 1; t <= 1800; t++)
            {
                ground.StepOnHost(t, (uint)t, 1f / 60); pushers.StepOnHost(t, (uint)t, 1f / 60);
                if (t == 780)
                {
                    var reversal = new List<BotHazardWarning>();
                    ground.CollectBotWarnings(reversal, t, 1f / 60);
                    Assert.That(reversal.Count(h => h.Awareness == BotAwareness.ObstacleBoxes), Is.EqualTo(3));
                    Assert.That(reversal.Any(h => h.Awareness == BotAwareness.TurntableReversal &&
                        Mathf.Abs(h.SecondsUntilImpact - 2) < 0.0001f), Is.True);
                }
                hints.Clear(); pushers.CollectBotWarnings(hints, t, 1f / 60);
                if (hints.Count == 0) continue;
                sawLane = true;
                Assert.That(hints.All(h => h.Shape == BotHazardShape.Box && h.Awareness == BotAwareness.PusherLanes &&
                    Mathf.Abs(Mathf.Abs(h.Center.y) - profile.PusherLaneZ) < 0.0001f &&
                    h.SecondsUntilImpact >= 0 && h.SecondsUntilImpact <= profile.PusherLightSeconds + 0.0001f), Is.True);
            }
            Assert.That(sawLane, Is.True);
            UnityEngine.Physics.SyncTransforms();
            var snapshot = new BotNavigationWorld(bounds.transform.position, balance, new IHazard[] { ground, pushers })
                .Capture(bounds.OuterRadius, 1800, 1f / 60);
            Assert.That(snapshot.Width, Is.LessThanOrEqualTo(26));
            Assert.That(snapshot.Height, Is.LessThanOrEqualTo(26));
        }

        [UnityTest]
        public IEnumerator L3_FireTargetOnlyVisibleAfterLaunch_AirConeAndBurnUseActualState()
        {
            yield return SceneManager.LoadSceneAsync("L3", LoadSceneMode.Single);
            var cannons = Object.FindFirstObjectByType<Cannons>();
            cannons.Bind(null, new EmptyFighters());
            var hints = new List<BotHazardWarning>();
            for (var t = 1; t <= 270; t++) cannons.StepOnHost(t, (uint)t, 1f / 60);
            cannons.CollectBotWarnings(hints, 270, 1f / 60);
            Assert.That(hints.Any(h => h.Shape == BotHazardShape.Circle), Is.False,
                "Nòng pháo đang sáng nhưng marker chưa hiện: không lộ điểm rơi cho bot.");
            for (var t = 271; t <= 300; t++) cannons.StepOnHost(t, (uint)t, 1f / 60);
            hints.Clear(); cannons.CollectBotWarnings(hints, 300, 1f / 60);
            Assert.That(hints.Any(h => h.Shape == BotHazardShape.Circle && h.SecondsUntilImpact > 0), Is.True);
            for (var t = 301; t <= 400; t++) cannons.StepOnHost(t, (uint)t, 1f / 60);
            hints.Clear(); cannons.CollectBotWarnings(hints, 400, 1f / 60);
            Assert.That(hints.Any(h => h.Awareness == BotAwareness.Cannons && h.Shape == BotHazardShape.Cone), Is.True);
            Assert.That(hints.Any(h => h.Awareness == BotAwareness.BurningPatches && h.SecondsUntilImpact == 0), Is.True);
        }
    }
}
#endif
