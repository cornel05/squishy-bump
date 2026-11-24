using System;
using System.Linq;
using System.Collections.Generic;
using SquishyBump.Core;
using SquishyBump.UI.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace SquishyBump.UI
{
    /// <summary>World boards + nút thao tác; không sửa LobbyState hoặc mô phỏng nhân vật.</summary>
    public sealed class LobbyPanel : MonoBehaviour
    {
        private Text board, summary;
        private LobbyConsole console;
        private RectTransform actions;
        private Button ready;
        private LobbyState state;
        private bool localReady, single, consoleClosed;
        private string[] animalNames = Array.Empty<string>();
        private readonly Dictionary<int, (Text Label, string Fruit, string Class)> stands = new();
        public string BoardText => board.text;
        public event Action<bool> ReadyRequested;
        /// <summary>Host desk: null = keep the room's current value (the panel's snapshot can be a tick old).</summary>
        public event Action<MatchRule?, int?> ConfigureRequested;
        /// <summary>Host desk: capacity (2–6) and public/private; null = unchanged.</summary>
        public event Action<int?, bool?> RoomRequested;
        public event Action<string> PasswordRequested;
        public LobbyConsole Console => console;
        public event Action LeaveRequested, StartSingleRequested, LookRequested;
        public void Init(Transform infoBoard, Transform controlsBoard, bool isSingle, IReadOnlyList<string> mapNames)
        {
            single = isSingle;
            board = WorldText(infoBoard, "Thông tin phòng");
            WorldText(controlsBoard, "ĐIỀU KHIỂN\nWASD: đi lại\nĐi vào vùng trước kệ để chọn trái\nTrái đã có người chọn bị khóa\nHost tới quầy chọn luật/độ dài\nEsc: mở con trỏ / rời phòng");
            var canvas = UiFactory.Canvas("Lobby actions", 15).transform; canvas.SetParent(transform, false);
            var page = actions = UiFactory.Page(canvas, "Lobby actions");
            page.anchorMin = page.anchorMax = new Vector2(0, 0); page.pivot = Vector2.zero;
            page.anchoredPosition = new Vector2(20, 20); page.sizeDelta = new Vector2(620, 460);
            summary = UiFactory.Label(page, "Đang đồng bộ lobby…", 24);
            ready = UiFactory.Button(page, single ? "Bắt đầu" : "Sẵn sàng / hủy", () =>
            { if (single) StartSingleRequested?.Invoke(); else ReadyRequested?.Invoke(!localReady); });
            UiFactory.Button(page, "Khóa chuột để xoay camera (Esc để mở)", () => LookRequested?.Invoke());
            UiFactory.Button(page, "Rời phòng", () => LeaveRequested?.Invoke());
            // The host desk is the team's lobby console (kit folder 7); it opens at the desk and hides again on X.
            console = new LobbyConsole(canvas, single, mapNames);
            console.ConfigureRequested += (rule, level) => ConfigureRequested?.Invoke(rule, level);
            console.RoomRequested += (capacity, isPublic) => RoomRequested?.Invoke(capacity, isPublic);
            console.PasswordChanged += text => PasswordRequested?.Invoke(text);
            console.StartRequested += () =>
            { if (single) StartSingleRequested?.Invoke(); else if (ready.interactable) ReadyRequested?.Invoke(!localReady); };
            console.CloseRequested += () => { consoleClosed = true; console.Root.gameObject.SetActive(false); };
            console.Root.gameObject.SetActive(false);
        }
        public void Show(LobbyState value, ulong localId, string hostAddress, int tickRate, Func<int, string> fruitName, bool atDesk)
        {
            if (value == null) return;
            state = value;
            var player = value.Players.FirstOrDefault(p => p.Id == localId);
            var present = value.Players.Any(p => p.Id == localId);
            localReady = player.Ready; ready.interactable = present && player.Fruit >= 0 && !value.Started;
            foreach (var stand in stands)
            {
                var occupied = value.Players.Any(p => p.Fruit == stand.Key);
                var owner = value.Players.FirstOrDefault(p => p.Fruit == stand.Key);
                var status = occupied ? $"ĐÃ CHỌN · {(owner.Id == localId ? "BẠN" : $"người {owner.Id}")}" : "ĐI VÀO ĐỂ CHỌN";
                stand.Value.Label.text = $"{stand.Value.Fruit}\n{stand.Value.Class}\n{status}";
                stand.Value.Label.color = occupied ? new Color(1, 0.75f, 0.25f) : Color.white;
            }
            var countdown = value.RemainingTicks > 0 ? $"\nBắt đầu sau {(value.RemainingTicks + tickRate - 1) / tickRate} s" : "";
            board.text = $"SQUISHY CUP · L1 → L{value.FinalLevel}\n{value.Rule}\nIP host: {hostAddress}\n" +
                string.Join("\n", value.Players.Select(p => $"Người {p.Id}: {(p.Fruit < 0 ? "chưa chọn" : fruitName(p.Fruit))} · {(p.Ready ? "READY" : "chưa ready")}")) + countdown;
            summary.text = $"{value.Rule} · L1 → L{value.FinalLevel}\nBạn: {(player.Fruit < 0 ? "đi vào kệ để chọn trái" : fruitName(player.Fruit))}" + countdown;
            if (!atDesk) consoleClosed = false;
            if (animalNames.Length == 0) animalNames = Enumerable.Range(0, 6).Select(fruitName).ToArray();
            console.Show(value, localId, animalNames);
            var open = atDesk && !value.Started && !consoleClosed;
            console.Root.gameObject.SetActive(open);
            actions.gameObject.SetActive(!open); // the console has its own START MATCH and would sit under this panel
        }
        private Text WorldText(Transform surface, string text)
            => WorldText(surface, text, Vector3.back * 0.2f, new Vector2(600, 600));

        /// <summary>Numbers of a class card, from the balance data (never typed in): kg, m/s, m/s of dash, grip.</summary>
        public readonly struct ClassStats
        {
            public ClassStats(float weight, float maxSpeed, float dash, float grip) { Weight = weight; MaxSpeed = maxSpeed; Dash = dash; Grip = grip; }
            public float Weight { get; }
            public float MaxSpeed { get; }
            public float Dash { get; }
            public float Grip { get; }
        }

        private static readonly Vector2 CardSheet = new(1572, 527);
        // Value column of the card art, in sprite pixels: left edge, right edge, and the vertical centre of its four rows.
        private const float CardValueLeft = 996, CardValueRight = 1255, CardRowHeight = 44;
        private static readonly float[] CardRows = { 145, 193, 248, 300 };
        private const float CardHeight = 3.4f, CardScale = 0.0012f;

        private void BuildCard(Transform surface, string className, ClassStats stats)
        {
            var art = UiFactory.Kit("card_" + className.ToLowerInvariant());
            if (art == null) return;
            var go = new GameObject($"Class card {className}", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(transform, false);
            go.transform.position = surface.position + Vector3.up * CardHeight;
            go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one * CardScale;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = CardSheet;
            var image = new GameObject("Art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(go.transform, false);
            image.sprite = art; image.raycastTarget = false;
            var artRect = (RectTransform)image.transform;
            artRect.anchorMin = Vector2.zero; artRect.anchorMax = Vector2.one; artRect.offsetMin = artRect.offsetMax = Vector2.zero;
            var values = new[] { $"{stats.Weight:0.#} KG", $"{stats.MaxSpeed:0.0} M/S", $"{stats.Dash:0.0} M/S", $"{stats.Grip:0.0}" };
            for (var i = 0; i < values.Length; i++)
                UiFactory.SheetText(artRect, $"Value {i + 1}", CardSheet, CardValueLeft, CardRows[i] - CardRowHeight / 2,
                    CardValueRight, CardRows[i] + CardRowHeight / 2, values[i], 44, TextAnchor.MiddleLeft);
        }

        public void RegisterStand(int index, Transform surface, string fruitName, string className, ClassStats stats = default)
        {
            if (stats.Weight > 0f) BuildCard(surface, className, stats);
            var label = WorldText(surface, "", Vector3.up * 2.3f, new Vector2(480, 160));
            label.name = $"Stand {index} status";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 26;
            stands.Add(index, (label, fruitName, className));
        }

        private Text WorldText(Transform surface, string text, Vector3 offset, Vector2 size)
        {
            var go = new GameObject("Lobby board text", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(transform, false);
            go.transform.position = surface.position + offset;
            go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one * 0.003f;
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)go.transform).sizeDelta = size;
            var label = UiFactory.Label(go.transform, text, 32); label.raycastTarget = false;
            var rect = (RectTransform)label.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            label.alignment = TextAnchor.UpperLeft; return label;
        }
    }
}
