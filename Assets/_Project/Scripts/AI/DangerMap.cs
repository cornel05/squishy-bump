using System;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.AI
{
    /// <summary>Lưới 1 m, GDD A.4. Một lần dựng ở mỗi quyết định từ ảnh chụp cảm nhận.</summary>
    public sealed class DangerMap
    {
        public const int MaximumSide = 26;
        private readonly float[] danger;
        private readonly bool[] walkable;
        public int Width { get; }
        public int Height { get; }
        public int Count => danger.Length;
        public Vector2 Origin { get; }
        public float this[int index] => danger[index];
        public bool Walkable(int index) => index >= 0 && index < Count && walkable[index];

        public DangerMap(IBotNavigationSnapshot snapshot, float edgeMargin,
            BotAwareness awareness, float iceMarginMultiplier = 1f)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Width < 1 || snapshot.Height < 1 || snapshot.Width > MaximumSide ||
                snapshot.Height > MaximumSide || !Finite(edgeMargin) || edgeMargin <= 0 ||
                !Finite(iceMarginMultiplier) || iceMarginMultiplier < 1)
                throw new ArgumentOutOfRangeException(nameof(snapshot));
            Width = snapshot.Width; Height = snapshot.Height; Origin = snapshot.Origin;
            danger = new float[Width * Height]; walkable = new bool[Count];
            for (var i = 0; i < Count; i++)
            {
                var cell = snapshot.Cell(i);
                if (!Finite(cell.EdgeDistance) || cell.EdgeDistance < 0)
                    throw new ArgumentException("Khoảng cách tới mép phải hữu hạn và không âm.");
                walkable[i] = cell.Walkable;
                var margin = edgeMargin * (cell.Ice ? iceMarginMultiplier : 1f);
                var value = cell.Walkable ? Mathf.Clamp01(1 - cell.EdgeDistance / margin) : 1f;
                foreach (var warning in snapshot.Warnings)
                    if ((warning.Awareness & awareness) != 0 && Contains(warning, Position(i)))
                        value = Mathf.Max(value, Mathf.Clamp01(1 - warning.SecondsUntilImpact / 2f));
                danger[i] = value;
            }
        }

        public int Index(int x, int z) => x < 0 || z < 0 || x >= Width || z >= Height ? -1 : z * Width + x;
        public int CellAt(Vector3 position) => Index(Mathf.RoundToInt(position.x - Origin.x),
            Mathf.RoundToInt(position.z - Origin.y));
        public Vector3 Position(int index) => new(Origin.x + index % Width, 0, Origin.y + index / Width);

        public static bool Contains(BotHazardWarning warning, Vector3 point)
        {
            var delta = new Vector2(point.x, point.z) - warning.Center;
            var yaw = warning.YawDegrees * Mathf.Deg2Rad;
            var local = new Vector2(Mathf.Cos(yaw) * delta.x - Mathf.Sin(yaw) * delta.y,
                Mathf.Sin(yaw) * delta.x + Mathf.Cos(yaw) * delta.y);
            switch (warning.Shape)
            {
                case BotHazardShape.Circle: return delta.sqrMagnitude <= warning.Size.x * warning.Size.x;
                case BotHazardShape.Annulus:
                    return delta.sqrMagnitude >= warning.Size.x * warning.Size.x &&
                        delta.sqrMagnitude <= warning.Size.y * warning.Size.y;
                case BotHazardShape.Box:
                    return Mathf.Abs(local.x) <= warning.Size.x && Mathf.Abs(local.y) <= warning.Size.y;
                case BotHazardShape.Cone:
                    return delta.sqrMagnitude <= warning.Size.x * warning.Size.x &&
                        (delta.sqrMagnitude < 0.000001f || local.y / delta.magnitude >= Mathf.Cos(warning.Size.y * Mathf.Deg2Rad));
                case BotHazardShape.Hex:
                    return Mathf.Abs(local.x) <= warning.Size.x / 2f && Mathf.Abs(local.y) <=
                        warning.Size.x / Mathf.Sqrt(3f) - Mathf.Abs(local.x) / Mathf.Sqrt(3f);
                default: return false;
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
