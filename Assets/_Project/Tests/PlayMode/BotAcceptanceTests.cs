#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquishyBump.AI;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>Chạy đúng motor, PhysX, floor collider và hazard thật ở bước 60 Hz.
    /// Điều khiển đồng hồ để cảnh AI-1 một bot không bị luật thắng một người dừng trận sớm.
    /// Không dùng bằng chứng này thay cho session, player build hoặc LAN.</summary>
    public sealed class BotAcceptanceTests
    {
        private const string Root = "Assets/_Project";
        private const float Dt = 1f / 60;
        private static void Record(FormattableString line) => TestContext.Out.WriteLine(FormattableString.Invariant(line));
        private SimulationMode previousSimulation;

        [SetUp]
        public void SetUp()
        {
            previousSimulation = UnityEngine.Physics.simulationMode;
            UnityEngine.Physics.simulationMode = SimulationMode.Script;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Physics.simulationMode = previousSimulation;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        }

        private static T Data<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>($"{Root}/Data/{path}.asset");

        [UnityTest, Timeout(600000)]
        public IEnumerator AI5_FiveD5ChaosCore_ProfilesAllLivingBotLogic()
        {
            var samples = new List<double>();
            for (var seed = 18; seed <= 20; seed++) yield return RunGroup(5, 5, 60, true, seed, samples);
            Record($"AI5,TOTAL,seeds=18/19/20,allFiveAliveSamples={samples.Count},meanMs={samples.Average():F4}");
            Assert.That(samples.Count, Is.GreaterThanOrEqualTo(600), "Collect at least 10 seconds of five-live-bot samples across three complete runs; do not freeze deaths or respawn in a run.");
            Assert.That(samples.Average(), Is.LessThan(1d));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator AI6_D4MovingGround120Seconds_RecordsEveryObstacleEncounter() => RunGroup(2, 4, 120, false);

        [UnityTest, Timeout(600000)]
        public IEnumerator AI6_D5MovingGround120Seconds_RecordsEveryObstacleEncounter() => RunGroup(2, 5, 120, false);

        private sealed class HazardProbe : IFighterPhysics
        {
            private readonly IFighterPhysics inner;
            private readonly Action<ulong> contact;
            public HazardProbe(IFighterPhysics inner, Action<ulong> contact) { this.inner = inner; this.contact = contact; }
            public IReadOnlyCollection<ulong> FighterIds => inner.FighterIds;
            public Vector3 GetPosition(ulong id) => inner.GetPosition(id);
            public bool TryGetFighter(Collider c, out ulong id) => inner.TryGetFighter(c, out id);
            public int GetWobble(ulong id) => inner.GetWobble(id);
            public void AddImpulse(ulong id, Vector3 impulse) { contact(id); inner.AddImpulse(id, impulse); }
            public void AddWobble(ulong id, int amount) => inner.AddWobble(id, amount);
        }

        // Oracle chung: tiếp tục vận tốc hiện tại sẽ đi vào hộp hoặc làn đã cảnh báo
        // trong khoảng phản ứng + một quyết định + phanh. Tính cả né sớm, giữ mọi
        // contact ngoài dự báo là lỗi. Pose tương lai chính xác chỉ dành cho oracle.
        private static bool ApproachesObstacle(BotHazardWarning warning, MovingGround ground, Rigidbody body,
            ClassProfile own, BotProfile profile, BalanceProfile balance, HazardProfile hazards, float now)
        {
            var velocity = body.linearVelocity; velocity.y = 0;
            var horizon = profile.ReactionDelaySeconds + 1f / profile.DecisionRateHz +
                velocity.magnitude / balance.MaxAcceleration(own, balance.FloorFriction);
            var origin = ground != null ? ground.transform.position : Vector3.zero;
            var offset = new Vector3(warning.Center.x - origin.x, 0, warning.Center.y - origin.z);
            var initialAngle = ground != null ? MovingGround.RingAngle(now, hazards) : 0;
            for (var future = 0f; future <= horizon + Dt * 0.5f; future += Dt)
            {
                var angle = ground != null ? MovingGround.RingAngle(now + future, hazards) - initialAngle : 0;
                var center = origin + Quaternion.Euler(0, angle, 0) * offset;
                var box = new BotHazardWarning(warning.Awareness, warning.Shape,
                    new Vector2(center.x, center.z), warning.Size, warning.YawDegrees + angle, future);
                if (DangerMap.ContainsExpanded(box, body.position + velocity * future, own.Radius)) return true;
            }
            return false;
        }

        private static IEnumerator RunGroup(int level, int tier, int seconds, bool profile, int seed = 18, List<double> combinedSamples = null)
        {
            yield return SceneManager.LoadSceneAsync($"L{level}", LoadSceneMode.Single);
            var bounds = UnityEngine.Object.FindFirstObjectByType<TrainingRingBounds>();
            var balance = Data<BalanceProfile>("Balance/BalanceProfile");
            var hazards = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IHazard>().ToArray();
            var actors = new GameObject[5]; var motors = new PlayerMotor[5]; var bodies = new Rigidbody[5];
            var brains = new BotBrain[5]; var facing = new float[5]; var alive = Enumerable.Repeat(true, 5).ToArray();
            var resolver = new GameObject("Bot group resolver").AddComponent<CollisionResolver>();
            resolver.Configure(balance); var physics = new FighterPhysicsAdapter(resolver);
            var names = new[] { "Bear", "Cat", "Frog", "Pig", "Chick" };
            for (var i = 0; i < 5; i++)
            {
                actors[i] = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/{names[i]}.prefab"));
                motors[i] = actors[i].GetComponent<PlayerMotor>(); bodies[i] = actors[i].GetComponent<Rigidbody>();
                bodies[i].position = bounds.SpawnPoints[i % bounds.SpawnPoints.Count].position + Vector3.up * (motors[i].FighterClass.Radius + 0.05f);
                resolver.Register((ulong)(1000 + i), motors[i]); physics.Add((ulong)(1000 + i), motors[i]);
                brains[i] = new BotBrain(Data<BotProfile>($"Bots/D{tier}"), (ushort)(1000 + i), (uint)seed, motors[i].FighterClass, balance);
            }
            var groupProfile = Data<BotProfile>($"Bots/D{tier}");
            var hazardData = Data<HazardProfile>("Hazards/HazardProfile");
            var warnings = new List<BotHazardWarning>();
            var episodes = new Dictionary<(int Slot, int Provider, int Warning), bool>();
            var resolved = new int[hazards.Length]; var avoided = new int[hazards.Length];
            var lastUntrackedHit = new Dictionary<(int, ulong), int>(); var tickNow = 0;
            for (var h = 0; h < hazards.Length; h++)
            {
                var index = h;
                hazards[h].Bind(null, new HazardProbe(physics, id =>
                {
                    if (profile || id < 1000 || id > 1004) return;
                    var slot = (int)id - 1000;
                    Record($"AI6HIT,D{tier},{hazards[index].GetType().Name},id={id},t={tickNow},p={bodies[slot].position:F3},v={bodies[slot].linearVelocity:F3},state={brains[slot].State}");
                    var active = episodes.Keys.Where(k => k.Slot == slot && k.Provider == index).ToArray();
                    foreach (var key in active) episodes[key] = true;
                    if (active.Length == 0 && (!lastUntrackedHit.TryGetValue((index, id), out var last) || tickNow - last >= 30))
                    { resolved[index]++; lastUntrackedHit[(index, id)] = tickNow; }
                }));
            }
            bounds.RingOut += (id, _, _) =>
            {
                if (id < 1000 || id > 1004) return;
                var i = (int)id - 1000; alive[i] = false; actors[i].SetActive(false); physics.Remove(id);
            };
            var world = new BotNavigationWorld(bounds.transform.position, balance, hazards);
            IBotNavigationSnapshot navigation = null; double nextCapture = 0;
            var anchors = bodies.Select(b => b.position).ToArray(); var stationaryTicks = new int[5];
            var failures = new List<string>(); var samples = new List<double>();
            var oldProfiler = UnityEngine.Profiling.Profiler.enabled;
            var oldBinary = UnityEngine.Profiling.Profiler.enableBinaryLog;
            var oldLog = UnityEngine.Profiling.Profiler.logFile;
            var oldVsync = QualitySettings.vSyncCount; var oldCap = Application.targetFrameRate;
            if (profile)
            {
                QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
                UnityEngine.Profiling.Profiler.logFile = System.IO.Path.GetFullPath($"TestResults/SB47-AI5-seed{seed}.raw");
                UnityEngine.Profiling.Profiler.enableBinaryLog = true;
                UnityEngine.Profiling.Profiler.enabled = true;
            }
            UnityEngine.Physics.SyncTransforms();
            // Production countdown settles the bodies before GO; keep hazards at t=0.
            for (var settle = 0; settle < 180; settle++)
            {
                foreach (var motor in motors) motor.Step(default, Dt);
                resolver.Step(Dt); UnityEngine.Physics.Simulate(Dt);
            }
            try
            {
                for (var tick = 1; tick <= seconds * 60; tick++)
                {
                    tickNow = tick;
                    var allLiving = alive.All(a => a);
                    var start = System.Diagnostics.Stopwatch.GetTimestamp();
                    UnityEngine.Profiling.Profiler.BeginSample("SquishyBump.FiveBotsIncludingNavigation");
                    var fighters = Enumerable.Range(0, 5).Select(i => new BotFighter((ushort)(1000 + i), bodies[i].position,
                        bodies[i].linearVelocity, alive[i], motors[i].FighterClass.Mass, resolver.GetWobble((ulong)(1000 + i)), facing[i], motors[i].IsDashing)).ToArray();
                    if (navigation == null || tick >= nextCapture)
                    { navigation = world.Capture(bounds.OuterRadius, tick - 1, Dt); if (nextCapture == 0) nextCapture = tick; nextCapture += 60d / groupProfile.DecisionRateHz; }
                    var commands = new InputCommand[5];
                    for (var i = 0; i < 5; i++)
                        commands[i] = brains[i].Step(true, true, alive[i], motors[i].IsDashing, motors[i].DashReady,
                            bodies[i].position, facing[i], bounds.transform.position, bounds.OuterRadius, fighters, Dt, navigation,
                            balance.TimeLimitSeconds - tick * Dt, 10, tick * Dt);
                    UnityEngine.Profiling.Profiler.EndSample();
                    var elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000d / System.Diagnostics.Stopwatch.Frequency;
                    if (profile && allLiving && tick > 60) samples.Add(elapsed);
                    if (!profile)
                    {
                        for (var h = 0; h < hazards.Length; h++)
                        {
                            if (!(hazards[h] is IBotHazardHints hints)) continue;
                            warnings.Clear(); hints.CollectBotWarnings(warnings, tick - 1, Dt);
                            warnings.RemoveAll(w => (w.Awareness & (BotAwareness.ObstacleBoxes | BotAwareness.PusherLanes)) == 0);
                            foreach (var old in episodes.Keys.Where(k => k.Provider == h && (!alive[k.Slot] || !Enumerable.Range(0, warnings.Count).Any(w => (hazards[h] is PusherBlocks ? (warnings[w].Center.y < bounds.transform.position.z ? 0 : 1) : w) == k.Warning))).ToArray())
                            { resolved[h]++; if (alive[old.Slot] && !episodes[old]) avoided[h]++; episodes.Remove(old); }
                            for (var i = 0; i < 5; i++)
                            for (var w = 0; w < warnings.Count; w++)
                            {
                                if ((warnings[w].Awareness & (BotAwareness.ObstacleBoxes | BotAwareness.PusherLanes)) == 0) continue;
                                var warningId = hazards[h] is PusherBlocks ? (warnings[w].Center.y < bounds.transform.position.z ? 0 : 1) : w;
                                var key = (i, h, warningId);
                                var contains = alive[i] && ApproachesObstacle(warnings[w], hazards[h] as MovingGround,
                                    bodies[i], motors[i].FighterClass, groupProfile, balance, hazardData, (tick - 1) * Dt);
                                if (contains && !episodes.ContainsKey(key))
                                {
                                    episodes[key] = false;
                                    Record($"AI6BEGIN,D{tier},{hazards[h].GetType().Name},id={1000+i},warning={warningId},t={tick},p={bodies[i].position:F3},v={bodies[i].linearVelocity:F3}");
                                }
                                else if (!contains && episodes.Remove(key, out var hit))
                                {
                                    resolved[h]++; if (!hit) avoided[h]++;
                                    Record($"AI6END,D{tier},{hazards[h].GetType().Name},id={1000+i},warning={warningId},t={tick},hit={hit}");
                                }
                            }
                        }
                    }
                    for (var i = 0; i < 5; i++)
                    {
                        if (!alive[i]) continue;
                        facing[i] = commands[i].YawDegrees; motors[i].Step(commands[i], Dt);
                        var path = brains[i].PlannedPath;
                        var needsProgress = path.Found && Vector3.Distance(bodies[i].position,
                            brains[i].Navigation.Position(path.Cells[path.Cells.Count - 1]) + Vector3.up * bodies[i].position.y) > 0.6f;
                        if (!needsProgress || Vector3.Distance(anchors[i], bodies[i].position) >= 0.5f)
                        { anchors[i] = bodies[i].position; stationaryTicks[i] = 0; }
                        else if (++stationaryTicks[i] == 181) failures.Add($"D{tier},id={1000 + i},tick={tick}: no 0.5m progress for >3s");
                    }
                    resolver.Step(Dt);
                    foreach (var hazard in hazards) hazard.StepOnHost(tick, (uint)tick, Dt);
                    UnityEngine.Physics.Simulate(Dt);
                    for (var i = 0; i < 5; i++) if (alive[i]) bounds.SampleOnHost((ulong)(1000 + i), (uint)tick, bodies[i].position);
                    if (tick % 60 == 0) yield return null;
                }
            }
            finally
            {
                if (profile)
                {
                    UnityEngine.Profiling.Profiler.enabled = oldProfiler;
                    UnityEngine.Profiling.Profiler.enableBinaryLog = oldBinary;
                    UnityEngine.Profiling.Profiler.logFile = oldLog;
                    QualitySettings.vSyncCount = oldVsync; Application.targetFrameRate = oldCap;
                }
            }
            if (profile)
            {
                samples.Sort();
                Record($"AI5,seed={seed},EditorNull,{SystemInfo.processorType},cores={SystemInfo.processorCount},allFiveAliveSamples={samples.Count},meanMs={(samples.Count == 0 ? 0 : samples.Average()):F4},p95Ms={(samples.Count == 0 ? 0 : samples[(int)(samples.Count * 0.95)]):F4},decisions={string.Join("/", brains.Select(b => b.Decisions))}");
                Assert.That(samples.Count, Is.GreaterThan(0), "Each complete run must contain five-live-bot samples after warmup.");
                combinedSamples?.AddRange(samples);
                Assert.That(samples.Average(), Is.LessThan(1d), "All five brains, perception and shared navigation: mean CPU ms per 60Hz simulation frame.");
            }
            else
            {
                for (var h = 0; h < hazards.Length; h++)
                {
                    if (!(hazards[h] is MovingGround) && !(hazards[h] is PusherBlocks)) continue;
                    Record($"AI6,D{tier},{hazards[h].GetType().Name},avoided={avoided[h]},resolved={resolved[h]},unresolved={episodes.Keys.Count(k => k.Provider == h)},stuckRecoveries={string.Join("/", brains.Select(b => b.StuckRecoveries))},alive={alive.Count(a => a)}");
                    if (resolved[h] == 0 || avoided[h] < resolved[h] * 0.7f) failures.Add($"{hazards[h].GetType().Name}: avoided {avoided[h]}/{resolved[h]}, requires >=70% with nonzero observations");
                }
                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator AI3_D5AgainstD1_TwentyPredeclaredDuels() => RunDuels(1, 5, 16, 20);

        [UnityTest, Timeout(600000)]
        public IEnumerator AI4_D2AgainstD1_TwentyPredeclaredDuels() => RunDuels(1, 2, 11, 15);

        [UnityTest, Timeout(600000)]
        public IEnumerator AI4_D3AgainstD2_TwentyPredeclaredDuels() => RunDuels(2, 3, 11, 15);

        [UnityTest, Timeout(600000)]
        public IEnumerator AI4_D4AgainstD3_TwentyPredeclaredDuels() => RunDuels(3, 4, 11, 15);

        [UnityTest, Timeout(600000)]
        public IEnumerator AI4_D5AgainstD4_TwentyPredeclaredDuels() => RunDuels(4, 5, 11, 15);

        private static IEnumerator RunDuels(int lowerTier, int higherTier, int minimumWins, int maximumWins)
        {
            var wins = 0; var losses = 0; var draws = 0;
            var balance = Data<BalanceProfile>("Balance/BalanceProfile");
            var setup = AssetDatabase.LoadAssetAtPath<MatchSetup>($"{Root}/Data/MatchSetup.asset");
            for (var run = 0; run < 20; run++)
            {
                yield return SceneManager.LoadSceneAsync("L1", LoadSceneMode.Single);
                var bounds = UnityEngine.Object.FindFirstObjectByType<TrainingRingBounds>();
                var hazards = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .OfType<IHazard>().ToArray();
                var actors = new GameObject[2]; var motors = new PlayerMotor[2]; var bodies = new Rigidbody[2];
                var brains = new BotBrain[2]; var facing = new float[2];
                var higherSlot = run % 2; // Đổi cả phía spawn và fruit mỗi lượt, không đổi seed sau kết quả.
                var resolver = new GameObject("Duel collision resolver").AddComponent<CollisionResolver>();
                resolver.Configure(balance);
                var physics = new FighterPhysicsAdapter(resolver);
                for (var slot = 0; slot < 2; slot++)
                {
                    var fruit = slot == 0 ? "Cat" : "Penguin"; // Cùng hạng Vừa, cùng dữ liệu vật lý.
                    actors[slot] = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"{Root}/Prefabs/Fighters/{fruit}.prefab"));
                    motors[slot] = actors[slot].GetComponent<PlayerMotor>();
                    bodies[slot] = actors[slot].GetComponent<Rigidbody>();
                    bodies[slot].position = bounds.SpawnPoints[slot * 3 % bounds.SpawnPoints.Count].position +
                        Vector3.up * (motors[slot].FighterClass.Radius + 0.05f);
                    var id = (ushort)(1000 + slot);
                    resolver.Register(id, motors[slot]); physics.Add(id, motors[slot]);
                    brains[slot] = new BotBrain(Data<BotProfile>($"Bots/D{(slot == higherSlot ? higherTier : lowerTier)}"),
                        id, (uint)(18 + run), motors[slot].FighterClass, balance);
                }
                foreach (var hazard in hazards) hazard.Bind(null, physics);
                using var director = new MatchDirector(() => true, new ulong[] { 1000, 1001 }, 1,
                    (uint)(18 + run), 60, 3, 0.5f, Mathf.RoundToInt(balance.TimeLimitSeconds), bounds);
                resolver.WobbleChanged += director.SetWobble;
                UnityEngine.Physics.SyncTransforms();
                director.FinishIntro(0);
                uint hostTick = 0;
                // Countdown như MatchHost: motor vẫn bám sàn, chưa có hazard/AI gameplay.
                while (director.Phase == MatchPhase.Countdown)
                {
                    for (var slot = 0; slot < 2; slot++) motors[slot].Step(default, Dt);
                    resolver.Step(Dt); UnityEngine.Physics.Simulate(Dt);
                    director.CompleteTick(++hostTick);
                }
                var world = new BotNavigationWorld(bounds.transform.position, balance, hazards);
                IBotNavigationSnapshot navigation = null;
                double nextCapture = 0;
                var fastestRate = Mathf.Max(brains[0].DecisionRateHz, brains[1].DecisionRateHz);
                var maximumTicks = Mathf.CeilToInt(balance.TimeLimitSeconds / Dt) + 60;
                for (var tick = 1; tick <= maximumTicks && director.Phase == MatchPhase.Playing; tick++)
                {
                    hostTick++;
                    if (navigation == null || tick >= nextCapture)
                    {
                        navigation = world.Capture(bounds.OuterRadius, tick - 1, Dt);
                        nextCapture = nextCapture == 0 ? tick : nextCapture;
                        nextCapture += 60d / fastestRate;
                    }
                    var sensed = new BotFighter[2];
                    for (var slot = 0; slot < 2; slot++)
                        sensed[slot] = new BotFighter((ushort)(1000 + slot), bodies[slot].position,
                            bodies[slot].linearVelocity, director.AliveFighters.Contains((ulong)(1000 + slot)),
                            motors[slot].FighterClass.Mass, resolver.GetWobble((ulong)(1000 + slot)), facing[slot], motors[slot].IsDashing);
                    for (var slot = 0; slot < 2; slot++)
                    {
                        if (!actors[slot].activeSelf) continue;
                        var command = brains[slot].Step(true, true, true, motors[slot].IsDashing, motors[slot].DashReady,
                            bodies[slot].position, facing[slot], bounds.transform.position, bounds.OuterRadius,
                            sensed, Dt, navigation, balance.TimeLimitSeconds - director.ElapsedTicks * Dt,
                            setup.FinalWarningSeconds, director.ElapsedTicks * Dt);
                        if (higherTier == 5 && lowerTier == 4 && run == 2 && command.Dash)
                            Record($"DUELDASH,D{(slot == higherSlot ? higherTier : lowerTier)},t={tick},p={bodies[slot].position:F3},v={bodies[slot].linearVelocity:F3},yaw={command.YawDegrees:F2},other={bodies[1-slot].position:F3},otherV={bodies[1-slot].linearVelocity:F3}");
                        facing[slot] = command.YawDegrees; motors[slot].Step(command, Dt);
                    }
                    resolver.Step(Dt);
                    foreach (var hazard in hazards) hazard.StepOnHost(director.ElapsedTicks + 1, hostTick, Dt);
                    for (var slot = 0; slot < 2; slot++)
                        if (actors[slot].activeSelf) director.SampleArena((ulong)(1000 + slot), hostTick, bodies[slot].position);
                    director.CompleteTick(hostTick);
                    for (var slot = 0; slot < 2; slot++)
                        if (actors[slot].activeSelf && !director.AliveFighters.Contains((ulong)(1000 + slot)))
                        { actors[slot].SetActive(false); physics.Remove((ulong)(1000 + slot)); }
                    UnityEngine.Physics.Simulate(Dt);
                    if (tick % 600 == 0) yield return null;
                }
                Assert.That(director.Phase, Is.EqualTo(MatchPhase.Result), $"seed {18 + run}: chưa chốt kết quả.");
                var top = director.Placements.Where(p => p.Rank == 1).ToArray();
                if (top.Length != 1) draws++;
                else if (top[0].FighterId == (ulong)(1000 + higherSlot)) wins++;
                else losses++;
                Record($"DUEL,D{higherTier}-D{lowerTier},seed={18 + run},higherSlot={higherSlot},elapsedTicks={director.ElapsedTicks},placements={string.Join(";", director.Placements.Select(p => $"{p.FighterId}:{p.Rank}"))}");
            }
            Record($"DUEL,TOTAL,D{higherTier}-D{lowerTier},higherWins={wins},lowerWins={losses},draws={draws},matches=20");
            Assert.That(wins + losses + draws, Is.EqualTo(20));
            Assert.That(wins, Is.InRange(minimumWins, maximumWins),
                $"D{higherTier}–D{lowerTier}: {wins}/20 thắng; {draws} hòa vẫn tính trong mẫu 20 trận.");
        }

        [UnityTest]
        public IEnumerator AI2_RealFrost60Seconds_FiveSeedsPerTier_RecordsEveryWarnedTileEncounter()
        {
            var failures = new List<string>();
            var balance = Data<BalanceProfile>("Balance/BalanceProfile");
            for (var tier = 3; tier <= 5; tier++)
            {
                var total = 0; var escaped = 0;
                for (var run = 0; run < 5; run++)
                {
                    yield return SceneManager.LoadSceneAsync("L4", LoadSceneMode.Single);
                    var bounds = UnityEngine.Object.FindFirstObjectByType<TrainingRingBounds>();
                    var ice = UnityEngine.Object.FindFirstObjectByType<IceTiles>();
                    ice.SetSeed((uint)(18 + run));
                    var fruit = new[] { "Bear", "Cat", "Frog" }[run % 3];
                    var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"{Root}/Prefabs/Fighters/{fruit}.prefab"));
                    var motor = actor.GetComponent<PlayerMotor>();
                    var body = actor.GetComponent<Rigidbody>();
                    var own = motor.FighterClass;
                    body.position = bounds.SpawnPoints[run % bounds.SpawnPoints.Count].position + Vector3.up * (own.Radius + 0.05f);
                    var resolver = new GameObject("AI2 resolver").AddComponent<CollisionResolver>();
                    resolver.Configure(balance); resolver.Register(1000, motor);
                    var physics = new FighterPhysicsAdapter(resolver); physics.Add(1000, motor);
                    ice.Bind(null, physics);
                    var profile = Data<BotProfile>($"Bots/D{tier}");
                    var brain = new BotBrain(profile, 1000, (uint)(18 + run), own, balance);
                    var world = new BotNavigationWorld(bounds.transform.position, balance, new IHazard[] { ice });
                    var seen = new HashSet<(int Tile, int Wave)>();
                    var pending = new HashSet<int>();
                    var resolved = 0; var left = 0; var fall = 0;
                    bool Contains(int tile) => DangerMap.Contains(new BotHazardWarning(BotAwareness.IceTiles,
                        BotHazardShape.Hex, new Vector2(ice.TileCenter(tile).x, ice.TileCenter(tile).z),
                        new Vector2(2, 0), ice.transform.eulerAngles.y, 0), body.position);
                    bounds.RingOut += (_, tick, _) => { if (fall == 0) fall = (int)tick; };
                    IBotNavigationSnapshot navigation = null;
                    double nextCapture = 0;
                    UnityEngine.Physics.SyncTransforms();
                    for (var tick = 1; tick <= 3600; tick++)
                    {
                        if (navigation == null || tick >= nextCapture)
                        {
                            navigation = world.Capture(bounds.OuterRadius, tick - 1, Dt);
                            nextCapture = nextCapture == 0 ? tick : nextCapture;
                            nextCapture += 60d / profile.DecisionRateHz;
                        }
                        for (var i = 0; i < ice.TileCount; i++)
                            if (fall == 0 && ice.Field.Stage(i) == IceStage.Cracking && Contains(i) &&
                                seen.Add((i, ice.Field.ChangedAt(i)))) pending.Add(i);
                        var fighters = new[] { new BotFighter(1000, body.position, body.linearVelocity,
                            fall == 0, own.Mass, resolver.GetWobble(1000)) };
                        var command = brain.Step(true, true, fall == 0, motor.IsDashing, motor.DashReady,
                            body.position, 0, bounds.transform.position, bounds.OuterRadius, fighters, Dt, navigation,
                            playingSeconds: tick * Dt);
                        if (fall == 0) { motor.Step(command, Dt); resolver.Step(Dt); }
                        ice.StepOnHost(tick, (uint)tick, Dt);
                        foreach (var tile in pending.ToArray())
                        {
                            if (ice.Field.Stage(tile) != IceStage.Fallen) continue;
                            resolved++;
                            if (fall == 0 && !Contains(tile)) left++;
                            else
                            {
                                var destination = brain.PlannedPath.Found ? brain.Navigation.Position(
                                    brain.PlannedPath.Cells[brain.PlannedPath.Cells.Count - 1]) : Vector3.zero;
                                Record($"AI2MISSED,D{tier},seed={18 + run},t={tick},tile={tile},tileCenter={ice.TileCenter(tile):F3},p={body.position:F3},v={body.linearVelocity:F3},state={brain.State},dest={destination:F3},move={command.Move:F3}");
                            }
                            pending.Remove(tile);
                        }
                        UnityEngine.Physics.Simulate(Dt);
                        bounds.SampleOnHost(1000, (uint)tick, body.position);
                        if (tick % 600 == 0) yield return null;
                    }
                    total += resolved; escaped += left;
                    if (fall != 0) failures.Add($"D{tier},seed={18 + run}: fell at {fall * Dt:F3}s");
                    Record($"AI2,D{tier},seed={18 + run},fruit={fruit},escaped={left},resolved={resolved},unresolved={pending.Count},fallTick={fall}");
                }
                Record($"AI2,TOTAL,D{tier},escaped={escaped},resolved={total}");
                if (total == 0 || escaped < total * 0.9f) failures.Add($"D{tier}: escaped {escaped}/{total}, requires >=90% with nonzero observations");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator AI1_OneBotRealRing120Seconds_FiveSeedsPerTier()
        {
            var failures = new List<string>();
            var d1Falls = 0;
            var balance = Data<BalanceProfile>("Balance/BalanceProfile");
            for (var tier = 1; tier <= 5; tier++)
            for (var run = 0; run < 5; run++)
            {
                yield return SceneManager.LoadSceneAsync("L1", LoadSceneMode.Single);
                var bounds = UnityEngine.Object.FindFirstObjectByType<TrainingRingBounds>();
                var hazards = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .OfType<IHazard>().ToArray();
                var fruit = new[] { "Bear", "Cat", "Frog" }[run % 3];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/{fruit}.prefab");
                var actor = UnityEngine.Object.Instantiate(prefab);
                var motor = actor.GetComponent<PlayerMotor>();
                var body = actor.GetComponent<Rigidbody>();
                var own = motor.FighterClass;
                body.position = bounds.SpawnPoints[run % bounds.SpawnPoints.Count].position +
                    Vector3.up * (own.Radius + 0.05f);
                var resolver = new GameObject("AI acceptance resolver").AddComponent<CollisionResolver>();
                resolver.Configure(balance); resolver.Register(1000, motor);
                var physics = new FighterPhysicsAdapter(resolver); physics.Add(1000, motor);
                foreach (var hazard in hazards) hazard.Bind(null, physics);
                var profile = Data<BotProfile>($"Bots/D{tier}");
                var brain = new BotBrain(profile, 1000, (uint)(18 + run), own, balance);
                var world = new BotNavigationWorld(bounds.transform.position, balance, hazards);
                var fall = 0;
                bounds.RingOut += (_, tick, _) => { if (fall == 0) fall = (int)tick; };
                IBotNavigationSnapshot navigation = null;
                double nextCapture = 0;
                UnityEngine.Physics.SyncTransforms();
                for (var tick = 1; tick <= 7200; tick++)
                {
                    if (navigation == null || tick >= nextCapture)
                    {
                        navigation = world.Capture(bounds.OuterRadius, tick - 1, Dt);
                        nextCapture = nextCapture == 0 ? tick : nextCapture;
                        nextCapture += 60d / profile.DecisionRateHz;
                    }
                    var fighters = new[] { new BotFighter(1000, body.position, body.linearVelocity,
                        fall == 0, own.Mass, resolver.GetWobble(1000), motor.transform.eulerAngles.y) };
                    var command = brain.Step(true, true, fall == 0, motor.IsDashing, motor.DashReady,
                        body.position, motor.transform.eulerAngles.y, bounds.transform.position,
                        bounds.OuterRadius, fighters, Dt, navigation, balance.TimeLimitSeconds - tick * Dt,
                        playingSeconds: tick * Dt);
                    motor.Step(command, Dt);
                    resolver.Step(Dt);
                    foreach (var hazard in hazards) hazard.StepOnHost(tick, (uint)tick, Dt);
                    UnityEngine.Physics.Simulate(Dt);
                    bounds.SampleOnHost(1000, (uint)tick, body.position);
                    if (tick % 600 == 0) yield return null;
                }
                var early = fall > 0 && fall < 105 * 60;
                if (fall > 0 && tier == 1) d1Falls++;
                if (early && tier > 1) failures.Add($"D{tier}, seed {18 + run}, {fruit}: fell at {fall * Dt:F3}s");
                Record($"AI1,D{tier},seed={18 + run},fruit={fruit},firstFallSeconds={(fall == 0 ? "none" : (fall * Dt).ToString("F3"))},decisions={brain.Decisions}");
            }
            Assert.That(d1Falls, Is.LessThanOrEqualTo(1), "D1: tối đa một lần rơi trong toàn bộ 120 giây, năm seed đã định trước.");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }
    }
}
#endif
