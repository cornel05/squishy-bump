#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-10 / SB-11: the six fighter prefabs on a real PhysX floor.</summary>
    public sealed class FighterTests
    {
        const string Root = "Assets/_Project";
        GameObject floor;
        GameObject fighter;

        static BalanceProfile Balance =>
            AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");

        PlayerMotor Spawn(string fruit)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.localScale = new Vector3(60f, 1f, 60f);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/{fruit}.prefab");
            Assert.That(prefab, Is.Not.Null, fruit);
            fighter = Object.Instantiate(prefab, Vector3.up, Quaternion.identity);
            return fighter.GetComponent<PlayerMotor>();
        }

        static IEnumerator Steps(PlayerMotor motor, int count, InputCommand command)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new WaitForFixedUpdate();
                motor.Step(command, Time.fixedDeltaTime);
            }
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Object.Destroy(floor);
            Object.Destroy(fighter);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawn_AppliesClassOfTheFruit(
            [Values("Watermelon", "Pineapple", "Apple", "Mangosteen", "Peach", "Blueberry")] string fruit)
        {
            var motor = Spawn(fruit);
            var definition = fighter.GetComponent<FighterProfileApplier>().Fruit;
            var body = fighter.GetComponent<Rigidbody>();
            var sphere = fighter.GetComponent<SphereCollider>();
            var renderer = fighter.GetComponentInChildren<MeshRenderer>();

            Assert.That(definition.DisplayName, Is.EqualTo(fruit));
            Assert.That(body.mass, Is.EqualTo(definition.Class.Mass));
            Assert.That(sphere.radius, Is.EqualTo(definition.Class.Radius));
            Assert.That(renderer.bounds.extents.x, Is.EqualTo(definition.Class.Radius).Within(0.001f));
            Assert.That((Color32)renderer.sharedMaterial.color, Is.EqualTo(definition.Color));

            // The class stays for the session: a second pick is ignored.
            var other = AssetDatabase.LoadAssetAtPath<FruitDefinition>(
                $"{Root}/Data/Fruits/{(fruit == "Apple" ? "Peach" : "Apple")}.asset");
            fighter.GetComponent<FighterProfileApplier>().Apply(other);
            Assert.That(body.mass, Is.EqualTo(definition.Class.Mass));

            // v_max of the class is reached and not exceeded on the floor.
            yield return Steps(motor, 120, new InputCommand(0, Vector2.up, false, false, 0f));
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(definition.Class.MaxSpeed).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator Jump_FromFloor_ReachesApexAndLands()
        {
            var motor = Spawn("Apple");
            var body = fighter.GetComponent<Rigidbody>();
            yield return Steps(motor, 60, default);
            var restY = body.position.y;

            var apex = restY;
            var airTicks = 0;
            yield return Steps(motor, 1, new InputCommand(0, Vector2.zero, true, false, 0f));
            for (var i = 0; i < 90; i++)
            {
                yield return Steps(motor, 1, default);
                apex = Mathf.Max(apex, body.position.y);
                if (body.position.y > restY + 0.01f) airTicks++;
            }

            // Fixed-step integration undershoots the analytic 1.2 m by about v·Δt/2 = 0.06 m.
            Assert.That(apex - restY, Is.EqualTo(Balance.JumpApexMeters).Within(0.08f));
            Assert.That(airTicks * Time.fixedDeltaTime, Is.EqualTo(Balance.JumpAirTime).Within(0.05f));
            Assert.That(body.position.y, Is.EqualTo(restY).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Dash_OnFloor_RespectsCooldown()
        {
            var motor = Spawn("Apple");
            var body = fighter.GetComponent<Rigidbody>();
            var dash = new InputCommand(0, Vector2.zero, false, true, 0f);
            yield return Steps(motor, 60, default);

            yield return Steps(motor, 1, dash);
            Assert.That(body.linearVelocity.z, Is.EqualTo(Balance.DashImpulse / body.mass).Within(0.25f));
            Assert.That(motor.IsDashing, Is.True);
            Assert.That(motor.DashReady, Is.False);

            yield return Steps(motor, 60, default);
            yield return Steps(motor, 1, dash);
            Assert.That(body.linearVelocity.z, Is.Zero.Within(0.01f), "dash inside the cooldown must be dropped");
            Assert.That(motor.IsDashing, Is.False);
        }
    }
}
#endif
