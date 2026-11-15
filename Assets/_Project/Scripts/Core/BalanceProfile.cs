using System.Collections.Generic;
using UnityEngine;

namespace SquishyBump.Core
{
    /// <summary>Class-independent values of GDD 6.1. Per-class values live in <see cref="ClassProfile"/>.</summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Balance Profile")]
    public sealed class BalanceProfile : ScriptableObject
    {
        [Header("Simulation")]
        [SerializeField] private float fixedStepHz;
        [Tooltip("Positive magnitude, m/s².")]
        [SerializeField] private float gravity;

        [Header("Movement")]
        [SerializeField] private float floorFriction;
        [SerializeField] private float iceFriction;
        [Tooltip("Fraction of a_max available in the air.")]
        [SerializeField] private float airControl;

        [Header("Jump")]
        [SerializeField] private float jumpApexMeters;

        [Header("Dash")]
        [SerializeField] private float dashImpulse;
        [SerializeField] private float dashCooldownSeconds;
        [SerializeField] private float dashStateSeconds;
        [SerializeField] private float dashBonus;

        [Header("Collision")]
        [SerializeField] private float restitution;
        [SerializeField] private float hitThreshold;
        [Tooltip("Share of the overlap beyond the slop that is removed per tick (GDD 6.3).")]
        [SerializeField] private float penetrationCorrection;
        [SerializeField] private float penetrationSlopMeters;

        [Header("Wobble")]
        [SerializeField] private float knockbackPerWobble;
        [SerializeField] private float knockbackCap;
        [SerializeField] private int wobbleMax;
        [SerializeField] private float wobbleDecaySeconds;

        [Header("Stun")]
        [SerializeField] private float stunBaseSeconds;
        [SerializeField] private float stunPerWobbleSeconds;
        [Tooltip("How long a press made during stun stays valid (GDD 4.4).")]
        [SerializeField] private float stunInputBufferSeconds;

        [Header("Match")]
        [SerializeField] private float timeLimitSeconds;
        [SerializeField] private float[] ringDropSeconds;
        [SerializeField] private float ringDropWarningSeconds;
        [SerializeField] private float ringWidthMeters;
        [SerializeField] private float killPlaneY;

        [Header("Rank rule")]
        [SerializeField] private int lives;
        [SerializeField] private float respawnDelaySeconds;
        [SerializeField] private float invulnerabilitySeconds;
        [Tooltip("Respawn points stay at least this far inside the current floor edge (GDD 3.6).")]
        [SerializeField] private float respawnEdgeMarginMeters;
        [SerializeField] private float lastTouchWindowSeconds;
        [SerializeField] private int pointsPerRingOut;

        [Header("Network")]
        [SerializeField] private float inputRateHz;
        [SerializeField] private float snapshotRateHz;
        [SerializeField] private float interpolationBufferSeconds;

        public float FixedStepHz => fixedStepHz;
        public float Gravity => gravity;
        public float FloorFriction => floorFriction;
        public float IceFriction => iceFriction;
        public float AirControl => airControl;
        public float JumpApexMeters => jumpApexMeters;
        public float DashImpulse => dashImpulse;
        public float DashCooldownSeconds => dashCooldownSeconds;
        public float DashStateSeconds => dashStateSeconds;
        public float DashBonus => dashBonus;
        public float Restitution => restitution;
        public float HitThreshold => hitThreshold;
        public float PenetrationCorrection => penetrationCorrection;
        public float PenetrationSlopMeters => penetrationSlopMeters;
        public float KnockbackPerWobble => knockbackPerWobble;
        public float KnockbackCap => knockbackCap;
        public int WobbleMax => wobbleMax;
        public float WobbleDecaySeconds => wobbleDecaySeconds;
        public float StunBaseSeconds => stunBaseSeconds;
        public float StunPerWobbleSeconds => stunPerWobbleSeconds;
        public float StunInputBufferSeconds => stunInputBufferSeconds;
        public float TimeLimitSeconds => timeLimitSeconds;
        public IReadOnlyList<float> RingDropSeconds => ringDropSeconds;
        public float RingDropWarningSeconds => ringDropWarningSeconds;
        public float RingWidthMeters => ringWidthMeters;
        public float KillPlaneY => killPlaneY;
        public int Lives => lives;
        public float RespawnDelaySeconds => respawnDelaySeconds;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;
        public float RespawnEdgeMarginMeters => respawnEdgeMarginMeters;
        public float LastTouchWindowSeconds => lastTouchWindowSeconds;
        public int PointsPerRingOut => pointsPerRingOut;
        public float InputRateHz => inputRateHz;
        public float SnapshotRateHz => snapshotRateHz;
        public float InterpolationBufferSeconds => interpolationBufferSeconds;

        /// <summary>Launch speed √(2·g·h) (GDD 6.2).</summary>
        public float JumpSpeed => Mathf.Sqrt(2f * gravity * jumpApexMeters);
        public float JumpAirTime => 2f * JumpSpeed / gravity;

        /// <summary>a_max = c·μ·g (GDD 6.2).</summary>
        public float MaxAcceleration(ClassProfile fighter, float friction) => fighter.Traction * friction * gravity;
        public float DashDeltaV(ClassProfile fighter) => dashImpulse / fighter.Mass;
        /// <summary>K(h) = min(1 + k·h, cap) (GDD 6.3).</summary>
        public float Knockback(int wobble) => Mathf.Min(1f + knockbackPerWobble * wobble, knockbackCap);
        public float StunSeconds(int wobble) => stunBaseSeconds + stunPerWobbleSeconds * wobble;
    }
}
