using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;
using SquishyBump.Physics;
using SquishyBump.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>Chỉ chạy trong Windows test Player có renderer thật; không đưa vào release game.</summary>
    public sealed class HudPlayerVisualTests
    {
        [Serializable] private sealed class Evidence
        {
            public string capture, graphics;
            public int width, height, visibleTags;
            public bool fixedHudClear;
            public List<string> ownBodyOverlaps = new();
            public List<string> labelOverlaps = new();
            public List<string> hiddenTags = new();
            public List<string> otherBodyOverlaps = new();
        }

        [UnityTest, UnityPlatform(RuntimePlatform.WindowsPlayer), Timeout(180000)]
        public IEnumerator WindowsPlayer_HudAndTrackedTags_AspectSweep()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                "Ảnh từ nographics không phải bằng chứng hình ảnh.");
            var project = new DirectoryInfo(Application.dataPath);
            while (project != null && !File.Exists(Path.Combine(project.FullName, "ProjectSettings/ProjectVersion.txt"))) project = project.Parent;
            Assert.That(project, Is.Not.Null, "Đặt test Player dưới Builds/ của checkout cần kiểm chứng.");
            var output = Path.Combine(project.FullName, "TestResults/SB17-Visual");
            Directory.CreateDirectory(output);
            GameplayBootstrap app = null;
            try
            {
                yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
                app = UnityEngine.Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None)
                    .First(b => b.Launcher != null);
                app.Store = new LocalSave(Path.Combine(Application.temporaryCachePath, "sb17-visual-save"));
                app.Settings.Rule = MatchRule.Survival;
                AudioListener.volume = 0;
                app.Saved.TutorialCompleted = true;
                app.Launcher.StartSinglePlayer(1);
                var deadline = Time.realtimeSinceStartup + 25;
                while ((app.Launcher.Match == null || !app.Launcher.Match.Started ||
                    app.Launcher.Match.Director.Phase != MatchPhase.Playing) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(app.Launcher.Match?.Director.Phase, Is.EqualTo(MatchPhase.Playing));
                yield return new WaitForSecondsRealtime(1);
                var match = app.Launcher.Match;
                var resolver = match.GetComponent<CollisionResolver>();
                for (var i = 0; i < match.Fighters.Count; i++) resolver.AddWobble(match.Fighters[i].Id, i * 2);
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                Time.timeScale = 0;
                var sizes = new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720),
                    new Vector2Int(1440, 900), new Vector2Int(1680, 720) };
                foreach (var packed in new[] { false, true })
                {
                    if (packed)
                    {
                        var camera = Camera.main;
                        var local = match.Fighters.First();
                        var distance = Vector3.Distance(camera.transform.position, local.Motor.transform.position);
                        foreach (var component in camera.GetComponents<MonoBehaviour>())
                            if (component.GetType().FullName == "Unity.Cinemachine.CinemachineBrain") component.enabled = false;
                        foreach (var rig in UnityEngine.Object.FindObjectsByType<CameraRig>(FindObjectsSortMode.None)) rig.enabled = false;
                        var positions = new[] { Vector2.zero, new Vector2(1.4f, 0), new Vector2(-1.4f, 0),
                            new Vector2(0, 1.4f), new Vector2(1.4f, 1.4f), new Vector2(-1.4f, 1.4f) };
                        for (var i = 0; i < match.Fighters.Count; i++)
                        {
                            var motor = match.Fighters[i].Motor;
                            var p = new Vector3(positions[i].x, motor.GetComponent<SphereCollider>().radius + 0.05f, positions[i].y);
                            motor.transform.position = p; motor.GetComponent<Rigidbody>().position = p;
                        }
                        camera.transform.position = local.Motor.transform.position - camera.transform.forward * distance;
                        UnityEngine.Physics.SyncTransforms();
                    }
                    foreach (var size in sizes)
                    {
                        Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                        for (var frame = 0; frame < 12; frame++) yield return null;
                        Assert.That(new Vector2Int(Screen.width, Screen.height), Is.EqualTo(size), "OS không áp dụng kích thước yêu cầu.");
                        Canvas.ForceUpdateCanvases();
                        var name = $"{(packed ? "packed" : "normal")}-{size.x}x{size.y}";
                        var evidence = new Evidence { capture = name, graphics = SystemInfo.graphicsDeviceType.ToString(),
                            width = Screen.width, height = Screen.height, fixedHudClear = true };
                        var protectedArea = new Rect(Screen.width * 0.25f, Screen.height * 0.25f, Screen.width * 0.5f, Screen.height * 0.5f);
                        var glyphs = new List<(string Name, Rect Bounds)>();
                        foreach (var label in match.Hud.GetComponentsInChildren<Text>(true)
                            .Where(t => t.name.StartsWith("Wobble ") && !t.gameObject.activeInHierarchy))
                        {
                            var id = ushort.Parse(label.name.Substring("Wobble ".Length));
                            var motor = match.Fighters.First(f => f.Id == id).Motor;
                            var head = Camera.main.WorldToScreenPoint(motor.transform.position +
                                Vector3.up * (motor.GetComponent<SphereCollider>().radius + 0.5f));
                            evidence.hiddenTags.Add($"{label.name}: head={head:F2}, activeFruit={motor.gameObject.activeInHierarchy}");
                        }
                        foreach (var text in match.Hud.GetComponentsInChildren<Text>())
                        {
                            if (!text.name.StartsWith("Wobble "))
                            { if (ScreenRect(text.rectTransform).Overlaps(protectedArea)) evidence.fixedHudClear = false; continue; }
                            evidence.visibleTags++;
                            var glyph = GlyphRect(text);
                            foreach (var other in glyphs)
                                if (glyph.Overlaps(other.Bounds)) evidence.labelOverlaps.Add(text.name + " / " + other.Name);
                            glyphs.Add((text.name, glyph));
                            var id = ushort.Parse(text.name.Substring("Wobble ".Length));
                            foreach (var otherFighter in match.Fighters.Where(f => f.Id != id && !f.Out))
                                foreach (var renderer in otherFighter.Motor.GetComponentsInChildren<Renderer>()
                                    .Where(r => r.enabled && r.gameObject.activeInHierarchy && Camera.main.WorldToScreenPoint(r.bounds.center).z > 0))
                                    if (glyph.Overlaps(ProjectBounds(Camera.main, renderer)))
                                        evidence.otherBodyOverlaps.Add(text.name + " / fruit " + otherFighter.Id);
                            var fighter = match.Fighters.First(f => f.Id == id);
                            var renderers = fighter.Motor.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
                            if (renderers.Length == 0) continue;
                            if (renderers.Any(renderer => ScreenRect(text.rectTransform).Overlaps(ProjectBounds(Camera.main, renderer))))
                                evidence.ownBodyOverlaps.Add(text.name);
                        }
                        var png = Path.Combine(output, name + ".png");
                        if (File.Exists(png)) File.Delete(png); // Không nhận nhầm ảnh của lượt kiểm chứng trước.
                        ScreenCapture.CaptureScreenshot(png);
                        yield return new WaitForSecondsRealtime(0.5f);
                        Assert.That(File.Exists(png), Is.True);
                        File.WriteAllText(Path.Combine(output, name + ".json"), JsonUtility.ToJson(evidence, true));
                        Assert.That(evidence.fixedHudClear, Is.True, name);
                        TestContext.Progress.WriteLine(JsonUtility.ToJson(evidence));
                        Assert.That(evidence.ownBodyOverlaps, Is.Empty, $"{name}: toàn bộ rect nhãn phải ở trên hình quả.");
                        Assert.That(evidence.labelOverlaps, Is.Empty, $"{name}: glyph và outline của hai tag không được đè nhau.");
                        Assert.That(evidence.otherBodyOverlaps, Is.Empty, $"{name}: nhãn không được che quả khác.");
                        if (packed) Assert.That(evidence.visibleTags, Is.EqualTo(match.Fighters.Count));
                    }
                }
                var tracked = match.Fighters.First();
                var trackedLabel = match.Hud.GetComponentsInChildren<Text>().Single(t => t.name == $"Wobble {tracked.Id}");
                var oldPosition = ScreenRect(trackedLabel.rectTransform).center;
                var movedPosition = tracked.Motor.transform.position + Vector3.right * 0.8f;
                tracked.Motor.transform.position = movedPosition;
                tracked.Motor.GetComponent<Rigidbody>().position = movedPosition;
                UnityEngine.Physics.SyncTransforms();
                for (var frame = 0; frame < 4; frame++) yield return null;
                Assert.That(Vector2.Distance(oldPosition, ScreenRect(trackedLabel.rectTransform).center), Is.GreaterThan(1),
                    "Tag phải tiếp tục bám vị trí mới sau khi fighter di chuyển.");
                Assert.That(match.Hud.GetComponentsInChildren<Text>().Any(t => t.text == "×8" && t.color == Color.red), Is.True);
            }
            finally
            {
                Time.timeScale = 1;
                if (app != null) { app.Session.Leave(); UnityEngine.Object.Destroy(app.gameObject); }
            }
        }

        private static Rect ScreenRect(RectTransform transform)
        {
            var corners = new Vector3[4]; transform.GetWorldCorners(corners);
            var a = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var b = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }
        private static Rect GlyphRect(Text text)
        {
            var verts = text.cachedTextGenerator.verts;
            var min = Vector2.one * float.PositiveInfinity; var max = Vector2.one * float.NegativeInfinity;
            var unit = 1f / text.pixelsPerUnit;
            var first = (Vector2)verts[0].position * unit;
            var rounding = text.PixelAdjustPoint(first) - first;
            for (var i = 0; i < verts.Count - 4; i++)
            {
                var p = (Vector2)verts[i].position * unit + rounding;
                var screen = RectTransformUtility.WorldToScreenPoint(null, text.rectTransform.TransformPoint(p));
                min = Vector2.Min(min, screen); max = Vector2.Max(max, screen);
            }
            var stroke = 2 * text.canvas.scaleFactor;
            return Rect.MinMaxRect(min.x - stroke, min.y - stroke, max.x + stroke, max.y + stroke);
        }
        private static Rect ProjectBounds(Camera camera, Renderer renderer)
        {
            var bounds = renderer.localBounds;
            var min = Vector2.one * float.PositiveInfinity; var max = Vector2.one * float.NegativeInfinity;
            for (var i = 0; i < 8; i++)
            {
                var p = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var screen = (Vector2)camera.WorldToScreenPoint(renderer.localToWorldMatrix.MultiplyPoint3x4(p));
                min = Vector2.Min(min, screen); max = Vector2.Max(max, screen);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
