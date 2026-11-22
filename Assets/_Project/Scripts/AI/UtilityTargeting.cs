using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquishyBump.AI
{
    /// <summary>Công thức A.3; không có trường phân biệt người thật và bot.</summary>
    public static class UtilityTargeting
    {
        public const float Hysteresis = 0.15f;
        public const float EngageThreshold = 0.4f;
        public readonly struct Candidate
        {
            public Candidate(ushort id, float utility) { Id = id; Utility = utility; }
            public ushort Id { get; }
            public float Utility { get; }
        }
        public static float Score(float distance, float targetEdgeDistance, int wobble,
            float targetMass, float ownMass, float pathDanger)
        {
            var vulnerability = targetMass < ownMass ? 1f : targetMass == ownMass ? 0.5f : 0f;
            return 0.4f * (1 - Mathf.Clamp01(distance / 12f)) +
                0.3f * (1 - Mathf.Clamp01(targetEdgeDistance / 6f)) +
                0.2f * Mathf.Clamp01(wobble / 10f) + 0.1f * vulnerability - 0.5f * Mathf.Clamp01(pathDanger);
        }
        public static ushort? Select(IReadOnlyList<Candidate> candidates, ushort? current, System.Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            Candidate? best = null, previous = null;
            var ties = 0;
            foreach (var c in candidates)
            {
                if (float.IsNaN(c.Utility) || float.IsInfinity(c.Utility) || c.Utility < EngageThreshold) continue;
                if (c.Id == current) previous = c;
                if (!best.HasValue || c.Utility > best.Value.Utility) { best = c; ties = 1; }
                else if (c.Utility == best.Value.Utility && random.Next(++ties) == 0) best = c;
            }
            if (!best.HasValue) return null;
            if (previous.HasValue && best.Value.Id != current)
            {
                var improvement = best.Value.Utility - previous.Value.Utility;
                // 0.75f - 0.60f nằm sát dưới 0.15f do biểu diễn float.
                if (improvement < Hysteresis && !Mathf.Approximately(improvement, Hysteresis))
                    return previous.Value.Id;
            }
            return best.Value.Id;
        }
    }
}
