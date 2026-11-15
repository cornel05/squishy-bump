using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquishyBump.Match
{
    public enum RingOutReason { BelowKillPlane, OutsideArena }

    /// <summary>Kiểm tra tâm quả theo GDD 3.6. MatchDirector gọi từ vòng mô phỏng host.</summary>
    public sealed class TrainingRingBounds : MonoBehaviour
    {
        [SerializeField] private float outerRadius;
        [SerializeField] private float killPlaneY;
        [SerializeField] private Transform[] spawnPoints;
        private readonly HashSet<ulong> reportedFighters = new();

        public float OuterRadius => outerRadius;
        public float KillPlaneY => killPlaneY;
        public IReadOnlyList<Transform> SpawnPoints => spawnPoints;
        public event Action<ulong, uint, RingOutReason> RingOut;

        public void SampleOnHost(ulong fighterId, uint hostTick, Vector3 worldCenter)
        {
            var reason = RingOutReason.BelowKillPlane;
            if (worldCenter.y >= killPlaneY)
            {
                var offset = worldCenter - transform.position;
                if (offset.x * offset.x + offset.z * offset.z <= outerRadius * outerRadius)
                    return;
                reason = RingOutReason.OutsideArena;
            }

            if (reportedFighters.Add(fighterId))
                RingOut?.Invoke(fighterId, hostTick, reason);
        }

        public void ResetFighterOnHost(ulong fighterId) => reportedFighters.Remove(fighterId);
    }
}
