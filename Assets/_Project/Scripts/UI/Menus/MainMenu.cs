using System;
using SquishyBump.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SquishyBump.UI.Menus
{
    /// <summary>
    /// Main menu (GDD 3.1, F01): Single Player (level picker), Multiplayer (rule, Host, Join by IP), Settings, Credits, Quit.
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
        private Button startMatch;

        /// <summary>Level number picked on the Single Player page.</summary>
        public event Action<int> SinglePlayerRequested;
        public event Action HostRequested;
        public event Action<string> JoinRequested;
        public event Action QuitRequested;
        /// <summary>Host only: everyone loads the first level (stand-in for the SB-30 lobby).</summary>
        public event Action StartMatchRequested;

        public string CurrentPage { get; private set; }

        public void Init(ISessionService sessionService, GameSettings gameSettings, string version, LevelDefinition[] levelList)
        {
            session = sessionService;
            settings = gameSettings;
            var canvas = UiFactory.Canvas("Main Menu", 10).transform;
            canvas.SetParent(transform, false);

            main = UiFactory.Page(canvas, "Main");
            UiFactory.Label(main, "SQUISHY BUMP", 64);
            UiFactory.Button(main, "Single Player", () => Show(levels));
            UiFactory.Button(main, "Multiplayer", () => Show(multiplayer));
            UiFactory.Button(main, "Settings", () => Show(options));
            UiFactory.Button(main, "Credits", () => Show(credits));
            UiFactory.Button(main, "Quit", () => QuitRequested?.Invoke());
            UiFactory.Label(main, version, 20);

            levels = UiFactory.Page(canvas, "Single Player");
            UiFactory.Label(levels, "Pick a level", 44);
            foreach (var level in levelList)
            {
                var number = level.LevelNumber;
                UiFactory.Button(levels, $"L{number} · {level.ArenaName}", () => SinglePlayerRequested?.Invoke(number));
            }
            UiFactory.Button(levels, "Back", () => Show(main));

            multiplayer = UiFactory.Page(canvas, "Multiplayer");
            UiFactory.Label(multiplayer, "Multiplayer (LAN)", 44);
            UiFactory.Toggle(multiplayer, "Rank rule (off = Survival)", settings.Rule == MatchRule.Rank, on =>
            {
                settings.Rule = on ? MatchRule.Rank : MatchRule.Survival;
                settings.NotifyChanged();
            });
            UiFactory.Button(multiplayer, "Host game", () => HostRequested?.Invoke());
            UiFactory.Label(multiplayer, "Host IP address", 26);
            address = UiFactory.InputField(multiplayer, "127.0.0.1");
            UiFactory.Button(multiplayer, "Join", () => JoinRequested?.Invoke(address.text.Trim()));
            status = UiFactory.Label(multiplayer, "", 24);
            startMatch = UiFactory.Button(multiplayer, "Start match", () => StartMatchRequested?.Invoke());
            UiFactory.Button(multiplayer, "Back", () => Show(main));

            options = BuildSettings(canvas, settings, () => Show(main));

            credits = UiFactory.Page(canvas, "Credits");
            UiFactory.Label(credits, "Credits", 44);
            UiFactory.Label(credits, "BKU Group 18\nĐình Duy · Minh Nhật\nUnity 6 · Netcode for GameObjects", 28)
                .GetComponent<LayoutElement>().minHeight = 200;
            UiFactory.Button(credits, "Back", () => Show(main));

            Show(main);
        }

        /// <summary>Options page shared with the pause overlay (GDD 3.1 game options).</summary>
        public static RectTransform BuildSettings(Transform canvas, GameSettings settings, Action back)
        {
            var page = UiFactory.Page(canvas, "Settings");
            UiFactory.Label(page, "Settings", 44);
            UiFactory.Slider(page, "Mouse sensitivity", MinSensitivity, MaxSensitivity, settings.MouseSensitivity,
                v => { settings.MouseSensitivity = v; settings.NotifyChanged(); });
            UiFactory.Slider(page, "Camera distance", MinDistance, MaxDistance, settings.CameraDistance,
                v => { settings.CameraDistance = v; settings.NotifyChanged(); });
            UiFactory.Slider(page, "Volume", 0f, 1f, settings.MasterVolume,
                v => { settings.MasterVolume = v; settings.NotifyChanged(); });
            UiFactory.Toggle(page, "Screen shake", settings.ScreenShake,
                v => { settings.ScreenShake = v; settings.NotifyChanged(); });
            UiFactory.Button(page, "Back", back);
            return page;
        }

        private void Update()
        {
            if (status == null || session == null) return;
            status.text = StatusText(session.State, session.Peers.Count);
            startMatch.gameObject.SetActive(session.State == SessionState.Hosting);
        }

        public static string StatusText(SessionState state, int peers) => state switch
        {
            SessionState.Hosting => $"Hosting on port 7777 · {peers} joined",
            SessionState.Joining => "Joining…",
            SessionState.Joined => "Joined · waiting for the host",
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
