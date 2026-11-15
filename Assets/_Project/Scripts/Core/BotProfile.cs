using System;
using UnityEngine;

namespace SquishyBump.Core
{
    public enum BotTargeting { Random, Utility }
    public enum BotIceHandling { None, WiderMargin, BrakeEarly, PredictSlide }
    public enum BotClassTactics { None, AvoidHeavier, PositionByClass, Predictive }

    [Flags]
    public enum BotAwareness
    {
        None = 0,
        ArenaEdge = 1 << 0,
        RingDrop = 1 << 1,
        Cannons = 1 << 2,
        TurntableReversal = 1 << 3,
        ObstacleBoxes = 1 << 4,
        PusherLanes = 1 << 5,
        IceTiles = 1 << 6,
        Blasts = 1 << 7,
        BurningPatches = 1 << 8,
        MatchTimeRemaining = 1 << 9,
        TimeToImpact = 1 << 10,
    }

    [CreateAssetMenu(menuName = "Squishy Bump/Bot Profile")]
    public sealed class BotProfile : ScriptableObject
    {
        [SerializeField] private int tier;
        [SerializeField] private float decisionRateHz;
        [SerializeField] private float reactionDelaySeconds;
        [SerializeField] private float aimNoiseDegrees;
        [SerializeField] private float chargeAlignmentDegrees;
        [SerializeField] private float targetLeadSeconds;
        [SerializeField] private float edgeMarginMeters;
        [SerializeField] private float speedFactor;
        [SerializeField] private BotTargeting targeting;
        [SerializeField] private BotAwareness awareness;
        [SerializeField] private BotIceHandling iceHandling;
        [SerializeField] private float iceEdgeMarginMultiplier;
        [SerializeField] private BotClassTactics classTactics;

        public int Tier => tier;
        public float DecisionRateHz => decisionRateHz;
        public float ReactionDelaySeconds => reactionDelaySeconds;
        public float AimNoiseDegrees => aimNoiseDegrees;
        public float ChargeAlignmentDegrees => chargeAlignmentDegrees;
        public float TargetLeadSeconds => targetLeadSeconds;
        public float EdgeMarginMeters => edgeMarginMeters;
        public float SpeedFactor => speedFactor;
        public BotTargeting Targeting => targeting;
        public BotAwareness Awareness => awareness;
        public BotIceHandling IceHandling => iceHandling;
        public float IceEdgeMarginMultiplier => iceEdgeMarginMultiplier;
        public BotClassTactics ClassTactics => classTactics;
    }
}
