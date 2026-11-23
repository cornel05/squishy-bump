using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;

namespace SquishyBump.UI.Menus
{
    /// <summary>
    /// Arena card (B02–B06) before the countdown and the hazard banner under the match clock (GDD 11.2, B.3, B.6).
    /// At most one banner: a new one replaces the current one. Display only; App decides what to show and when.
    /// </summary>
    public sealed class MatchOverlay : MonoBehaviour
    {
        private RectTransform card;
        private Text cardTitle, cardLine, banner;
        private float cardUntil, bannerUntil;
        private Text result;
        private Text actionText, spectating;
        private Button resultButton;
        private Action resultAction;
        private float resultReadyAt;
        private const float ResultReadSeconds = 3f; // GDD 11.3
        private RectTransform resultPage, hud;
        private readonly List<Image> hearts = new();
        private int shownLives = -1, shownMax = -1;
        private bool winnerLayout;
        private static readonly Color Greyed = new(0.72f, 0.72f, 0.75f);
        private Transform canvasRoot;
        public string ResultText => result != null && result.gameObject.activeSelf ? result.text : "";

        public bool CardVisible => card.gameObject.activeSelf;
        /// <summary>Sprite names of the hearts, left to right (tests read the HUD through this).</summary>
        public IEnumerable<string> HeartSprites => hearts.Select(h => h.sprite != null ? h.sprite.name : "");
        public string BannerText => banner.gameObject.activeSelf ? banner.text : "";

        public void Init()
        {
            var canvas = UiFactory.Canvas("Match Overlay", 15).transform;
            canvas.SetParent(transform, false);
            canvasRoot = canvas;
            card = UiFactory.Page(canvas, "Arena Card");
            card.sizeDelta = new Vector2(900, 300);
            cardTitle = UiFactory.Label(card, "", 64);
            cardLine = UiFactory.Label(card, "", 32);

            banner = UiFactory.Label(canvas, "", 36);
            var rect = (RectTransform)banner.transform;
            // Under the clock at the top centre.
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -110f);
            rect.sizeDelta = new Vector2(1000, 60);
            banner.color = new Color(1f, 0.718f, 0.012f); // amber #FFB703 (GDD 12.5)
            card.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);
            result = UiFactory.Label(canvas, "", 32);
            var resultRect = (RectTransform)result.transform;
            resultRect.anchorMin = resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.pivot = new Vector2(0.5f, 0.5f);
            resultRect.sizeDelta = new Vector2(1000, 600);
            result.raycastTarget = false;
            result.enabled = false; // the kit rows below are what the player sees; the text stays as the accessible summary
            result.gameObject.SetActive(false);

            resultButton = UiFactory.Button(canvas, "", () => resultAction?.Invoke());
            actionText = resultButton.GetComponentInChildren<Text>();
            var actionRect = (RectTransform)resultButton.transform;
            actionRect.anchorMin = actionRect.anchorMax = new Vector2(0.5f, 0.5f);
            actionRect.anchorMin = actionRect.anchorMax = new Vector2(0.5f, 0f);
            actionRect.anchoredPosition = new Vector2(0f, 70f);
            actionRect.sizeDelta = new Vector2(340f, 88f);
            resultButton.gameObject.SetActive(false);

            spectating = UiFactory.Label(canvas, "ĐANG QUAN SÁT · ← → đổi góc nhìn", 26);
            var spectatorRect = (RectTransform)spectating.transform;
            spectatorRect.anchorMin = spectatorRect.anchorMax = new Vector2(0.5f, 0f);
            spectatorRect.anchoredPosition = new Vector2(0f, 78f);
            spectatorRect.sizeDelta = new Vector2(700f, 45f);
            spectating.raycastTarget = false;
            spectating.gameObject.SetActive(false);
        }

        public void ShowCard(string title, string line, float seconds)
        {
            cardTitle.text = title;
            cardLine.text = line;
            card.gameObject.SetActive(true);
            cardUntil = Time.unscaledTime + seconds;
        }

        public void ShowBanner(string text, float seconds)
        {
            banner.text = text;
            banner.gameObject.SetActive(true);
            bannerUntil = Time.unscaledTime + seconds;
        }

        /// <summary>Hearts top-left (kit "5 - HUD"): full for lives left, grey for lives lost. Display only: host-confirmed values.</summary>
        public void ShowHud(int lives, int maxLives)
        {
            if (hud == null)
            {
                hud = UiFactory.Layer(canvasRoot, "Hud");
                hud.SetAsFirstSibling();
            }
            if (resultPage != null && resultPage.gameObject.activeSelf) return;
            hud.gameObject.SetActive(true);
            if (lives == shownLives && maxLives == shownMax) return;
            shownLives = lives; shownMax = maxLives;
            while (hearts.Count < maxLives)
                hearts.Add(UiFactory.Place(hud, "hud_heart", Vector2.up, new Vector2(40 + 120 * hearts.Count, -36), new Vector2(113, 91)));
            for (var i = 0; i < hearts.Count; i++)
            {
                hearts[i].gameObject.SetActive(i < maxLives);
                hearts[i].sprite = UiFactory.Kit(i < lives ? "hud_heart" : "hud_heart_lost");
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Round result (kit "3 - Giao diện tổng điểm mỗi round"): Round n banner, the ranked column on the right with the
        /// local fighter highlighted, and the local score ribbon. Chỉ trình bày kết quả đã được host xác nhận; không tự xếp lại thứ hạng.
        /// </summary>
        public void ShowResult(IReadOnlyList<MatchStanding> standings, Func<ulong, string> label, Func<ulong, int> fruit = null,
            int round = 0, ulong localId = 0)
        {
            BuildRoundPage(round, standings, label, fruit ?? DefaultFruit, localId);
            ShowResultText("KẾT QUẢ\n" + string.Join("\n", standings.Select(s =>
                $"{s.Rank}. {label(s.FighterId)} — {s.Points} điểm · {s.Lives} mạng")));
        }

        /// <summary>Session result (kit "4 - Giao diện final"): WINNER frame with the top three on podiums.</summary>
        public void ShowSessionResult(IReadOnlyList<MatchStanding> standings, Func<ulong, string> label, Func<ulong, int> fruit = null)
        {
            var headline = standings.Count(s => s.Rank == 1) == 1
                ? $"VÔ ĐỊCH · {label(standings[0].FighterId)}"
                : "PHIÊN ĐÃ KẾT THÚC";
            BuildWinnerPage(headline, standings, label, fruit ?? DefaultFruit);
            ShowResultText(headline + "\n" + string.Join("\n", standings.Select(s => $"{s.Rank}. {label(s.FighterId)}")));
        }

        public void OfferResultAction(string caption, Action onClick)
        {
            // Kit art exists for "Next" (pill on the round page, round button on the WINNER page); other captions
            // ("Về menu", "Tiếp tục") get the blank kit pill with their own text.
            var art = UiFactory.Kit(winnerLayout && caption == "Next" ? "btn_next_big" : "btn_" + UiFactory.Slug(caption));
            var image = resultButton.GetComponent<Image>();
            if (art != null) { image.sprite = art; image.type = Image.Type.Simple; image.preserveAspect = true; }
            else UiFactory.Slice(image, "btn_blank", 1f);
            actionText.enabled = art == null;
            actionText.text = caption;
            var rect = (RectTransform)resultButton.transform;
            if (winnerLayout)
            {
                // The empty Next slot of the WINNER frame.
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-12, -290);
                rect.sizeDelta = art != null ? new Vector2(239, 205) : new Vector2(340, 96);
            }
            else
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.right;
                rect.anchoredPosition = new Vector2(-100, 75);
                rect.sizeDelta = art != null ? new Vector2(233, 105) : new Vector2(340, 96);
            }
            resultAction = onClick;
        }

        public void ShowSpectating(bool visible) => spectating.gameObject.SetActive(visible && !result.gameObject.activeSelf);

        private void ShowResultText(string content)
        {
            card.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);
            result.text = content;
            if (hud != null) hud.gameObject.SetActive(false);
            result.gameObject.SetActive(true);
            resultReadyAt = Time.unscaledTime + ResultReadSeconds;
            resultAction = null;
            resultButton.gameObject.SetActive(false);
        }

        private static int DefaultFruit(ulong id) => (int)(id % 6);

        private static string Avatar(int fruit) => "avatar_" + Mathf.Abs(fruit) % 6;

        private RectTransform NewResultPage()
        {
            if (resultPage != null) Destroy(resultPage.gameObject);
            resultPage = UiFactory.Layer(canvasRoot, "Results");
            resultButton.transform.SetAsLastSibling();
            return resultPage;
        }

        private void BuildRoundPage(int round, IReadOnlyList<MatchStanding> standings, Func<ulong, string> label,
            Func<ulong, int> fruit, ulong localId)
        {
            winnerLayout = false;
            var page = NewResultPage();
            if (round is >= 1 and <= 5) UiFactory.Place(page, $"banner_round_{round}", Vector2.up, new Vector2(40, -30));
            else
            {
                var title = UiFactory.Outlined(UiFactory.Label(page, "KẾT QUẢ", 64), 3);
                var rect = title.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
                rect.anchoredPosition = new Vector2(40, -30);
                rect.sizeDelta = new Vector2(600, 110);
            }
            var y = -40f;
            foreach (var s in standings.Take(6))
            {
                var mine = s.FighterId == localId;
                var tint = mine ? Color.white : Greyed;
                var row = UiFactory.Place(page, "row_score", Vector2.one, new Vector2(0, y), new Vector2(s.Rank <= 3 ? 560 : 470, 120));
                row.name = $"Row {s.Rank}" + (mine ? " (you)" : "");
                row.preserveAspect = false;
                UiFactory.Slice(row, "row_score", 1f);
                row.color = tint;
                var x = 18f;
                if (s.Rank <= 3)
                {
                    // The medal hangs over the row's left end, as in the mockup.
                    var medal = UiFactory.Place(row.transform, "medal_" + s.Rank, new Vector2(0, 0.5f), new Vector2(-46, 0), new Vector2(110, 140));
                    medal.color = tint;
                    x = 76f;
                }
                else
                {
                    var place = UiFactory.Label(row.transform, $"{s.Rank}", 44);
                    place.color = mine ? UiFactory.Ink : Color.gray;
                    var placeRect = place.rectTransform;
                    placeRect.anchorMin = placeRect.anchorMax = placeRect.pivot = new Vector2(0, 0.5f);
                    placeRect.anchoredPosition = new Vector2(x, 0);
                    placeRect.sizeDelta = new Vector2(50, 90);
                    x += 50f;
                }
                UiFactory.Place(row.transform, Avatar(fruit(s.FighterId)), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(92, 92)).color = tint;
                var text = UiFactory.Label(row.transform, $"{label(s.FighterId).ToUpperInvariant()}\nSCORE: {s.Points} · {s.Lives} mạng", 30);
                text.alignment = TextAnchor.MiddleLeft;
                text.color = mine ? UiFactory.Ink : new Color(0.3f, 0.3f, 0.35f);
                var textRect = text.rectTransform;
                textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0, 0.5f);
                textRect.anchoredPosition = new Vector2(x + 108, 0);
                textRect.sizeDelta = new Vector2(360, 100);
                y -= 136f;
            }
            var own = standings.FirstOrDefault(s => s.FighterId == localId);
            if (standings.Any(s => s.FighterId == localId))
            {
                var ribbon = UiFactory.Place(page, "ribbon_score", Vector2.zero, new Vector2(0, 90));
                var score = UiFactory.Outlined(UiFactory.Label(ribbon.transform, $"SCORE: {own.Points}", 84), 4);
                score.color = Color.white;
                var scoreRect = score.rectTransform;
                scoreRect.anchorMin = Vector2.zero;
                scoreRect.anchorMax = Vector2.one;
                scoreRect.offsetMin = new Vector2(60, 40);
                scoreRect.offsetMax = new Vector2(-120, -60);
            }
        }

        private void BuildWinnerPage(string headline, IReadOnlyList<MatchStanding> standings, Func<ulong, string> label, Func<ulong, int> fruit)
        {
            winnerLayout = true;
            var page = NewResultPage();
            UiFactory.FullScreen(page, "frame_winner");
            var line = UiFactory.Outlined(UiFactory.Label(page, headline, 34), 2);
            var lineRect = line.rectTransform;
            lineRect.anchorMin = lineRect.anchorMax = lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.anchoredPosition = new Vector2(0, -190);
            lineRect.sizeDelta = new Vector2(1200, 60);
            // Podium slots of the mockup (canvas px from the centre): 2nd left, 1st centre, 3rd right.
            var slots = new[] { (x: 3f, y: 150f), (x: -482f, y: 115f), (x: 482f, y: 125f) };
            var podium = standings.Where(s => s.Rank <= 3).Take(3).ToList();
            for (var i = 0; i < podium.Count; i++)
            {
                var s = podium[i];
                var (x, y) = slots[i];
                var centre = new Vector2(0.5f, 0.5f);
                UiFactory.Place(page, Avatar(fruit(s.FighterId)), centre, new Vector2(x, y), new Vector2(210, 210)).name = $"Podium {s.Rank}";
                UiFactory.Place(page, "medal_" + s.Rank, centre, new Vector2(x - 160, y - 110), new Vector2(120, 156));
                var plate = UiFactory.Place(page, "plate", centre, new Vector2(x, y - 260), new Vector2(340, 112));
                plate.preserveAspect = false;
                UiFactory.Slice(plate, "plate", 1f);
                UiFactory.Place(plate.transform, Avatar(fruit(s.FighterId)), new Vector2(0, 0.5f), new Vector2(22, 4), new Vector2(76, 76));
                var text = UiFactory.Label(plate.transform, $"{label(s.FighterId).ToUpperInvariant()}\nSCORE: {s.Points}", 28);
                text.alignment = TextAnchor.MiddleLeft;
                var textRect = text.rectTransform;
                textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0, 0.5f);
                textRect.anchoredPosition = new Vector2(110, 4);
                textRect.sizeDelta = new Vector2(220, 90);
            }
        }

        private void Update()
        {
            if (card.gameObject.activeSelf && Time.unscaledTime >= cardUntil) card.gameObject.SetActive(false);
            if (banner.gameObject.activeSelf && Time.unscaledTime >= bannerUntil) banner.gameObject.SetActive(false);
            var actionReady = resultAction != null && Time.unscaledTime >= resultReadyAt;
            if (resultButton.gameObject.activeSelf != actionReady) resultButton.gameObject.SetActive(actionReady);
        }
    }
}
