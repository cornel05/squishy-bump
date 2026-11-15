#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-41: 40 s of L2 hazards on PhysX — departure rules and no block ever meets an H07 box.</summary>
    public sealed class PusherBlocksPlayTests
    {
        const string Root = "Assets/_Project";
        GameObject root;

        sealed class Sink : IGameEventSink
        {
            uint id;
            public readonly List<HazardEvent> Events = new();
            public uint NextEventId() => ++id;
            public void Publish(HazardEvent e) => Events.Add(e);
            public void Publish(MatchEvent e) { }
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1f;
            Object.Destroy(root);
            yield return null;
        }

        static void Set(Object target, params (string, Object)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (f, v) in values) so.FindProperty(f).objectReferenceValue = v;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [UnityTest]
        public IEnumerator Pushers_DepartByTheRulesAndNeverMeetABox()
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");
            var hazards = AssetDatabase.LoadAssetAtPath<HazardProfile>($"{Root}/Data/Hazards/HazardProfile.asset");
            root = new GameObject("L2 hazards");
            root.SetActive(false);
            var ground = root.AddComponent<MovingGround>();
            Set(ground, ("balance", balance), ("hazards", hazards));
            var pushers = root.AddComponent<PusherBlocks>();
            Set(pushers, ("balance", balance), ("hazards", hazards), ("ground", ground));
            root.SetActive(true);
            var sink = new Sink();
            pushers.Bind(sink, null);

            var dt = Time.fixedDeltaTime;
            var blockColliders = root.GetComponentsInChildren<BoxCollider>();
            Time.timeScale = 20f;
            for (var tick = 1; tick <= Mathf.RoundToInt(40f / dt); tick++)
            {
                yield return new WaitForFixedUpdate();
                ground.StepOnHost(tick, (uint)tick, dt);
                pushers.StepOnHost(tick, (uint)tick, dt);
                foreach (var a in blockColliders)
                {
                    if (!a.name.StartsWith("Pusher")) continue;
                    // Exact shapes, not axis-aligned bounds (a box on the ring is rotated).
                    foreach (var b in ground.Boxes)
                        Assert.That(UnityEngine.Physics.ComputePenetration(a, a.transform.position, a.transform.rotation,
                                b, b.transform.position, b.transform.rotation, out _, out _),
                            Is.False, $"{a.name} met {b.name} at tick {tick}");
                }
            }

            var departures = sink.Events.FindAll(e => e.Kind == HazardEventKind.PusherDeparture);
            Assert.That(departures.Count, Is.GreaterThanOrEqualTo(4), "blocks keep running");
            Assert.That(departures[0].HostTick, Is.GreaterThanOrEqualTo((uint)Mathf.RoundToInt(5f / dt)), "first at 5 s");
            var a0 = departures.Find(e => e.Index == 0);
            var b0 = departures.Find(e => e.Index == 1);
            Assert.That((a0.Direction, b0.Direction), Is.EqualTo((1, -1)), "A leaves west, B east");
            var pass = Mathf.RoundToInt(PusherBlocks.PassSeconds(hazards) / dt);
            var wait = Mathf.RoundToInt(hazards.PusherDockWaitSeconds / dt);
            for (var block = 0; block < 2; block++)
            {
                var mine = departures.FindAll(e => e.Index == block);
                for (var i = 1; i < mine.Count; i++)
                    Assert.That((int)(mine[i].HostTick - mine[i - 1].HostTick), Is.GreaterThanOrEqualTo(pass + wait),
                        "waits at least 2 s at the dock");
            }
        }
    }
}
#endif
