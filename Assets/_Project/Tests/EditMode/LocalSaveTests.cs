using System.IO;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-51: local JSON save of GDD 10.5.</summary>
    public sealed class LocalSaveTests
    {
        string folder;

        [SetUp]
        public void Fresh()
        {
            folder = Path.Combine(Path.GetTempPath(), "squishy-save-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void Clean() => Directory.Delete(folder, true);

        [Test]
        public void SaveThenLoad_KeepsSettingsBindingsFruitAndRecords()
        {
            var store = new LocalSave(folder);
            var data = new SaveData { Bindings = "{\"bindings\":[]}", LastFruit = "Peach" };
            data.Settings.MouseSensitivity = 1.7f;
            data.Settings.ScreenShake = false;
            data.Settings.Rule = MatchRule.Rank;
            data.RecordSurvival(3);
            data.RecordRank(12);
            store.Save(data);
            store.Save(data); // replacing an existing save

            var loaded = store.Load();
            Assert.That((loaded.Settings.MouseSensitivity, loaded.Settings.ScreenShake, loaded.Settings.Rule),
                Is.EqualTo((1.7f, false, MatchRule.Rank)));
            Assert.That((loaded.Bindings, loaded.LastFruit, loaded.BestSurvivalLevel, loaded.BestRankScore),
                Is.EqualTo(("{\"bindings\":[]}", "Peach", 3, 12)));
            Assert.That(File.Exists(store.FilePath + ".tmp"), Is.False, "atomic write leaves no temp file");
        }

        [Test]
        public void Records_OnlyImprove()
        {
            var data = new SaveData();
            Assert.That(data.RecordSurvival(2), Is.True);
            Assert.That(data.RecordSurvival(1), Is.False);
            Assert.That(data.BestSurvivalLevel, Is.EqualTo(2));
            Assert.That(data.RecordRank(5) && !data.RecordRank(5), Is.True);
        }

        [Test]
        public void Missing_GivesDefaults() =>
            Assert.That(new LocalSave(folder).Load().Settings.MouseSensitivity, Is.EqualTo(1f));

        [Test]
        public void Corrupt_KeepsBadCopyAndGivesDefaults()
        {
            var store = new LocalSave(folder);
            File.WriteAllText(store.FilePath, "{ not json");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unreadable"));
            var loaded = store.Load();
            Assert.That(loaded.BestSurvivalLevel, Is.Zero);
            Assert.That(File.Exists(store.FilePath + ".bad"), Is.True);
        }

        [Test]
        public void Json_HoldsNoMatchState()
        {
            var json = JsonUtility.ToJson(new SaveData());
            foreach (var key in new[] { "Wobble", "Position", "Tick", "Phase", "Lives" })
                Assert.That(json, Does.Not.Contain(key));
        }
    }
}
