using System.Collections.Generic;
using UnityEngine;

namespace SquishyBump.Net.Sync
{
    /// <summary>
    /// Client side: keeps recent snapshots and samples every fighter <c>delay</c> seconds behind the newest
    /// host time (GDD 10.4: 100 ms buffer). Host time = tick × dt; between packets it advances with local time.
    /// Stale or reordered snapshots (older tick) are ignored.
    /// </summary>
    public sealed class SnapshotBuffer
    {
        private const int Keep = 16;
        private readonly List<WorldSnapshot> snapshots = new();
        private readonly float tickSeconds;
        private readonly float delay;
        private float newestHostTime = float.NaN;
        private float receivedAt;

        public SnapshotBuffer(float tickSeconds, float delaySeconds)
        {
            this.tickSeconds = tickSeconds;
            delay = delaySeconds;
        }

        public WorldSnapshot Newest => snapshots.Count > 0 ? snapshots[^1] : null;

        public bool Add(WorldSnapshot snapshot, float localTime)
        {
            if (Newest != null && snapshot.HostTick <= Newest.HostTick) return false;
            snapshots.Add(snapshot);
            if (snapshots.Count > Keep) snapshots.RemoveAt(0);
            newestHostTime = snapshot.HostTick * tickSeconds;
            receivedAt = localTime;
            return true;
        }

        public float RenderTime(float localTime) => newestHostTime + (localTime - receivedAt) - delay;

        /// <summary>Position of a fighter at render time, linearly between the two snapshots around it.</summary>
        public bool TrySample(ushort id, float localTime, out Vector3 position, out FighterSnapshot state)
        {
            position = default;
            state = default;
            if (snapshots.Count == 0) return false;
            var t = RenderTime(localTime);

            for (var i = snapshots.Count - 1; i >= 0; i--)
            {
                var a = snapshots[i];
                var aTime = a.HostTick * tickSeconds;
                if (aTime > t && i > 0) continue;
                if (!Find(a, id, out var sa)) return false;
                state = sa;
                position = sa.Position;
                if (i + 1 < snapshots.Count && Find(snapshots[i + 1], id, out var sb))
                {
                    var bTime = snapshots[i + 1].HostTick * tickSeconds;
                    position = Vector3.Lerp(sa.Position, sb.Position, Mathf.InverseLerp(aTime, bTime, t));
                }
                return true;
            }
            return false;
        }

        private static bool Find(WorldSnapshot s, ushort id, out FighterSnapshot f)
        {
            foreach (var x in s.Fighters)
                if (x.Id == id) { f = x; return true; }
            f = default;
            return false;
        }
    }
}
