using System;
using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.AI
{
    public enum BotState { Countdown, Roam, Chase, Charge, Evade, Recover, Eliminated }

    public readonly struct BotFighter
    {
        public BotFighter(ushort id, Vector3 position, Vector3 velocity, bool alive, float mass = 1f, int wobble = 0,
            float facingYaw = 0, bool isDashing = false)
        { Id = id; Position = position; Velocity = velocity; Alive = alive; Mass = mass; Wobble = wobble; FacingYaw = facingYaw; IsDashing = isDashing; }
        public ushort Id { get; }
        public Vector3 Position { get; }
        public Vector3 Velocity { get; }
        public bool Alive { get; }
        public float Mass { get; }
        public int Wobble { get; }
        public float FacingYaw { get; }
        public bool IsDashing { get; }
    }

    /// <summary>FSM chạy trên host; D1 chọn ngẫu nhiên, bậc khác theo profile và utility.
    /// App cấp cảm nhận qua Core; đầu ra duy nhất là InputCommand.</summary>
    public sealed class BotBrain
    {
        // Hằng hành vi A.2/A.3; thông số bậc lấy từ BotProfile.
        public const float RetargetSeconds = 4f, StuckSeconds = 2f, MinimumProgress = 0.5f;
        private const float ChargeDistance = 3.5f, RecoverSeconds = 0.4f;
        // Sai số tới waypoint bằng 1/10 ô 1 m, tách khỏi ngưỡng tiến bộ anti-stuck 0,5 m.
        private const float WaypointArrivalMeters = 0.1f;
        private const float MinimumSideSpeed = 0.1f; // m/s: bỏ nhiễu khi chọn hướng né, độc lập khoảng tới waypoint.
        private sealed class Sense
        {
            public float Time;
            public Vector3 Position, Center;
            public float Radius, Yaw;
            public float SecondsRemaining;
            public float EndgameWindow;
            public BotFighter[] Fighters;
            public IBotNavigationSnapshot Navigation;
        }
        private readonly BotProfile profile;
        private readonly ushort id;
        private readonly System.Random random;
        private readonly Queue<Sense> senses = new();
        private Sense sensed;
        private float time, nextTarget, recoverUntil, stuckAt, goalDistance;
        private double nextDecision;
        private Vector3 goal;
        private ushort? target;
        private ushort? progressTarget;
        private Vector2 move;
        private float yaw;
        private bool dash, jump;
        private IBotNavigationSnapshot previousNavigation, observedNavigation;
        private uint sequence;
        private bool chargeObserved;
        private float chargeAt;
        private bool avoidStuckGoal;
        private Vector3 actPosition;
        private Vector3 chaseAim;
        private Vector3 actVelocity;
        private bool urgentEscape;
        private float playingClock, warningAge;
        private readonly GridPathfinder pathfinder = new();
        private readonly List<int> roamCandidates = new(DangerMap.MaximumSide * DangerMap.MaximumSide);
        private readonly float[] roamDanger = new float[DangerMap.MaximumSide * DangerMap.MaximumSide];
        private int waypoint;
        public DangerMap Navigation { get; private set; }
        public BotPath PlannedPath { get; private set; }
        public int Replans { get; private set; }
        public int Decisions { get; private set; }
        public float DecisionRateHz => profile.DecisionRateHz;

        private readonly ClassProfile ownClass;
        private readonly BalanceProfile balance;
        public BotBrain(BotProfile profile, ushort id, uint seed, ClassProfile ownClass = null, BalanceProfile balance = null)
        {
            this.profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
            this.id = id;
            this.ownClass = ownClass;
            this.balance = balance;
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
            IBotNavigationSnapshot navigation = null, float secondsRemaining = float.PositiveInfinity,
            float endgameWindowSeconds = 0, float playingSeconds = float.NaN)
        {
            if (!host) return default;
            jump = false;
            actPosition = position;
            playingClock = playingSeconds;
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
            actVelocity = Vector3.zero;
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = fighters[i];
                if (copy[i].Id == id) actVelocity = copy[i].Velocity;
            }
            actVelocity.y = 0; // Phản hồi vận tốc bản thân, như actPosition; đối thủ/hazard vẫn trễ.
            senses.Enqueue(new Sense { Time = time, Position = position, Yaw = sequence == 0 ? facingYaw : yaw,
                Center = center, Radius = radius, Fighters = copy, Navigation = navigation,
                SecondsRemaining = secondsRemaining, EndgameWindow = endgameWindowSeconds });
            while (senses.Count > 0 && senses.Peek().Time <= time - profile.ReactionDelaySeconds + dt * 0.01f)
                sensed = senses.Dequeue();
            dash = false; jump = false;
            if (State == BotState.Charge)
            {
                chargeObserved |= isDashing;
                if (!isDashing && (chargeObserved || time > chargeAt + dt))
                { recoverUntil = time + RecoverSeconds; move = Vector2.zero; Change(BotState.Recover); }
            }
            if (sensed != null && time >= nextDecision)
            {
                var period = 1d / profile.DecisionRateHz;
                // Giữ phần dư của lịch giữa các bước 60 Hz, tránh 8 Hz bị làm tròn thành 7,5 Hz.
                // Cảm nhận đầu trận có độ trễ; không chạy bù các quyết định chưa có dữ liệu.
                if (time - nextDecision > period) nextDecision = time;
                nextDecision += period;
                Decisions++;
                Decide(dashReady);
            }
            if (Navigation != null && (State == BotState.Roam || State == BotState.Chase || State == BotState.Evade))
                FollowPath();
            return Command(move, dash);
        }

        private void Decide(bool dashReady)
        {
            warningAge = sensed.Navigation is ITimedBotNavigationSnapshot timed && !float.IsNaN(playingClock) ?
                Mathf.Max(0, playingClock - timed.CapturedAtSeconds) : 0;
            if (sensed.Navigation != null)
                Navigation = new DangerMap(sensed.Navigation, profile.EdgeMarginMeters, profile.Awareness,
                    profile.Tier >= 3 ? profile.IceEdgeMarginMultiplier : 1f, warningAge);
            if (!ReferenceEquals(observedNavigation, sensed.Navigation))
            { previousNavigation = observedNavigation; observedNavigation = sensed.Navigation; }
            jump = ShouldJumpObstacle();
            BotFighter? self = null;
            foreach (var fighter in sensed.Fighters) if (fighter.Id == id) self = fighter;
            var distanceToEdge = sensed.Radius - FlatDistance(sensed.Position, sensed.Center);
            var currentCell = Navigation?.CellAt(actPosition) ?? -1;
            var danger = Navigation == null ? Mathf.Clamp01(1 - distanceToEdge / profile.EdgeMarginMeters) :
                Navigation.Walkable(currentCell) ? Navigation[currentCell] : 1f;
            if (Navigation != null && Navigation.Walkable(currentCell))
            {
                var cell = sensed.Navigation.Cell(currentCell);
                // Khoảng cách tâm ô không phải khoảng cách của tâm quả đang ở giữa hai ô.
                var edge = Mathf.Max(0, cell.EdgeDistance - FlatDistance(actPosition, Navigation.Position(currentCell)));
                var margin = profile.EdgeMarginMeters * (cell.Ice && profile.Tier >= 3 ? profile.IceEdgeMarginMultiplier : 1);
                danger = Mathf.Max(danger, Mathf.Clamp01(1 - edge / margin));
                danger = Mathf.Max(danger, KnownHazardDanger(actPosition, ownClass != null ? ownClass.Radius : 0));
            }
            if (profile.Tier >= 4 && ownClass != null && balance != null && sensed.Navigation != null)
            {
                // Dừng trước làn đang cảnh báo, thay vì đợi footprint đã lọt vào làn mới Evade.
                var acceleration = balance.MaxAcceleration(ownClass, OnIce(currentCell) ? balance.IceFriction : balance.FloorFriction);
                var distance = actVelocity.magnitude * (profile.ReactionDelaySeconds + 1f / profile.DecisionRateHz) +
                    actVelocity.sqrMagnitude / (2 * acceleration);
                var stop = actPosition + actVelocity.normalized * distance;
                foreach (var warning in sensed.Navigation.Warnings)
                    if (warning.Awareness == BotAwareness.PusherLanes &&
                        DangerMap.ContainsExpanded(warning, stop, ownClass.Radius))
                        danger = Mathf.Max(danger, 0.5f);
            }
            if (profile.IceHandling == BotIceHandling.PredictSlide && OnIce(currentCell))
            {
                var stop = actPosition + actVelocity.normalized * StoppingDistance(actVelocity.magnitude);
                var stopCell = Navigation.CellAt(stop);
                danger = Mathf.Max(danger, Navigation.Walkable(stopCell) ? Navigation[stopCell] : 1f);
            }
            urgentEscape = false;
            var predictedEscape = Vector3.zero;
            if (Navigation != null && ownClass != null && balance != null &&
                (profile.Awareness & BotAwareness.TimeToImpact) != 0 && KnownHazardDanger(actPosition, ownClass.Radius) > 0)
            {
                predictedEscape = SafeGoal(false, true);
                var escapeDirection = predictedEscape - actPosition; escapeDirection.y = 0;
                var acceleration = balance.MaxAcceleration(ownClass, OnIce(currentCell) ? balance.IceFriction : balance.FloorFriction);
                var opposingSpeed = Mathf.Max(0, -Vector3.Dot(actVelocity, escapeDirection.normalized));
                var escapeSeconds = profile.ReactionDelaySeconds + 1f / profile.DecisionRateHz +
                    opposingSpeed / acceleration + Mathf.Sqrt(2 * escapeDirection.magnitude / acceleration);
                foreach (var warning in sensed.Navigation.Warnings)
                    if ((warning.Awareness & profile.Awareness) != 0 &&
                        DangerMap.ContainsExpanded(warning, actPosition, ownClass.Radius) &&
                        warning.SecondsUntilImpact - warningAge <= escapeSeconds)
                    { urgentEscape = true; danger = Mathf.Max(0.5f, danger); break; }
            }
            if (jump)
            {
                // Leave the obstacle's track while airborne instead of landing on the same track.
                goal = actPosition + InwardAt(actPosition) * (ownClass.MaxSpeed * balance.JumpSpeed * 2 / balance.Gravity);
                Change(BotState.Evade); Steer(goal); return;
            }
            if (danger >= 0.5f) { goal = urgentEscape ? predictedEscape : SafeGoal(false); Change(BotState.Evade); }
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

            var remainingGoalDistance = DistanceToMappedGoal(sensed.Position);
            var progress = goalDistance - remainingGoalDistance;
            if (progress >= MinimumProgress) { stuckAt = time; goalDistance = remainingGoalDistance; }
            else if (time - stuckAt >= StuckSeconds && remainingGoalDistance > MinimumProgress)
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
                if (!avoidStuckGoal) goal = RoamGoal();
                Change(BotState.Roam); Steer(goal); return;
            }
            // Cửa sổ cuối trận trùng cảnh báo HUD do App cấp; không biết trước kết quả/ranking.
            if (profile.ClassTactics == BotClassTactics.Predictive && sensed.EndgameWindow > 0 &&
                sensed.SecondsRemaining <= sensed.EndgameWindow)
            {
                target = null; goal = SafeGoal(false); Change(BotState.Roam); Steer(goal); return;
            }
            if (State != BotState.Chase || progressTarget != target)
            {
                progressTarget = target;
                stuckAt = time;
                goalDistance = FlatDistance(sensed.Position, victim.Value.Position);
            }
            Change(BotState.Chase);
            var targetPoint = victim.Value.Position;
            // D5 reacts to a dash that it has already observed; no future input is read.
            if (profile.ClassTactics == BotClassTactics.Predictive && victim.Value.IsDashing &&
                Vector3.Dot(victim.Value.Velocity.normalized, (actPosition - targetPoint).normalized) >
                Mathf.Cos(profile.ChargeAlignmentDegrees * Mathf.Deg2Rad))
            {
                Dodge(victim.Value, dashReady);
                return;
            }
            var inward = InwardAt(targetPoint);
            goal = Navigation == null ? targetPoint : targetPoint + inward * (ChargeDistance * 0.5f);
            var own = Navigation == null ? sensed.Position : actPosition;
            var heavierHeadOn = profile.ClassTactics != BotClassTactics.None && self.HasValue &&
                victim.Value.Mass > self.Value.Mass && Vector3.Dot(
                    Quaternion.Euler(0, victim.Value.FacingYaw, 0) * Vector3.forward,
                    (own - targetPoint).normalized) > 0.5f;
            if (heavierHeadOn || (profile.ClassTactics >= BotClassTactics.PositionByClass &&
                ownClass != null && ownClass.Category == FruitWeightClass.Light))
            {
                var flank = Vector3.Cross(Vector3.up, inward) * (id % 2 == 0 ? 1 : -1);
                goal = targetPoint + flank * (ChargeDistance * 0.75f) + inward * (ChargeDistance * 0.5f);
            }
            if (profile.ClassTactics >= BotClassTactics.PositionByClass && ownClass != null &&
                ownClass.Category == FruitWeightClass.Heavy && FlatDistance(targetPoint, sensed.Center) > ChargeDistance)
            {
                goal = RoamGoal(); Change(BotState.Roam); Steer(goal); return;
            }
            if (profile.ClassTactics == BotClassTactics.Predictive && self.HasValue &&
                victim.Value.Mass > self.Value.Mass && TryBaitGoal(targetPoint, out var bait))
            {
                goal = bait; Steer(goal); return;
            }
            chaseAim = targetPoint + victim.Value.Velocity * profile.TargetLeadSeconds;
            if (profile.ClassTactics == BotClassTactics.Predictive &&
                Vector3.Dot(chaseAim - own, targetPoint - own) <= 0)
            {
                Dodge(victim.Value, dashReady);
                return;
            }
            var direction = chaseAim - own;
            var aim = Facing(direction);
            var behind = Navigation == null || Vector3.Dot(own - targetPoint, inward) >= 0;
            if (dashReady && !heavierHeadOn && behind && FlatDistance(targetPoint, own) <= ChargeDistance &&
                Mathf.Abs(Mathf.DeltaAngle(sensed.Yaw, aim)) <= profile.ChargeAlignmentDegrees && SafeDash(aim))
            {
                // Box–Muller: σ đúng A.5, RNG riêng theo seed trận và ID bot.
                var noise = Mathf.Sqrt(-2 * Mathf.Log(Mathf.Max(float.Epsilon, (float)random.NextDouble()))) *
                    Mathf.Cos(2 * Mathf.PI * (float)random.NextDouble()) * profile.AimNoiseDegrees;
                BeginDash(aim + noise);
            }
            else Steer(goal);
        }

        private void Dodge(BotFighter opponent, bool dashReady)
        {
            var side = Vector3.Cross(Vector3.up, opponent.Velocity); side.y = 0; side.Normalize();
            var left = actPosition + side * (ChargeDistance * 0.5f);
            var right = actPosition - side * (ChargeDistance * 0.5f);
            var lateral = Vector3.Dot(side, actVelocity);
            goal = Mathf.Abs(lateral) > MinimumSideSpeed ? (lateral > 0 ? left : right) :
                FlatDistance(left, sensed.Center) <= FlatDistance(right, sensed.Center) ? left : right;
            var other = goal == left ? right : left;
            if (dashReady && !SafeDash(Facing(goal - actPosition)) && SafeDash(Facing(other - actPosition))) goal = other;
            Steer(goal);
            var dodgeYaw = Facing(goal - actPosition);
            if (dashReady && SafeDash(dodgeYaw)) BeginDash(dodgeYaw);
        }

        private void BeginDash(float angle)
        {
            yaw = angle;
            var forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            move = new Vector2(forward.x, forward.z) * profile.SpeedFactor;
            dash = true; chargeObserved = false; chargeAt = time; Change(BotState.Charge);
        }

        // D5 predicts its own impulse and braking on the floor it has already sensed.
        // It may punish an opponent near the edge, but must not dash off after a miss.
        private bool SafeDash(float aim)
        {
            if (profile.ClassTactics != BotClassTactics.Predictive || Navigation == null || ownClass == null || balance == null)
                return true;
            var direction = Quaternion.Euler(0, aim, 0) * Vector3.forward;
            var velocity = actVelocity + direction * balance.DashDeltaV(ownClass);
            var point = actPosition;
            const float step = 1f / 60;
            var minimumAcceleration = balance.MaxAcceleration(ownClass, Mathf.Min(balance.IceFriction, balance.FloorFriction));
            if (minimumAcceleration <= 0 || float.IsNaN(minimumAcceleration) || float.IsInfinity(minimumAcceleration) ||
                float.IsNaN(velocity.sqrMagnitude) || float.IsInfinity(velocity.sqrMagnitude)) return false;
            var maximumSteps = Mathf.CeilToInt((balance.DashStateSeconds +
                (velocity.magnitude + ownClass.MaxSpeed) / minimumAcceleration) / step) + 1;
            for (var n = 0; n < maximumSteps; n++)
            {
                var elapsed = n * step;
                if (elapsed >= balance.DashStateSeconds && velocity.sqrMagnitude <= 0.0001f) break;
                var cell = Navigation.CellAt(point);
                if (!Navigation.Walkable(cell) || EdgeClearance(point) < ownClass.Radius) return false;
                var acceleration = balance.MaxAcceleration(ownClass, OnIce(cell) ? balance.IceFriction : balance.FloorFriction);
                var targetVelocity = elapsed < balance.DashStateSeconds ? direction * ownClass.MaxSpeed * profile.SpeedFactor : Vector3.zero;
                velocity += Vector3.ClampMagnitude(targetVelocity - velocity, acceleration * step);
                point += velocity * step;
            }
            return Navigation.Walkable(Navigation.CellAt(point)) && EdgeClearance(point) >= ownClass.Radius;
        }

        private bool ShouldJumpObstacle()
        {
            if (profile.Tier < 4 || ownClass == null || balance == null || sensed.Navigation == null) return false;
            // Begin the jump before the projected overlap, using only observed box motion.
            var lead = balance.JumpSpeed / balance.Gravity;
            foreach (var warning in sensed.Navigation.Warnings)
            {
                if (warning.Awareness != BotAwareness.ObstacleBoxes) continue;
                var velocity = Vector2.zero;
                if (observedNavigation is ITimedBotNavigationSnapshot now && previousNavigation is ITimedBotNavigationSnapshot before &&
                    now.CapturedAtSeconds > before.CapturedAtSeconds)
                {
                    var nearest = float.PositiveInfinity; var prior = warning.Center;
                    foreach (var old in previousNavigation.Warnings)
                        if (old.Awareness == warning.Awareness && old.Shape == warning.Shape)
                        {
                            var distance = (old.Center - warning.Center).sqrMagnitude;
                            if (distance < nearest) { nearest = distance; prior = old.Center; }
                        }
                    if (!float.IsPositiveInfinity(nearest)) velocity = (warning.Center - prior) / (now.CapturedAtSeconds - before.CapturedAtSeconds);
                }
                var projected = new BotHazardWarning(warning.Awareness, warning.Shape,
                    warning.Center + velocity * (warningAge + lead), warning.Size, warning.YawDegrees, 0);
                if (DangerMap.ContainsExpanded(projected, actPosition + actVelocity * lead, ownClass.Radius)) return true;
            }
            return false;
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
                FlatDistance(actPosition, Navigation.Position(PlannedPath.Cells[waypoint])) <= WaypointArrivalMeters) waypoint++;
            // Nhìn tới cuối đoạn thẳng; chỉ phanh ở góc rẽ hoặc ô đích, không ở mỗi ô 1 m.
            while (waypoint < PlannedPath.Cells.Count - 1)
            {
                var toPoint = Navigation.Position(PlannedPath.Cells[waypoint]) - actPosition;
                var nextLeg = Navigation.Position(PlannedPath.Cells[waypoint + 1]) - Navigation.Position(PlannedPath.Cells[waypoint]);
                toPoint.y = 0;
                if (toPoint.sqrMagnitude < 0.0001f || Vector3.Dot(toPoint.normalized, nextLeg.normalized) < 0.99f) break;
                waypoint++;
            }
            var delta = Navigation.Position(PlannedPath.Cells[waypoint]) - actPosition; delta.y = 0;
            var currentCell = Navigation.CellAt(actPosition);
            var controlledIce = profile.IceHandling >= BotIceHandling.BrakeEarly && OnIce(currentCell);
            if (controlledIce && delta.sqrMagnitude > 0 && EdgeClearance(actPosition) <= ownClass.Radius)
            {
                var ahead = actPosition + delta.normalized * Mathf.Min(1, delta.magnitude);
                var toCellCenter = Navigation.Position(currentCell) - actPosition; toCellCenter.y = 0;
                if (EdgeClearance(ahead) + 0.0001f < EdgeClearance(actPosition) &&
                    toCellCenter.magnitude > WaypointArrivalMeters)
                    delta = toCellCenter; // Về giữa hành lang trước, tránh cap=0 làm đứng trên ô sắp rơi.
            }
            yaw = Facing(State == BotState.Chase ? chaseAim - actPosition : delta);
            move = delta.magnitude <= WaypointArrivalMeters ? Vector2.zero :
                new Vector2(delta.normalized.x, delta.normalized.z) * profile.SpeedFactor;
            ApplyArrivalBraking(delta.magnitude);
            if (controlledIce && move.sqrMagnitude > 0)
            {
                var direction = new Vector3(move.x, 0, move.y).normalized;
                var lateral = actVelocity - direction * Vector3.Dot(actVelocity, direction);
                var targetVelocity = new Vector3(move.x, 0, move.y) * ownClass.MaxSpeed - lateral;
                targetVelocity = Vector3.ClampMagnitude(targetVelocity, ownClass.MaxSpeed * profile.SpeedFactor);
                move = new Vector2(targetVelocity.x, targetVelocity.z) / ownClass.MaxSpeed;
            }
            if (ownClass != null && Navigation.Walkable(currentCell))
            {
                // Path/arrival dùng vận tốc thế giới; PlayerMotor cộng v_surface vào input.
                // Bù kể cả khi đã tới đích để bàn xoay không cuốn bot trở vào làn khối đẩy.
                move = Vector2.ClampMagnitude(move - sensed.Navigation.Cell(currentCell).SurfaceVelocity / ownClass.MaxSpeed,
                    profile.SpeedFactor);
            }
        }
        private bool OnIce(int cell) => Navigation != null && Navigation.Walkable(cell) &&
            sensed.Navigation.Cell(cell).Ice && ownClass != null && balance != null;
        private float DistanceToMappedGoal(Vector3 position) => Navigation != null && PlannedPath.Found ?
            FlatDistance(position, Navigation.Position(PlannedPath.Cells[PlannedPath.Cells.Count - 1])) :
            FlatDistance(position, goal);
        private float StoppingDistance(float speed) => speed * speed /
            (2 * Mathf.Max(float.Epsilon, balance.MaxAcceleration(ownClass, balance.IceFriction)));
        private void ApplyArrivalBraking(float distanceToTurn)
        {
            if (ownClass == null || balance == null) return;
            var ice = OnIce(Navigation.CellAt(actPosition));
            var acceleration = balance.MaxAcceleration(ownClass, ice ? balance.IceFriction : balance.FloorFriction);
            var escapingWarning = State == BotState.Evade &&
                (urgentEscape || KnownHazardDanger(actPosition, ownClass.Radius) >= 0.5f);
            var delay = ice && profile.IceHandling >= BotIceHandling.BrakeEarly && !escapingWarning ?
                profile.ReactionDelaySeconds + 1f / profile.DecisionRateHz : 0;
            // Giải v·delay + v²/(2a) <= d; không trừ quãng trễ theo vận tốc cũ rồi
            // đặt v=0 quá sớm khi quả vẫn còn nằm trên ô sắp rơi.
            var safeSpeed = SafeSpeed(distanceToTurn, acceleration, delay);
            if (move.sqrMagnitude > 0)
            {
                var edge = EdgeClearance(actPosition);
                var ahead = actPosition + new Vector3(move.normalized.x, 0, move.normalized.y) *
                    Mathf.Min(1f, distanceToTurn); // Một ô 1 m trên đoạn path hiện tại, không đọc sàn tương lai.
                if (EdgeClearance(ahead) < edge)
                    safeSpeed = Mathf.Min(safeSpeed, SafeSpeed(Mathf.Max(0, edge - ownClass.Radius),
                        acceleration, profile.ReactionDelaySeconds + 1f / profile.DecisionRateHz));
            }
            var factor = Mathf.Min(profile.SpeedFactor, safeSpeed / ownClass.MaxSpeed);
            if (move.sqrMagnitude > 0) move = move.normalized * factor;
        }
        private static float SafeSpeed(float distance, float acceleration, float delay) =>
            Mathf.Sqrt(acceleration * acceleration * delay * delay + 2 * acceleration * distance) - acceleration * delay;
        private float EdgeClearance(Vector3 point)
        {
            var cell = Navigation.CellAt(point);
            return Navigation.Walkable(cell) ? Mathf.Max(0, sensed.Navigation.Cell(cell).EdgeDistance -
                FlatDistance(point, Navigation.Position(cell))) : 0;
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
        private Vector3 SafeGoal(bool randomized, bool leaveKnownWarnings = false)
        {
            if (Navigation == null) return sensed.Center;
            var start = Navigation.CellAt(actPosition);
            if (!Navigation.Walkable(start)) start = NearestWalkable(actPosition);
            bool ReachableCandidate(int cell)
            {
                var point = Navigation.Position(cell);
                if (FlatDistance(actPosition, point) > 4f) return false;
                if (leaveKnownWarnings)
                    foreach (var warning in sensed.Navigation.Warnings)
                        if ((warning.Awareness & profile.Awareness) != 0 &&
                            DangerMap.ContainsExpanded(warning, actPosition, ownClass.Radius) &&
                            DangerMap.ContainsExpanded(warning, point, ownClass.Radius + WaypointArrivalMeters))
                            return false;
                return true;
            }
            bool Eligible(int cell) => StandingDanger(cell) < 0.2f && ReachableCandidate(cell);
            if (!randomized)
            {
                var escape = pathfinder.FindCheapest(Navigation, start, Eligible);
                if (!escape.Found)
                    escape = pathfinder.FindLeastDanger(Navigation, start,
                        ReachableCandidate, StandingDanger);
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
        private float StandingDanger(int cell) => Mathf.Max(Navigation[cell], KnownHazardDanger(Navigation.Position(cell),
            WaypointArrivalMeters + (ownClass != null ? ownClass.Radius : 0)));
        private Vector3 RoamGoal()
        {
            if (Navigation == null) return sensed.Center;
            var start = Navigation.CellAt(actPosition);
            if (!Navigation.Walkable(start)) start = NearestWalkable(actPosition);
            roamCandidates.Clear();
            for (var cell = 0; cell < Navigation.Count; cell++)
            {
                if (!Navigation.Walkable(cell)) continue;
                roamDanger[cell] = StandingDanger(cell); roamCandidates.Add(cell);
            }
            roamCandidates.Sort(CompareRoamGoals);
            foreach (var cell in roamCandidates)
                if (pathfinder.Find(Navigation, start, cell).Found) return Navigation.Position(cell);
            return sensed.Center;
        }
        private int CompareRoamGoals(int a, int b)
        {
            var danger = roamDanger[a].CompareTo(roamDanger[b]);
            if (danger != 0) return danger;
            var da = Navigation.Position(a) - sensed.Center; da.y = 0;
            var db = Navigation.Position(b) - sensed.Center; db.y = 0;
            var central = da.sqrMagnitude.CompareTo(db.sqrMagnitude);
            return central != 0 ? central : a.CompareTo(b);
        }
        private float KnownHazardDanger(Vector3 position, float padding)
        {
            if (sensed.Navigation == null) return 0;
            var danger = 0f;
            foreach (var warning in sensed.Navigation.Warnings)
                if ((warning.Awareness & profile.Awareness) != 0 && DangerMap.ContainsExpanded(warning, position, padding))
                    danger = Mathf.Max(danger, Mathf.Clamp01(1 - (warning.SecondsUntilImpact - warningAge) / 2f));
            return danger;
        }
        private bool TryBaitGoal(Vector3 opponent, out Vector3 bait)
        {
            bait = default;
            if (Navigation == null) return false;
            var start = Navigation.CellAt(actPosition);
            if (!Navigation.Walkable(start)) return false;
            var best = float.PositiveInfinity;
            // Chỉ đứng ở ô an toàn; quỹ đạo húc kéo dài qua bot mới đi vào vùng đã cảnh báo.
            for (var i = 0; i < Navigation.Count; i++)
            {
                if (!Navigation.Walkable(i) || Navigation[i] >= 0.2f) continue;
                var point = Navigation.Position(i);
                if (FlatDistance(actPosition, point) > 4 || FlatDistance(opponent, point) > ChargeDistance) continue;
                var beyond = point + (point - opponent).normalized * ChargeDistance;
                var hazardBeyond = false;
                foreach (var warning in sensed.Navigation.Warnings)
                    if ((warning.Awareness & profile.Awareness) != 0 && warning.SecondsUntilImpact - warningAge <= 2 &&
                        DangerMap.Contains(warning, beyond)) { hazardBeyond = true; break; }
                if (!hazardBeyond) continue;
                var path = pathfinder.Find(Navigation, start, i);
                if (!path.Found || path.Cost >= best) continue;
                bait = point; best = path.Cost;
            }
            return !float.IsPositiveInfinity(best);
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
            return new InputCommand(++sequence, new Vector2(local.x, local.z), jump, charge, yaw);
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
