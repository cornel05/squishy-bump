using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using SquishyBump.Net.Sync;
using SquishyBump.Physics;
using SquishyBump.Presentation;
using SquishyBump.UI.Menus;
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
        private int snapshotEveryTicks;
        private PauseMenu pause;
        private readonly List<IHazard> hazardList = new();
        private FighterPhysicsAdapter fighterPhysics;
        private uint goTick;
        private bool pausesGame;

        public MatchDirector Director { get; private set; }
        public IReadOnlyList<Fighter> Fighters => fighters;
        public MatchClientView ClientView => clientView;
        public bool Started => Director != null;
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

        /// <summary>Esc menu: pauses in Single Player, overlay only in Multiplayer (GDD 3.4).</summary>
        public void AttachPause(PauseMenu menu, bool singlePlayer, System.Action leave)
        {
            pause = menu;
            pausesGame = singlePlayer;
            pause.OpenChanged += OnPauseChanged;
            pause.LeaveRequested += leave;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnPauseChanged(bool open)
        {
            localInput.Suspended = open;
            if (pausesGame) Time.timeScale = open ? 0f : 1f;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
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
            public bool Out;
        }

        private void Start()
        {
            if (bootstrap == null) bootstrap = FindAnyObjectByType<GameplayBootstrap>();
            localInput = new LocalPlayerInput(cameraRig);
            // Every hazard of this level scene (H01 floor, moving ground, cannons…).
            foreach (var root in gameObject.scene.GetRootGameObjects())
                hazardList.AddRange(root.GetComponentsInChildren<IHazard>(true));
            if (ringDrop != null && !hazardList.Contains(ringDrop)) hazardList.Add(ringDrop);
            if (ringDrop == null) ringDrop = hazardList.OfType<RingDrop>().FirstOrDefault();
            ApplySettings(bootstrap.Settings);
            bootstrap.Settings.Changed += ApplySettings;
            bootstrap.Session.StateChanged += OnSessionState;
            bootstrap.Events.HazardRaised += OnHazard;
            if (autoHost && bootstrap.Session.State == SessionState.Idle) bootstrap.Session.Host(SessionMode.SinglePlayer);
            if (autoHost && bootstrap.Session.State == SessionState.Hosting) StartMatch();
            if (bootstrap.Session.State == SessionState.Joined) OnSessionState(SessionState.Joined);
        }

        private void OnSessionState(SessionState state)
        {
            if (state == SessionState.Joined && clientView == null)
                clientView = new MatchClientView(bootstrap, fighterPrefabs, cameraRig, localInput, balance);
        }

        /// <summary>Host: spawns the roster (host, connected clients, idle slots) and starts the countdown.</summary>
        public void StartMatch()
        {
            if (Started || !bootstrap.Session.CanSimulateGameplay) return;
            resolver = gameObject.AddComponent<CollisionResolver>();
            resolver.Configure(balance);
            fighterPhysics = new FighterPhysicsAdapter(resolver);
            snapshotEveryTicks = Mathf.Max(1, Mathf.RoundToInt(balance.InputRateHz / balance.SnapshotRateHz));

            var humans = new List<ulong> { 0 };
            humans.AddRange(bootstrap.Session.Peers.Take(fighterPrefabs.Length - 1));
            var spawns = bounds.SpawnPoints;
            for (var i = 0; i < fighterPrefabs.Length; i++)
            {
                var id = i < humans.Count ? (ushort)humans[i] : (ushort)(FirstBotId + i);
                var spawn = spawns[i % spawns.Count];
                var go = Instantiate(fighterPrefabs[i], spawn.position + Vector3.up * SpawnLift(fighterPrefabs[i]), Quaternion.identity);
                var f = new Fighter
                {
                    Id = id, Fruit = (byte)i, Motor = go.GetComponent<PlayerMotor>(),
                    Visual = go.GetComponentInChildren<RollVisual>(),
                };
                fighters.Add(f);
                resolver.Register(f.Id, f.Motor);
                fighterPhysics.Add(f.Id, f.Motor);
                if (i > 0 && i < humans.Count) inboxes[id] = new InputInbox();
            }

            Director = new MatchDirector(() => bootstrap.Session.CanSimulateGameplay, fighters.Select(f => (ulong)f.Id), level,
                seed, Mathf.RoundToInt(1f / Time.fixedDeltaTime), countdownSeconds, resultConfirmSeconds,
                Mathf.RoundToInt(balance.TimeLimitSeconds), bounds);

            if (cameraRig != null) cameraRig.Follow(fighters[0].Motor.transform);
            resolver.WobbleChanged += Director.SetWobble;
            resolver.Hit += OnHit;
            foreach (var h in hazardList) h.Bind(bootstrap.EventSink, fighterPhysics);
            bootstrap.Sync.InputReceived += OnRemoteInput;
            bootstrap.Clock.Ticked += OnTick;
        }

        private static float SpawnLift(GameObject prefab) => prefab.GetComponent<SphereCollider>().radius;

        private void Update()
        {
            if (StartWhenReady && !Started) StartMatch();
            if (pause != null && localInput != null && localInput.MenuPressed) pause.Toggle();
            localInput?.UpdateLook();
            clientView?.Update(Time.time);
            if (clientView != null && clientView.TryPlayingSeconds(Time.time, out var seconds))
                foreach (var h in hazardList) h.ShowAt(seconds);
        }

        private void FixedUpdate() => clientView?.FixedUpdate();

        // Host only: a client's commands steer only that client's own fighter.
        private void OnRemoteInput(ulong client, IReadOnlyList<InputCommand> commands)
        {
            if (inboxes.TryGetValue(client, out var inbox)) inbox.Offer(commands);
        }

        private void OnTick(uint tick)
        {
            var dt = Time.fixedDeltaTime;
            if (Director.Phase == MatchPhase.Intro) Director.FinishIntro(tick);
            var playing = Director.Phase == MatchPhase.Playing;
            var local = localInput.Sample();

            foreach (var f in fighters)
            {
                if (f.Out) continue;
                var input = f.Id == 0 ? local : inboxes.TryGetValue(f.Id, out var inbox) ? inbox.Take() : default;
                // Countdown and idle slots send an empty command; motors still integrate gravity and traction.
                if (!playing || f.Id >= FirstBotId)
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
                resolver.Unregister(f.Id);
                fighterPhysics.Remove(f.Id);
            }

            if (tick % snapshotEveryTicks == 0) bootstrap.Sync.BroadcastSnapshot(Snapshot(tick));
        }

        public WorldSnapshot Snapshot(uint tick) => new(tick, goTick, fighters.Select(f =>
        {
            var body = f.Motor.GetComponent<Rigidbody>();
            var flags = (f.Motor.IsDashing ? FighterFlags.Dashing : 0) | (f.Motor.IsStunned ? FighterFlags.Stunned : 0)
                | (f.Out ? FighterFlags.Out : 0);
            var wobble = (byte)Mathf.Clamp(resolver.GetWobble(f.Id), 0, byte.MaxValue);
            var lastInput = inboxes.TryGetValue(f.Id, out var inbox) ? inbox.LastProcessed : 0u;
            return new FighterSnapshot(f.Id, f.Fruit, body.position, body.linearVelocity, wobble, (byte)(f.Out ? 0 : 1),
                flags, lastInput);
        }).ToArray());

        private void OnHit(FighterHit hit)
        {
            foreach (var f in fighters)
                if ((f.Id == hit.ReceiverId || f.Id == hit.OtherId) && f.Visual != null) f.Visual.PlaySquash();
            if (hit.ReceiverId == 0 && cameraRig != null) cameraRig.Shake(hit.Impulse);
        }

        private void OnHazard(HazardEvent e)
        {
            // Clients replay pusher departures from the host tick they were scheduled for.
            if (e.Kind == HazardEventKind.PusherDeparture && clientView?.Buffer.Newest is { GoTick: > 0 } newest)
                foreach (var p in hazardList.OfType<PusherBlocks>())
                    p.ApplyDeparture(e.Index, (int)(e.HostTick - newest.GoTick));
            if (e.Kind != HazardEventKind.RingDropWarning || ringDrop == null) return;
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
                bootstrap.Sync.InputReceived -= OnRemoteInput;
            }
            if (pausesGame) Time.timeScale = 1f;
            clientView?.Dispose();
            localInput?.Dispose();
            Director?.Dispose();
        }
    }
}
