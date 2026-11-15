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

        [Header("H03 Pusher blocks (GDD 7.2)")]
        [Tooltip("Lanes run east–west at z = ±this.")]
        [SerializeField] private float pusherLaneZ;
        [Tooltip("Docks at x = ±this (off the floor).")]
        [SerializeField] private float pusherDockX;
        [Tooltip("x along the lane, y height, z across; metres.")]
        [SerializeField] private Vector3 pusherSize;
        [SerializeField] private float pusherSpeed;
        [SerializeField] private float pusherEaseSeconds;
        [SerializeField] private float pusherDockWaitSeconds;
        [SerializeField] private float pusherFirstDepartureSeconds;
        [Tooltip("Dock light turns amber this long before a departure.")]
        [SerializeField] private float pusherLightSeconds;

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
        public float PusherLaneZ => pusherLaneZ;
        public float PusherDockX => pusherDockX;
        public Vector3 PusherSize => pusherSize;
        public float PusherSpeed => pusherSpeed;
        public float PusherEaseSeconds => pusherEaseSeconds;
        public float PusherDockWaitSeconds => pusherDockWaitSeconds;
        public float PusherFirstDepartureSeconds => pusherFirstDepartureSeconds;
        public float PusherLightSeconds => pusherLightSeconds;
        public float ObstacleImpulse => obstacleImpulse;
        public float ObstacleRadialShare => obstacleRadialShare;
        public int ObstacleWobble => obstacleWobble;
        public float ObstacleRehitSeconds => obstacleRehitSeconds;
    }
}
