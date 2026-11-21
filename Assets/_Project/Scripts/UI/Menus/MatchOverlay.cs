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
        public string ResultText => result != null && result.gameObject.activeSelf ? result.text : "";

        public bool CardVisible => card.gameObject.activeSelf;
        public string BannerText => banner.gameObject.activeSelf ? banner.text : "";

        public void Init()
        {
            var canvas = UiFactory.Canvas("Match Overlay", 15).transform;
            canvas.SetParent(transform, false);
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

        /// <summary>Chỉ trình bày kết quả đã được host xác nhận; không tự xếp lại thứ hạng.</summary>
        public void ShowResult(IReadOnlyList<MatchStanding> standings, Func<ulong, string> label)
        {
            card.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);
            result.text = "KẾT QUẢ\n" + string.Join("\n", standings.Select(s =>
                $"{s.Rank}. {label(s.FighterId)} — {s.Points} điểm · {s.Lives} mạng"));
            result.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (card.gameObject.activeSelf && Time.unscaledTime >= cardUntil) card.gameObject.SetActive(false);
            if (banner.gameObject.activeSelf && Time.unscaledTime >= bannerUntil) banner.gameObject.SetActive(false);
        }
    }
}
