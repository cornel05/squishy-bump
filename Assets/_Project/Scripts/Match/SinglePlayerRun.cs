using System;
using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;

namespace SquishyBump.Match
{
    /// <summary>GDD 3.5/A.6: host giữ danh tính, roster và seed qua mỗi lần chơi lại.</summary>
    public sealed class SinglePlayerRun
    {
        public const int FinalLevel = 5;
        public const int FighterCount = 6;
        public sealed class Round
        {
            internal Round(int level, uint seed, int[] fruits, int[] tiers)
            { Level = level; Seed = seed; Fruits = Array.AsReadOnly(fruits); Tiers = Array.AsReadOnly(tiers); }
            public int Level { get; }
            public uint Seed { get; }
            /// <summary>Slot 0 là người; năm slot bot giữ nguyên nhân vật suốt run (D39).</summary>
            public IReadOnlyList<int> Fruits { get; }
            /// <summary>Năm bậc bot, cùng thứ tự Fruits[1..5].</summary>
            public IReadOnlyList<int> Tiers { get; }
        }

        private readonly int[] fruits;
        private readonly Round[] rounds = new Round[FinalLevel];
        public SinglePlayerRun(int selectedCharacter, uint seed, MatchRule rule, int firstLevel = 1)
        {
            if (selectedCharacter < 0 || selectedCharacter >= FighterCount) throw new ArgumentOutOfRangeException(nameof(selectedCharacter));
            if (firstLevel < 1 || firstLevel > FinalLevel) throw new ArgumentOutOfRangeException(nameof(firstLevel));
            if (rule != MatchRule.Survival && rule != MatchRule.Rank) throw new ArgumentOutOfRangeException(nameof(rule));
            Seed = seed; Rule = rule; Level = firstLevel;
            var random = new Random(unchecked((int)seed));
            var bots = Enumerable.Range(0, FighterCount).Where(i => i != selectedCharacter).ToArray();
            for (var i = bots.Length - 1; i > 0; i--)
            { var j = random.Next(i + 1); (bots[i], bots[j]) = (bots[j], bots[i]); }
            fruits = new[] { selectedCharacter }.Concat(bots).ToArray();
            for (var n = 1; n <= FinalLevel; n++)
            {
                var matchSeed = unchecked((uint)random.Next() ^ ((uint)random.Next() << 1));
                var rosterRandom = new Random(unchecked((int)matchSeed));
                var tiers = new int[FighterCount - 1];
                tiers[0] = n;
                for (var i = 1; i < tiers.Length; i++) tiers[i] = rosterRandom.Next(Math.Max(1, n - 2), n + 1);
                rounds[n - 1] = new Round(n, matchSeed, (int[])fruits.Clone(), tiers);
            }
        }

        public uint Seed { get; }
        public MatchRule Rule { get; }
        public int Level { get; private set; }
        public int Retries { get; private set; }
        public int LevelsCleared { get; private set; }
        public bool Finished { get; private set; }
        public Round Current => rounds[Level - 1];

        /// <summary>Chỉ gọi một lần khi nhận nút tiếp tục của kết quả đã xác nhận.</summary>
        public void Continue(bool soleWinner)
        {
            if (Finished) return;
            if (Rule == MatchRule.Survival && !soleWinner) { Retries++; return; }
            LevelsCleared++;
            if (Level == FinalLevel) Finished = true;
            else Level++;
        }
    }
}
