using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-11: the six A15 .glb fruits sit under Visual/Body; the collider stays the class-radius sphere (GDD 12.2).</summary>
    public sealed class FighterVisualTests
    {
        static readonly string[] Fruits = { "Watermelon", "Pineapple", "Apple", "Mangosteen", "Peach", "Blueberry" };

        static GameObject Prefab(string fruit) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Fighters/{fruit}.prefab");

        [Test]
        public void Prefabs_HaveGlbModelUnderBody([ValueSource(nameof(Fruits))] string fruit)
        {
            var model = Prefab(fruit).transform.Find("Visual/Body/Model");

            Assert.That(model, Is.Not.Null);
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(model.gameObject);
            Assert.That(AssetDatabase.GetAssetPath(source), Is.EqualTo($"Assets/_Project/Art/Fruits/{fruit}.glb"));
            Assert.That(model.GetComponentsInChildren<MeshRenderer>(), Is.Not.Empty);
            Assert.That(Prefab(fruit).transform.Find("Visual/Body").GetComponent<MeshRenderer>().enabled, Is.False,
                "placeholder sphere hidden");
        }

        [Test]
        public void Model_HasNoCollider_ColliderStaysClassSphere([ValueSource(nameof(Fruits))] string fruit)
        {
            var prefab = Prefab(fruit);
            var definition = AssetDatabase.LoadAssetAtPath<FruitDefinition>($"Assets/_Project/Data/Fruits/{fruit}.asset");

            var colliders = prefab.GetComponentsInChildren<Collider>(true);

            Assert.That(colliders, Has.Length.EqualTo(1));
            Assert.That(colliders[0], Is.InstanceOf<SphereCollider>());
            Assert.That(colliders[0].gameObject, Is.SameAs(prefab));
            Assert.That(((SphereCollider)colliders[0]).radius, Is.EqualTo(definition.Class.Radius));
        }

        [Test]
        public void ModelBody_WorldSizeMatchesClassRadius([ValueSource(nameof(Fruits))] string fruit)
        {
            var prefab = Prefab(fruit);
            var radius = ((SphereCollider)prefab.GetComponent<Collider>()).radius;
            var body = prefab.transform.Find("Visual/Body/Model").GetComponentsInChildren<MeshFilter>()
                .Single(m => m.name == "Body");

            // Width of the body mesh in the prefab's space; the oval fruits (Watermelon, Pineapple) differ by <= 15%.
            var width = body.sharedMesh.bounds.size.x * body.transform.lossyScale.x / prefab.transform.lossyScale.x;

            Assert.That(width, Is.EqualTo(2f * radius).Within(2f * radius * 0.15f));
        }
    }
}
