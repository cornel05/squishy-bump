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
        private RectTransform settings;
        private Button ready;
        private LobbyState state;
        private bool localReady, single;
        private readonly Dictionary<int, (Text Label, string Fruit, string Class)> stands = new();
        public string BoardText => board.text;
        public event Action<bool> ReadyRequested;
        /// <summary>Host desk: null = keep the room's current value (the panel's snapshot can be a tick old).</summary>
        public event Action<MatchRule?, int?> ConfigureRequested;
        public event Action LeaveRequested, StartSingleRequested, LookRequested;
        public void Init(Transform infoBoard, Transform controlsBoard, bool isSingle)
        {
            single = isSingle;
            board = WorldText(infoBoard, "Thông tin phòng");
            WorldText(controlsBoard, "ĐIỀU KHIỂN\nWASD: đi lại\nĐi vào vùng trước kệ để chọn trái\nTrái đã có người chọn bị khóa\nHost tới quầy chọn luật/độ dài\nEsc: mở con trỏ / rời phòng");
            var canvas = UiFactory.Canvas("Lobby actions", 15).transform; canvas.SetParent(transform, false);
            var page = UiFactory.Page(canvas, "Lobby actions");
            page.anchorMin = page.anchorMax = new Vector2(0, 0); page.pivot = Vector2.zero;
            page.anchoredPosition = new Vector2(20, 20); page.sizeDelta = new Vector2(620, 460);
            summary = UiFactory.Label(page, "Đang đồng bộ lobby…", 24);
            ready = UiFactory.Button(page, single ? "Bắt đầu" : "Sẵn sàng / hủy", () =>
            { if (single) StartSingleRequested?.Invoke(); else ReadyRequested?.Invoke(!localReady); });
            UiFactory.Button(page, "Khóa chuột để xoay camera (Esc để mở)", () => LookRequested?.Invoke());
            UiFactory.Button(page, "Rời phòng", () => LeaveRequested?.Invoke());
            settings = UiFactory.Page(canvas, "Host desk"); settings.sizeDelta = new Vector2(600, 900);
            settings.anchorMin = settings.anchorMax = new Vector2(1, 0.5f); settings.pivot = new Vector2(1, 0.5f);
            settings.anchoredPosition = new Vector2(-20, 0);
            UiFactory.Label(settings, "QUẦY HOST", 32);
            UiFactory.Button(settings, "Survival", () => ConfigureRequested?.Invoke(MatchRule.Survival, null));
            UiFactory.Button(settings, "Rank", () => ConfigureRequested?.Invoke(MatchRule.Rank, null));
            if (!single)
                for (var i = 1; i <= 5; i++)
                { var level = i; UiFactory.Button(settings, $"L1 → L{level}", () => ConfigureRequested?.Invoke(null, level)); }
            settings.gameObject.SetActive(false);
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
            settings.gameObject.SetActive(atDesk && !value.Started);
        }
        private Text WorldText(Transform surface, string text)
            => WorldText(surface, text, Vector3.back * 0.2f, new Vector2(600, 600));

        public void RegisterStand(int index, Transform surface, string fruitName, string className)
        {
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
