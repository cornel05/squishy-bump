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

        [Header("H04 Air cannon / H05 fire cannon (GDD 7.3, 6.5)")]
        [Tooltip("Cannons stand at this radius, just outside the rim.")]
        [SerializeField] private float cannonRimRadius;
        [Tooltip("Muzzle height above the floor (not in the GDD; visual, enters Δh).")]
        [SerializeField] private float cannonMuzzleHeight;
        [Tooltip("Sweep ± this around the home direction.")]
        [SerializeField] private float cannonSweepDegrees;
        [SerializeField] private float cannonSweepDegreesPerSecond;
        [SerializeField] private float cannonPeriodSeconds;
        [Tooltip("L5 Chaos Core.")]
        [SerializeField] private float cannonChaosPeriodSeconds;
        [SerializeField] private float cannonStaggerSeconds;
        [SerializeField] private float cannonTelegraphSeconds;
        [SerializeField] private float airConeHalfAngle;
        [SerializeField] private float airRange;
        [SerializeField] private float airGustSeconds;
        [Tooltip("F₀ in N.")]
        [SerializeField] private float airForce;
        [SerializeField] private float fireLaunchAngle;
        [SerializeField] private float fireMinRange;
        [SerializeField] private float fireMaxRange;
        [SerializeField] private float fireFallbackRange;
        [SerializeField] private float blastRadius;
        [Tooltip("J₀ in N·s.")]
        [SerializeField] private float blastImpulse;
        [SerializeField] private float blastUpShare;
        [SerializeField] private int blastWobble;
        [SerializeField] private float patchRadius;
        [SerializeField] private float patchSeconds;

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
        public float CannonRimRadius => cannonRimRadius;
        public float CannonMuzzleHeight => cannonMuzzleHeight;
        public float CannonSweepDegrees => cannonSweepDegrees;
        public float CannonSweepDegreesPerSecond => cannonSweepDegreesPerSecond;
        public float CannonPeriodSeconds => cannonPeriodSeconds;
        public float CannonChaosPeriodSeconds => cannonChaosPeriodSeconds;
        public float CannonStaggerSeconds => cannonStaggerSeconds;
        public float CannonTelegraphSeconds => cannonTelegraphSeconds;
        public float AirConeHalfAngle => airConeHalfAngle;
        public float AirRange => airRange;
        public float AirGustSeconds => airGustSeconds;
        public float AirForce => airForce;
        public float FireLaunchAngle => fireLaunchAngle;
        public float FireMinRange => fireMinRange;
        public float FireMaxRange => fireMaxRange;
        public float FireFallbackRange => fireFallbackRange;
        public float BlastRadius => blastRadius;
        public float BlastImpulse => blastImpulse;
        public float BlastUpShare => blastUpShare;
        public int BlastWobble => blastWobble;
        public float PatchRadius => patchRadius;
        public float PatchSeconds => patchSeconds;
        public float ObstacleImpulse => obstacleImpulse;
        public float ObstacleRadialShare => obstacleRadialShare;
        public int ObstacleWobble => obstacleWobble;
        public float ObstacleRehitSeconds => obstacleRehitSeconds;
    }
}
