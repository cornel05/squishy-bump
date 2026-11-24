using NUnit.Framework;
using SquishyBump.Core;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-76: the six animal .glb sit under Visual/Body; the collider stays the class-radius sphere (GDD 12.2).</summary>
    public sealed class FighterVisualTests
    {
        static readonly string[] Animals = { "Bear", "Pig", "Cat", "Penguin", "Chick", "Frog" };

        static GameObject Prefab(string animal) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Fighters/{animal}.prefab");

        [Test]
        public void Prefabs_HaveGlbModelUnderBody([ValueSource(nameof(Animals))] string animal)
        {
            var model = Prefab(animal).transform.Find("Visual/Body/Model");

            Assert.That(model, Is.Not.Null);
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(model.gameObject);
            Assert.That(AssetDatabase.GetAssetPath(source), Is.EqualTo($"Assets/_Project/Art/Characters/{animal}.glb"));
            Assert.That(model.GetComponentsInChildren<MeshRenderer>(), Is.Not.Empty);
            Assert.That(Prefab(animal).transform.Find("Visual/Body").GetComponent<MeshRenderer>().enabled, Is.False,
                "placeholder sphere hidden");
        }

        [Test]
        public void Model_HasNoCollider_ColliderStaysClassSphere([ValueSource(nameof(Animals))] string animal)
        {
            var prefab = Prefab(animal);
            var definition = AssetDatabase.LoadAssetAtPath<FruitDefinition>($"Assets/_Project/Data/Fruits/{animal}.asset");

            var colliders = prefab.GetComponentsInChildren<Collider>(true);

            Assert.That(colliders, Has.Length.EqualTo(1));
            Assert.That(colliders[0], Is.InstanceOf<SphereCollider>());
            Assert.That(colliders[0].gameObject, Is.SameAs(prefab));
            Assert.That(((SphereCollider)colliders[0]).radius, Is.EqualTo(definition.Class.Radius));
        }

        [Test]
        public void Model_WidthFromRendererBoundsIsTwiceRadius_FeetOnSphereBottom([ValueSource(nameof(Animals))] string animal)
        {
            var instance = Object.Instantiate(Prefab(animal));
            try
            {
                var radius = ((SphereCollider)instance.GetComponent<Collider>()).radius;
                var bounds = new Bounds();
                var first = true;
                foreach (var r in instance.transform.Find("Visual/Body/Model").GetComponentsInChildren<Renderer>())
                {
                    if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
                }

                Assert.That(bounds.size.x, Is.EqualTo(2f * radius).Within(0.02f * radius));
                Assert.That(bounds.min.y, Is.EqualTo(-radius).Within(0.02f));
                Assert.That(bounds.center.x, Is.EqualTo(0f).Within(0.02f));
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
