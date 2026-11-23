using NUnit.Framework;
using SquishyBump.UI.Menus;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-74/SB-75: the UI kit sprites and font are reachable through Resources, and button labels map to sprite names.</summary>
    public sealed class UiKitTests
    {
        [TestCase("Create Server", "create_server")]
        [TestCase("Exit Game", "exit_game")]
        [TestCase("L1 · Play mat", "l1_play_mat")]
        public void Slug_LowercasesAndJoinsWithUnderscore(string text, string expected) =>
            Assert.That(UiFactory.Slug(text), Is.EqualTo(expected));

        [TestCase("btn_create_server"), TestCase("btn_find_server"), TestCase("btn_settings"), TestCase("btn_credit"),
         TestCase("btn_exit_game"), TestCase("btn_resume"), TestCase("btn_next"), TestCase("btn_back"), TestCase("btn_home"),
         TestCase("btn_blank"), TestCase("btn_next_big"), TestCase("bg_start"), TestCase("logo"),
         TestCase("console_settings"), TestCase("console_pause"), TestCase("slider_track"), TestCase("slider_fill"),
         TestCase("slider_knob"), TestCase("toggle"), TestCase("panel_box"), TestCase("row_score"), TestCase("plate"),
         TestCase("ribbon_score"), TestCase("frame_winner"), TestCase("banner_round_1"), TestCase("banner_round_5"),
         TestCase("hud_heart"), TestCase("hud_heart_lost"), TestCase("hud_timer"), TestCase("hud_board"), TestCase("hud_weight"),
         TestCase("medal_1"), TestCase("medal_3"), TestCase("avatar_0"), TestCase("avatar_5"),
         TestCase("console_lobby"), TestCase("console_rooms"), TestCase("lobby_row"), TestCase("card_heavy"), TestCase("card_normal"),
         TestCase("card_light")]
        public void Kit_SpriteExists(string name) => Assert.That(UiFactory.Kit(name), Is.Not.Null, name);

        [TestCase("panel_box"), TestCase("row_score"), TestCase("plate"), TestCase("hud_board"), TestCase("btn_blank"), TestCase("lobby_row")]
        public void Kit_StretchedArt_IsNineSliced(string name) => Assert.That(UiFactory.Kit(name).border.x, Is.GreaterThan(0f), name);

        [TestCase("btn_back"), TestCase("console_settings"), TestCase("bg_start")]
        public void Kit_FixedArt_HasNoBorder(string name) => Assert.That(UiFactory.Kit(name).border, Is.EqualTo(Vector4.zero), name);

        [Test]
        public void Kit_ConsoleArt_MatchesTheScreenRectUsedByLayout()
        {
            // UiFactory.ConsolePage places the controls on the console screen assuming the 1576x770 art.
            Assert.That(UiFactory.Kit("console_settings").rect.size, Is.EqualTo(new Vector2(1576, 770)));
            Assert.That(UiFactory.Kit("console_pause").rect.size, Is.EqualTo(new Vector2(1576, 770)));
        }

        [Test]
        public void Kit_LobbyConsoles_MatchTheSheetUsedByLayout()
        {
            // LobbyConsole and MainMenu.BuildFindServer place controls by pixel box on these 2316 x 1131 sheets.
            Assert.That(UiFactory.Kit("console_lobby").rect.size, Is.EqualTo(new Vector2(2316, 1131)));
            Assert.That(UiFactory.Kit("console_rooms").rect.size, Is.EqualTo(new Vector2(2316, 1131)));
        }

        [Test]
        public void Kit_ClassCards_MatchTheValueColumnUsedByLayout()
        {
            // LobbyPanel.BuildCard draws the numbers over a 1572 x 527 card whose baked numbers were wiped.
            foreach (var name in new[] { "card_heavy", "card_normal", "card_light" })
                Assert.That(UiFactory.Kit(name).rect.size.x, Is.EqualTo(1572), name);
        }

        [Test]
        public void Font_SleepingLoads() => Assert.That(Resources.Load<Font>("Fonts/Sleeping"), Is.Not.Null);
    }
}
