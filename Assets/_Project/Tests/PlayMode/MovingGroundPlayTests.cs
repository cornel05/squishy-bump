#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-40: a real fruit on the turntable is carried by v_surface; a box hits once per 0.5 s.</summary>
    public sealed class MovingGroundPlayTests
    {
        const string Root = "Assets/_Project";
        readonly List<GameObject> spawned = new();

        sealed class Fighters : IFighterPhysics
        {
            public Collider Fighter;
            public readonly List<Vector3> Impulses = new();
            public int Wobble;
            public bool TryGetFighter(Collider c, out ulong id) { id = 1; return c == Fighter; }
            public int GetWobble(ulong id) => Wobble;
            public void AddImpulse(ulong id, Vector3 j) => Impulses.Add(j);
            public void AddWobble(ulong id, int amount) => Wobble += amount;
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            foreach (var go in spawned) Object.Destroy(go);
            spawned.Clear();
            yield return null;
        }

        MovingGround BuildGround()
        {
            var go = new GameObject("Moving Ground");
            go.SetActive(false);
            var ground = go.AddComponent<MovingGround>();
            var so = new SerializedObject(ground);
            so.FindProperty("balance").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");
            so.FindProperty("hazards").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<HazardProfile>($"{Root}/Data/Hazards/HazardProfile.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(true);
            spawned.Add(go);
            return ground;
        }

        PlayerMotor Fruit(Vector3 at)
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/Apple.prefab"),
                at, Quaternion.identity);
            spawned.Add(go);
            return go.GetComponent<PlayerMotor>();
        }

        [UnityTest]
        public IEnumerator Turntable_CarriesIdleFruitAlongTheRotation()
        {
            var ground = BuildGround();
            var motor = Fruit(new Vector3(0f, 0.5f, 3f));
            var idle = new InputCommand(0, Vector2.zero, false, false, 0f);
            for (var tick = 1; tick <= 120; tick++)
            {
                yield return new WaitForFixedUpdate();
                ground.StepOnHost(tick, (uint)tick, Time.fixedDeltaTime);
                motor.Step(idle, Time.fixedDeltaTime);
            }

            var p = motor.transform.position;
            Assert.That(p.y, Is.EqualTo(0.5f).Within(0.05f), "still on the disc");
            // 2 s at 20°/s clockwise from +z: about 40° toward +x, same radius.
            var angle = Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;
            Assert.That(angle, Is.EqualTo(40f).Within(6f));
            Assert.That(new Vector2(p.x, p.z).magnitude, Is.EqualTo(3f).Within(0.3f));
        }

        [UnityTest]
        public IEnumerator Box_HitsOncePerHalfSecond()
        {
            var ground = BuildGround();
            var fighters = new Fighters();
            ground.Bind(null, fighters);
            yield return new WaitForFixedUpdate();
            var box = ground.Boxes[0].transform;
            var motor = Fruit(box.position + Vector3.up * 0.1f);
            motor.GetComponent<Rigidbody>().isKinematic = true;
            fighters.Fighter = motor.GetComponent<SphereCollider>();
            for (var tick = 1; tick <= 40; tick++)
            {
                yield return new WaitForFixedUpdate();
                // Keep the fruit inside the moving box.
                ground.StepOnHost(tick, (uint)tick, Time.fixedDeltaTime);
                motor.transform.position = box.position;
                if (tick == 20) Assert.That(fighters.Impulses, Has.Count.EqualTo(1), "re-hit within 0.5 s ignored");
            }
            Assert.That(fighters.Impulses, Has.Count.EqualTo(2), "hits again after 0.5 s");
            Assert.That(fighters.Impulses[0].magnitude, Is.EqualTo(50f).Within(0.01f));
            Assert.That(fighters.Wobble, Is.EqualTo(2));
        }
    }
}
#endif
