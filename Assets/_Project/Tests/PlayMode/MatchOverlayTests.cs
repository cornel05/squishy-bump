using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.UI.Menus;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-75: hearts and result pages follow the UX/UI kit and only show the values they are given.</summary>
    public sealed class MatchOverlayTests
    {
        private GameObject root;

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (root != null) Object.Destroy(root);
            yield return null;
        }

        private MatchOverlay NewOverlay()
        {
            root = new GameObject("Overlay test");
            var overlay = root.AddComponent<MatchOverlay>();
            overlay.Init();
            return overlay;
        }

        [UnityTest]
        public IEnumerator Hearts_LostLivesTurnGrey()
        {
            var overlay = NewOverlay();
            overlay.ShowHud(2, 3);
            Assert.That(overlay.HeartSprites, Is.EqualTo(new[] { "hud_heart", "hud_heart", "hud_heart_lost" }));
            overlay.ShowHud(0, 3);
            Assert.That(overlay.HeartSprites, Is.EqualTo(new[] { "hud_heart_lost", "hud_heart_lost", "hud_heart_lost" }));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RoundResult_ShowsRoundBannerAndHighlightsOnlyTheLocalRow()
        {
            var overlay = NewOverlay();
            var standings = new[] { new MatchStanding(5, 1, 10), new MatchStanding(2, 2, 6), new MatchStanding(7, 3, 1) };
            overlay.ShowResult(standings, id => $"Fruit {id}", id => (int)id, round: 3, localId: 2);
            var images = root.GetComponentsInChildren<Image>();
            Assert.That(images.Any(i => i.sprite != null && i.sprite.name == "banner_round_3"), Is.True);
            var rows = images.Where(i => i.name.StartsWith("Row ")).ToList();
            Assert.That(rows.Select(r => r.name), Is.EqualTo(new[] { "Row 1", "Row 2 (you)", "Row 3" }));
            Assert.That(rows[1].color, Is.EqualTo(Color.white));
            Assert.That(rows[0].color, Is.Not.EqualTo(Color.white));
            Assert.That(overlay.ResultText, Does.StartWith("KẾT QUẢ"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SessionResult_PutsTopThreeOnPodiums()
        {
            var overlay = NewOverlay();
            var standings = new[] { new MatchStanding(1, 1, 0), new MatchStanding(2, 2, 0), new MatchStanding(3, 3, 0), new MatchStanding(4, 4, 0) };
            overlay.ShowSessionResult(standings, id => $"Fruit {id}");
            var podiums = root.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Podium ")).Select(i => i.name);
            Assert.That(podiums, Is.EquivalentTo(new[] { "Podium 1", "Podium 2", "Podium 3" }));
            Assert.That(overlay.ResultText, Does.Contain("VÔ ĐỊCH · Fruit 1"));
            yield return null;
        }
    }
}
