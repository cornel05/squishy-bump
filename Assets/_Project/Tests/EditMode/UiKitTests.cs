using NUnit.Framework;
using SquishyBump.UI.Menus;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-74: the UI kit sprites and font are reachable through Resources, and button labels map to sprite names.</summary>
    public sealed class UiKitTests
    {
        [TestCase("Create Server", "create_server")]
        [TestCase("Exit Game", "exit_game")]
        [TestCase("L1 · Play mat", "l1_play_mat")]
        public void Slug_LowercasesAndJoinsWithUnderscore(string text, string expected) =>
            Assert.That(UiFactory.Slug(text), Is.EqualTo(expected));

        [TestCase("btn_create_server"), TestCase("btn_find_server"), TestCase("btn_settings"), TestCase("btn_credit"),
         TestCase("btn_exit_game"), TestCase("btn_resume"), TestCase("btn_next"), TestCase("btn_back"),
         TestCase("title_settings"), TestCase("slider_track"), TestCase("slider_fill"), TestCase("slider_knob"),
         TestCase("toggle"), TestCase("panel_box"), TestCase("row_result"), TestCase("hud_lives"), TestCase("hud_timer"),
         TestCase("medal_1"), TestCase("medal_3"), TestCase("avatar_0"), TestCase("avatar_5")]
        public void Kit_SpriteExists(string name) => Assert.That(UiFactory.Kit(name), Is.Not.Null, name);

        [Test]
        public void Kit_PanelsAreNineSliced()
        {
            Assert.That(UiFactory.Kit("panel_box").border.x, Is.GreaterThan(0f));
            Assert.That(UiFactory.Kit("btn_back").border, Is.EqualTo(Vector4.zero));
        }

        [Test]
        public void Font_SleepingLoads() => Assert.That(Resources.Load<Font>("Fonts/Sleeping"), Is.Not.Null);
    }
}
