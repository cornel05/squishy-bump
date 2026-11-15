using System.Collections.Generic;
using UnityEngine;

namespace SquishyBump.Core
{
    public enum HazardId
    {
        RingDrop = 1,
        Turntable = 2,
        PusherBlocks = 3,
        AirCannon = 4,
        FireCannon = 5,
        CrackingIce = 6,
        ObstacleRing = 7,
    }

    public enum LevelEndTrigger { TimeLimit, FinalRingDrop }

    [CreateAssetMenu(menuName = "Squishy Bump/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private int levelNumber;
        [SerializeField] private string arenaName;
        [SerializeField] private string scenePath;
        [SerializeField] private float arenaRadiusMeters;
        [Tooltip("Initial floor positions. Add the fighter's collider offset when spawning.")]
        [SerializeField] private Vector3[] spawnPoints;
        [SerializeField] private HazardId[] hazards;
        [SerializeField] private LevelEndTrigger endTrigger;
        [Tooltip("Seconds from GO to the configured end trigger.")]
        [SerializeField] private float endTriggerSeconds;

        public int LevelNumber => levelNumber;
        public string ArenaName => arenaName;
        public string ScenePath => scenePath;
        public float ArenaRadiusMeters => arenaRadiusMeters;
        public IReadOnlyList<Vector3> SpawnPoints => spawnPoints;
        public IReadOnlyList<HazardId> Hazards => hazards;
        public LevelEndTrigger EndTrigger => endTrigger;
        public float EndTriggerSeconds => endTriggerSeconds;
    }
}
