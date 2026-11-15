using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Match
{
    /// <summary>
    /// Rank respawn point (GDD 3.6, LD13): on the remaining floor, at least the edge margin inside it,
    /// never on an L1 band that drops within the warning lead, outside active hazards, and as far as
    /// possible from other fruits and hazards. Pure and deterministic, so the host and tests agree.
    /// </summary>
    public static class RespawnPicker
    {
        // Sampling resolution of the candidate grid, not a balance value.
        private const float SampleSpacingMeters = 0.5f;

        /// <summary>
        /// Radius of the floor that is still safe to stand on: the remaining L1 floor, minus the band that
        /// drops within <see cref="BalanceProfile.RingDropWarningSeconds"/>, minus the edge margin.
        /// Levels without H01 pass <paramref name="ringDrop"/> = false.
        /// </summary>
        public static float SafeRadius(float arenaRadius, int playingTicks, BalanceProfile balance, float dt, bool ringDrop)
        {
            var radius = arenaRadius;
            if (ringDrop)
            {
                var horizon = playingTicks + Mathf.RoundToInt(balance.RingDropWarningSeconds / dt);
                foreach (var seconds in balance.RingDropSeconds)
                    if (Mathf.RoundToInt(seconds / dt) <= horizon) radius -= balance.RingWidthMeters;
            }
            return radius - balance.RespawnEdgeMarginMeters;
        }

        /// <summary>
        /// Best point inside <paramref name="safeRadius"/> around <paramref name="center"/>.
        /// False when no floor or every point lies in a hazard.
        /// </summary>
        public static bool TryPick(Vector3 center, float safeRadius, IReadOnlyList<Vector3> otherFruits,
            IReadOnlyList<HazardZone> hazards, out Vector3 point)
        {
            point = center;
            if (safeRadius < 0f) return false;

            var found = false;
            var best = float.NegativeInfinity;
            var steps = Mathf.FloorToInt(safeRadius / SampleSpacingMeters);
            for (var ix = -steps; ix <= steps; ix++)
            for (var iz = -steps; iz <= steps; iz++)
            {
                var offset = new Vector3(ix * SampleSpacingMeters, 0f, iz * SampleSpacingMeters);
                if (offset.sqrMagnitude > safeRadius * safeRadius) continue;
                var candidate = center + offset;
                var score = Clearance(candidate, otherFruits, hazards);
                // Ties (e.g. an empty arena) go to the point nearest the centre.
                if (score < 0f || score < best || score == best && offset.sqrMagnitude >= (point - center).sqrMagnitude)
                    continue;
                best = score;
                point = candidate;
                found = true;
            }
            return found;
        }

        /// <summary>Distance to the nearest fruit or hazard edge (negative inside a hazard; +∞ if none).</summary>
        public static float Clearance(Vector3 p, IReadOnlyList<Vector3> otherFruits, IReadOnlyList<HazardZone> hazards)
        {
            var clearance = float.PositiveInfinity;
            if (otherFruits != null)
                foreach (var f in otherFruits) clearance = Mathf.Min(clearance, Flat(p - f));
            if (hazards != null)
                foreach (var h in hazards) clearance = Mathf.Min(clearance, Flat(p - h.Center) - h.Radius);
            return clearance;
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
