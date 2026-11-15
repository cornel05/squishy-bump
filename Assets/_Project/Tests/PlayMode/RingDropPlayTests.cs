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
    /// <summary>SB-15: the TrainingRingFloor prefab with a fruit on the outer band.</summary>
    public sealed class RingDropPlayTests
    {
        const string Root = "Assets/_Project";
        GameObject floor;
        GameObject fighter;

        sealed class Sink : IGameEventSink
        {
            uint id;
            public readonly List<HazardEvent> Hazards = new();
            public uint NextEventId() => ++id;
            public void Publish(HazardEvent e) => Hazards.Add(e);
            public void Publish(MatchEvent e) { }
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Object.Destroy(floor);
            Object.Destroy(fighter);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OuterBand_WarnsThreeSecondsAheadThenDropsWithFruit()
        {
            floor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Hazards/TrainingRingFloor.prefab"));
            var drop = floor.GetComponent<RingDrop>();
            Assert.That(drop.BandCount, Is.EqualTo(4));
            var sink = new Sink();
            drop.Bind(sink);
            var onOuter = new Vector3(10.5f, 0.5f, 0f);
            var onInner = new Vector3(4.5f, 0.5f, 0f);
            fighter = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/Apple.prefab"),
                onOuter, Quaternion.identity);
            var inner = Object.Instantiate(fighter, onInner, Quaternion.identity);
            var dt = Time.fixedDeltaTime;

            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            drop.StepOnHost(2519, 100, dt);
            Assert.That(sink.Hazards, Is.Empty, "no warning before 42 s");
            drop.StepOnHost(2520, 101, dt);
            Assert.That(sink.Hazards, Has.Count.EqualTo(1));
            Assert.That(sink.Hazards[0].Kind, Is.EqualTo(HazardEventKind.RingDropWarning));
            Assert.That(sink.Hazards[0].Index, Is.EqualTo(0));
            Assert.That(sink.Hazards[0].HostTick, Is.EqualTo(101u + 180u), "event carries the drop tick");
            drop.StepOnHost(2699, 280, dt);
            Assert.That(drop.IsDropped(0), Is.False);

            drop.StepOnHost(2700, 281, dt);
            Assert.That(drop.IsDropped(0), Is.True);
            Assert.That(drop.IsDropped(1), Is.False);
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

            Assert.That(fighter.transform.position.y, Is.LessThan(-0.5f), "fruit on the dropped band falls");
            Assert.That(inner.transform.position.y, Is.EqualTo(0.5f).Within(0.05f), "fruit on the next band stays");
            Object.Destroy(inner);
        }
    }
}
#endif
