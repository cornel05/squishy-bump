using System;
using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.AI
{
    public enum BotState { Countdown, Roam, Chase, Charge, Evade, Recover, Eliminated }

    public readonly struct BotFighter
    {
        public BotFighter(ushort id, Vector3 position, Vector3 velocity, bool alive)
        { Id = id; Position = position; Velocity = velocity; Alive = alive; }
        public ushort Id { get; }
        public Vector3 Position { get; }
        public Vector3 Velocity { get; }
        public bool Alive { get; }
    }

    /// <summary>FSM D1 chạy trên host. App cấp cảm nhận; đầu ra duy nhất là InputCommand.</summary>
    public sealed class BotBrain
    {
        // Hằng hành vi A.2/A.3; thông số bậc lấy từ BotProfile.
        public const float RetargetSeconds = 4f, StuckSeconds = 2f, MinimumProgress = 0.5f;
        private const float ChargeDistance = 3.5f, RecoverSeconds = 0.4f;
        private sealed class Sense
        {
            public float Time;
            public Vector3 Position, Center;
            public float Radius, Yaw;
            public BotFighter[] Fighters;
        }
        private readonly BotProfile profile;
        private readonly ushort id;
        private readonly System.Random random;
        private readonly Queue<Sense> senses = new();
        private Sense sensed;
        private float time, nextDecision, nextTarget, recoverUntil, stuckAt, goalDistance;
        private Vector3 goal;
        private ushort? target;
        private ushort? progressTarget;
        private Vector2 move;
        private float yaw;
        private bool dash;
        private uint sequence;
        private bool chargeObserved;
        private float chargeAt;
        private bool avoidStuckGoal;

        public BotBrain(BotProfile profile, ushort id, uint seed)
        {
            this.profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
            this.id = id;
            random = new System.Random(unchecked((int)(seed ^ id)));
        }

        public BotState State { get; private set; } = BotState.Countdown;
        public ushort? Target => target;
        public int StuckRecoveries { get; private set; }
        public int TargetSelections { get; private set; }
        public bool DebugTransitions { get; set; }
        public event Action<BotState> StateChanged;

        public InputCommand Step(bool host, bool playing, bool alive, bool isDashing, bool dashReady,
            Vector3 position, float facingYaw, Vector3 center, float radius, IReadOnlyList<BotFighter> fighters, float dt)
        {
            if (!host) return default;
            time += dt;
            if (!alive) { Change(BotState.Eliminated); return Command(Vector2.zero, false); }
            if (!playing) { Change(BotState.Countdown); return Command(Vector2.zero, false); }
            if (State == BotState.Countdown || State == BotState.Eliminated)
            {
                senses.Clear(); sensed = null;
                goal = center; stuckAt = time; goalDistance = FlatDistance(position, goal);
                nextDecision = time; nextTarget = time;
                Change(BotState.Roam);
            }
            var copy = new BotFighter[fighters.Count];
            for (var i = 0; i < copy.Length; i++) copy[i] = fighters[i];
            senses.Enqueue(new Sense { Time = time, Position = position, Yaw = sequence == 0 ? facingYaw : yaw,
                Center = center, Radius = radius, Fighters = copy });
            while (senses.Count > 0 && senses.Peek().Time <= time - profile.ReactionDelaySeconds + dt * 0.01f)
                sensed = senses.Dequeue();
            dash = false;
            if (State == BotState.Charge)
            {
                chargeObserved |= isDashing;
                if (!isDashing && (chargeObserved || time > chargeAt + dt))
                { recoverUntil = time + RecoverSeconds; move = Vector2.zero; Change(BotState.Recover); }
            }
            if (sensed != null && time >= nextDecision)
            {
                nextDecision = time + 1f / profile.DecisionRateHz;
                Decide(dashReady);
            }
            return Command(move, dash);
        }

        private void Decide(bool dashReady)
        {
            var distanceToEdge = sensed.Radius - FlatDistance(sensed.Position, sensed.Center);
            var danger = Mathf.Clamp01(1 - distanceToEdge / profile.EdgeMarginMeters);
            if (danger >= 0.5f) { goal = sensed.Center; Change(BotState.Evade); }
            else if (State == BotState.Evade && danger < 0.2f) Change(BotState.Roam);
            if (State == BotState.Evade) { Steer(goal); return; }
            if (State == BotState.Recover)
            {
                move = Vector2.zero;
                yaw = Facing(sensed.Center - sensed.Position);
                if (time < recoverUntil) return;
                Change(BotState.Roam);
            }
            if (State == BotState.Charge) return;

            var progress = goalDistance - FlatDistance(sensed.Position, goal);
            if (progress >= MinimumProgress) { stuckAt = time; goalDistance = FlatDistance(sensed.Position, goal); }
            else if (time - stuckAt >= StuckSeconds && FlatDistance(sensed.Position, goal) > MinimumProgress)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2;
                var radius = Mathf.Sqrt((float)random.NextDouble()) * Mathf.Max(0, sensed.Radius - profile.EdgeMarginMeters);
                goal = sensed.Center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
                target = null; avoidStuckGoal = true; nextTarget = time + RetargetSeconds;
                StuckRecoveries++; stuckAt = time; goalDistance = FlatDistance(sensed.Position, goal);
                Change(BotState.Roam); Steer(goal); return;
            }
            if (time >= nextTarget)
            {
                var candidates = new List<ushort>();
                foreach (var f in sensed.Fighters) if (f.Id != id && f.Alive) candidates.Add(f.Id);
                target = candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
                TargetSelections++;
                avoidStuckGoal = false;
                nextTarget = time + RetargetSeconds;
            }
            BotFighter? victim = null;
            foreach (var f in sensed.Fighters) if (f.Id == target && f.Alive) victim = f;
            if (victim == null)
            {
                target = null;
                if (!avoidStuckGoal) goal = sensed.Center;
                Change(BotState.Roam); Steer(goal); return;
            }
            if (State != BotState.Chase || progressTarget != target)
            {
                progressTarget = target;
                stuckAt = time;
                goalDistance = FlatDistance(sensed.Position, victim.Value.Position);
            }
            Change(BotState.Chase);
            goal = victim.Value.Position;
            var direction = goal + victim.Value.Velocity * profile.TargetLeadSeconds - sensed.Position;
            var aim = Facing(direction);
            if (dashReady && FlatDistance(goal, sensed.Position) <= ChargeDistance &&
                Mathf.Abs(Mathf.DeltaAngle(sensed.Yaw, aim)) <= profile.ChargeAlignmentDegrees)
            {
                // Box–Muller: σ đúng A.5, RNG riêng theo seed trận và ID bot.
                var noise = Mathf.Sqrt(-2 * Mathf.Log(Mathf.Max(float.Epsilon, (float)random.NextDouble()))) *
                    Mathf.Cos(2 * Mathf.PI * (float)random.NextDouble()) * profile.AimNoiseDegrees;
                yaw = aim + noise;
                var forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                move = new Vector2(forward.x, forward.z) * profile.SpeedFactor;
                dash = true; chargeObserved = false; chargeAt = time; Change(BotState.Charge);
            }
            else Steer(goal);
        }

        private void Steer(Vector3 destination)
        {
            var delta = destination - sensed.Position; delta.y = 0;
            yaw = Facing(delta);
            move = delta.magnitude <= MinimumProgress ? Vector2.zero : new Vector2(delta.normalized.x, delta.normalized.z) * profile.SpeedFactor;
        }
        private InputCommand Command(Vector2 direction, bool charge)
        {
            // PlayerMotor xoay Move theo Yaw; chuyển vector thế giới về hệ trục input trước khi gửi.
            var local = Quaternion.Euler(0, -yaw, 0) * new Vector3(direction.x, 0, direction.y);
            return new InputCommand(++sequence, new Vector2(local.x, local.z), false, charge, yaw);
        }
        private static float Facing(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        private static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        private void Change(BotState next)
        {
            if (State == next) return;
            if (DebugTransitions) Debug.Log($"Bot {id}: {State} → {next}");
            State = next; StateChanged?.Invoke(next);
        }
    }
}
