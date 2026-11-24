using System.Collections.Generic;
using System.Linq;
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
            public Renderer[] Body;
            public ushort Id;
            public Vector2 ScreenPoint;
        }

        private readonly Dictionary<ushort, Tag> tags = new();
        private readonly List<Tag> layout = new(6);
        private readonly List<Rect> occupied = new(6);
        private Canvas canvas;
        private Text clock, ringCaption, remaining, fruit, classBadge, dash, boardRanks, boardValues;
        private Image badgeBackground, avatar, board;
        private string boardKey;
        private RectTransform scoreboard;
        private Text scoreboardTitle, scoreboardRows;
        private Image dashRing;
        private Sprite ringSprite;
        private Texture2D ringTexture;
        private BalanceProfile balance;
        private int level;
        private const float TagGapPixels = 6;

        public string ClockText => clock.text;
        public Color ClockColor => clock.color;
        public string RemainingText => remaining.text;
        public string FruitText => fruit.text;
        public string ClassText => classBadge.text;
        public string DashText => dash.text;
        public float DashFill => dashRing.fillAmount;
        /// <summary>Corner board as shown, one "rank name value" line per fighter.</summary>
        public string BoardText { get; private set; } = "";
        public bool ScoreboardVisible => canvas.enabled && scoreboard != null && scoreboard.gameObject.activeSelf;

        public void SetVisible(bool visible) => canvas.enabled = visible;
        public void ShowScoreboard(bool held)
        {
            if (scoreboard != null) scoreboard.gameObject.SetActive(held);
        }

        /// <summary>Hiển thị hạng/điểm/mạng đã được host xác nhận; client không tự xếp lại.</summary>
        public void SetStandings(IReadOnlyList<MatchStanding> rows, System.Func<ulong, string> label,
            System.Func<ulong, Color> color, MatchRule rule, int currentLevel, int finalLevel)
        {
            if (scoreboard == null)
            {
                var panel = UiFactory.Place(canvas.transform, "panel_box", Vector2.one * 0.5f, Vector2.zero, new Vector2(1100, 620));
                UiFactory.Slice(panel, "panel_box");
                panel.name = "Scoreboard";
                scoreboard = panel.rectTransform;
                scoreboardTitle = UiFactory.Label(scoreboard, "", 42);
                scoreboardTitle.rectTransform.anchorMin = scoreboardTitle.rectTransform.anchorMax = new Vector2(0.5f, 1);
                scoreboardTitle.rectTransform.anchoredPosition = new Vector2(0, -65);
                scoreboardTitle.rectTransform.sizeDelta = new Vector2(1000, 80);
                scoreboardRows = UiFactory.Label(scoreboard, "", 34);
                scoreboardRows.alignment = TextAnchor.UpperLeft;
                scoreboardRows.lineSpacing = 1.5f;
                scoreboardRows.rectTransform.anchorMin = Vector2.zero;
                scoreboardRows.rectTransform.anchorMax = Vector2.one;
                scoreboardRows.rectTransform.offsetMin = new Vector2(60, 40);
                scoreboardRows.rectTransform.offsetMax = new Vector2(-60, -130);
                scoreboard.gameObject.SetActive(false);
            }
            scoreboardTitle.text = $"SCOREBOARD - L{currentLevel}/{finalLevel} - {rule}";
            string Value(MatchStanding row) => rule == MatchRule.Rank ? $"{row.Points} pts - {row.Lives} lives" : row.Lives > 0 ? "ALIVE" : "OUT";
            BoardText = string.Join("\n", rows.Select(row => $"{row.Rank} {label(row.FighterId)} - {Value(row)}"));
            scoreboardRows.text = string.Join("\n", rows.Select(row =>
                $"{row.Rank}. {label(row.FighterId).ToUpperInvariant()} - {Value(row)}{(rule == MatchRule.Rank && row.Lives == 0 ? " - OUT" : "")}"));
            boardRanks.fontSize = boardValues.fontSize = 22;
            boardRanks.rectTransform.anchorMax = new Vector2(0.65f, 1);
            boardValues.rectTransform.anchorMin = new Vector2(0.65f, 0);
            boardRanks.text = string.Join("\n", rows.Select(row => $"{row.Rank} <color=#{ColorUtility.ToHtmlStringRGB(color(row.FighterId))}>{label(row.FighterId).ToUpperInvariant()}</color>"));
            boardValues.text = string.Join("\n", rows.Select(row => rule == MatchRule.Rank ? $"{row.Points}p {row.Lives}L" : row.Lives > 0 ? "O" : "X"));
            board.gameObject.SetActive(rows.Count > 0);
        }

        public void Init(BalanceProfile profile, int levelNumber)
        {
            balance = profile;
            level = levelNumber;
            canvas = UiFactory.Canvas("Match HUD", 10);
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            // HUD không có điều khiển, không chặn menu/pause.
            Destroy(canvas.GetComponent<GraphicRaycaster>());
            // Layout of the kit mockup "5 - HUD": clock pill top centre, ranking board top right, weight frame bottom left.
            // The hearts are drawn by MatchOverlay.ShowHud (top left).
            var timer = UiFactory.Place(canvas.transform, "hud_timer", new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(325, 117));
            clock = InArt(timer, "Clock", new Vector2(0.34f, 0.08f), new Vector2(0.95f, 0.92f), 52);
            ringCaption = Label("Ring Caption", new Vector2(0.5f, 1), new Vector2(0, -144), new Vector2(320, 40), 26);
            ringCaption.text = level == 1 ? "Ring" : "";
            remaining = Label("Remaining", new Vector2(0.5f, 1), new Vector2(0, level == 1 ? -180 : -144), new Vector2(360, 44), 28);
            board = UiFactory.Place(canvas.transform, "hud_board", Vector2.one, new Vector2(-30, -30), new Vector2(383, 236));
            board.preserveAspect = false;
            UiFactory.Slice(board, "hud_board", 1f);
            boardRanks = InArt(board, "Board", new Vector2(0.05f, 0.04f), new Vector2(0.8f, 0.96f), 30);
            boardRanks.alignment = TextAnchor.UpperLeft;
            boardRanks.color = Color.white;
            boardValues = InArt(board, "Board Values", new Vector2(0.75f, 0.04f), new Vector2(0.95f, 0.96f), 30);
            boardValues.alignment = TextAnchor.UpperRight;
            boardValues.color = Color.white;
            board.gameObject.SetActive(false);
            badgeBackground = UiFactory.Place(canvas.transform, "hud_weight", Vector2.zero, new Vector2(24, 24), new Vector2(280, 140));
            avatar = UiFactory.Place(badgeBackground.transform, "avatar_0", new Vector2(0, 0.5f), new Vector2(14, 8), new Vector2(100, 100));
            classBadge = InArt(badgeBackground, "Class Badge", new Vector2(0.44f, 0.1f), new Vector2(0.97f, 0.95f), 30);
            classBadge.color = UiFactory.Ink;
            classBadge.GetComponent<Outline>().effectColor = Color.white;
            fruit = Label("Fruit", Vector2.zero, new Vector2(24, 170), new Vector2(400, 50), 30);
            SetFruit(null);
            dash = Label("Dash", Vector2.zero, new Vector2(104, 226), new Vector2(360, 65), 30);
            var ring = new GameObject("Dash Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)ring.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24, 226);
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
            text.color = Color.white;
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

        /// <summary>Text laid over kit art, stretched between two anchors of it.</summary>
        private Text InArt(Image art, string name, Vector2 min, Vector2 max, int fontSize)
        {
            var text = UiFactory.Label(art.transform, "", fontSize);
            text.name = name;
            text.raycastTarget = false;
            // Vertical anchors stay 0..1 (the HUD keeps its labels on edges); the y insets become offsets.
            var rect = text.rectTransform;
            var height = ((RectTransform)art.transform).sizeDelta.y;
            rect.anchorMin = new Vector2(min.x, 0);
            rect.anchorMax = new Vector2(max.x, 1);
            rect.offsetMin = new Vector2(0, min.y * height);
            rect.offsetMax = new Vector2(0, (max.y - 1) * height);
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = UiFactory.Ink;
            outline.effectDistance = new Vector2(2, -2);
            return text;
        }

        /// <summary>Ranking board (top right), sorted by value, highest first; ties keep the caller's order.</summary>
        public void SetBoard(IReadOnlyList<(string Name, int Value, Color Color)> rows)
        {
            var sorted = rows.Select((r, i) => (r, i)).OrderByDescending(p => p.r.Value).ThenBy(p => p.i).Select(p => p.r).ToList();
            var key = string.Join("|", sorted.Select(r => $"{r.Name}:{r.Value}"));
            if (key == boardKey) return;
            boardKey = key;
            board.gameObject.SetActive(sorted.Count > 0);
            boardRanks.text = string.Join("\n", sorted.Select((r, i) =>
                $"{i + 1}  <color=#{ColorUtility.ToHtmlStringRGB(r.Color)}>{r.Name.ToUpperInvariant()}</color>"));
            boardValues.text = string.Join("\n", sorted.Select(r => r.Value.ToString()));
            BoardText = string.Join("\n", sorted.Select((r, i) => $"{i + 1} {r.Name} {r.Value}"));
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
            var whole = Mathf.CeilToInt(seconds);
            clock.text = $"{whole / 60:00}:{whole % 60:00}";
            clock.color = seconds <= 10 ? Color.red : UiFactory.Teal;
        }

        public void SetRemaining(int alive, int total) => remaining.text = $"Fruit {alive}/{total}";

        /// <summary>Weight frame (bottom left): the fruit's avatar, class and mass. <paramref name="fruitIndex"/> picks the avatar.</summary>
        public void SetFruit(FruitDefinition definition, int fruitIndex = 0)
        {
            fruit.text = definition == null ? "" : definition.DisplayName;
            classBadge.text = definition == null ? "" : $"{definition.Class.name}\n{definition.Class.Mass:0.#} kg";
            badgeBackground.gameObject.SetActive(definition != null);
            avatar.sprite = UiFactory.Kit("avatar_" + Mathf.Abs(fruitIndex) % 6);
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
            var label = Label($"Wobble {id}", Vector2.one * 0.5f, Vector2.zero, new Vector2(110, 50), 30);
            // Điểm bám là đáy nhãn: toàn bộ chữ nằm phía trên hình quả, không cắt qua đầu.
            label.rectTransform.pivot = new Vector2(0.5f, 0);
            tags.Add(id, new Tag { Id = id, Target = target, Height = height, Label = label,
                Body = target.GetComponentsInChildren<Renderer>().Where(r => r.enabled &&
                    (r is MeshRenderer || r is SkinnedMeshRenderer)).ToArray() });
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
            layout.Clear(); occupied.Clear();
            foreach (var tag in tags.Values)
            {
                var point = tag.Target != null ? camera.WorldToScreenPoint(tag.Target.position + Vector3.up * tag.Height) : Vector3.back;
                var bodyArea = tag.Alive && tag.Target != null && tag.Target.gameObject.activeInHierarchy ?
                    CollectBodyAreas(camera, tag.Body) : null;
                if (bodyArea.HasValue)
                    point = new Vector3(bodyArea.Value.center.x,
                        bodyArea.Value.yMax + TagGapPixels * canvas.scaleFactor, 1);
                var size = tag.Label.rectTransform.rect.size * canvas.scaleFactor;
                var visible = tag.Alive && tag.Target != null && tag.Target.gameObject.activeInHierarchy && point.z > 0 &&
                    point.x >= size.x * 0.5f && point.x <= Screen.width - size.x * 0.5f &&
                    point.y >= 0 && point.y + size.y <= Screen.height;
                tag.Label.gameObject.SetActive(visible);
                if (!visible) continue;
                tag.ScreenPoint = point;
                layout.Add(tag);
            }
            // Xếp từ thấp lên cao: chỉ nâng nhãn bị chạm, không đưa chữ xuống che quả.
            layout.Sort((a, b) =>
            {
                var vertical = a.ScreenPoint.y.CompareTo(b.ScreenPoint.y);
                return vertical != 0 ? vertical : a.Id.CompareTo(b.Id);
            });
            foreach (var tag in layout)
            {
                var stroke = 4 * canvas.scaleFactor; // Outline 2 px mỗi phía, theo reference canvas.
                var width = tag.Label.preferredWidth * canvas.scaleFactor + stroke;
                var height = tag.Label.preferredHeight * canvas.scaleFactor + stroke;
                var centerY = tag.ScreenPoint.y + tag.Label.rectTransform.rect.height * canvas.scaleFactor * 0.5f;
                var area = new Rect(tag.ScreenPoint.x - width * 0.5f, centerY - height * 0.5f, width, height);
                for (var attempt = 0; attempt <= occupied.Count; attempt++)
                {
                    var moved = false;
                    foreach (var other in occupied)
                    {
                        if (!area.Overlaps(other)) continue;
                        var lift = other.yMax + TagGapPixels * canvas.scaleFactor - area.yMin;
                        tag.ScreenPoint.y += lift; area.y += lift; moved = true; break;
                    }
                    if (!moved) break;
                }
                if (tag.ScreenPoint.y + tag.Label.rectTransform.rect.height * canvas.scaleFactor > Screen.height)
                { tag.Label.gameObject.SetActive(false); continue; }
                occupied.Add(area);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, tag.ScreenPoint, null, out var local);
                // Local point dùng gốc giữa canvas; các tag được đặt bằng localPosition.
                tag.Label.rectTransform.localPosition = local;
            }
        }

        private Rect? CollectBodyAreas(Camera camera, Renderer[] renderers)
        {
            Rect? bodyArea = null;
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                if (camera.WorldToScreenPoint(renderer.bounds.center).z <= 0) continue;
                var bounds = renderer.localBounds;
                var min = Vector2.one * float.PositiveInfinity;
                var max = Vector2.one * float.NegativeInfinity;
                for (var i = 0; i < 8; i++)
                {
                    var corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var screen = camera.WorldToScreenPoint(renderer.localToWorldMatrix.MultiplyPoint3x4(corner));
                    if (screen.z <= 0) continue;
                    min = Vector2.Min(min, screen); max = Vector2.Max(max, screen);
                }
                if (float.IsPositiveInfinity(min.x)) continue;
                var area = Rect.MinMaxRect(Mathf.Max(0, min.x), Mathf.Max(0, min.y),
                    Mathf.Min(Screen.width, max.x), Mathf.Min(Screen.height, max.y));
                if (area.width <= 0 || area.height <= 0) continue;
                occupied.Add(area);
                bodyArea = bodyArea.HasValue ? Rect.MinMaxRect(
                    Mathf.Min(bodyArea.Value.xMin, area.xMin), Mathf.Min(bodyArea.Value.yMin, area.yMin),
                    Mathf.Max(bodyArea.Value.xMax, area.xMax), Mathf.Max(bodyArea.Value.yMax, area.yMax)) : area;
            }
            return bodyArea;
        }

        private void OnDestroy()
        {
            if (ringSprite != null) Destroy(ringSprite);
            if (ringTexture != null) Destroy(ringTexture);
        }
    }
}
