using System.Text.RegularExpressions;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.EditMode
{
    public sealed class LevelLaunchArgsTests
    {
        [TestCase("-level", "1", 1)]
        [TestCase("-level", "5", 5)]
        [TestCase("-level", "L3", 3)]
        [TestCase("-level", "l4", 4)]
        public void ReadStartLevel_NumericAndPrefixed_ReturnsLevel(string flag, string value, int expected)
        {
            var level = LevelLauncher.ReadStartLevel(new[] { "SquishyBump.exe", flag, value });
            Assert.That(level, Is.EqualTo(expected));
        }

        [Test]
        public void ReadStartLevel_Missing_ReturnsZero()
        {
            Assert.That(LevelLauncher.ReadStartLevel(null), Is.Zero);
            Assert.That(LevelLauncher.ReadStartLevel(new[] { "SquishyBump.exe", "-perf", "10" }), Is.Zero);
        }

        [TestCase("-level", "0")]
        [TestCase("-level", "6")]
        [TestCase("-level", "abc")]
        [TestCase("-level", null)]
        public void ReadStartLevel_Invalid_LogsWarningAndReturnsZero(string flag, string value)
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"Expected -level"));
            var args = value == null ? new[] { flag } : new[] { flag, value };
            Assert.That(LevelLauncher.ReadStartLevel(args), Is.Zero);
        }

        [TestCase("survival", MatchRule.Survival)]
        [TestCase("Rank", MatchRule.Rank)]
        [TestCase("RANK", MatchRule.Rank)]
        public void ReadStartRule_Valid_ReturnsRule(string value, MatchRule expected)
        {
            var rule = LevelLauncher.ReadStartRule(new[] { "-rule", value });
            Assert.That(rule, Is.EqualTo(expected));
        }

        [Test]
        public void ReadStartRule_MissingOrInvalid_ReturnsNull()
        {
            Assert.That(LevelLauncher.ReadStartRule(new[] { "SquishyBump.exe" }), Is.Null);
            LogAssert.Expect(LogType.Warning, new Regex(@"Expected -rule"));
            Assert.That(LevelLauncher.ReadStartRule(new[] { "-rule", "unknown" }), Is.Null);
        }
    }
}
