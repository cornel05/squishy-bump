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
        private RectTransform resultPage, hud;
        private Text livesText;
        private int shownLives = -1, shownMax = -1;
        private Transform canvasRoot;
        public string ResultText => result != null && result.gameObject.activeSelf ? result.text : "";

        public bool CardVisible => card.gameObject.activeSelf;
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

        /// <summary>Hearts pill under the "players left" text (MatchHud draws the clock). Display only: the caller passes host-confirmed values.</summary>
        public void ShowHud(int lives, int maxLives)
        {
            if (hud == null)
            {
                hud = (RectTransform)new GameObject("Hud", typeof(RectTransform)).transform;
                hud.SetParent(canvasRoot, false);
                Stretch(hud);
                livesText = HudPill("hud_lives", new Vector2(0f, 1f), new Vector2(24f, -90f), 0.4f);
            }
            if (resultPage != null && resultPage.gameObject.activeSelf) return;
            hud.gameObject.SetActive(true);
            if (lives != shownLives || maxLives != shownMax) livesText.text = $"{lives}/{maxLives}";
            shownLives = lives; shownMax = maxLives;
        }

        private Text HudPill(string sprite, Vector2 anchor, Vector2 position, float textStart)
        {
            var pill = UiFactory.Icon(hud, sprite, new Vector2(300f, 103f));
            var rect = (RectTransform)pill.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(anchor.x, 1f);
            rect.anchoredPosition = position;
            var text = UiFactory.Label(rect, "", 40);
            text.color = Color.white;
            text.raycastTarget = false;
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = new Vector2(textStart, 0f);
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Chỉ trình bày kết quả đã được host xác nhận; không tự xếp lại thứ hạng.</summary>
        public void ShowResult(IReadOnlyList<MatchStanding> standings, Func<ulong, string> label,
            Func<ulong, int> fruit = null, Action onNext = null)
        {
            card.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);
            if (hud != null) hud.gameObject.SetActive(false);
            BuildResultPage(standings, label, fruit, onNext);
            result.text = "KẾT QUẢ\n" + string.Join("\n", standings.Select(s =>
                $"{s.Rank}. {label(s.FighterId)} — {s.Points} điểm · {s.Lives} mạng"));
            result.gameObject.SetActive(true);
        }

        private void BuildResultPage(IReadOnlyList<MatchStanding> standings, Func<ulong, string> label, Func<ulong, int> fruit, Action onNext)
        {
            if (resultPage != null) Destroy(resultPage.gameObject);
            resultPage = UiFactory.Page(canvasRoot, "Results");
            resultPage.sizeDelta = new Vector2(1000, 940);
            UiFactory.Label(resultPage, "KẾT QUẢ", 56);
            foreach (var s in standings.Take(6))
            {
                var row = new GameObject("Row " + s.Rank, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(resultPage, false);
                UiFactory.Slice(row.GetComponent<Image>(), "row_result");
                row.GetComponent<LayoutElement>().minHeight = 110;
                var layout = row.GetComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(24, 24, 8, 8);
                layout.spacing = 16;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = layout.childControlHeight = false;
                if (s.Rank <= 3) UiFactory.Icon(row.transform, "medal_" + s.Rank, new Vector2(70, 90));
                else
                {
                    var place = UiFactory.Label(row.transform, $"{s.Rank}.", 40);
                    ((RectTransform)place.transform).sizeDelta = new Vector2(70, 90);
                }
                var index = fruit != null ? fruit(s.FighterId) : (int)(s.FighterId % 6);
                UiFactory.Icon(row.transform, "avatar_" + Mathf.Abs(index) % 6, new Vector2(84, 84));
                var text = UiFactory.Label(row.transform, $"{label(s.FighterId).ToUpperInvariant()}\nSCORE: {s.Points} · {s.Lives} mạng", 30);
                text.alignment = TextAnchor.MiddleLeft;
                ((RectTransform)text.transform).sizeDelta = new Vector2(620, 90);
            }
            if (onNext != null) UiFactory.Button(resultPage, "Next", onNext);
        }

        private void Update()
        {
            if (card.gameObject.activeSelf && Time.unscaledTime >= cardUntil) card.gameObject.SetActive(false);
            if (banner.gameObject.activeSelf && Time.unscaledTime >= bannerUntil) banner.gameObject.SetActive(false);
        }
    }
}
