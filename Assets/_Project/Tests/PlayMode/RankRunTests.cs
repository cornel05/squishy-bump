#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class RankRunTests
    {
        private const string Root = "Assets/_Project";
        private const float RunTimeScale = 4f;
        private const float DeadlineSlackSeconds = 30f;
        private const int RespawnToleranceTicks = 1;
        private const ulong LocalFighterId = 0;
        private GameplayBootstrap app;
        private GameObject closeHostObject;

        [UnityTest] public IEnumerator T05_SingleRank_L1_FollowsRankRules() => RunLevel(1);
        [UnityTest] public IEnumerator T05_SingleRank_L2_FollowsRankRules() => RunLevel(2);
        [UnityTest] public IEnumerator T05_SingleRank_L3_FollowsRankRules() => RunLevel(3);
        [UnityTest] public IEnumerator T05_SingleRank_L4_FollowsRankRules() => RunLevel(4);
        [UnityTest] public IEnumerator T05_SingleRank_L5_FollowsRankRules() => RunLevel(5);

        private static readonly int[] CloseLevels = { 1, 2 };

        [UnityTest]
        public IEnumerator RankRespawnClose_AfterCloseTick_FallEliminates(
            [ValueSource(nameof(CloseLevels))] int number)
        {
            const float speed = 10f;
            var level = LoadLevelDefinition(number);
            var balance = LoadBalance();
            var close = CloseTick(balance, level);
            var delay = Ticks(balance, balance.RespawnDelaySeconds);
            var protection = Ticks(balance, balance.InvulnerabilitySeconds);
            // The time-limit tick is the last Playing tick on L2. Arena sampling precedes Result on that tick.
            var lateTick = level.EndTrigger == LevelEndTrigger.TimeLimit ? close : close + 1;
            var deadlineSeconds = level.EndTriggerSeconds / speed + DeadlineSlackSeconds;
            using var log = OpenLog($"T05-Close-L{number}");

            yield return Boot();
            yield return SceneManager.LoadSceneAsync(level.ScenePath, LoadSceneMode.Additive);
            var bounds = Object.FindObjectsByType<TrainingRingBounds>(FindObjectsSortMode.None)
                .Single(candidate => candidate.gameObject.scene.path == level.ScenePath);
            closeHostObject = new GameObject("Rank respawn window test host");
            closeHostObject.SetActive(false);
            var host = closeHostObject.AddComponent<MatchHost>();
            var setup = AssetDatabase.LoadAssetAtPath<MatchSetup>($"{Root}/Data/MatchSetup.asset");
            // Same standalone host and additive arena as MatchHostTests, without bots or arena hazard steps.
            host.Configure(app, balance, bounds, null, null, setup.Roster.Take(2).ToArray(), number, true);

            var nextFallTick = 1;
            var earlyFallsLeft = balance.Lives - 1;
            void PlaceFalls(uint _)
            {
                if (!host.Started || host.Director.Phase != MatchPhase.Playing) return;
                var nextTick = host.Director.ElapsedTicks + 1;
                if (earlyFallsLeft > 0 && nextTick == nextFallTick)
                {
                    MoveBelowKillPlane(host.Fighters[1], bounds.KillPlaneY);
                    earlyFallsLeft--;
                    nextFallTick += delay + RespawnToleranceTicks + protection + 1;
                }
                if (nextTick == lateTick)
                {
                    // Drop the one-life fruit and its opponent together. The latter still has spare lives,
                    // so this also proves the close tick itself prevents respawn and causes elimination.
                    foreach (var fighter in host.Fighters) MoveBelowKillPlane(fighter, bounds.KillPlaneY);
                }
            }

            var start = Time.realtimeSinceStartupAsDouble;
            // Register before MatchHost so the forced falls reach arena sampling on the exact target tick.
            app.Clock.Ticked += PlaceFalls;
            closeHostObject.SetActive(true);
            yield return null;
            Assert.That(host.Started, Is.True);
            Assert.That(host.Fighters, Has.Count.EqualTo(2));
            var run = new RunEvidence(host, balance, level, log, new SessionEvidence());
            var resolver = host.GetComponent<CollisionResolver>();
            app.Events.MatchRaised += run.RecordEvent;
            resolver.Contact += run.RecordContact;
            app.Clock.Ticked += run.Sample;
            Time.timeScale = speed;
            try
            {
                while (host.Director.Phase != MatchPhase.Result &&
                    Time.realtimeSinceStartupAsDouble < start + deadlineSeconds)
                    yield return null;
            }
            finally
            {
                app.Clock.Ticked -= PlaceFalls;
                app.Clock.Ticked -= run.Sample;
                resolver.Contact -= run.RecordContact;
                app.Events.MatchRaised -= run.RecordEvent;
            }

            run.Finish(Time.realtimeSinceStartupAsDouble - start, deadlineSeconds, true);
            Assert.That(run.Failures, Is.Empty);
            var victim = host.Fighters[1];
            Assert.That(run.Respawns, Is.EqualTo(balance.Lives - 1));
            Assert.That(run.ClosedFalls, Is.EqualTo(host.Fighters.Count));
            Assert.That(run.ClosedFallsWithLives, Is.EqualTo(1));
            Assert.That(run.LastClosedFallTick, Is.EqualTo(lateTick));
            Assert.That(host.Director.Lives(victim.Id), Is.Zero);
            Assert.That(host.Director.Lives(LocalFighterId), Is.EqualTo(balance.Lives - 1));
            Assert.That(host.Fighters.All(f => f.Out && !host.Director.IsRespawning(f.Id)), Is.True);
            Assert.That(host.Director.Placements.Select(row => row.Rank), Is.All.EqualTo(1));
        }

        private static void MoveBelowKillPlane(MatchHost.Fighter fighter, float killPlane)
        {
            var body = fighter.Motor.GetComponent<Rigidbody>();
            var position = body.position;
            position.y = killPlane - fighter.Motor.GetComponent<SphereCollider>().radius;
            body.position = position;
            // Arena sampling reads Transform during this same physics tick.
            fighter.Motor.transform.position = position;
        }

        [UnityTest]
        public IEnumerator T05_SingleRank_FullSession_L1toL5()
        {
            yield return Boot();
            using var log = OpenLog("T05-Session");
            var session = new SessionEvidence();
            var first = true;
            ScoreService scores = null;
            var levels = AssetDatabase.LoadAssetAtPath<MatchSetup>($"{Root}/Data/MatchSetup.asset").Levels
                .OrderBy(level => level.LevelNumber).ToArray();
            Assert.That(levels.Select(level => level.LevelNumber), Is.EqualTo(Enumerable.Range(1, 5)));
            foreach (var level in levels)
            {
                using var levelLog = OpenLog($"T05-Session-L{level.LevelNumber}");
                yield return PlayLevel(level, first, levelLog, session, level == levels.Last());
                var match = app.Launcher.Match;
                Assert.That(app.Session.State, Is.EqualTo(Core.SessionState.Hosting));
                if (first) scores = match.Director.Scores;
                Assert.That(match.Director.Scores, Is.SameAs(scores), "Rank totals must stay in one ScoreService");
                first = false;
                log.WriteLine(session.LastSummary);
            }
            log.WriteLine(session.RankingEvidence);
            log.WriteLine($"Summary levels={session.CompletedLevels} ring-outs={session.RingOuts} " +
                $"respawns={session.Respawns} points={session.Points}");
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1f;
            if (closeHostObject != null) Object.Destroy(closeHostObject);
            if (app != null)
            {
                app.Session.Leave();
                Object.Destroy(app.gameObject);
            }
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/MainMenu.unity", LoadSceneMode.Single);
        }

        private IEnumerator RunLevel(int number)
        {
            yield return Boot();
            using var log = OpenLog($"T05-L{number}");
            yield return PlayLevel(LoadLevelDefinition(number), true, log, new SessionEvidence(), true);
        }

        private IEnumerator Boot()
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            app = Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None)
                .First(b => b.Launcher != null);
            app.Store = new LocalSave(Path.Combine(Application.temporaryCachePath, "t05"));
            app.Settings.Rule = MatchRule.Rank;
        }

        private static StreamWriter OpenLog(string name)
        {
            var results = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TestResults");
            Directory.CreateDirectory(results);
            return new StreamWriter(Path.Combine(results, $"{name}.log")) { AutoFlush = true };
        }

        private static BalanceProfile LoadBalance() =>
            AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");

        private static LevelDefinition LoadLevelDefinition(int number) =>
            AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{Root}/Data/Levels/L{number}.asset");

        private static int Ticks(BalanceProfile balance, float seconds) => Mathf.RoundToInt(seconds * balance.FixedStepHz);

        private static int CloseTick(BalanceProfile balance, LevelDefinition level) =>
            Ticks(balance, level.EndTrigger == LevelEndTrigger.FinalRingDrop
                // L1 closes respawns on the penultimate ring drop (105 s in GDD 3.6).
                ? balance.RingDropSeconds[balance.RingDropSeconds.Count - 2]
                : level.EndTriggerSeconds);

        private IEnumerator PlayLevel(LevelDefinition level, bool startSession, StreamWriter log,
            SessionEvidence session, bool rankSession)
        {
            var number = level.LevelNumber;
            var previous = app.Launcher.Match;
            var start = Time.realtimeSinceStartupAsDouble;
            var deadline = start + level.EndTriggerSeconds / RunTimeScale + DeadlineSlackSeconds;
            if (startSession) app.Launcher.StartSinglePlayer(number);
            else app.Launcher.LoadLevel(number);
            bool Started() => app.Launcher.Match != previous && app.Launcher.Match?.Started == true;
            while (!Started() && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            if (!Started())
            {
                log.WriteLine("Summary checks=NR/NR/NR/NR/NR/NR/NR/FAIL ring-outs=0 respawns=0 points=0 no host match");
                Assert.Fail($"L{number} host match never started");
            }

            var match = app.Launcher.Match;
            Assert.That(match.Director.Level, Is.EqualTo(number));
            Assert.That(match.Director.Rule, Is.EqualTo(MatchRule.Rank));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(level.ScenePath));
            Assert.That(match.Fighters.Select(f => (ulong)f.Id),
                Is.EquivalentTo(match.Bots.Keys.Select(id => (ulong)id).Append(LocalFighterId)));
            Assert.That(match.Bots, Is.Not.Empty);

            var run = new RunEvidence(match, LoadBalance(), level, log, session);
            var resolver = match.GetComponent<CollisionResolver>();
            app.Events.MatchRaised += run.RecordEvent;
            resolver.Contact += run.RecordContact;
            // MatchHost subscribed first. Sample after it has stepped the director and updated the fighters.
            app.Clock.Ticked += run.Sample;
            try
            {
                Time.timeScale = RunTimeScale;
                while (match.Director.Phase != MatchPhase.Result && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    // Hit-stop restores normal speed. Keep its pause and resume at the test speed.
                    if (Time.timeScale > 0f) Time.timeScale = RunTimeScale;
                    yield return null;
                }
            }
            finally
            {
                app.Clock.Ticked -= run.Sample;
                resolver.Contact -= run.RecordContact;
                app.Events.MatchRaised -= run.RecordEvent;
            }

            run.Finish(Time.realtimeSinceStartupAsDouble - start, deadline - start, rankSession);
            Assert.That(run.Failures, Is.Empty, $"L{number}: {session.LastSummary}");
        }

        private sealed class SessionEvidence
        {
            private readonly Dictionary<ulong, (int Points, int Lives, int Ticks)> totals = new();
            private readonly HashSet<int> levels = new();
            public int CompletedLevels => levels.Count;
            public int RingOuts { get; private set; }
            public int Respawns { get; private set; }
            public int Points => totals.Values.Sum(row => row.Points);
            public string LastSummary { get; set; }
            public string RankingEvidence { get; private set; }

            public int PointsFor(ulong id) => totals.TryGetValue(id, out var total) ? total.Points : 0;

            public void AddMatch(int number, IEnumerable<MatchStanding> rows, int ringOuts, int respawns)
            {
                Assert.That(levels.Add(number), Is.True, $"L{number} counted twice");
                RingOuts += ringOuts;
                Respawns += respawns;
                foreach (var row in rows)
                {
                    totals.TryGetValue(row.FighterId, out var total);
                    totals[row.FighterId] =
                        (row.Points, total.Lives + row.Lives, total.Ticks + row.SurvivalTicks);
                }
            }

            public bool CheckRanking(ScoreService scores, out int tiedFighters)
            {
                var groups = totals.OrderByDescending(p => p.Value.Points).ThenByDescending(p => p.Value.Lives)
                    .ThenByDescending(p => p.Value.Ticks).GroupBy(p => p.Value);
                var expected = new List<MatchStanding>();
                tiedFighters = 0;
                foreach (var group in groups)
                {
                    var rank = expected.Count + 1;
                    if (group.Count() > 1) tiedFighters += group.Count();
                    expected.AddRange(group.Select(p => new MatchStanding(p.Key, rank,
                        group.Key.Points, group.Key.Lives, group.Key.Ticks)));
                }
                var expectedRows = Describe(expected);
                var actualRows = Describe(scores.Standings().Select(s =>
                    new MatchStanding(s.FighterId, s.Rank, s.Points, s.Lives, checked((int)s.SurvivalTicks))));
                RankingEvidence = $"Session ranking expected=[{expectedRows}] actual=[{actualRows}]";
                return actualRows == expectedRows;
            }
        }

        private static string Describe(IEnumerable<MatchStanding> rows) => string.Join(" ",
            rows.OrderBy(row => row.FighterId).Select(row =>
                $"{row.FighterId}(rank={row.Rank},points={row.Points},lives={row.Lives},ticks={row.SurvivalTicks})"));

        private sealed class RunEvidence
        {
            private enum Check { Lives, Respawn, Protection, Credit, Close, MatchRanking, SessionRanking, Deadline }

            private sealed class FighterEvidence
            {
                public int Lives;
                public int Points;
                public int SurvivalTicks;
                public bool WasActive;
                public (ulong Other, int Tick)? Touch;
                public int? FellAt;
                public int? ProtectedUntil;
                public int? ClosedFallAt;
                public uint? EliminatedAt;
            }

            private readonly MatchHost match;
            private readonly MatchDirector director;
            private readonly StreamWriter log;
            private readonly SessionEvidence session;
            private readonly Dictionary<ulong, FighterEvidence> fighters;
            private readonly Dictionary<Check, string> failures = new();
            private readonly int delay, protection, window, closeTick, pointsPerRingOut;
            private readonly int floorMask = UnityEngine.Physics.DefaultRaycastLayers &
                ~(1 << CollisionResolver.FighterLayer);
            private IReadOnlyList<MatchStanding> result;
            private int previousElapsed, ringOuts, respawns, completedProtections, credited, untouched, expired;
            private int closedFalls, closedFallsWithLives, eliminations, matchTies, sessionTies, resultEvents;
            private bool reachedClose;
            private int? localEliminatedAt;
            public int Respawns => respawns;
            public int ClosedFalls => closedFalls;
            public int ClosedFallsWithLives => closedFallsWithLives;
            public int? LastClosedFallTick { get; private set; }

            public RunEvidence(MatchHost match, BalanceProfile balance, LevelDefinition level, StreamWriter log,
                SessionEvidence session)
            {
                this.match = match;
                director = match.Director;
                this.log = log;
                this.session = session;
                delay = Ticks(balance, balance.RespawnDelaySeconds);
                protection = Ticks(balance, balance.InvulnerabilitySeconds);
                window = Ticks(balance, balance.LastTouchWindowSeconds);
                closeTick = CloseTick(balance, level);
                pointsPerRingOut = balance.PointsPerRingOut;
                fighters = match.Fighters.ToDictionary(f => (ulong)f.Id, f =>
                    new FighterEvidence { Lives = balance.Lives, Points = session.PointsFor(f.Id), WasActive = !f.Out });

                log.WriteLine($"L{level.LevelNumber} seed={director.Seed} roster={string.Join(",", fighters.Keys)} " +
                    $"lives={balance.Lives} respawnDelay={delay} invulnerability={protection} touchWindow={window} " +
                    $"closeTick={closeTick} pointsPerRingOut={pointsPerRingOut}");
                foreach (var pair in fighters)
                    Expect(Check.Lives, director.Lives(pair.Key) == pair.Value.Lives,
                        $"Fighter {pair.Key} starts with {director.Lives(pair.Key)} lives, expected {pair.Value.Lives}");
            }

            public IEnumerable<string> Failures => failures.Select(pair => $"{pair.Key}: {pair.Value}");

            private void Expect(Check check, bool matches, string detail)
            {
                if (!matches && !failures.ContainsKey(check))
                {
                    failures.Add(check, detail);
                    log.WriteLine($"FAIL {check} elapsed={director.ElapsedTicks} {detail}");
                }
            }

            public void RecordContact(ulong first, ulong second)
            {
                if (director.Phase != MatchPhase.Playing ||
                    director.IsInvulnerable(first, 1) || director.IsInvulnerable(second, 1))
                    return;
                // Contact occurs before the director advances elapsed time for this tick.
                var tick = director.ElapsedTicks + 1;
                fighters[first].Touch = (second, tick);
                fighters[second].Touch = (first, tick);
            }

            public void RecordEvent(MatchEvent e)
            {
                var tick = director.ElapsedTicks;
                var detail = "";
                switch (e.Kind)
                {
                    case MatchEventKind.RingOut:
                        detail = RecordFall(e, tick);
                        break;
                    case MatchEventKind.Respawn:
                        detail = RecordRespawn(e, tick);
                        break;
                    case MatchEventKind.Elimination:
                        eliminations++;
                        if (e.FighterId == LocalFighterId) localEliminatedAt = tick;
                        var eliminated = fighters[e.FighterId];
                        Expect(Check.MatchRanking, !eliminated.EliminatedAt.HasValue,
                            $"Fighter {e.FighterId} received a duplicate Elimination");
                        eliminated.EliminatedAt = e.HostTick;
                        if (eliminated.ClosedFallAt == tick) eliminated.ClosedFallAt = null;
                        detail = $"rank={e.Value} lives={director.Lives(e.FighterId)}";
                        break;
                    case MatchEventKind.MatchResult:
                        resultEvents++;
                        result = e.Standings;
                        detail = Describe(e.Standings);
                        break;
                    case MatchEventKind.Scoreboard:
                        foreach (var row in e.Standings)
                            Expect(Check.Credit, row.Points == fighters[row.FighterId].Points,
                                $"Scoreboard fighter {row.FighterId} points={row.Points}, " +
                                $"expected {fighters[row.FighterId].Points}");
                        detail = Describe(e.Standings);
                        break;
                    default:
                        return;
                }
                log.WriteLine($"event={e.EventId} hostTick={e.HostTick} elapsed={tick} {e.Kind} " +
                    $"fighter={FighterName(e.FighterId)} {detail}");
            }

            private static string FighterName(ulong id) => id == MatchEvent.NoFighter ? "none" : id.ToString();

            private string RecordFall(MatchEvent e, int tick)
            {
                ringOuts++;
                var fighter = fighters[e.FighterId];
                fighter.Lives--;
                Expect(Check.Lives, fighter.WasActive && fighter.Lives >= 0 &&
                    director.Lives(e.FighterId) == fighter.Lives,
                    $"RingOut {e.FighterId} lives={director.Lives(e.FighterId)}, expected {fighter.Lives}");
                fighter.FellAt = tick;
                fighter.ProtectedUntil = null;
                var touch = fighter.Touch;
                var age = touch.HasValue ? tick - touch.Value.Tick : (int?)null;
                var recent = age.HasValue && age.Value <= window;
                var expectedCredit = recent ? touch.Value.Other : MatchEvent.NoFighter;
                Expect(Check.Credit, e.CreditedFighterId == expectedCredit,
                    $"RingOut {e.FighterId} credited={FighterName(e.CreditedFighterId)}, " +
                    $"expected {FighterName(expectedCredit)} (touch age {age?.ToString() ?? "none"})");
                if (recent)
                {
                    credited++;
                    fighters[expectedCredit].Points += pointsPerRingOut;
                }
                else if (age.HasValue) expired++;
                else untouched++;
                fighter.Touch = null;

                if (tick >= closeTick)
                {
                    closedFalls++;
                    if (fighter.Lives > 0) closedFallsWithLives++;
                    fighter.ClosedFallAt = tick;
                    LastClosedFallTick = tick;
                }
                return $"lives={director.Lives(e.FighterId)} toucher={FighterName(touch?.Other ?? MatchEvent.NoFighter)} " +
                    $"touchAge={age?.ToString() ?? "none"} credit={FighterName(e.CreditedFighterId)}";
            }

            private string RecordRespawn(MatchEvent e, int tick)
            {
                respawns++;
                var fighter = fighters[e.FighterId];
                var elapsed = tick - fighter.FellAt;
                Expect(Check.Respawn, elapsed.HasValue && Mathf.Abs(elapsed.Value - delay) <= RespawnToleranceTicks,
                    $"Respawn {e.FighterId} delay={elapsed}, expected {delay} ± {RespawnToleranceTicks} ticks");
                Expect(Check.Close, tick < closeTick, $"Respawn {e.FighterId} at {tick}, close={closeTick}");
                fighter.FellAt = null;
                fighter.ProtectedUntil = tick + protection;
                var motor = match.Fighters.First(f => f.Id == e.FighterId).Motor;
                var radius = motor.GetComponent<SphereCollider>().radius;
                var position = motor.transform.position;
                var onFloor = UnityEngine.Physics.Raycast(position, Vector3.down, out var hit,
                    radius + radius, floorMask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0f;
                Expect(Check.Respawn, onFloor, $"Respawn {e.FighterId} at {position} has no floor below");
                return $"delay={elapsed} floor={(onFloor ? hit.collider.name : "none")} position={position}";
            }

            public void Sample(uint hostTick)
            {
                var tick = director.ElapsedTicks;
                var playing = director.Phase == MatchPhase.Playing;
                Expect(Check.Deadline, tick == previousElapsed || tick == previousElapsed + 1,
                    $"Missed simulation ticks between {previousElapsed} and {tick} at host tick {hostTick}");
                Expect(Check.Close, director.RespawnsOpen == (playing && tick < closeTick),
                    $"RespawnsOpen={director.RespawnsOpen} at {director.Phase} tick {tick}, close={closeTick}");
                reachedClose |= tick >= closeTick;

                foreach (var f in match.Fighters)
                {
                    var fighter = fighters[f.Id];
                    if (tick > previousElapsed && fighter.WasActive) fighter.SurvivalTicks++;
                    fighter.WasActive = !f.Out;
                    if (director.Lives(f.Id) != fighter.Lives)
                        Expect(Check.Lives, false, $"Fighter {f.Id} lives={director.Lives(f.Id)}, expected {fighter.Lives}");
                    if (director.Scores.Points(f.Id) != fighter.Points)
                        Expect(Check.Credit, false,
                            $"Fighter {f.Id} points={director.Scores.Points(f.Id)}, expected {fighter.Points}");

                    var respawning = director.IsRespawning(f.Id);
                    if (playing && fighter.FellAt.HasValue && fighter.Lives > 0 && tick < closeTick)
                    {
                        Expect(Check.Respawn, respawning && f.Out, $"Fighter {f.Id} missing respawn wait");
                        // A due tick at or past close is cancelled by the rules.
                        var due = fighter.FellAt.Value + delay;
                        Expect(Check.Respawn, due >= closeTick || tick <= due + RespawnToleranceTicks,
                            $"Fighter {f.Id} still waiting for respawn at {tick}, due {due}");
                    }
                    if (tick >= closeTick)
                        Expect(Check.Close, !respawning && !fighter.ClosedFallAt.HasValue,
                            $"Fighter {f.Id} still waiting or missing Elimination event at {tick}, close={closeTick}");

                    var invulnerable = fighter.ProtectedUntil.HasValue && tick < fighter.ProtectedUntil.Value;
                    Expect(Check.Protection, director.IsInvulnerable(f.Id) == invulnerable,
                        $"Fighter {f.Id} invulnerable={director.IsInvulnerable(f.Id)}, expected {invulnerable} " +
                        $"(end={fighter.ProtectedUntil?.ToString() ?? "none"})");
                    if (fighter.ProtectedUntil == tick)
                    {
                        completedProtections++;
                        fighter.ProtectedUntil = null;
                    }
                }
                previousElapsed = tick;
            }

            private void CheckRankings(bool rankSession)
            {
                Expect(Check.MatchRanking, resultEvents == 1, $"Expected one MatchResult event, saw {resultEvents}");
                if (result == null) return;
                var survivors = fighters.Where(p => !p.Value.EliminatedAt.HasValue)
                    .OrderByDescending(p => p.Value.Lives).ThenBy(p => director.Wobble(p.Key))
                    .GroupBy(p => (p.Value.Lives, Wobble: director.Wobble(p.Key)))
                    .Select(g => g.AsEnumerable());
                var eliminated = fighters.Where(p => p.Value.EliminatedAt.HasValue)
                    .OrderByDescending(p => p.Value.EliminatedAt).GroupBy(p => p.Value.EliminatedAt)
                    .Select(g => g.AsEnumerable());
                var expected = new List<MatchStanding>();
                foreach (var group in survivors.Concat(eliminated))
                {
                    var rank = expected.Count + 1;
                    if (group.Count() > 1) matchTies += group.Count();
                    expected.AddRange(group.Select(p => new MatchStanding(p.Key, rank,
                        p.Value.Points, p.Value.Lives, p.Value.SurvivalTicks)));
                }
                var expectedRows = Describe(expected);
                var resultRows = Describe(result);
                log.WriteLine($"Match ranking expected=[{expectedRows}] actual=[{resultRows}]");
                Expect(Check.MatchRanking, resultRows == expectedRows, "MatchResult placements differ from match ranking");
                session.AddMatch(director.Level, expected, ringOuts, respawns);
                if (!rankSession) return;
                var sessionMatches = session.CheckRanking(director.Scores, out sessionTies);
                log.WriteLine(session.RankingEvidence);
                Expect(Check.SessionRanking, sessionMatches, "ScoreService.Standings differs from session ranking");
            }

            public void Finish(double seconds, double budget, bool rankSession)
            {
                var finished = director.Phase == MatchPhase.Result;
                Expect(Check.Deadline, finished && seconds <= budget,
                    $"Phase={director.Phase} after {seconds:F2}s, deadline={budget:F2}s");
                if (finished) CheckRankings(rankSession);

                var coverage = new[]
                {
                    ringOuts > 0 ? "PASS" : "PARTIAL",
                    respawns > 0 ? "PASS" : "NR",
                    completedProtections > 0 ? "PASS" : respawns > 0 ? "PARTIAL" : "NR",
                    credited > 0 && untouched > 0 ? "PASS" : ringOuts > 0 ? "PARTIAL" : "NR",
                    closedFallsWithLives > 0 ? "PASS" : reachedClose ? "PARTIAL" : "NR",
                    result != null ? "PASS" : "NR",
                    result != null && rankSession ? "PASS" : "NR",
                    finished ? "PASS" : "NR"
                };
                var checks = string.Join("/", coverage.Select((status, index) =>
                    failures.ContainsKey((Check)index) ? "FAIL" : status));
                session.LastSummary = $"L{director.Level} Summary checks={checks} phase={director.Phase} elapsed={director.ElapsedTicks} " +
                    $"realSeconds={seconds:F2} deadlineSeconds={budget:F2} ring-outs={ringOuts} respawns={respawns} " +
                    $"protectionsCompleted={completedProtections} points={credited * pointsPerRingOut} " +
                    $"cumulativePoints={fighters.Keys.Sum(director.Scores.Points)} " +
                    $"credited={credited} noTouch={untouched} expiredTouch={expired} eliminations={eliminations} " +
                    $"closeReached={reachedClose} closedFalls={closedFalls} closedFallsWithLives={closedFallsWithLives} " +
                    $"localEliminatedAt={localEliminatedAt?.ToString() ?? "none"} matchTies={matchTies} sessionTies={sessionTies}";
                log.WriteLine(session.LastSummary);
            }
        }
    }
}
#endif
