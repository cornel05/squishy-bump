using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquishyBump.AI
{
    public readonly struct BotPath
    {
        public BotPath(int[] cells, float cost, float meanDanger)
        { Cells = Array.AsReadOnly(cells); Cost = cost; MeanDanger = meanDanger; }
        public IReadOnlyList<int> Cells { get; }
        public float Cost { get; }
        public float MeanDanger { get; }
        public bool Found => Cells != null && Cells.Count > 0;
    }

    /// <summary>A* 8 hướng; mỗi bước vào ô có cost 1+8D. Không cắt góc qua ô bị chặn.</summary>
    public sealed class GridPathfinder
    {
        private const int Capacity = DangerMap.MaximumSide * DangerMap.MaximumSide;
        private readonly float[] g = new float[Capacity], f = new float[Capacity];
        private readonly int[] parent = new int[Capacity], heap = new int[Capacity], position = new int[Capacity];
        private readonly bool[] closed = new bool[Capacity];
        private int heapCount;
        public int ExpandedNodes { get; private set; }

        public BotPath Find(DangerMap map, int start, int goal, bool useHeuristic = true) =>
            Search(map, start, goal, useHeuristic, null);

        /// <summary>Dijkstra tới mục tiêu hợp lệ rẻ nhất, dùng cùng graph/cost khi chọn ô né.</summary>
        public BotPath FindCheapest(DangerMap map, int start, Predicate<int> eligible) =>
            Search(map, start, -1, false, eligible ?? throw new ArgumentNullException(nameof(eligible)));

        /// <summary>Khi không còn ô dưới ngưỡng an toàn, chọn danger thấp nhất trong vùng tới được.
        /// Dijkstra thăm cost tăng dần nên các ô cùng danger giữ lựa chọn rẻ nhất.</summary>
        public BotPath FindLeastDanger(DangerMap map, int start, Predicate<int> eligible, Func<int, float> danger = null)
        {
            if (eligible == null) throw new ArgumentNullException(nameof(eligible));
            var best = -1; var lowest = float.PositiveInfinity;
            Search(map, start, -1, false, cell =>
            {
                if (eligible(cell))
                {
                    var value = danger == null ? map[cell] : danger(cell);
                    if (value < lowest) { best = cell; lowest = value; }
                }
                return false;
            });
            return best < 0 ? default : Reconstruct(map, start, best);
        }

        private BotPath Search(DangerMap map, int start, int goal, bool useHeuristic, Predicate<int> eligible)
        {
            ExpandedNodes = 0;
            if (!map.Walkable(start) || (eligible == null && !map.Walkable(goal))) return default;
            for (var i = 0; i < map.Count; i++)
            { g[i] = float.PositiveInfinity; f[i] = float.PositiveInfinity; parent[i] = position[i] = -1; closed[i] = false; }
            heapCount = 0; ExpandedNodes = 0; g[start] = 0;
            f[start] = Heuristic(map, start, goal, useHeuristic); PushOrDecrease(start);
            while (heapCount > 0)
            {
                var current = Pop(); closed[current] = true; ExpandedNodes++;
                if (eligible == null ? current == goal : eligible(current)) return Reconstruct(map, start, current);
                var x = current % map.Width; var z = current / map.Width;
                for (var dz = -1; dz <= 1; dz++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        var next = map.Index(x + dx, z + dz);
                        if (!map.Walkable(next) || closed[next]) continue;
                        if (dx != 0 && dz != 0 && (!map.Walkable(map.Index(x + dx, z)) ||
                            !map.Walkable(map.Index(x, z + dz)))) continue;
                        var cost = g[current] + 1f + 8f * map[next];
                        if (cost >= g[next]) continue;
                        g[next] = cost; parent[next] = current;
                        f[next] = cost + Heuristic(map, next, goal, useHeuristic);
                        PushOrDecrease(next);
                    }
            }
            return default;
        }

        private static float Heuristic(DangerMap map, int from, int to, bool enabled)
        {
            if (!enabled) return 0;
            var dx = from % map.Width - to % map.Width;
            var dz = from / map.Width - to / map.Width;
            // Bước chéo vẫn cost 1, nên Euclid/sqrt(2) là cận dưới nhất quán.
            // Giữ nguyên cost 1+8D của GDD; không dùng Euclid thô (có thể đánh giá quá cao).
            return Mathf.Sqrt((dx * dx + dz * dz) * 0.5f);
        }
        private BotPath Reconstruct(DangerMap map, int start, int goal)
        {
            var count = 1;
            for (var at = goal; at != start; at = parent[at]) count++;
            var cells = new int[count]; var node = goal; var sum = 0f;
            for (var i = count - 1; i >= 0; i--) { cells[i] = node; sum += map[node]; node = parent[node]; }
            return new BotPath(cells, g[goal], sum / count);
        }
        private bool Before(int a, int b) => f[a] < f[b] || (f[a] == f[b] && (g[a] > g[b] || (g[a] == g[b] && a < b)));
        private void PushOrDecrease(int node)
        {
            var at = position[node];
            if (at < 0) { at = heapCount++; heap[at] = node; position[node] = at; }
            while (at > 0)
            {
                var above = (at - 1) / 2;
                if (!Before(heap[at], heap[above])) break;
                Swap(at, above); at = above;
            }
        }
        private int Pop()
        {
            var result = heap[0]; position[result] = -1;
            heapCount--;
            if (heapCount == 0) return result;
            heap[0] = heap[heapCount]; position[heap[0]] = 0;
            var at = 0;
            while (at * 2 + 1 < heapCount)
            {
                var child = at * 2 + 1;
                if (child + 1 < heapCount && Before(heap[child + 1], heap[child])) child++;
                if (!Before(heap[child], heap[at])) break;
                Swap(at, child); at = child;
            }
            return result;
        }
        private void Swap(int a, int b)
        {
            (heap[a], heap[b]) = (heap[b], heap[a]); position[heap[a]] = a; position[heap[b]] = b;
        }
    }
}
