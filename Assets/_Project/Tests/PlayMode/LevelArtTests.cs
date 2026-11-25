using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Hazards;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class LevelArtTests
    {
        private const string Root = "Assets/_Project";
        private GameplayBootstrap app;

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1f;
            if (app != null)
            {
                app.Session.Leave();
                Object.Destroy(app.gameObject);
            }
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/MainMenu.unity", LoadSceneMode.Single);
        }

        [UnityTest]
        public IEnumerator Level_WithCreativeArt_SpawnsFightersAndBindsVisuals([Values(1, 2, 3, 4, 5)] int level)
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None)
                .First(b => b.gameObject.scene.name == "DontDestroyOnLoad" && b.Launcher != null);
            app.Store = new LocalSave(System.IO.Path.Combine(Application.temporaryCachePath, "sb-tests"));

            app.Launcher.StartSinglePlayer(level);
            var sceneName = $"L{level}";
            yield return WaitFor(() =>
                SceneManager.GetActiveScene().name == sceneName &&
                app.Launcher.Match != null &&
                app.Launcher.Match.Started &&
                app.Launcher.Match.Fighters.Count == 6);
            yield return null;
            for (var i = 0; i < 3; i++) yield return new WaitForFixedUpdate();

            var scene = SceneManager.GetActiveScene();
            Assert.That(scene.name, Is.EqualTo(sceneName));
            Assert.That(app.Launcher.Match.Fighters, Has.Count.EqualTo(6));
            Assert.That(app.Launcher.Match.Fighters.All(f => f.Motor.transform.position.y > 0f), Is.True,
                $"L{level} fighters should stand above y = 0.");

            var artRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == $"Creative L{level} Art");
            Assert.That(artRoot, Is.Not.Null, $"L{level} missing Creative L{level} Art.");
            Assert.That(artRoot.GetComponentsInChildren<Camera>(true).Any(c => c.enabled), Is.False);
            Assert.That(artRoot.GetComponentsInChildren<Light>(true).Any(l => l.enabled), Is.False);
            Assert.That(artRoot.GetComponentsInChildren<AudioListener>(true).Any(a => a.enabled), Is.False);
            Assert.That(artRoot.GetComponentsInChildren<Collider>(true).Any(c => c.enabled), Is.False);

            var visibleRenderers = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(false))
                .Where(r => r.enabled && !r.forceRenderingOff)
                .ToArray();
            Assert.That(visibleRenderers, Is.Not.Empty);
            Assert.That(visibleRenderers.All(r => r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m != null)),
                Is.True, $"L{level} has an enabled renderer with a missing material.");

            switch (level)
            {
                case 1:
                    var ringDrop = Object.FindFirstObjectByType<RingDrop>();
                    Assert.That(ringDrop, Is.Not.Null);
                    Assert.That(ringDrop.BandCount, Is.EqualTo(4));
                    for (var i = 0; i < 4; i++)
                    {
                        var band = ringDrop.transform.Find($"Band {i}");
                        Assert.That(band.GetComponent<MeshRenderer>().enabled, Is.False);
                        Assert.That(band.GetComponent<MeshCollider>().enabled, Is.True);
                        var vong = band.Find($"Vong{i + 1}Pivot/Vong{i + 1}");
                        Assert.That(vong, Is.Not.Null);
                        var vr = vong.GetComponentInChildren<Renderer>(false);
                        Assert.That(vr.enabled, Is.True);
                        Assert.That(vr.sharedMaterial.GetTexture("_BumpMap"), Is.Not.Null, "L1 sand normal map should be assigned.");
                        var sandCol = vr.sharedMaterial.GetColor("_BaseColor");
                        Assert.That(sandCol.r - sandCol.b, Is.GreaterThan(0.2f), "L1 ring should be warm sand, not grey.");
                        Assert.That(vong.GetComponentsInChildren<Collider>(true).Any(c => c.enabled), Is.False);
                    }
                    Assert.That(artRoot.transform.Find("CreativeFloats/PhaoLgbt"), Is.Not.Null);
                    Assert.That(artRoot.transform.Find("Creative_Map1"), Is.Null,
                        "All L1 island props should be reparented to RingDrop bands, leaving no static Creative_Map1.");
                    foreach (var removed in new[] { "Sphere.006", "Cube.001", "Circle.001", "Circle.002", "Circle.003", "Circle.009", "Torus.001" })
                        Assert.That(new[] { artRoot, ringDrop.gameObject }.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                            .FirstOrDefault(t => t.name == removed), Is.Null, $"L1 leftover {removed} should be removed.");
                    var planks = ringDrop.GetComponentsInChildren<Renderer>(false)
                        .Where(r => r.name.StartsWith("Plane.", StringComparison.Ordinal)).ToArray();
                    Assert.That(planks, Has.Length.EqualTo(10), "L1 should bind 10 wooden planks onto RingDrop bands.");
                    Assert.That(planks.All(p => p.sharedMaterial != null && p.sharedMaterial.GetTexture("_BaseMap") != null), Is.True,
                        "Every L1 wooden plank should use the WoodPlank texture.");
                    var band0 = ringDrop.transform.Find("Band 0");
                    Assert.That(band0.Find("Object_14"), Is.Not.Null, "Outer treasure chest should belong to Band 0.");
                    Assert.That(band0.Find("Box001_Material #25_0"), Is.Not.Null, "Outer shovel should belong to Band 0.");
                    Assert.That(band0.Find("Low Poly Palm Tree"), Is.Not.Null, "Outer palm tree should belong to Band 0.");
                    Assert.That(ringDrop.transform.Find("Band 1/Low Poly Palm Tree.002"), Is.Not.Null, "Second-ring palm tree should belong to Band 1.");
                    var chest = band0.Find("Object_14");
                    var chestBefore = chest.position.y;
                    var water = artRoot.transform.Find("Water");
                    Assert.That(water, Is.Not.Null);
                    Assert.That(water.position.y, Is.EqualTo(RingDrop.InitialWaterY).Within(0.01f),
                        "L1 water should start close below the sand surface.");
                    // BalanceProfile ringDropSeconds[0] = 45 s → band 0 drops at playing tick 2700 (60 Hz).
                    ringDrop.StepOnHost(2700, 2700, 1f / 60f);
                    Assert.That(ringDrop.IsDropped(0), Is.True);
                    yield return WaitFor(() => chest.position.y < chestBefore - 0.05f && water.position.y > RingDrop.InitialWaterY + 0.02f, 2f);
                    Assert.That(chest.position.y, Is.LessThan(chestBefore - 0.05f),
                        "Band 0 props should fall together with Band 0 when H01 drops.");
                    Assert.That(water.position.y, Is.GreaterThan(RingDrop.InitialWaterY + 0.02f),
                        "L1 water should rise when Band 0 drops.");
                    // BalanceProfile ringDropSeconds[3] = 135 s → all 4 bands dropped at playing tick 8100 (60 Hz).
                    ringDrop.StepOnHost(8100, 8100, 1f / 60f);
                    yield return WaitFor(() => water.position.y > 0.2f, 3f);
                    Assert.That(water.position.y, Is.GreaterThan(0.2f),
                        "L1 water should submerge the entire ring once all bands have dropped.");
                    break;
                case 2:
                    var binder2 = artRoot.GetComponent<LevelArtBinder>();
                    var moving = Object.FindFirstObjectByType<MovingGround>();
                    Assert.That(binder2, Is.Not.Null);
                    Assert.That(binder2.IsBound, Is.True);
                    Assert.That(binder2.BoundObstacleCount, Is.EqualTo(3));
                    Assert.That(moving.Turntable.GetComponent<Renderer>().enabled, Is.False);
                    Assert.That(moving.Ring.GetComponent<Renderer>().enabled, Is.False);
                    Assert.That(moving.Turntable.transform.Find("Cylinder.001"), Is.Not.Null);
                    Assert.That(moving.Ring.transform.Find("Cylinder"), Is.Not.Null);
                    Assert.That(moving.Boxes.All(b => b.enabled), Is.True);
                    for (var i = 0; i < 3; i++)
                    {
                        var box = moving.Ring.transform.Find($"Box {i}");
                        Assert.That(box.GetComponent<Renderer>().enabled, Is.False);
                        Assert.That(box.Find($"ObstacleArt_{i}"), Is.Not.Null);
                    }
                    break;
                case 3:
                    var floor = GameObject.Find("Floor r 9 m");
                    Assert.That(floor.GetComponent<Renderer>().enabled, Is.False);
                    Assert.That(floor.GetComponent<Collider>().enabled, Is.True);
                    var sphere = artRoot.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "Sphere");
                    Assert.That(sphere.enabled, Is.False);
                    break;
                case 4:
                    var binder4 = artRoot.GetComponent<LevelArtBinder>();
                    var ice4 = Object.FindFirstObjectByType<IceTiles>();
                    Assert.That(binder4, Is.Not.Null);
                    Assert.That(binder4.IsBound, Is.True);
                    Assert.That(binder4.BoundTileCount, Is.EqualTo(61));
                    Assert.That(ice4.GetComponentsInChildren<Renderer>(true)
                        .Count(r => r.name == "CreativeIceTile" && r.enabled), Is.EqualTo(61));
                    break;
                case 5:
                    var binder5 = artRoot.GetComponent<LevelArtBinder>();
                    var ice5 = Object.FindFirstObjectByType<IceTiles>();
                    Assert.That(binder5, Is.Not.Null);
                    Assert.That(binder5.IsBound, Is.True);
                    Assert.That(binder5.BoundTileCount, Is.EqualTo(84));
                    Assert.That(ice5.GetComponentsInChildren<Renderer>(true)
                        .Count(r => r.name.StartsWith("Brick2x", StringComparison.Ordinal) && r.enabled), Is.EqualTo(84));
                    break;
            }
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds = 12f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out waiting for level art setup.");
        }
    }
}
