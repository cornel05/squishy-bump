using System;
using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.AI
{
    public enum BotState { Countdown, Roam, Chase, Charge, Evade, Recover, Eliminated }

    public readonly struct BotFighter
    {
        public BotFighter(ushort id, Vector3 position, Vector3 velocity, bool alive, float mass = 1f, int wobble = 0)
        { Id = id; Position = position; Velocity = velocity; Alive = alive; Mass = mass; Wobble = wobble; }
        public ushort Id { get; }
        public Vector3 Position { get; }
        public Vector3 Velocity { get; }
        public bool Alive { get; }
        public float Mass { get; }
        public int Wobble { get; }
    }

    /// <summary>FSM chạy trên host; D1 chọn ngẫu nhiên, bậc khác theo profile và utility.
    /// App cấp cảm nhận qua Core; đầu ra duy nhất là InputCommand.</summary>
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
            public IBotNavigationSnapshot Navigation;
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
        private Vector3 actPosition;
        private Vector3 chaseAim;
        private readonly GridPathfinder pathfinder = new();
        private int waypoint;
        public DangerMap Navigation { get; private set; }
        public BotPath PlannedPath { get; private set; }
        public int Replans { get; private set; }
        public float DecisionRateHz => profile.DecisionRateHz;

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
            Vector3 position, float facingYaw, Vector3 center, float radius, IReadOnlyList<BotFighter> fighters, float dt,
            IBotNavigationSnapshot navigation = null)
        {
            if (!host) return default;
            actPosition = position;
            time += dt;
            if (!alive) { Navigation = null; PlannedPath = default; Change(BotState.Eliminated); return Command(Vector2.zero, false); }
            if (!playing) { Navigation = null; PlannedPath = default; Change(BotState.Countdown); return Command(Vector2.zero, false); }
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
                Center = center, Radius = radius, Fighters = copy, Navigation = navigation });
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
            if (Navigation != null && (State == BotState.Roam || State == BotState.Chase || State == BotState.Evade))
                FollowPath();
            return Command(move, dash);
        }

        private void Decide(bool dashReady)
        {
            if (sensed.Navigation != null)
                Navigation = new DangerMap(sensed.Navigation, profile.EdgeMarginMeters, profile.Awareness,
                    profile.Tier >= 3 ? profile.IceEdgeMarginMultiplier : 1f);
            var distanceToEdge = sensed.Radius - FlatDistance(sensed.Position, sensed.Center);
            var currentCell = Navigation?.CellAt(actPosition) ?? -1;
            var danger = Navigation == null ? Mathf.Clamp01(1 - distanceToEdge / profile.EdgeMarginMeters) :
                Navigation.Walkable(currentCell) ? Navigation[currentCell] : 1f;
            if (danger >= 0.5f) { goal = SafeGoal(false); Change(BotState.Evade); }
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
                goal = Navigation == null ? sensed.Center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius : SafeGoal(true);
                target = null; avoidStuckGoal = true; nextTarget = time + RetargetSeconds;
                StuckRecoveries++; stuckAt = time; goalDistance = FlatDistance(sensed.Position, goal);
                Change(BotState.Roam); Steer(goal); return;
            }
            if (profile.Targeting == BotTargeting.Utility && Navigation != null)
            {
                var candidates = new List<UtilityTargeting.Candidate>();
                var ownMass = 1f;
                foreach (var f in sensed.Fighters) if (f.Id == id) ownMass = f.Mass;
                foreach (var f in sensed.Fighters)
                {
                    if (f.Id == id || !f.Alive) continue;
                    var cell = Navigation.CellAt(f.Position);
                    var path = pathfinder.Find(Navigation, currentCell, cell);
                    if (!path.Found) continue;
                    candidates.Add(new UtilityTargeting.Candidate(f.Id, UtilityTargeting.Score(
                        FlatDistance(actPosition, f.Position), sensed.Navigation.Cell(cell).EdgeDistance,
                        f.Wobble, f.Mass, ownMass, path.MeanDanger)));
                }
                var selected = UtilityTargeting.Select(candidates, target, random);
                if (selected != target) TargetSelections++;
                target = selected; avoidStuckGoal = false;
            }
            else if (time >= nextTarget)
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
            var targetPoint = victim.Value.Position;
            var inward = InwardAt(targetPoint);
            goal = Navigation == null ? targetPoint : targetPoint + inward * (ChargeDistance * 0.5f);
            var own = Navigation == null ? sensed.Position : actPosition;
            chaseAim = targetPoint + victim.Value.Velocity * profile.TargetLeadSeconds;
            var direction = chaseAim - own;
            var aim = Facing(direction);
            var behind = Navigation == null || Vector3.Dot(own - targetPoint, inward) >= 0;
            if (dashReady && behind && FlatDistance(targetPoint, own) <= ChargeDistance &&
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
            if (Navigation != null)
            {
                var start = Navigation.CellAt(actPosition);
                if (!Navigation.Walkable(start)) start = NearestWalkable(actPosition);
                PlannedPath = pathfinder.Find(Navigation, start, NearestWalkable(destination));
                Replans++; waypoint = PlannedPath.Found && PlannedPath.Cells.Count > 1 ? 1 : 0;
                FollowPath(); return;
            }
            var delta = destination - sensed.Position; delta.y = 0;
            yaw = Facing(delta);
            move = delta.magnitude <= MinimumProgress ? Vector2.zero : new Vector2(delta.normalized.x, delta.normalized.z) * profile.SpeedFactor;
        }
        private void FollowPath()
        {
            if (!PlannedPath.Found) { move = Vector2.zero; return; }
            while (waypoint < PlannedPath.Cells.Count - 1 &&
                FlatDistance(actPosition, Navigation.Position(PlannedPath.Cells[waypoint])) <= MinimumProgress) waypoint++;
            var delta = Navigation.Position(PlannedPath.Cells[waypoint]) - actPosition; delta.y = 0;
            yaw = Facing(State == BotState.Chase ? chaseAim - actPosition : delta);
            move = delta.magnitude <= MinimumProgress ? Vector2.zero :
                new Vector2(delta.normalized.x, delta.normalized.z) * profile.SpeedFactor;
        }
        private int NearestWalkable(Vector3 point)
        {
            var result = -1; var best = float.PositiveInfinity;
            for (var i = 0; i < Navigation.Count; i++)
            {
                if (!Navigation.Walkable(i)) continue;
                var distance = FlatDistance(point, Navigation.Position(i));
                if (distance >= best) continue;
                result = i; best = distance;
            }
            return result;
        }
        private Vector3 SafeGoal(bool randomized)
        {
            if (Navigation == null) return sensed.Center;
            var start = Navigation.CellAt(actPosition);
            if (!Navigation.Walkable(start)) start = NearestWalkable(actPosition);
            bool Eligible(int cell) => Navigation[cell] < 0.2f &&
                FlatDistance(actPosition, Navigation.Position(cell)) <= 4f;
            if (!randomized)
            {
                var escape = pathfinder.FindCheapest(Navigation, start, Eligible);
                return escape.Found ? Navigation.Position(escape.Cells[escape.Cells.Count - 1]) : sensed.Center;
            }
            var choices = new List<int>();
            for (var i = 0; i < Navigation.Count; i++)
            {
                if (!Navigation.Walkable(i) || !Eligible(i)) continue;
                choices.Add(i);
            }
            // Rút không hoàn lại: mục tiêu anti-stuck ngẫu nhiên nhưng vẫn phải có đường tới.
            while (choices.Count > 0)
            {
                var pick = random.Next(choices.Count); var cell = choices[pick]; choices.RemoveAt(pick);
                if (pathfinder.Find(Navigation, start, cell).Found) return Navigation.Position(cell);
            }
            return sensed.Center;
        }
        private Vector3 InwardAt(Vector3 point)
        {
            if (Navigation == null) return Vector3.zero;
            var cell = Navigation.CellAt(point); var inward = Vector3.zero;
            if (Navigation.Walkable(cell))
            {
                var edge = sensed.Navigation.Cell(cell).EdgeDistance;
                foreach (var step in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                {
                    var next = Navigation.Index(cell % Navigation.Width + step.x, cell / Navigation.Width + step.y);
                    if (Navigation.Walkable(next)) inward += new Vector3(step.x, 0, step.y) *
                        (sensed.Navigation.Cell(next).EdgeDistance - edge);
                }
            }
            if (inward.sqrMagnitude < 0.0001f) { inward = sensed.Center - point; inward.y = 0; }
            if (inward.sqrMagnitude < 0.0001f) { inward = actPosition - point; inward.y = 0; }
            return inward.normalized;
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
