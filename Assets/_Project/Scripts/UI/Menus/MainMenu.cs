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
        private InputField address;
        private Text status;

        /// <summary>Single luôn vào lobby rồi bắt đầu L1.</summary>
        public event Action<int> SinglePlayerRequested;
        public event Action HostRequested;
        public event Action<string> JoinRequested;
        public event Action QuitRequested;

        public string CurrentPage { get; private set; }

        public void Init(ISessionService sessionService, GameSettings gameSettings, string version, LevelDefinition[] levelList)
        {
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

            multiplayer = UiFactory.Page(canvas, "Multiplayer");
            UiFactory.Label(multiplayer, "Find Server (LAN)", 44);
            UiFactory.Label(multiplayer, "Host IP address", 26);
            address = UiFactory.InputField(multiplayer, "127.0.0.1");
            UiFactory.Button(multiplayer, "Join", () => JoinRequested?.Invoke(address.text.Trim()));
            status = UiFactory.Label(multiplayer, "", 24);
            UiFactory.Button(multiplayer, "Back", () => Show(main));

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
            foreach (var p in new[] { main, levels, multiplayer, options, credits }) p.gameObject.SetActive(p == page);
            CurrentPage = page.name;
        }
    }
}
