#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class MatchHudTests
    {
        private GameObject root;
        private const string Data = "Assets/_Project/Data";

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (root != null) Object.Destroy(root);
            yield return null;
        }

        private static BalanceProfile Balance() => AssetDatabase.FindAssets("t:BalanceProfile", new[] { Data })
            .Select(g => AssetDatabase.LoadAssetAtPath<BalanceProfile>(AssetDatabase.GUIDToAssetPath(g))).First();

        [UnityTest]
        public IEnumerator Hud_ClockDashAndWobbleFollowConfirmedValues()
        {
            root = new GameObject("HUD test");
            var hud = root.AddComponent<MatchHud>();
            hud.Init(Balance(), 2);
            hud.SetClock(139);
            Assert.That(hud.ClockColor, Is.EqualTo(Color.white));
            hud.SetClock(140);
            Assert.That(hud.ClockColor, Is.EqualTo(Color.red));
            Assert.That(hud.ClockText, Does.Contain("10 s"));
            hud.SetRemaining(4, 6);
            Assert.That(hud.RemainingText, Is.EqualTo("Fruit 4/6"));
            var definition = AssetDatabase.FindAssets("t:FruitDefinition", new[] { Data })
                .Select(g => AssetDatabase.LoadAssetAtPath<FruitDefinition>(AssetDatabase.GUIDToAssetPath(g))).First();
            hud.SetFruit(definition);
            Assert.That(hud.FruitText, Is.EqualTo(definition.DisplayName));
            Assert.That(hud.ClassText, Does.Contain(definition.Class.name).And.Contain($"{definition.Class.Mass:0.#} kg"));
            hud.SetDash(1.5f);
            Assert.That(hud.DashFill, Is.EqualTo(0.5f).Within(0.001f));
            hud.SetDash(0);
            Assert.That(hud.DashText, Does.Contain("READY"));
            var target = new GameObject("Fruit");
            target.transform.SetParent(root.transform);
            hud.Register(1, target.transform, 1);
            hud.SetWobble(1, 5, true);
            // UI assembly types are read through Graphic to avoid depending on the layout implementation.
            var tag = root.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(t => t.name == "Wobble 1");
            Assert.That(tag.text, Is.EqualTo("×5"));
            Assert.That(tag.color, Is.EqualTo(Color.red));
            yield return null;
            foreach (var label in root.GetComponentsInChildren<UnityEngine.UI.Text>(true).Where(t => !t.name.StartsWith("Wobble")))
                Assert.That(label.rectTransform.anchorMin.y, Is.EqualTo(0).Or.EqualTo(1), "HUD nằm sát cạnh, giữ trống giữa màn hình");
        }

        [UnityTest]
        public IEnumerator L1_ClockUsesNextRingDrop()
        {
            root = new GameObject("HUD L1 test");
            var hud = root.AddComponent<MatchHud>();
            hud.Init(Balance(), 1);
            hud.SetClock(35);
            Assert.That(hud.ClockText, Is.EqualTo("Ring 10 s"));
            Assert.That(hud.ClockColor, Is.EqualTo(Color.red));
            hud.SetClock(45);
            Assert.That(hud.ClockText, Is.EqualTo("Ring 30 s"));
            Assert.That(hud.ClockColor, Is.EqualTo(Color.white));
            hud.SetClock(135);
            Assert.That(hud.ClockText, Is.EqualTo("Ring 0 s"), "Không đếm lại tới 150 s sau khi vành cuối đã rụng");
            yield return null;
        }
    }
}
#endif
