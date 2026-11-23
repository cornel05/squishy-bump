using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;
using SquishyBump.AI;
using SquishyBump.Hazards;
using SquishyBump.Match;
using SquishyBump.Net.Sync;
using SquishyBump.Physics;
using SquishyBump.Presentation;
using SquishyBump.UI.Menus;
using Unity.Netcode;
using SquishyBump.UI;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>
    /// One arena match on this peer (GDD 10.4).
    /// Host: each host tick every fighter's motor steps from its command (local input, a remote client's
    /// <see cref="InputInbox"/>, or idle for bot slots), then the collision resolver, then hazards, then ring-out
    /// sampling, then the match tick; every second tick a snapshot goes to the clients.
    /// Client: <see cref="MatchClientView"/> shows the host's snapshots and uploads local input.
    /// Wires modules through events; holds no rules of its own.
    /// </summary>
    public sealed class MatchHost : MonoBehaviour
    {
        /// <summary>Bot / idle slot ids start here so they never collide with NGO client ids.</summary>
        public const ushort FirstBotId = 1000;

        [SerializeField] private GameplayBootstrap bootstrap;
        [SerializeField] private BalanceProfile balance;
        [SerializeField] private TrainingRingBounds bounds;
        [Tooltip("Optional: the level's H01 floor.")]
        [SerializeField] private RingDrop ringDrop;
        [SerializeField] private CameraRig cameraRig;
        [Tooltip("Roster slots, in order: host player, remote players, then idle slots.")]
        [SerializeField] private GameObject[] fighterPrefabs;
        [SerializeField] private int level = 1;
        [SerializeField] private uint seed;
        [Tooltip("GDD 3.6.")]
        [SerializeField] private int countdownSeconds = 3;
        [Tooltip("GDD 3.6: result confirmed this long after the last elimination.")]
        [SerializeField] private float resultConfirmSeconds = 0.5f;
        [Tooltip("Single Player: host and start the match on Start.")]
        [SerializeField] private bool autoHost = true;

        private readonly List<Fighter> fighters = new();
        private readonly Dictionary<ulong, InputInbox> inboxes = new();
        private CollisionResolver resolver;
        private LocalPlayerInput localInput;
        private MatchClientView clientView;
        private NetworkManager networkManager;
        private int snapshotEveryTicks;
        private PauseMenu pause;
        private readonly List<IHazard> hazardList = new();
        private FighterPhysicsAdapter fighterPhysics;
        private uint goTick;
        private MatchOverlay overlay;
        private System.Action leaveMatch;
        private float introSeconds, bannerSeconds, finalWarningSeconds;
        private bool finalWarningShown;
        private uint? introStartTick;
        private AudioDirector sfxPlayer;
        private readonly HashSet<ulong> dashing = new();
        private readonly HashSet<Collider> nearBoxes = new();
        private bool hitStopping;
        private bool pausesGame;
        private bool lanSurvival;
        private bool survivalResult;
        private bool sessionFinished;
        private IReadOnlyList<LobbyPlayer> survivalRoster;
        private ushort? cameraFighter;
        private System.Action leaveCompletedSession;
        public MatchHud Hud { get; private set; }
        private WorldSnapshot hudSnapshot;
        private ScoreService sessionScores;
        private uint matchId, simulationTick;
        public void ConfigureRank(ScoreService scores, uint id) { sessionScores = scores; matchId = id; }
        private BotProfile botProfile;
        private readonly Dictionary<ushort, BotBrain> bots = new();
        private BotNavigationWorld navigationWorld;
        private uint nextNavigationTick;
        public IBotNavigationSnapshot BotNavigation { get; private set; }
        public IReadOnlyDictionary<ushort, BotBrain> Bots => bots;
        public void ConfigureBots(BotProfile profile) => botProfile = profile;

        public MatchDirector Director { get; private set; }
        public IReadOnlyList<Fighter> Fighters => fighters;
        public MatchClientView ClientView => clientView;
        public bool Started => Director != null;
        internal bool Initialized => localInput != null;
        public IReadOnlyList<IHazard> Hazards => hazardList;
        /// <summary>Set by the launcher: start the match on the first frame after Start (host only).</summary>
        public bool StartWhenReady { get; set; }

        /// <summary>For matches built at runtime: call on an inactive object, before Start.</summary>
        public void Configure(GameplayBootstrap owner, BalanceProfile balanceProfile, TrainingRingBounds arenaBounds,
            RingDrop ringDropHazard, CameraRig rig, GameObject[] roster, int levelNumber, bool autoHost)
        {
            bootstrap = owner;
            balance = balanceProfile;
            bounds = arenaBounds;
            ringDrop = ringDropHazard;
            cameraRig = rig;
            fighterPrefabs = roster;
            level = levelNumber;
            this.autoHost = autoHost;
        }

        public void ConfigureLanSurvival(IReadOnlyList<LobbyPlayer> contestants, System.Action leave)
        {
            lanSurvival = true;
            survivalRoster = contestants;
            leaveCompletedSession = leave;
        }

        /// <summary>Esc menu: pauses in Single Player, overlay only in Multiplayer (GDD 3.4).</summary>
        public void AttachPause(PauseMenu menu, bool singlePlayer, System.Action leave)
        {
            pause = menu;
            pausesGame = singlePlayer;
            pause.OpenChanged += OnPauseChanged;
            pause.LeaveRequested += leave;
            leaveMatch = leave;
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>Arena card before the countdown and hazard banners (GDD B.3, B.6). The host holds the intro for the card.</summary>
        public void AttachOverlay(MatchOverlay matchOverlay, float cardSeconds, float banner, float finalWarning)
        {
            overlay = matchOverlay;
            introSeconds = cardSeconds;
            bannerSeconds = banner;
            finalWarningSeconds = finalWarning;
        }

        /// <summary>SFX for this peer's events (GDD 12.7). Optional.</summary>
        public void AttachAudio(AudioDirector director) => sfxPlayer = director;

        private void Sound(Sfx id, Vector3 at) { if (sfxPlayer != null) sfxPlayer.Play(id, at); }

        private void OnTookOff(RollVisual visual) => Sound(Sfx.Jump, visual.transform.position);

        // GDD 3.4: 40 ms hit-stop on a host-confirmed hit. Only when the local fruit is involved, so the host's
        // simulation is not slowed for every distant bump; never while paused.
        private System.Collections.IEnumerator HitStop()
        {
            if (hitStopping || Time.timeScale == 0f || sfxPlayer == null) yield break;
            hitStopping = true;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(setupHitStopSeconds);
            // Stay frozen only if the Esc menu really pauses (Single Player); a LAN menu never resumes time itself.
            if (!pausesGame || pause == null || !pause.IsOpen) Time.timeScale = 1f;
            hitStopping = false;
        }

        private float setupHitStopSeconds;

        /// <summary>B07 on L2–L5: true once, when the match clock reaches the last-seconds warning.</summary>
        public static bool FinalWarningDue(int level, float playingSeconds, float limitSeconds, float warningSeconds) =>
            level != 1 && playingSeconds >= limitSeconds - warningSeconds;

        private void OnPauseChanged(bool open)
        {
            localInput.Suspended = open || survivalResult;
            if (pausesGame) Time.timeScale = open ? 0f : 1f;
            Cursor.lockState = open || survivalResult ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void ApplySettings(GameSettings settings)
        {
            AudioListener.volume = settings.MasterVolume;
            if (cameraRig == null) return;
            cameraRig.SensitivityScale = settings.MouseSensitivity;
            cameraRig.SetDistanceScale(settings.CameraDistance);
            cameraRig.ShakeEnabled = settings.ScreenShake;
        }

        public sealed class Fighter
        {
            public ushort Id;
            public byte Fruit;
            public PlayerMotor Motor;
            public RollVisual Visual;
            public Renderer[] VisibleRenderers;
            public bool Out;
        }

        private void Start()
        {
            if (bootstrap == null) bootstrap = FindAnyObjectByType<GameplayBootstrap>();
            networkManager = bootstrap.GetComponent<NetworkManager>();
            localInput = new LocalPlayerInput(cameraRig, bootstrap.Saved.Bindings);
            Hud = gameObject.AddComponent<MatchHud>();
            Hud.Init(balance, level);
            bootstrap.Sync.SnapshotReceived += RefreshClientHud;
            // Every hazard of this level scene (H01 floor, moving ground, cannons…).
            foreach (var root in gameObject.scene.GetRootGameObjects())
                hazardList.AddRange(root.GetComponentsInChildren<IHazard>(true));
            if (ringDrop != null && !hazardList.Contains(ringDrop)) hazardList.Add(ringDrop);
            if (ringDrop == null) ringDrop = hazardList.OfType<RingDrop>().FirstOrDefault();
            ApplySettings(bootstrap.Settings);
            bootstrap.Settings.Changed += ApplySettings;
            bootstrap.Session.StateChanged += OnSessionState;
            bootstrap.Events.HazardRaised += OnHazard;
            bootstrap.Events.MatchRaised += OnMatchEvent;
            if (autoHost && bootstrap.Session.State == SessionState.Idle) bootstrap.Session.Host(SessionMode.SinglePlayer);
            if (autoHost && bootstrap.Session.State == SessionState.Hosting) StartMatch();
            if (bootstrap.Session.State == SessionState.Joined) OnSessionState(SessionState.Joined);
        }

        private void OnSessionState(SessionState state)
        {
            if (state != SessionState.Joined || clientView != null) return;
            clientView = new MatchClientView(bootstrap, fighterPrefabs, cameraRig, localInput, balance);
            {
                clientView.ViewSpawned += (_, view) =>
                {
                    var visual = view.GetComponentInChildren<RollVisual>();
                    if (visual != null) visual.TookOff += OnTookOff;
                };
                clientView.ViewSpawned += (id, view) =>
                {
                    Hud.Register(id, view.transform, view.GetComponent<SphereCollider>().radius + 0.5f);
                    if (id == (ushort)Unity.Netcode.NetworkManager.Singleton.LocalClientId)
                        Hud.SetFruit(view.GetComponent<FighterProfileApplier>()?.Fruit);
                    if (hudSnapshot != null) RefreshClientHud(hudSnapshot);
                };
            }
        }

        /// <summary>Host: spawns the roster (host, connected clients, idle slots) and starts the countdown.</summary>
        public void StartMatch()
        {
            if (Started || !bootstrap.Session.CanSimulateGameplay) return;
            resolver = gameObject.AddComponent<CollisionResolver>();
            resolver.Configure(balance);
            fighterPhysics = new FighterPhysicsAdapter(resolver);
            snapshotEveryTicks = Mathf.Max(1, Mathf.RoundToInt(balance.InputRateHz / balance.SnapshotRateHz));

            var roster = survivalRoster ?? DefaultRoster();
            var spawns = bounds.SpawnPoints;
            for (var i = 0; i < roster.Count; i++)
            {
                var slot = roster[i];
                var id = (ushort)slot.Id;
                var spawn = spawns[i % spawns.Count];
                var prefab = fighterPrefabs[slot.Fruit];
                var go = Instantiate(prefab, spawn.position + Vector3.up * SpawnLift(prefab), Quaternion.identity);
                var f = new Fighter
                {
                    Id = id, Fruit = (byte)slot.Fruit, Motor = go.GetComponent<PlayerMotor>(),
                    Visual = go.GetComponentInChildren<RollVisual>(),
                    VisibleRenderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray(),
                };
                fighters.Add(f);
                Hud.Register(id, f.Motor.transform, go.GetComponent<SphereCollider>().radius + 0.5f);
                Hud.SetWobble(id, 0, true);
                if (id >= FirstBotId && botProfile != null) bots[id] = new BotBrain(botProfile, id, seed);
                resolver.Register(f.Id, f.Motor);
                fighterPhysics.Add(f.Id, f.Motor);
                if (id != 0 && id < FirstBotId) inboxes[id] = new InputInbox();
            }

            Director = new MatchDirector(() => bootstrap.Session.CanSimulateGameplay, fighters.Select(f => (ulong)f.Id), level,
                seed, Mathf.RoundToInt(1f / Time.fixedDeltaTime), countdownSeconds, resultConfirmSeconds,
                Mathf.RoundToInt(balance.TimeLimitSeconds), bounds, bootstrap.Settings.Rule, balance, sessionScores, matchId, bootstrap.EventSink);
            Director.TryPlaceRespawn = TryRespawn;

            if (cameraRig != null) cameraRig.Follow(fighters[0].Motor.transform);
            Hud.SetFruit(fighters[0].Motor.GetComponent<FighterProfileApplier>()?.Fruit, fighters[0].Fruit);
            Hud.SetRemaining(fighters.Count, fighters.Count);
            resolver.WobbleChanged += Director.SetWobble;
            resolver.Hit += OnHit;
            resolver.Contact += OnContact;
            foreach (var f in fighters)
                if (f.Visual != null) f.Visual.TookOff += OnTookOff;
            Director.ResultConfirmed += _ =>
            {
                Sound(Sfx.Win, cameraRig != null ? cameraRig.transform.position : Vector3.zero);
                // Best Single Player Survival run: the player won this level outright.
                if (pausesGame && Director.Rule == MatchRule.Survival && Director.GetSinglePlayerOutcome(0) != SurvivalOutcome.Retry)
                    bootstrap.RecordSurvival(level);
                if (pausesGame && Director.Rule == MatchRule.Rank && level == 5) bootstrap.RecordRank(Director.Scores.Points(0));
            };
            foreach (var ice in hazardList.OfType<IceTiles>()) ice.SetSeed(seed);
            foreach (var h in hazardList) h.Bind(bootstrap.EventSink, fighterPhysics);
            if (bots.Count > 0) navigationWorld = new BotNavigationWorld(bounds.transform.position, balance, hazardList);
            bootstrap.Sync.InputReceived += OnRemoteInput;
            bootstrap.Clock.Ticked += OnTick;
        }

        private IReadOnlyList<LobbyPlayer> DefaultRoster()
        {
            var humans = new List<ulong> { 0 };
            humans.AddRange(bootstrap.Session.Peers.OrderBy(id => id).Take(fighterPrefabs.Length - 1));
            return Enumerable.Range(0, fighterPrefabs.Length).Select(i =>
                new LobbyPlayer(i < humans.Count ? humans[i] : (ulong)(FirstBotId + i), i, false)).ToArray();
        }

        private static float SpawnLift(GameObject prefab) => prefab.GetComponent<SphereCollider>().radius;

        private void Update()
        {
            if (bootstrap == null) return;
            if (StartWhenReady && !Started) StartMatch();
            if (pause != null && localInput != null && localInput.MenuPressed) pause.Toggle();
            localInput?.UpdateLook();
            UpdateSounds();
            clientView?.Update(Time.time);
            if (lanSurvival && (Started || clientView != null)) UpdateSurvivalCamera();
            foreach (var f in fighters)
            {
                if (f.Out) continue;
                var visible = Director == null || !Director.IsInvulnerable(f.Id) || Mathf.Repeat(Time.time, 0.5f) < 0.25f;
                foreach (var renderer in f.VisibleRenderers) renderer.enabled = visible;
            }
            if (clientView != null && clientView.TryPlayingSeconds(Time.time, out var seconds))
                foreach (var h in hazardList) h.ShowAt(seconds);
            else if (Started) seconds = Director.ElapsedTicks * Time.fixedDeltaTime;
            else seconds = 0f;
            if (overlay != null && !survivalResult && !finalWarningShown &&
                FinalWarningDue(level, seconds, balance.TimeLimitSeconds, finalWarningSeconds))
            {
                finalWarningShown = true;
                overlay.ShowBanner($"{finalWarningSeconds:0} seconds left!", bannerSeconds);
            }
            // HUD for the host's local fighter (id 0); display only.
            if (overlay != null && Started && Director != null)
                overlay.ShowHud(Director.Lives(0), Director.Rule == MatchRule.Rank ? balance.Lives : 1);
            if (Started && Director != null && Time.unscaledTime >= nextBoardAt)
            {
                nextBoardAt = Time.unscaledTime + BoardRefreshSeconds;
                var rank = Director.Rule == MatchRule.Rank && Director.Scores != null;
                Hud.SetBoard(fighters.Select(f => (FighterLabel(f.Id),
                    rank ? Director.Scores.Points(f.Id) : Director.Lives(f.Id), FruitColor(f.Fruit))).ToList());
            }
        }

        private void UpdateSurvivalCamera()
        {
            if (cameraRig == null) return;
            var own = (ushort)networkManager.LocalClientId;
            var views = clientView != null ? clientView.Views : null;
            var ownView = views != null ? (views.TryGetValue(own, out var view) ? view : null) :
                fighters.FirstOrDefault(f => f.Id == own)?.Motor.gameObject;
            if (ownView != null && ownView.activeSelf)
            {
                FollowFighter(own, ownView);
                overlay?.ShowSpectating(false);
                return;
            }

            var available = views != null
                ? views.Where(pair => pair.Value.activeSelf).OrderBy(pair => pair.Key).ToArray()
                : fighters.Where(f => !f.Out).Select(f => new KeyValuePair<ushort, GameObject>(f.Id, f.Motor.gameObject)).ToArray();
            overlay?.ShowSpectating(available.Length > 0);
            if (available.Length == 0) return;
            var current = System.Array.FindIndex(available, pair => pair.Key == cameraFighter);
            var index = current < 0 ? 0 : (current + localInput.SpectatorCycle + available.Length) % available.Length;
            var target = available[index];
            FollowFighter(target.Key, target.Value);
        }

        private void FollowFighter(ushort id, GameObject target)
        {
            if (cameraFighter == id) return;
            cameraFighter = id;
            cameraRig.Follow(target.transform);
        }

        private void FixedUpdate() => clientView?.FixedUpdate();

        // Host only: a client's commands steer only that client's own fighter.
        private void OnRemoteInput(ulong client, IReadOnlyList<InputCommand> commands)
        {
            if (inboxes.TryGetValue(client, out var inbox)) inbox.Offer(commands);
        }

        private void OnTick(uint tick)
        {
            if (lanSurvival && (sessionFinished || Director.Phase == MatchPhase.Result)) return;
            simulationTick = tick;
            var dt = Time.fixedDeltaTime;
            introStartTick ??= tick;
            if (Director.Phase == MatchPhase.Intro && tick - introStartTick.Value >= Mathf.RoundToInt(introSeconds / dt))
                Director.FinishIntro(tick);
            var playing = Director.Phase == MatchPhase.Playing;
            var local = localInput.Sample();
            var sensedFighters = fighters.Select(f => new BotFighter(f.Id, f.Motor.transform.position,
                f.Motor.GetComponent<Rigidbody>().linearVelocity, !f.Out,
                f.Motor.GetComponent<Rigidbody>().mass, resolver.GetWobble(f.Id))).ToArray();
            var radius = bounds.OuterRadius;
            if (ringDrop != null && goTick != 0)
            {
                var dropped = balance.RingDropSeconds.Count(at => Director.ElapsedTicks * dt + balance.RingDropWarningSeconds >= at);
                radius *= 1f - dropped / (float)balance.RingDropSeconds.Count;
            }
            if (playing && navigationWorld != null && (BotNavigation == null || tick >= nextNavigationTick))
            {
                BotNavigation = navigationWorld.Capture(bounds.OuterRadius, Director.ElapsedTicks, dt);
                var fastest = bots.Values.Max(b => b.DecisionRateHz);
                nextNavigationTick = tick + (uint)Mathf.Max(1, Mathf.RoundToInt(1f / (fastest * dt)));
            }

            foreach (var f in fighters)
            {
                if (f.Out) continue;
                if (lanSurvival && f.Id != 0 && !bootstrap.Session.Peers.Contains(f.Id))
                {
                    if (playing) Director.ReportRingOut(f.Id, tick);
                    continue;
                }
                f.Motor.Invulnerable = Director.IsInvulnerable(f.Id, 1);
                var input = f.Id == 0 ? local : inboxes.TryGetValue(f.Id, out var inbox) ? inbox.Take() : default;
                if (bots.TryGetValue(f.Id, out var brain))
                    input = brain.Step(bootstrap.Session.CanSimulateGameplay, playing, !f.Out,
                        f.Motor.IsDashing, f.Motor.DashReady, f.Motor.transform.position,
                        f.Motor.transform.eulerAngles.y, bounds.transform.position, radius, sensedFighters, dt, BotNavigation);
                // Countdown and idle slots send an empty command; motors still integrate gravity and traction.
                if (!playing || (f.Id >= FirstBotId && !bots.ContainsKey(f.Id)))
                    input = new InputCommand(input.Sequence, Vector2.zero, false, false, input.YawDegrees);
                bootstrap.TrySimulate(f.Motor, input);
            }
            resolver.Step(dt);
            if (playing)
            {
                if (goTick == 0) goTick = tick - (uint)Director.ElapsedTicks;
                foreach (var h in hazardList) h.StepOnHost(Director.ElapsedTicks + 1, tick, dt);
            }

            foreach (var f in fighters.Where(f => !f.Out))
                Director.SampleArena(f.Id, tick, f.Motor.transform.position);
            Director.CompleteTick(tick);

            foreach (var f in fighters.Where(f => !f.Out && !Director.AliveFighters.Contains(f.Id)))
            {
                f.Out = true;
                if (bots.TryGetValue(f.Id, out var eliminatedBot))
                    eliminatedBot.Step(true, playing, false, false, false, f.Motor.transform.position, 0,
                        bounds.transform.position, radius, sensedFighters, dt);
                Sound(Sfx.FallOff, f.Motor.transform.position);
                resolver.Unregister(f.Id);
                fighterPhysics.Remove(f.Id);
                f.Motor.gameObject.SetActive(false);
            }

            Hud.SetClock(Director.ElapsedTicks * dt);
            Hud.SetRemaining(Director.AliveFighters.Count, fighters.Count);
            Hud.SetDash(fighters[0].Motor.DashCooldownTicks * dt);
            foreach (var f in fighters) Hud.SetWobble(f.Id, resolver.GetWobble(f.Id), !f.Out);

            if (tick % snapshotEveryTicks == 0) bootstrap.Sync.BroadcastSnapshot(Snapshot(tick));
        }

        public WorldSnapshot Snapshot(uint tick) => new(tick, goTick, fighters.Select(f =>
        {
            var body = f.Motor.GetComponent<Rigidbody>();
            var flags = (f.Motor.IsDashing ? FighterFlags.Dashing : 0) | (f.Motor.IsStunned ? FighterFlags.Stunned : 0)
                | (f.Out ? FighterFlags.Out : 0) | (Director.IsRespawning(f.Id) ? FighterFlags.Respawning : 0)
                | (Director.IsInvulnerable(f.Id) ? FighterFlags.Invulnerable : 0);
            var wobble = (byte)Mathf.Clamp(resolver.GetWobble(f.Id), 0, byte.MaxValue);
            var lastInput = inboxes.TryGetValue(f.Id, out var inbox) ? inbox.LastProcessed : 0u;
            return new FighterSnapshot(f.Id, f.Fruit, body.position, body.linearVelocity, wobble, (byte)Director.Lives(f.Id),
                flags, lastInput, (ushort)f.Motor.DashCooldownTicks);
        }).ToArray());

        private void OnContact(ulong a, ulong b)
        {
            Director.RecordTouch(a, b, simulationTick);
            Director.RecordTouch(b, a, simulationTick);
        }

        private bool TryRespawn(ulong id)
        {
            if (!bootstrap.Session.CanSimulateGameplay) return false;
            var f = fighters.FirstOrDefault(candidate => candidate.Id == id);
            if (f == null) return false;
            var sphere = f.Motor.GetComponent<SphereCollider>();
            var others = fighters.Where(other => other.Id != id && !other.Out).Select(other => other.Motor.transform.position).ToArray();
            var zones = new List<HazardZone>();
            foreach (var provider in hazardList.OfType<IHazardZones>())
                foreach (var zone in provider.ThreatenedZones(Director.ElapsedTicks, Time.fixedDeltaTime))
                    zones.Add(new HazardZone(zone.Center, zone.Radius + sphere.radius));
            foreach (var ground in hazardList.OfType<MovingGround>())
                foreach (var box in ground.Boxes)
                    zones.Add(new HazardZone(box.bounds.center, new Vector2(box.bounds.extents.x, box.bounds.extents.z).magnitude + sphere.radius));
            foreach (var pusher in hazardList.OfType<PusherBlocks>())
                foreach (var box in pusher.GetComponentsInChildren<BoxCollider>())
                    zones.Add(new HazardZone(box.bounds.center, new Vector2(box.bounds.extents.x, box.bounds.extents.z).magnitude + sphere.radius));
            var safeRadius = RespawnPicker.SafeRadius(bounds.OuterRadius, Director.ElapsedTicks, balance, Time.fixedDeltaTime, ringDrop != null);
            var floorMask = UnityEngine.Physics.DefaultRaycastLayers & ~(1 << CollisionResolver.FighterLayer);
            bool HasFloor(Vector3 point) => UnityEngine.Physics.Raycast(point + Vector3.up * (sphere.radius + 0.1f),
                Vector3.down, out var hit, 2 * (sphere.radius + 0.1f), floorMask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.5f;
            if (!RespawnPicker.TryPick(bounds.transform.position, safeRadius, others, zones, out var position, HasFloor)) return false;
            f.Motor.ResetForRespawn(position + Vector3.up * sphere.radius);
            f.Motor.Invulnerable = true;
            f.Motor.gameObject.SetActive(true);
            f.Out = false;
            resolver.Register(f.Id, f.Motor);
            resolver.ResetWobble(f.Id);
            fighterPhysics.Add(f.Id, f.Motor);
            return true;
        }

        private void RefreshClientHud(WorldSnapshot snapshot)
        {
            if (bootstrap.Session.State != SessionState.Joined) return;
            hudSnapshot = snapshot;
            Hud.SetClock(snapshot.GoTick == 0 ? 0 : (snapshot.HostTick - snapshot.GoTick) / balance.FixedStepHz);
            Hud.SetRemaining(snapshot.Fighters.Count(f => (f.Flags & FighterFlags.Out) == 0), snapshot.Fighters.Count);
            var rule = bootstrap.LobbySelection?.Rule ?? bootstrap.Settings.Rule;
            foreach (var f in snapshot.Fighters)
            {
                Hud.SetWobble(f.Id, f.Wobble, (f.Flags & FighterFlags.Out) == 0);
                if (f.Id == (ushort)Unity.Netcode.NetworkManager.Singleton.LocalClientId)
                {
                    Hud.SetDash(f.DashCooldownTicks / balance.FixedStepHz);
                    overlay?.ShowHud(f.Lives, rule == MatchRule.Rank && !lanSurvival ? balance.Lives : 1);
                }
            }
            // Clients get lives in the snapshot, not points: their board ranks by lives left.
            Hud.SetBoard(snapshot.Fighters.Select(f => (FighterLabel(f.Id), (int)f.Lives, FruitColor(f.Fruit))).ToList());
        }

        private void OnHit(FighterHit hit)
        {
            foreach (var f in fighters)
                if ((f.Id == hit.ReceiverId || f.Id == hit.OtherId) && f.Visual != null) f.Visual.PlaySquash();
            if (hit.ReceiverId == 0 && cameraRig != null) cameraRig.Shake(hit.Impulse);
            var receiver = fighters.FirstOrDefault(f => f.Id == hit.ReceiverId);
            if (sfxPlayer != null && receiver != null) sfxPlayer.PlayHit(hit.Impulse, receiver.Motor.transform.position);
            if ((hit.ReceiverId == 0 || hit.OtherId == 0) && hit.Impulse >= balance.HitThreshold) StartCoroutine(HitStop());
        }

        private void OnMatchEvent(MatchEvent e)
        {
            if (e.Kind == MatchEventKind.Respawn) Sound(Sfx.Respawn, LocalFighterPosition());
            if (overlay == null) return;
            if (e.Kind == MatchEventKind.Countdown)
                overlay.ShowBanner(e.Value == 0 ? "GO!" : e.Value.ToString(), 1f);
            if (e.Kind == MatchEventKind.MatchResult)
            {
                overlay.ShowResult(e.Standings, FighterLabel, FighterFruit, level, networkManager.LocalClientId);
                if (!lanSurvival && leaveMatch != null) overlay.OfferResultAction("Next", leaveMatch);
            }
            if (!lanSurvival || (e.Kind != MatchEventKind.MatchResult && e.Kind != MatchEventKind.SessionResult)) return;

            survivalResult = true;
            localInput.Suspended = true;
            Cursor.lockState = CursorLockMode.None;
            if (e.Kind == MatchEventKind.SessionResult)
            {
                sessionFinished = true;
                overlay.ShowSessionResult(e.Standings, FighterLabel, FighterFruit);
                overlay.OfferResultAction("Về menu", leaveCompletedSession);
            }
        }

        private Color FruitColor(int fruit)
        {
            var definition = fruit >= 0 && fruit < fighterPrefabs.Length
                ? fighterPrefabs[fruit].GetComponent<FighterProfileApplier>()?.Fruit : null;
            return definition != null ? definition.Color : Color.white;
        }

        private int FighterFruit(ulong id)
        {
            if (lanSurvival && bootstrap.LobbySelection != null)
                foreach (var player in bootstrap.LobbySelection.Players)
                    if (player.Id == id) return player.Fruit;
            var hostFighter = fighters.FirstOrDefault(f => f.Id == id);
            return hostFighter != null ? hostFighter.Fruit : (int)(id % 6);
        }

        private string FighterLabel(ulong id)
        {
            if (lanSurvival && bootstrap.LobbySelection != null)
                foreach (var player in bootstrap.LobbySelection.Players)
                    if (player.Id == id) return fighterPrefabs[player.Fruit].name;
            var hostFighter = fighters.FirstOrDefault(f => f.Id == id);
            if (hostFighter != null) return fighterPrefabs[hostFighter.Fruit].name;
            if (clientView != null && clientView.Views.TryGetValue((ushort)id, out var view) && view != null)
                return view.name.Replace("(Clone)", "");
            return $"Fruit {id}";
        }

        private void PlayHazardSound(HazardEvent e)
        {
            switch (e.Kind)
            {
                case HazardEventKind.RingDropWarning: Sound(Sfx.RingDrop, LocalFighterPosition()); break;
                case HazardEventKind.PusherDeparture: Sound(Sfx.PusherHorn, LocalFighterPosition()); break;
                case HazardEventKind.IceTileWarning: Sound(Sfx.IceCrack, LocalFighterPosition()); break;
                case HazardEventKind.CannonAim:
                    foreach (var c in hazardList.OfType<Cannons>())
                        if (e.Index < c.Count) Sound(Sfx.Cannon, c.Muzzle(e.Index));
                    break;
            }
        }

        private Vector3 LocalFighterPosition()
        {
            if (clientView != null)
                return clientView.Views.TryGetValue((ushort)networkManager.LocalClientId, out var v) && v != null
                    ? v.transform.position : Vector3.zero;
            return fighters.Count > 0 ? fighters[0].Motor.transform.position : Vector3.zero;
        }

        // Host: dash starts (motor flag rising edge). Every peer: whoosh when an H07 box comes within 2 m of the own fruit.
        private void UpdateSounds()
        {
            if (sfxPlayer == null) return;
            foreach (var f in fighters)
            {
                if (f.Out) continue;
                if (f.Motor.IsDashing && dashing.Add(f.Id)) Sound(Sfx.Dash, f.Motor.transform.position);
                else if (!f.Motor.IsDashing) dashing.Remove(f.Id);
            }
            var me = LocalFighterPosition();
            foreach (var ground in hazardList.OfType<MovingGround>())
                foreach (var box in ground.Boxes)
                {
                    var near = Vector3.Distance(box.transform.position, me) <= audioWhoosh;
                    if (near && nearBoxes.Add(box)) Sound(Sfx.ObstacleWhoosh, box.transform.position);
                    else if (!near) nearBoxes.Remove(box);
                }
        }

        private float audioWhoosh;
        private float nextBoardAt;
        private const float BoardRefreshSeconds = 0.25f; // display only; the values are already host-confirmed

        public void ConfigureFeedback(float hitStopSeconds, float whooshDistance)
        {
            setupHitStopSeconds = hitStopSeconds;
            audioWhoosh = whooshDistance;
        }

        private void OnHazard(HazardEvent e)
        {
            if ((e.Kind == HazardEventKind.IceTileWarning || e.Kind == HazardEventKind.IceTileFading ||
                e.Kind == HazardEventKind.IceTileSolid) && clientView?.Buffer.Newest is { GoTick: > 0 } iceGo)
                foreach (var ice in hazardList.OfType<IceTiles>())
                    ice.ApplyEvent(e, (e.HostTick - iceGo.GoTick) * Time.fixedDeltaTime);
            // Clients replay pusher departures from the host tick they were scheduled for.
            if (e.Kind == HazardEventKind.PusherDeparture && clientView?.Buffer.Newest is { GoTick: > 0 } newest)
                foreach (var p in hazardList.OfType<PusherBlocks>())
                    p.ApplyDeparture(e.Index, (int)(e.HostTick - newest.GoTick));
            if (e.Kind == HazardEventKind.CannonAim && clientView?.Buffer.Newest is { GoTick: > 0 } go)
                foreach (var c in hazardList.OfType<Cannons>())
                    c.ApplyAim(e.Index, e.Aim, (int)(e.HostTick - go.GoTick));
            PlayHazardSound(e);
            if (e.Kind != HazardEventKind.RingDropWarning || ringDrop == null) return;
            // B07 on L1: one banner per ring drop, under the clock.
            if (overlay != null) overlay.ShowBanner($"Ring drop in {balance.RingDropWarningSeconds:0} s!", bannerSeconds);
            // A client hears the event one trip late: start the telegraph as far in as the host time it renders.
            var elapsed = clientView?.Buffer.Newest != null
                ? RingDrop.TelegraphElapsed(e.HostTick, balance.RingDropWarningSeconds,
                    clientView.Buffer.RenderTime(Time.time), Time.fixedDeltaTime)
                : 0f;
            ringDrop.BeginWarning(e.Index, elapsed);
        }

        private void OnDestroy()
        {
            if (bootstrap != null)
            {
                bootstrap.Session.StateChanged -= OnSessionState;
                bootstrap.Settings.Changed -= ApplySettings;
                bootstrap.Clock.Ticked -= OnTick;
                bootstrap.Events.HazardRaised -= OnHazard;
                bootstrap.Events.MatchRaised -= OnMatchEvent;
                bootstrap.Sync.InputReceived -= OnRemoteInput;
                bootstrap.Sync.SnapshotReceived -= RefreshClientHud;
            }
            if (pausesGame) Time.timeScale = 1f;
            clientView?.Dispose();
            localInput?.Dispose();
            Director?.Dispose();
        }
    }
}
