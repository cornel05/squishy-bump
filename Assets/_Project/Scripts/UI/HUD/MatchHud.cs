using System.Collections.Generic;
using SquishyBump.Core;
using SquishyBump.UI.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace SquishyBump.UI
{
    /// <summary>HUD chỉ nhận trạng thái đã xác nhận từ App; không sửa gameplay (GDD 11.2).</summary>
    public sealed class MatchHud : MonoBehaviour
    {
        private sealed class Tag
        {
            public Transform Target;
            public float Height;
            public Text Label;
            public bool Alive;
        }

        private readonly Dictionary<ushort, Tag> tags = new();
        private Canvas canvas;
        private Text clock, remaining, fruit, classBadge, dash;
        private Image badgeBackground;
        private Image dashRing;
        private Sprite ringSprite;
        private Texture2D ringTexture;
        private BalanceProfile balance;
        private int level;

        public string ClockText => clock.text;
        public Color ClockColor => clock.color;
        public string RemainingText => remaining.text;
        public string FruitText => fruit.text;
        public string ClassText => classBadge.text;
        public string DashText => dash.text;
        public float DashFill => dashRing.fillAmount;

        public void Init(BalanceProfile profile, int levelNumber)
        {
            balance = profile;
            level = levelNumber;
            canvas = UiFactory.Canvas("Match HUD", 10);
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            // HUD không có điều khiển, không chặn menu/pause.
            Destroy(canvas.GetComponent<GraphicRaycaster>());
            clock = Label("Clock", new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(320, 70), 44);
            remaining = Label("Remaining", Vector2.up, new Vector2(24, -24), new Vector2(360, 60), 32);
            fruit = Label("Fruit", Vector2.zero, new Vector2(24, 86), new Vector2(400, 60), 30);
            var badge = new GameObject("Class Badge Background", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(canvas.transform, false);
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = badgeRect.pivot = Vector2.zero;
            badgeRect.anchoredPosition = new Vector2(24, 24);
            badgeRect.sizeDelta = new Vector2(400, 52);
            badgeBackground = badge.GetComponent<Image>();
            badgeBackground.color = new Color(0.12f, 0.1f, 0.18f, 0.92f);
            badgeBackground.raycastTarget = false;
            classBadge = Label("Class Badge", Vector2.zero, new Vector2(24, 24), new Vector2(400, 52), 30);
            SetFruit(null);
            dash = Label("Dash", Vector2.zero, new Vector2(104, 150), new Vector2(360, 65), 30);
            var ring = new GameObject("Dash Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)ring.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24, 150);
            rect.sizeDelta = new Vector2(64, 64);
            dashRing = ring.GetComponent<Image>();
            dashRing.raycastTarget = false;
            ringTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            for (var y = 0; y < 64; y++)
                for (var x = 0; x < 64; x++)
                {
                    var radius = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                    pixels[y * 64 + x] = radius >= 23 && radius <= 31 ? Color.white : Color.clear;
                }
            ringTexture.SetPixels(pixels);
            ringTexture.Apply();
            ringSprite = Sprite.Create(ringTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
            dashRing.sprite = ringSprite;
            dashRing.type = Image.Type.Filled;
            dashRing.fillMethod = Image.FillMethod.Radial360;
            SetClock(0);
            SetDash(0);
        }

        private Text Label(string name, Vector2 anchor, Vector2 offset, Vector2 size, int fontSize)
        {
            var text = UiFactory.Label(canvas.transform, "", fontSize);
            text.name = name;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleCenter;
            var rect = (RectTransform)text.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
            return text;
        }

        public void SetClock(float elapsedSeconds)
        {
            var until = balance.TimeLimitSeconds;
            var ringPending = level == 1;
            if (ringPending)
            {
                until = 0;
                foreach (var drop in balance.RingDropSeconds)
                    if (drop > elapsedSeconds) { until = drop; break; }
            }
            var seconds = Mathf.Max(0, until - elapsedSeconds);
            clock.text = $"{(ringPending ? "Ring" : "Time")} {Mathf.CeilToInt(seconds)} s";
            clock.color = seconds <= 10 ? Color.red : Color.white;
        }

        public void SetRemaining(int alive, int total) => remaining.text = $"Fruit {alive}/{total}";

        public void SetFruit(FruitDefinition definition)
        {
            fruit.text = definition == null ? "" : definition.DisplayName;
            classBadge.text = definition == null ? "" : $"{definition.Class.name} · {definition.Class.Mass:0.#} kg";
            badgeBackground.enabled = definition != null;
            if (definition != null) fruit.color = definition.Color;
        }

        public void SetDash(float seconds)
        {
            seconds = Mathf.Clamp(seconds, 0, balance.DashCooldownSeconds);
            dash.text = seconds > 0 ? $"Dash {seconds:0.0} s" : "Dash READY";
            dashRing.fillAmount = 1 - seconds / balance.DashCooldownSeconds;
            dashRing.color = seconds > 0 ? Color.white : Color.green;
        }

        public void Register(ushort id, Transform target, float height)
        {
            if (tags.ContainsKey(id)) return;
            tags.Add(id, new Tag { Target = target, Height = height,
                Label = Label($"Wobble {id}", Vector2.one * 0.5f, Vector2.zero, new Vector2(110, 50), 30) });
        }

        public void SetWobble(ushort id, int hits, bool alive)
        {
            if (!tags.TryGetValue(id, out var tag)) return;
            tag.Alive = alive;
            tag.Label.text = $"×{hits}";
            tag.Label.color = hits >= 5 ? Color.red : Color.white;
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null) return;
            var parent = (RectTransform)canvas.transform;
            foreach (var tag in tags.Values)
            {
                var point = tag.Target != null ? camera.WorldToScreenPoint(tag.Target.position + Vector3.up * tag.Height) : Vector3.back;
                var visible = tag.Alive && tag.Target != null && point.z > 0 &&
                    point.x >= 0 && point.x <= Screen.width && point.y >= 0 && point.y <= Screen.height;
                tag.Label.gameObject.SetActive(visible);
                if (!visible) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, point, null, out var local);
                // Local point dùng gốc giữa canvas; các tag được đặt bằng localPosition.
                tag.Label.rectTransform.localPosition = local;
            }
        }

        private void OnDestroy()
        {
            if (ringSprite != null) Destroy(ringSprite);
            if (ringTexture != null) Destroy(ringTexture);
        }
    }
}
