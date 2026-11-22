using System;
using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;

namespace SquishyBump.Match
{
    /// <summary>Host-owned LAN Survival progression. Each group shares a final session rank.</summary>
    public sealed class LanSurvivalRun
    {
        private readonly int finalLevel;
        private readonly List<ulong[]> eliminatedGroups = new();

        public LanSurvivalRun(int finalLevel, IEnumerable<ulong> entrants)
        {
            this.finalLevel = finalLevel;
            Active = Array.AsReadOnly(entrants.OrderBy(id => id).ToArray());
        }

        public int Level { get; private set; } = 1;
        public IReadOnlyList<ulong> Active { get; private set; }

        public sealed class Decision
        {
            internal Decision(int? nextLevel, IReadOnlyList<ulong> active, IReadOnlyList<ulong[]> eliminated,
                IReadOnlyList<MatchStanding> standings = null)
            {
                NextLevel = nextLevel;
                Active = active;
                Eliminated = eliminated;
                Standings = standings;
            }

            public int? NextLevel { get; }
            public IReadOnlyList<ulong> Active { get; }
            public IReadOnlyList<MatchStanding> Standings { get; }
            internal IReadOnlyList<ulong[]> Eliminated { get; }
        }

        /// <summary>Resolve the confirmed match, or complete early when too few contestants remain connected.</summary>
        public Decision Evaluate(IReadOnlyList<ulong> peers, IReadOnlyList<SurvivalPlacement> placements = null)
        {
            var connected = Active.Where(id => id == 0 || peers.Contains(id)).ToArray();
            var forfeited = Active.Except(connected).ToArray();
            var removed = new List<ulong[]>(); // Worst group first. Forfeits rank below connected players.
            if (forfeited.Length > 0) removed.Add(forfeited);
            if (connected.Length <= 1) return Finish(connected, removed);
            if (placements == null) return null;

            var ranked = placements.Where(row => connected.Contains(row.FighterId)).OrderBy(row => row.Rank).ToArray();
            var final = Level == finalLevel;
            var levelsLeft = finalLevel - Level + 1;
            var scheduled = (Active.Count - 1 + levelsLeft - 1) / levelsLeft;
            var knockouts = final ? connected.Length - 1 :
                Math.Min(connected.Length - 2, Math.Max(0, scheduled - forfeited.Length));
            var cutoff = ranked.Length - knockouts;
            if (knockouts > 0 && ranked[cutoff - 1].Rank == ranked[cutoff].Rank)
                return new Decision(Level, Array.AsReadOnly(connected), removed);

            foreach (var group in ranked.Skip(cutoff).GroupBy(row => row.Rank).Reverse())
                removed.Add(group.Select(row => row.FighterId).ToArray());
            var remaining = ranked.Take(cutoff).Select(row => row.FighterId).OrderBy(id => id).ToArray();
            return final ? Finish(remaining, removed) : new Decision(Level + 1, Array.AsReadOnly(remaining), removed);
        }

        public void Advance(Decision decision)
        {
            Level = decision.NextLevel.Value;
            Active = decision.Active;
            eliminatedGroups.AddRange(decision.Eliminated);
        }

        private Decision Finish(ulong[] champions, List<ulong[]> removed)
        {
            var standings = new List<MatchStanding>();
            Append(champions);
            foreach (var group in removed.AsEnumerable().Reverse()) Append(group);
            foreach (var group in eliminatedGroups.AsEnumerable().Reverse()) Append(group);
            return new Decision(null, Array.Empty<ulong>(), removed, standings.AsReadOnly());

            void Append(ulong[] group)
            {
                var rank = standings.Count + 1;
                foreach (var id in group) standings.Add(new MatchStanding(id, rank, 0));
            }
        }
    }
}
