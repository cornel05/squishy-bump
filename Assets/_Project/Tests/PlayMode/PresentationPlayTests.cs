#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Physics;
using SquishyBump.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-13: RollVisual on a real fighter prefab, CameraRig with Cinemachine.</summary>
    public sealed class PresentationPlayTests
    {
        const string Root = "Assets/_Project";
        GameObject floor;
        GameObject fighter;
        GameObject cameraObject;
        GameObject rigObject;

        static PresentationProfile Profile =>
            AssetDatabase.LoadAssetAtPath<PresentationProfile>($"{Root}/Data/Presentation/PresentationProfile.asset");

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            foreach (var go in new[] { floor, fighter, cameraObject, rigObject })
                if (go != null) Object.Destroy(go);
            yield return null;
        }

        PlayerMotor SpawnFighter(Vector3 position)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.localScale = new Vector3(60f, 1f, 60f);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/Apple.prefab");
            fighter = Object.Instantiate(prefab, position, Quaternion.identity);
            return fighter.GetComponent<PlayerMotor>();
        }

        [UnityTest]
        public IEnumerator Rolling_TurnsBodyButNotCollider()
        {
            var motor = SpawnFighter(new Vector3(0f, 0.5f, 0f));
            var roll = fighter.GetComponentInChildren<RollVisual>();
            Assert.That(roll, Is.Not.Null);
            Assert.That(roll.Body.GetComponent<MeshRenderer>(), Is.Not.Null, "mesh lives on the rolling Body");
            var start = roll.Body.rotation;

            var previous = start;
            var minAxisX = 1f;
            for (var i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                motor.Step(new InputCommand((uint)i, Vector2.up, false, false, 0f), Time.fixedDeltaTime);
                yield return null;
                // Moving +z rolls about +x: every frame's turn has that axis.
                (roll.Body.rotation * Quaternion.Inverse(previous)).ToAngleAxis(out var angle, out var axis);
                if (angle > 0.01f && angle < 180f) minAxisX = Mathf.Min(minAxisX, axis.x);
                previous = roll.Body.rotation;
            }

            Assert.That(fighter.transform.rotation, Is.EqualTo(Quaternion.identity), "collider never rotates");
            Assert.That(Quaternion.Angle(start, roll.Body.rotation), Is.GreaterThan(10f));
            Assert.That(minAxisX, Is.GreaterThan(0.99f));
        }

        [UnityTest]
        public IEnumerator Landing_SquashesVisualOnly()
        {
            SpawnFighter(new Vector3(0f, 3f, 0f));
            var roll = fighter.GetComponentInChildren<RollVisual>();
            var visual = roll.transform;
            var baseScale = visual.localScale;
            var squashed = false;
            for (var t = 0f; t < 2f && !squashed; t += Time.deltaTime)
            {
                yield return null;
                squashed = visual.localScale.y < baseScale.y * 0.95f;
            }

            Assert.That(squashed, "landing squash played");
            Assert.That(visual.localScale.x, Is.GreaterThan(baseScale.x));
            Assert.That(fighter.GetComponent<SphereCollider>().radius, Is.EqualTo(0.5f), "collider unchanged");
            yield return new WaitForSeconds(Profile.SquashSeconds * 2f);
            Assert.That(visual.localScale.y, Is.EqualTo(baseScale.y).Within(1e-4f));
        }

        [UnityTest]
        public IEnumerator CameraRig_OrbitsAtDistanceWithClampedPitch()
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera));
            var target = new GameObject("Target");
            rigObject = new GameObject("Rig");
            rigObject.SetActive(false);
            var rig = rigObject.AddComponent<CameraRig>();
            SetField(rig, "profile", Profile);
            SetField(rig, "outputCamera", cameraObject.GetComponent<Camera>());
            rigObject.SetActive(true);
            rig.Follow(target.transform);

            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(rig.Distance, Is.EqualTo(9f));
            Assert.That(rig.Pitch, Is.EqualTo(35f));
            Assert.That(Vector3.Distance(cameraObject.transform.position, target.transform.position),
                Is.EqualTo(9f).Within(0.1f));

            rig.Orbit(new Vector2(0f, -10000f));
            Assert.That(rig.Pitch, Is.EqualTo(60f));
            rig.Orbit(new Vector2(0f, 10000f));
            Assert.That(rig.Pitch, Is.EqualTo(15f));

            Assert.That(rig.Shake(10f), Is.True);
            rig.ShakeEnabled = false;
            Assert.That(rig.Shake(10f), Is.False);
            Object.Destroy(target);
        }

        static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
