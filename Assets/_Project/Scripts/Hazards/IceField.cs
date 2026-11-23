using System;
using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;

namespace SquishyBump.Hazards
{
    public enum IceStage { Solid, Cracking, Fallen, Fading }

    /// <summary>Bản sao balance theo tick; dùng cùng dữ liệu SO trên host và trong test.</summary>
    public readonly struct IceRules
    {
        public readonly int Rings, MinimumSolid, WaveTicks, CrackTicks, ReturnTicks, FadeTicks;
        public readonly int PhaseTwoTicks, PhaseThreeTicks, FirstCount, SecondCount, ThirdCount;
        public readonly double FighterWeight;
        public IceRules(int rings, int minimumSolid, int waveTicks, int crackTicks, int returnTicks,
            int fadeTicks, int phaseTwoTicks, int phaseThreeTicks, int firstCount, int secondCount,
            int thirdCount, double fighterWeight)
        {
            if (rings < 1 || minimumSolid < 1 || minimumSolid > 1 + 3 * rings * (rings + 1) ||
                waveTicks < 1 || crackTicks < 1 || returnTicks < 1 || fadeTicks < 1 ||
                phaseTwoTicks < 0 || phaseThreeTicks < phaseTwoTicks || firstCount < 1 ||
                secondCount < 1 || thirdCount < 1 || fighterWeight < 0)
                throw new ArgumentOutOfRangeException(nameof(rings), "Cấu hình H06 không hợp lệ.");
            Rings = rings; MinimumSolid = minimumSolid; WaveTicks = waveTicks; CrackTicks = crackTicks;
            ReturnTicks = returnTicks; FadeTicks = fadeTicks; PhaseTwoTicks = phaseTwoTicks;
            PhaseThreeTicks = phaseThreeTicks; FirstCount = firstCount; SecondCount = secondCount;
            ThirdCount = thirdCount; FighterWeight = fighterWeight;
        }
        public static IceRules FromProfile(IceProfile p, float dt, bool chaos = false)
        {
            int T(float seconds) => Math.Max(1, (int)Math.Round(seconds / dt));
            return new IceRules(p.Rings, p.MinimumSolid, T(chaos ? p.ChaosWaveSeconds : p.WaveSeconds),
                T(p.CrackSeconds), T(p.ReturnSeconds), T(p.FadeSeconds),
                T(chaos ? p.ChaosPhaseSeconds : p.SecondPhaseSeconds),
                chaos ? int.MaxValue : T(p.ThirdPhaseSeconds), p.FirstWaveCount,
                p.SecondWaveCount, p.ThirdWaveCount, p.FighterWeight);
        }
        public int WaveCount(int tick) => tick < PhaseTwoTicks ? FirstCount : tick < PhaseThreeTicks ? SecondCount : ThirdCount;
    }

    /// <summary>H06 thuần dữ liệu. Chỉ App/host gọi Step; client nhận từng chuyển trạng thái.</summary>
    public sealed class IceField
    {
        public readonly struct Hex
        {
            public readonly int Q, R;
            public Hex(int q, int r) { Q = q; R = r; }
        }
        private static readonly (int q, int r)[] Directions = { (1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1) };
        private readonly IceRules rules;
        private readonly Random random;
        private readonly IceStage[] stages;
        private readonly int[] changedAt;
        private readonly int[][] neighbours;
        private int nextWave, lastTick;
        public IReadOnlyList<Hex> Cells { get; }
        public int Count => stages.Length;
        public event Action<int, IceStage, int> Changed;
        public IceStage Stage(int index) => stages[index];
        public int ChangedAt(int index) => changedAt[index];
        public int SolidCount => stages.Count(s => s == IceStage.Solid || s == IceStage.Cracking);
        // Ô nứt đã được dự trữ để cả nhóm rụng không làm mất liên thông.
        public int ReservedSolidCount => stages.Count(s => s == IceStage.Solid);

        public IceField(IceRules config, uint seed)
        {
            rules = config; random = new Random(unchecked((int)seed)); nextWave = rules.WaveTicks;
            var cells = new List<Hex>();
            for (var q = -rules.Rings; q <= rules.Rings; q++)
                for (var r = Math.Max(-rules.Rings, -q - rules.Rings); r <= Math.Min(rules.Rings, -q + rules.Rings); r++)
                    cells.Add(new Hex(q, r));
            Cells = cells.AsReadOnly(); stages = new IceStage[cells.Count]; changedAt = new int[cells.Count];
            var lookup = cells.Select((h, i) => (h, i)).ToDictionary(x => (x.h.Q, x.h.R), x => x.i);
            neighbours = cells.Select(h => Directions.Select(d => (h.Q + d.q, h.R + d.r))
                .Where(lookup.ContainsKey).Select(p => lookup[p]).ToArray()).ToArray();
        }

        public void Step(int tick, Func<int, int> standingFighters, Func<int, bool> overlaps)
        {
            if (tick <= lastTick) return;
            lastTick = tick;
            for (var i = 0; i < Count; i++)
            {
                var age = tick - changedAt[i];
                if (stages[i] == IceStage.Cracking && age >= rules.CrackTicks) Set(i, IceStage.Fallen, tick);
                else if (stages[i] == IceStage.Fallen && age >= rules.ReturnTicks) Set(i, IceStage.Fading, tick);
                else if (stages[i] == IceStage.Fading && age >= rules.FadeTicks && !overlaps(i) &&
                    neighbours[i].Any(n => stages[n] == IceStage.Solid)) Set(i, IceStage.Solid, tick);
            }
            if (tick < nextWave) return;
            nextWave = tick + rules.WaveTicks;
            var candidates = Enumerable.Range(0, Count).Where(i => stages[i] == IceStage.Solid).ToList();
            var picked = 0;
            while (picked < rules.WaveCount(tick) && candidates.Count > 0 && ReservedSolidCount > rules.MinimumSolid)
            {
                double Weight(int i) => 1 + rules.FighterWeight * Math.Max(0, standingFighters(i));
                var roll = random.NextDouble() * candidates.Sum(Weight);
                var index = candidates[candidates.Count - 1];
                foreach (var c in candidates) { roll -= Weight(c); if (roll < 0) { index = c; break; } }
                candidates.Remove(index);
                if (!CanRemove(index)) continue;
                Set(index, IceStage.Cracking, tick); picked++;
            }
        }

        // Toàn bộ sàn được giữ liên thông từ trạng thái ban đầu, mạnh hơn kiểm tra từng ô riêng lẻ.
        public bool CanRemove(int index)
        {
            if (stages[index] != IceStage.Solid || ReservedSolidCount <= rules.MinimumSolid) return false;
            return Connected(i => i != index && stages[i] == IceStage.Solid);
        }
        public bool SolidConnected(bool reserveCracks = false) => Connected(i => stages[i] == IceStage.Solid ||
            (!reserveCracks && stages[i] == IceStage.Cracking));
        private bool Connected(Func<int, bool> included)
        {
            var total = 0; var first = -1;
            for (var i = 0; i < Count; i++) if (included(i)) { total++; first = i; }
            if (first < 0) return false;
            var seen = new HashSet<int> { first }; var queue = new Queue<int>(); queue.Enqueue(first);
            while (queue.Count > 0)
                foreach (var n in neighbours[queue.Dequeue()]) if (included(n) && seen.Add(n)) queue.Enqueue(n);
            return seen.Count == total;
        }
        private void Set(int index, IceStage stage, int tick)
        {
            stages[index] = stage; changedAt[index] = tick; Changed?.Invoke(index, stage, tick);
        }
    }
}
