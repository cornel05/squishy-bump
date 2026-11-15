using UnityEngine;

namespace SquishyBump.Core
{
    /// <summary>
    /// Hazard timings and telegraphs of GDD chapter 7 that are not in the 6.1 table.
    /// Ring drop times and warning lead stay in <see cref="BalanceProfile"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Hazard Profile")]
    public sealed class HazardProfile : ScriptableObject
    {
        [Header("H01 Ring drop (GDD 7.1)")]
        [Tooltip("Last part of the warning where the seam turns red and blinks.")]
        [SerializeField] private float ringDropFinalWarningSeconds;
        [SerializeField] private float telegraphBlinkHz;

        [Header("H02 Turntable (GDD 7.2)")]
        [SerializeField] private float turntableRadius;
        [SerializeField] private float turntableDegreesPerSecond;
        [SerializeField] private float turntableReversalSeconds;
        [SerializeField] private float turntableRampSeconds;
        [SerializeField] private float turntableWarningSeconds;

        [Header("H07 Obstacle ring (GDD 7.2, 6.5)")]
        [SerializeField] private float obstacleRingOuterRadius;
        [Tooltip("Turns against the turntable.")]
        [SerializeField] private float obstacleRingDegreesPerSecond;
        [SerializeField] private int obstacleBoxCount;
        [SerializeField] private float obstacleBoxRadius;
        [Tooltip("x along the ring, y height, z radial; metres.")]
        [SerializeField] private Vector3 obstacleBoxSize;

        [Header("Box and block contact (GDD 6.5)")]
        [SerializeField] private float obstacleImpulse;
        [Tooltip("Weight of r̂ in normalize(t̂ + k·r̂) for H07.")]
        [SerializeField] private float obstacleRadialShare;
        [SerializeField] private int obstacleWobble;
        [SerializeField] private float obstacleRehitSeconds;

        public float RingDropFinalWarningSeconds => ringDropFinalWarningSeconds;
        public float TelegraphBlinkHz => telegraphBlinkHz;
        public float TurntableRadius => turntableRadius;
        public float TurntableDegreesPerSecond => turntableDegreesPerSecond;
        public float TurntableReversalSeconds => turntableReversalSeconds;
        public float TurntableRampSeconds => turntableRampSeconds;
        public float TurntableWarningSeconds => turntableWarningSeconds;
        public float ObstacleRingOuterRadius => obstacleRingOuterRadius;
        public float ObstacleRingDegreesPerSecond => obstacleRingDegreesPerSecond;
        public int ObstacleBoxCount => obstacleBoxCount;
        public float ObstacleBoxRadius => obstacleBoxRadius;
        public Vector3 ObstacleBoxSize => obstacleBoxSize;
        public float ObstacleImpulse => obstacleImpulse;
        public float ObstacleRadialShare => obstacleRadialShare;
        public int ObstacleWobble => obstacleWobble;
        public float ObstacleRehitSeconds => obstacleRehitSeconds;
    }
}
