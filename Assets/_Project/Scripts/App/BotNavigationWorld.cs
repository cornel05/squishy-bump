using System;
using System.Collections.Generic;
using System.Linq;
using SquishyBump.AI;
using SquishyBump.Core;
using SquishyBump.Hazards;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>Adapter chụp floor collider và hint hazard thành dữ liệu Core, dùng chung cho các bot.</summary>
    public sealed class BotNavigationWorld
    {
        private sealed class Snapshot : IBotNavigationSnapshot
        {
            private readonly BotTerrainCell[] cells;
            public Snapshot(int side, Vector2 origin, BotTerrainCell[] terrain, List<BotHazardWarning> warnings)
            { Width = Height = side; Origin = origin; cells = terrain; Warnings = warnings.AsReadOnly(); }
            public int Width { get; }
            public int Height { get; }
            public Vector2 Origin { get; }
            public BotTerrainCell Cell(int index) => cells[index];
            public IReadOnlyList<BotHazardWarning> Warnings { get; }
        }
        private readonly Vector3 center;
        private readonly BalanceProfile balance;
        private readonly IBotHazardHints[] providers;
        private readonly int floorMask;
        public BotNavigationWorld(Vector3 arenaCenter, BalanceProfile physics, IEnumerable<IHazard> hazards)
        {
            center = arenaCenter; balance = physics;
            providers = hazards.OfType<IBotHazardHints>().ToArray();
            var layer = LayerMask.NameToLayer("Fighter");
            if (layer < 0) throw new InvalidOperationException("Thiếu layer Fighter cho cảm nhận sàn.");
            floorMask = ~(1 << layer);
        }
        public IBotNavigationSnapshot Capture(float radius, int playingTicks, float dt)
        {
            var side = Mathf.CeilToInt(radius * 2f);
            if (side < 1 || side > DangerMap.MaximumSide) throw new ArgumentOutOfRangeException(nameof(radius));
            var origin = new Vector2(center.x, center.z) - Vector2.one * ((side - 1) * 0.5f);
            var count = side * side;
            var grounded = new bool[count]; var ice = new bool[count];
            var gaps = new List<int>();
            var height = Mathf.Max(1f, Mathf.Abs(balance.KillPlaneY));
            for (var i = 0; i < count; i++)
            {
                var point = new Vector3(origin.x + i % side, center.y, origin.y + i / side);
                var offset = point - center;
                if (offset.x * offset.x + offset.z * offset.z <= radius * radius &&
                    UnityEngine.Physics.Raycast(point + Vector3.up * height, Vector3.down, out var hit,
                        height * 2f, floorMask, QueryTriggerInteraction.Ignore))
                {
                    grounded[i] = true;
                    ice[i] = hit.collider.GetComponentInParent<IceTiles>() != null;
                }
                if (!grounded[i]) gaps.Add(i);
            }
            var cells = new BotTerrainCell[count];
            for (var i = 0; i < count; i++)
            {
                if (!grounded[i]) { cells[i] = new BotTerrainCell(false, ice[i], 0); continue; }
                var x = i % side; var z = i / side;
                var offset = new Vector2(origin.x + x - center.x, origin.y + z - center.z);
                var circleEdge = Mathf.Max(0, radius - offset.magnitude);
                var nearestSquared = circleEdge * circleEdge;
                // Lỗ là ô vuông của lưới 1 m. Dùng khoảng cách tới biên ô, không tới tâm lỗ.
                foreach (var gap in gaps)
                {
                    var dx = Mathf.Max(0, Mathf.Abs(x - gap % side) - 0.5f);
                    var dz = Mathf.Max(0, Mathf.Abs(z - gap / side) - 0.5f);
                    nearestSquared = Mathf.Min(nearestSquared, dx * dx + dz * dz);
                }
                cells[i] = new BotTerrainCell(true, ice[i], Mathf.Sqrt(nearestSquared));
            }
            var warnings = new List<BotHazardWarning>();
            foreach (var provider in providers) provider.CollectBotWarnings(warnings, playingTicks, dt);
            return new Snapshot(side, origin, cells, warnings);
        }
    }
}
