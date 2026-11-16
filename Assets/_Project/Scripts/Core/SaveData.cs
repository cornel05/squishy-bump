using System;

namespace SquishyBump.Core
{
    /// <summary>
    /// Everything this player keeps between runs (GDD 10.5): settings, key bindings, last fruit and two records.
    /// Match state is never part of it: a match is short and restarts cleanly.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public GameSettings Settings = new();
        /// <summary>Input System binding overrides as JSON (empty = defaults).</summary>
        public string Bindings = "";
        /// <summary>FruitDefinition asset name of the last pick.</summary>
        public string LastFruit = "";
        /// <summary>Highest Single Player Survival level won (0 = none, 5 = the whole Cup).</summary>
        public int BestSurvivalLevel;
        public int BestRankScore;

        /// <summary>Keeps the better record; true when it improved.</summary>
        public bool RecordSurvival(int levelWon)
        {
            if (levelWon <= BestSurvivalLevel) return false;
            BestSurvivalLevel = levelWon;
            return true;
        }

        public bool RecordRank(int points)
        {
            if (points <= BestRankScore) return false;
            BestRankScore = points;
            return true;
        }
    }
}
