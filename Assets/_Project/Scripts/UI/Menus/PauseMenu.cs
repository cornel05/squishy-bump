using System;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.UI.Menus
{
    /// <summary>
    /// Esc menu (GDD 3.4): in Single Player it pauses, in Multiplayer it is only an overlay (the match runs on).
    /// The pause itself is App's job; this screen only reports <see cref="OpenChanged"/>.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private RectTransform root, options;

        public bool IsOpen { get; private set; }
        public event Action<bool> OpenChanged;
        public event Action LeaveRequested;

        public void Init(GameSettings settings, bool pausesGame)
        {
            var canvas = UiFactory.Canvas("Pause Menu", 20).transform;
            canvas.SetParent(transform, false);
            // Kit mockup "2 - Giao diện lúc ấn Pause": the console art has the PAUSE title baked in. Home = leave.
            root = UiFactory.ConsolePage(canvas, "Pause", "console_pause", () => LeaveRequested?.Invoke(), out var screen);
            if (!pausesGame) UiFactory.Outlined(UiFactory.Label(screen, "Match continues", 26));
            UiFactory.Button(screen, "Resume", () => SetOpen(false));
            UiFactory.Button(screen, "Settings", () => { root.gameObject.SetActive(false); options.gameObject.SetActive(true); });
            UiFactory.Button(screen, "Exit Game", () => LeaveRequested?.Invoke());
            options = MainMenu.BuildSettings(canvas, settings, () => { options.gameObject.SetActive(false); root.gameObject.SetActive(true); });
            SetOpen(false);
        }

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool open)
        {
            root.gameObject.SetActive(open);
            options.gameObject.SetActive(false);
            if (IsOpen == open) return;
            IsOpen = open;
            OpenChanged?.Invoke(open);
        }
    }
}
