using System;
using SquishyBump.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SquishyBump.UI.Menus
{
    /// <summary>
    /// Main menu (GDD 3.1, F01): Single Player → lobby, Multiplayer (rule, Host, Join by IP), Settings, Credits, Quit.
    /// The main page and Settings follow the team's UX/UI kit mockups; the other pages use the plain kit panel.
    /// Raises requests only; App decides what they do. Session state is read from <see cref="ISessionService"/>.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        // Slider ranges of the options screen (multipliers on the presentation profile).
        public const float MinSensitivity = 0.2f, MaxSensitivity = 3f, MinDistance = 0.7f, MaxDistance = 1.5f;

        private ISessionService session;
        private GameSettings settings;
        private RectTransform main, levels, multiplayer, options, credits;
        private InputField address, roomPassword;
        private IRoomBrowser rooms;
        private Text status;

        /// <summary>Single luôn vào lobby rồi bắt đầu L1.</summary>
        public event Action<int> SinglePlayerRequested;
        public event Action HostRequested;
        /// <summary>Address and room password (empty when none).</summary>
        public event Action<string, string> JoinRequested;
        public event Action QuitRequested;

        public string CurrentPage { get; private set; }

        public void Init(ISessionService sessionService, GameSettings gameSettings, string version, LevelDefinition[] levelList,
            IRoomBrowser roomBrowser = null)
        {
            rooms = roomBrowser;
            session = sessionService;
            settings = gameSettings;
            var canvas = UiFactory.Canvas("Main Menu", 10).transform;
            canvas.SetParent(transform, false);

            // Start screen (kit mockup "6 - Start Screen"): background, logo, then the button stack.
            main = UiFactory.Layer(canvas, "Main");
            UiFactory.FullScreen(main, "bg_start");
            UiFactory.Place(main, "logo", new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(440, 273));
            var stack = UiFactory.Layer(main, "Buttons");
            stack.anchorMin = new Vector2(0.5f, 0f);
            stack.anchorMax = new Vector2(0.5f, 0.69f);
            stack.offsetMin = new Vector2(-300, 20);
            stack.offsetMax = new Vector2(300, 0);
            var column = stack.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 8;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = column.childForceExpandHeight = false;
            UiFactory.Button(stack, "Single Player", () => Show(levels));
            UiFactory.Toggle(stack, "Rank (off: Survival)", settings.Rule == MatchRule.Rank, on =>
            {
                settings.Rule = on ? MatchRule.Rank : MatchRule.Survival;
                settings.NotifyChanged();
            });
            // Create Server hosts, then shows the page that holds the host status and Start match.
            UiFactory.Button(stack, "Create Server", () => { HostRequested?.Invoke(); Show(multiplayer); });
            UiFactory.Button(stack, "Find Server", () => Show(multiplayer));
            UiFactory.Button(stack, "Settings", () => Show(options));
            UiFactory.Button(stack, "Credit", () => Show(credits));
            UiFactory.Button(stack, "Exit Game", () => QuitRequested?.Invoke());
            var versionLabel = UiFactory.Outlined(UiFactory.Label(main, version, 20));
            versionLabel.alignment = TextAnchor.LowerRight;
            var versionRect = versionLabel.rectTransform;
            versionRect.anchorMin = versionRect.anchorMax = versionRect.pivot = Vector2.right;
            versionRect.anchoredPosition = new Vector2(-16, 12);
            versionRect.sizeDelta = new Vector2(400, 30);

            levels = UiFactory.Page(canvas, "Single Player");
            UiFactory.Label(levels, "Squishy Cup · L1 → L5", 44);
            UiFactory.Label(levels, "Chọn trái và luật tại lobby cửa hàng.", 28);
            UiFactory.Button(levels, "Vào lobby", () => SinglePlayerRequested?.Invoke(1));
            UiFactory.Button(levels, "Back", () => Show(main));

            multiplayer = BuildFindServer(canvas);

            options = BuildSettings(canvas, settings, () => Show(main));

            credits = UiFactory.Page(canvas, "Credits");
            UiFactory.Label(credits, "Credits", 44);
            UiFactory.Label(credits,
                "BKU Group 18\nĐình Duy · Minh Nhật\nUnity 6 · URP · Netcode · glTFast\n" +
                "Font: Sleeping (Darrell Flood) · UI kit: nhóm\n6 model: Blender / script nhóm\n12 SFX: Python / script nhóm\n" +
                "Nhạc: chưa có\nAI hỗ trợ code và asset: Claude / Codex\nChi tiết nguồn: CREDITS.md", 24)
                .GetComponent<LayoutElement>().minHeight = 400;
            UiFactory.Button(credits, "Back", () => Show(main));

            Show(main);
        }

        // Find Server = the team's room console (kit folder 8): public LAN rooms in the window, manual IP on the bottom bar.
        private static readonly Vector2 Sheet = new(2316, 1131);
        private const float ConsoleWidth = 1500f, RowTop = 272f, RowHeight = 96f, RowGap = 10f;
        private const int MaxRows = 6;
        private RectTransform roomList;
        private Text noRooms;
        private string roomSignature = "";

        private RectTransform BuildFindServer(Transform canvas)
        {
            var root = UiFactory.Layer(canvas, "Multiplayer");
            var console = (RectTransform)new GameObject("Console", typeof(RectTransform), typeof(Image)).transform;
            console.SetParent(root, false);
            console.anchorMin = console.anchorMax = console.pivot = new Vector2(0.5f, 0.5f);
            console.sizeDelta = new Vector2(ConsoleWidth, ConsoleWidth * Sheet.y / Sheet.x);
            console.GetComponent<Image>().sprite = UiFactory.Kit("console_rooms");

            UiFactory.SheetText(console, "Title", Sheet, 944, 74, 1408, 186, "FIND SERVER", 30);
            status = UiFactory.SheetText(console, "Status", Sheet, 504, 60, 800, 150, "", 22);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            noRooms = UiFactory.SheetText(console, "No rooms", Sheet, 700, 400, 1650, 700,
                "NO PUBLIC ROOMS FOUND\nTYPE THE HOST IP BELOW", 28, TextAnchor.MiddleCenter, true);
            roomList = UiFactory.PxRect(console, "Rooms", Sheet, 0, 0, Sheet.x, Sheet.y);

            // Bottom bar: host IP. Top-right box: room password (when the room has one).
            address = BarField(console, "Host IP", 1076, 954, 1592, 1004, "127.0.0.1", "HOST IP");
            roomPassword = BarField(console, "Room password", 1540, 62, 1860, 148, "", "PASSWORD");
            roomPassword.characterLimit = 16;

            var join = UiFactory.SheetText(console, "Join label", Sheet, 215, 432, 435, 652, "JOIN", 46, TextAnchor.MiddleCenter, true);
            join.raycastTarget = false;
            UiFactory.Hotspot(console, "Join", Sheet, 215, 432, 435, 652, () => JoinRequested?.Invoke(address.text.Trim(), roomPassword.text));
            UiFactory.Hotspot(console, "Back", Sheet, 1934, 330, 2046, 442, () => Show(main));
            return root;
        }

        private static InputField BarField(Transform console, string name, float x0, float y0, float x1, float y1, string value, string hint)
        {
            var field = UiFactory.InputField(UiFactory.PxRect(console, name, Sheet, x0, y0, x1, y1), value);
            UiFactory.FitBar(field, hint, 26);
            return field;
        }

        // Rebuilds the rows only when the heard list changes (rooms drop off after a few silent seconds).
        private void RefreshRooms()
        {
            var heard = rooms.Rooms;
            var signature = string.Join("|", System.Linq.Enumerable.Select(heard, r => $"{r.Name}/{r.Address}/{r.Players}/{r.Capacity}/{r.Rule}/{r.HasPassword}"));
            if (signature == roomSignature) return;
            roomSignature = signature;
            for (var i = roomList.childCount - 1; i >= 0; i--) Destroy(roomList.GetChild(i).gameObject);
            noRooms.gameObject.SetActive(heard.Count == 0);
            for (var i = 0; i < heard.Count && i < MaxRows; i++)
            {
                var room = heard[i];
                var y = RowTop + i * (RowHeight + RowGap);
                var rect = UiFactory.PxRect(roomList, $"Room {i + 1}", Sheet, 652, y, 1696, y + RowHeight);
                var image = rect.gameObject.AddComponent<Image>();
                UiFactory.Slice(image, "lobby_row", 1f);
                if (room.Full) image.color = new Color(0.8f, 0.8f, 0.8f, 0.6f);
                var row = new Vector2(1044, RowHeight);
                UiFactory.SheetText(rect, "Name", row, 40, 0, 560, RowHeight, room.Name.ToUpperInvariant(), 30, TextAnchor.MiddleLeft).raycastTarget = false;
                UiFactory.SheetText(rect, "Players", row, 580, 0, 700, RowHeight, $"{room.Players}/{room.Capacity}", 28).raycastTarget = false;
                UiFactory.SheetText(rect, "Rule", row, 710, 0, 890, RowHeight, room.Rule.ToString().ToUpperInvariant(), 22).raycastTarget = false;
                UiFactory.SheetText(rect, "Lock", row, 900, 0, 1020, RowHeight, room.HasPassword ? "PW" : "JOIN", 24).raycastTarget = false;
                var address = room.Address;
                rect.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                { this.address.text = address; JoinRequested?.Invoke(address, roomPassword.text); });
            }
        }

        /// <summary>Options page shared with the pause overlay (GDD 3.1 game options).</summary>
        public static RectTransform BuildSettings(Transform canvas, GameSettings settings, Action back)
        {
            // Kit mockup "1 - Giao diện Settings": the console art has the SETTINGS title baked in.
            var root = UiFactory.ConsolePage(canvas, "Settings", "console_settings", back, out var page);
            UiFactory.Slider(page, "Mouse sensitivity", MinSensitivity, MaxSensitivity, settings.MouseSensitivity,
                v => { settings.MouseSensitivity = v; settings.NotifyChanged(); });
            UiFactory.Slider(page, "Camera distance", MinDistance, MaxDistance, settings.CameraDistance,
                v => { settings.CameraDistance = v; settings.NotifyChanged(); });
            UiFactory.Slider(page, "Volume", 0f, 1f, settings.MasterVolume,
                v => { settings.MasterVolume = v; settings.NotifyChanged(); });
            UiFactory.Toggle(page, "Screen shake", settings.ScreenShake,
                v => { settings.ScreenShake = v; settings.NotifyChanged(); });
            UiFactory.Button(page, "Back", back);
            return root;
        }

        private void Update()
        {
            if (rooms != null && multiplayer != null && multiplayer.gameObject.activeInHierarchy) RefreshRooms();
            if (status == null || session == null) return;
            status.text = session.State == SessionState.Idle ? ErrorText(session.LastEndReason) : StatusText(session.State, session.Peers.Count);
        }

        public static string StatusText(SessionState state, int peers) => state switch
        {
            SessionState.Hosting => $"Hosting on port 7777 · {peers} joined",
            SessionState.Joining => "Joining…",
            SessionState.Joined => "Joined · waiting for the host",
            _ => "",
        };

        public static string ErrorText(SessionEndReason reason) => reason switch
        {
            SessionEndReason.VersionMismatch => "Sai phiên bản build. Hãy dùng cùng bản với host.",
            SessionEndReason.RoomFull => "Phòng đã đủ 6 người hoặc không nhận kết nối.",
            SessionEndReason.MatchInProgress => "Phòng đang trong phiên đấu.",
            SessionEndReason.WrongPassword => "Sai mật khẩu phòng.",
            SessionEndReason.InvalidAddress => "Địa chỉ IPv4 hoặc port không hợp lệ.",
            SessionEndReason.ConnectFailed => "Không tới được host. Kiểm tra IP và kết nối LAN.",
            SessionEndReason.HostLost => "Host đã rời phòng hoặc mất kết nối.",
            SessionEndReason.StartFailed => "Không thể mở kết nối. Hãy thử lại.",
            _ => "",
        };

        public void ShowPage(string name)
        {
            foreach (var page in new[] { main, levels, multiplayer, options, credits })
                if (page.name == name) Show(page);
        }

        private void Show(RectTransform page)
        {
            rooms?.Listen(page == multiplayer);
            foreach (var p in new[] { main, levels, multiplayer, options, credits }) p.gameObject.SetActive(p == page);
            CurrentPage = page.name;
        }
    }
}
