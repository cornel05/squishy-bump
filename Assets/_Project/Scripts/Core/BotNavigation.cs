using System.Collections.Generic;
using UnityEngine;

namespace SquishyBump.Core
{
    public enum BotHazardShape { Circle, Annulus, Box, Cone, Hex }

    /// <summary>Ảnh chụp dữ liệu cảnh báo; không điều khiển hazard. Toạ độ phẳng là (x,z).</summary>
    public readonly struct BotHazardWarning
    {
        public BotHazardWarning(BotAwareness awareness, BotHazardShape shape, Vector2 center,
            Vector2 size, float yawDegrees, float secondsUntilImpact)
        {
            Awareness = awareness; Shape = shape; Center = center; Size = size;
            YawDegrees = yawDegrees; SecondsUntilImpact = secondsUntilImpact;
        }
        public BotAwareness Awareness { get; }
        public BotHazardShape Shape { get; }
        public Vector2 Center { get; }
        /// <summary>Circle: (radius,0); Annulus: (inner,outer); Box: half extents;
        /// Cone: (range,half angle degrees); Hex: (flat-to-flat,0).</summary>
        public Vector2 Size { get; }
        public float YawDegrees { get; }
        public float SecondsUntilImpact { get; }
    }

    public readonly struct BotTerrainCell
    {
        public BotTerrainCell(bool walkable, bool ice, float edgeDistance)
        { Walkable = walkable; Ice = ice; EdgeDistance = edgeDistance; }
        public bool Walkable { get; }
        public bool Ice { get; }
        public float EdgeDistance { get; }
    }

    /// <summary>Ảnh chụp bất biến do App cấp. AI không tìm scene singleton hoặc ghi vật lý.</summary>
    public interface IBotNavigationSnapshot
    {
        int Width { get; }
        int Height { get; }
        /// <summary>Tâm ô (0,0), trong toạ độ thế giới (x,z); khoảng cách ô là 1 m.</summary>
        Vector2 Origin { get; }
        BotTerrainCell Cell(int index);
        IReadOnlyList<BotHazardWarning> Warnings { get; }
    }

    public interface IBotHazardHints
    {
        /// <summary>Chỉ đọc trạng thái host đã cảnh báo/đang tác động; không tự lập lịch hazard mới.</summary>
        void CollectBotWarnings(List<BotHazardWarning> warnings, int playingTicks, float dt);
    }
}
