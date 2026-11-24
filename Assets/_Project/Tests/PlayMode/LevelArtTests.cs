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
                    var staticFloat = artRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Torus.001");
                    Assert.That(staticFloat != null && staticFloat.gameObject.activeSelf, Is.False);
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
