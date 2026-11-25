using System;
using System.Collections.Generic;
using SquishyBump.Core;
using SquishyBump.UI.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace SquishyBump.UI
{
    /// <summary>
    /// The lobby's host desk as the team's handheld console (kit "7 - giao diện lobby"). Tabs MATCH SETUP (six player slots)
    /// and MAP (final level), D-pad rule, CAPACITY, LOBBY MODE and PASSWORD controls sit on top of the baked art
    /// (<c>console_lobby</c>). It only raises requests; the host's LobbyRoom decides. Layout is in sprite pixels (2316 × 1131).
    /// </summary>
    public sealed class LobbyConsole
    {
        private static readonly Vector2 Sheet = new(2316, 1131);
        private const float Width = 1500f;
        private const int Slots = 6;
        private static readonly Color Dim = new(0.65f, 0.55f, 0.93f, 0.7f), Lit = new(0.2f, 0.9f, 0.9f, 0.4f);
        private static readonly Color Selected = new(1f, 0.62f, 0.86f, 1f), Plain = Color.white, Disabled = new(0.8f, 0.8f, 0.8f, 0.6f);

        private readonly bool single;
        private readonly Text ruleText, capacityText;
        private readonly Image setupDim, mapDim, mapLit, privateLit, publicLit;
        private readonly RectTransform setupPage, mapPage;
        private readonly List<(Image Row, Image Avatar, Text Name, Text Ready)> slots = new();
        private readonly List<Image> maps = new();
        private readonly Slider capacity;
        private readonly InputField password;
        private MatchRule rule = MatchRule.Survival;
        private int finalLevel = 1;
        private bool mapTab;

        /// <summary>Host actions (rule, final level), (capacity, public), password, start/ready and close.</summary>
        public event Action<MatchRule?, int?> ConfigureRequested;
        public event Action<int?, bool?> RoomRequested;
        public event Action<string> PasswordChanged;
        public event Action StartRequested, CloseRequested;

        public RectTransform Root { get; }

        public LobbyConsole(Transform canvas, bool isSingle, IReadOnlyList<string> mapNames)
        {
            single = isSingle;
            Root = (RectTransform)new GameObject("Host desk", typeof(RectTransform), typeof(Image)).transform;
            Root.SetParent(canvas, false);
            Root.anchorMin = Root.anchorMax = Root.pivot = new Vector2(0.5f, 0.5f);
            Root.sizeDelta = new Vector2(Width, Width * Sheet.y / Sheet.x);
            var art = Root.GetComponent<Image>();
            art.sprite = UiFactory.Kit("console_lobby");
            art.raycastTarget = true; // the console swallows clicks: nothing behind it reacts

            setupDim = UiFactory.Tint(Root, "Setup dim", Sheet, 486, 60, 836, 160, Dim); setupDim.gameObject.SetActive(false);
            mapDim = UiFactory.Tint(Root, "Map dim", Sheet, 1532, 60, 1844, 160, Dim);
            mapLit = UiFactory.Tint(Root, "Map lit", Sheet, 1532, 60, 1844, 160, Lit); mapLit.gameObject.SetActive(false);
            mapDim.gameObject.SetActive(false);
            ruleText = UiFactory.SheetText(Root, "Rule", Sheet, 944, 54, 1408, 166, "", 40);

            setupPage = UiFactory.PxRect(Root, "Setup page", Sheet, 0, 0, Sheet.x, Sheet.y); // pages cover the sheet so pixel boxes stay valid
            mapPage = UiFactory.PxRect(Root, "Map page", Sheet, 0, 0, Sheet.x, Sheet.y);
            BuildSlots();
            BuildMaps(mapNames);

            // Bottom bar: capacity slider, private/public, password.
            var track = UiFactory.PxRect(Root, "Capacity", Sheet, 800, 884, 940, 924);
            capacity = UiFactory.BareSlider(track, "Capacity slider", 26, LobbyState.MinCapacity, LobbyState.MaxCapacity, LobbyState.MaxCapacity,
                v => RoomRequested?.Invoke(Mathf.RoundToInt(v), null));
            var capacityRect = (RectTransform)capacity.transform; // fill the box placed over the baked track
            capacityRect.anchorMin = Vector2.zero; capacityRect.anchorMax = Vector2.one;
            capacityRect.offsetMin = capacityRect.offsetMax = Vector2.zero;
            capacity.wholeNumbers = true;
            capacityText = UiFactory.SheetText(Root, "Capacity value", Sheet, 946, 876, 1000, 932, "6", 36);
            publicLit = UiFactory.Tint(Root, "Public lit", Sheet, 1171, 868, 1280, 920, new Color(1f, 1f, 1f, 0.38f));
            privateLit = UiFactory.Tint(Root, "Private lit", Sheet, 1055, 868, 1171, 920, new Color(1f, 1f, 1f, 0.38f));
            UiFactory.Hotspot(Root, "Mode private", Sheet, 1055, 868, 1171, 920, () => RoomRequested?.Invoke(null, false));
            UiFactory.Hotspot(Root, "Mode public", Sheet, 1171, 868, 1280, 920, () => RoomRequested?.Invoke(null, true));
            password = BuildPassword();

            if (single) UiFactory.Tint(Root, "Single: room options off", Sheet, 766, 858, 1546, 1004, Dim).raycastTarget = true;

            // Baked controls: tabs, MODE D-pad, START MATCH.
            UiFactory.Hotspot(Root, "Tab setup", Sheet, 486, 60, 836, 160, () => ShowTab(false));
            if (!single) UiFactory.Hotspot(Root, "Tab map", Sheet, 1532, 60, 1844, 160, () => ShowTab(true));
            UiFactory.Hotspot(Root, "Rule prev", Sheet, 1798, 510, 1910, 622, () => ConfigureRequested?.Invoke(Other(), null));
            UiFactory.Hotspot(Root, "Rule next", Sheet, 2100, 510, 2212, 622, () => ConfigureRequested?.Invoke(Other(), null));
            UiFactory.Hotspot(Root, "Start", Sheet, 152, 412, 440, 700, () => StartRequested?.Invoke());
            UiFactory.Hotspot(Root, "Start arrow", Sheet, 1938, 658, 2050, 770, () => StartRequested?.Invoke());
            UiFactory.Hotspot(Root, "Close", Sheet, 1938, 360, 2050, 472, () => CloseRequested?.Invoke());
            ShowTab(false);
        }

        // Survival and Rank are the only two rules, so both arrows flip between them.
        private MatchRule Other() => rule == MatchRule.Survival ? MatchRule.Rank : MatchRule.Survival;

        private void BuildSlots()
        {
            const float x0 = 652, colWidth = 500, gap = 44, y0 = 284, rowHeight = 150, rowGap = 16;
            for (var i = 0; i < Slots; i++)
            {
                float x = x0 + i % 2 * (colWidth + gap), y = y0 + i / 2 * (rowHeight + rowGap);
                var row = UiFactory.PxRect(setupPage, $"Slot {i + 1}", Sheet, x, y, x + colWidth, y + rowHeight);
                var image = row.gameObject.AddComponent<Image>();
                UiFactory.Slice(image, "lobby_row", 1f);
                image.raycastTarget = false;
                var avatar = UiFactory.PxRect(setupPage, $"Slot {i + 1} avatar", Sheet, x + 22, y + 20, x + 132, y + 130).gameObject.AddComponent<Image>();
                avatar.preserveAspect = true; avatar.raycastTarget = false; avatar.enabled = false;
                var name = UiFactory.SheetText(setupPage, $"Slot {i + 1} name", Sheet, x + 148, y + 18, x + 484, y + 80, "", 34, TextAnchor.MiddleLeft);
                var ready = UiFactory.SheetText(setupPage, $"Slot {i + 1} ready", Sheet, x + 148, y + 84, x + 484, y + 132, "", 28, TextAnchor.MiddleLeft);
                ready.color = UiFactory.Pink;
                slots.Add((image, avatar, name, ready));
            }
        }

        private void BuildMaps(IReadOnlyList<string> mapNames)
        {
            const float y0 = 272, rowHeight = 94, gap = 10;
            for (var i = 0; i < mapNames.Count; i++)
            {
                var level = i + 1;
                var y = y0 + i * (rowHeight + gap);
                var rect = UiFactory.PxRect(mapPage, $"Map {level}", Sheet, 652, y, 1696, y + rowHeight);
                var image = rect.gameObject.AddComponent<Image>();
                UiFactory.Slice(image, "lobby_row", 1f);
                rect.gameObject.AddComponent<Button>().onClick.AddListener(() => ConfigureRequested?.Invoke(null, level));
                var label = UiFactory.SheetText(rect, "Label", new Vector2(1044, rowHeight), 36, 0, 1008, rowHeight,
                    $"MAP {level} - {mapNames[i].ToUpperInvariant()}", 40, TextAnchor.MiddleLeft);
                label.raycastTarget = false;
                maps.Add(image);
            }
        }

        private InputField BuildPassword()
        {
            var rect = UiFactory.PxRect(Root, "Password", Sheet, 1375, 883, 1536, 928);
            var field = UiFactory.InputField(rect, "");
            UiFactory.FitBar(field, "NONE", 24);
            field.characterLimit = 16;
            field.onEndEdit.AddListener(text => PasswordChanged?.Invoke(text));
            return field;
        }

        private void ShowTab(bool map)
        {
            mapTab = map;
            setupPage.gameObject.SetActive(!map);
            mapPage.gameObject.SetActive(map);
            setupDim.gameObject.SetActive(map);
            mapLit.gameObject.SetActive(map);
        }

        /// <summary>Redraws from the host's confirmed lobby state. <paramref name="animals"/> is indexed by roster slot.</summary>
        public void Show(LobbyState state, ulong localId, IReadOnlyList<string> animals)
        {
            rule = state.Rule;
            finalLevel = state.FinalLevel;
            ruleText.text = single ? rule.ToString().ToUpperInvariant() : $"{rule.ToString().ToUpperInvariant()} - L1->L{finalLevel}";
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var open = i < state.Capacity;
                slot.Row.color = open ? Plain : Disabled;
                if (i < state.Players.Count)
                {
                    var player = state.Players[i];
                    var animal = player.Fruit >= 0 && player.Fruit < animals.Count ? animals[player.Fruit] : "-";
                    slot.Name.text = $"{(player.Id == localId ? "YOU" : $"P{player.Id + 1}")} - {animal}";
                    slot.Ready.text = player.Ready ? "READY" : "";
                    slot.Avatar.enabled = player.Fruit >= 0;
                    if (player.Fruit >= 0) slot.Avatar.sprite = UiFactory.Kit("avatar_" + player.Fruit);
                }
                else
                {
                    slot.Name.text = open ? "-" : "LOCKED";
                    slot.Ready.text = "";
                    slot.Avatar.enabled = false;
                }
            }
            for (var i = 0; i < maps.Count; i++) maps[i].color = i + 1 == finalLevel ? Selected : Plain;
            capacity.SetValueWithoutNotify(state.Capacity);
            capacityText.text = state.Capacity.ToString();
            publicLit.gameObject.SetActive(state.Public);
            privateLit.gameObject.SetActive(!state.Public);
        }

        public bool MapTabOpen => mapTab;
        public string Password => password.text;
    }
}
