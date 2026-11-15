using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using SquishyBump.Physics;
using SquishyBump.Presentation;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>
    /// Host loop of one arena (GDD 10.4). Each host tick: every fighter's motor steps from its command,
    /// then the collision resolver, then the level's hazards, then ring-out sampling, then the match tick.
    /// Wires Physics, Hazards, Match and Presentation through events; holds no rules of its own.
    /// Fighter 0 is this peer's player, the rest idle until bots (SB-34) and remote input (SB-21) land.
    /// </summary>
    public sealed class MatchHost : MonoBehaviour
    {
        [SerializeField] private GameplayBootstrap bootstrap;
        [SerializeField] private BalanceProfile balance;
        [SerializeField] private TrainingRingBounds bounds;
        [Tooltip("Optional: the level's H01 floor.")]
        [SerializeField] private RingDrop ringDrop;
        [SerializeField] private CameraRig cameraRig;
        [Tooltip("Roster, player first.")]
        [SerializeField] private GameObject[] fighterPrefabs;
        [SerializeField] private int level = 1;
        [SerializeField] private uint seed;
        [Tooltip("GDD 3.6.")]
        [SerializeField] private int countdownSeconds = 3;
        [Tooltip("GDD 3.6: result confirmed this long after the last elimination.")]
        [SerializeField] private float resultConfirmSeconds = 0.5f;
        [Tooltip("Start a Single Player host session if none is running.")]
        [SerializeField] private bool autoHost = true;

        private readonly List<Fighter> fighters = new();
        private CollisionResolver resolver;
        private LocalPlayerInput localInput;

        public MatchDirector Director { get; private set; }
        public IReadOnlyList<Fighter> Fighters => fighters;

        public sealed class Fighter
        {
            public ulong Id;
            public PlayerMotor Motor;
            public RollVisual Visual;
            public bool Out;
        }

        private void Start()
        {
            if (bootstrap == null) bootstrap = FindAnyObjectByType<GameplayBootstrap>();
            if (autoHost && bootstrap.Session.State == SessionState.Idle) bootstrap.Session.Host(SessionMode.SinglePlayer);

            resolver = gameObject.AddComponent<CollisionResolver>();
            resolver.Configure(balance);
            var spawns = bounds.SpawnPoints;
            for (var i = 0; i < fighterPrefabs.Length; i++)
            {
                var spawn = spawns[i % spawns.Count];
                var go = Instantiate(fighterPrefabs[i], spawn.position + Vector3.up * SpawnLift(fighterPrefabs[i]), Quaternion.identity);
                var f = new Fighter { Id = (ulong)i, Motor = go.GetComponent<PlayerMotor>(), Visual = go.GetComponentInChildren<RollVisual>() };
                fighters.Add(f);
                resolver.Register(f.Id, f.Motor);
            }

            Director = new MatchDirector(() => bootstrap.Session.CanSimulateGameplay, fighters.Select(f => f.Id), level, seed,
                Mathf.RoundToInt(1f / Time.fixedDeltaTime), countdownSeconds, resultConfirmSeconds,
                Mathf.RoundToInt(balance.TimeLimitSeconds), bounds);

            if (cameraRig != null) cameraRig.Follow(fighters[0].Motor.transform);
            localInput = new LocalPlayerInput(cameraRig);

            resolver.WobbleChanged += Director.SetWobble;
            resolver.Hit += OnHit;
            if (ringDrop != null) ringDrop.Bind(bootstrap.EventSink);
            bootstrap.Events.HazardRaised += OnHazard;
            bootstrap.Clock.Ticked += OnTick;
        }

        private static float SpawnLift(GameObject prefab) => prefab.GetComponent<SphereCollider>().radius;

        private void Update() => localInput?.UpdateLook();

        private void OnTick(uint tick)
        {
            var dt = Time.fixedDeltaTime;
            if (Director.Phase == MatchPhase.Intro) Director.FinishIntro(tick);
            var playing = Director.Phase == MatchPhase.Playing;
            var command = localInput.Sample();

            foreach (var f in fighters)
            {
                if (f.Out) continue;
                // Countdown and idle roster slots send an empty command; motors still integrate gravity and traction.
                var input = playing && f.Id == 0 ? command : new InputCommand(command.Sequence, Vector2.zero, false, false, command.YawDegrees);
                bootstrap.TrySimulate(f.Motor, input);
            }
            resolver.Step(dt);
            if (playing && ringDrop != null) ringDrop.StepOnHost(Director.ElapsedTicks + 1, tick, dt);

            foreach (var f in fighters.Where(f => !f.Out))
                Director.SampleArena(f.Id, tick, f.Motor.transform.position);
            Director.CompleteTick(tick);

            foreach (var f in fighters.Where(f => !f.Out && !Director.AliveFighters.Contains(f.Id)))
            {
                f.Out = true;
                resolver.Unregister(f.Id);
            }
        }

        private void OnHit(FighterHit hit)
        {
            foreach (var f in fighters)
                if ((f.Id == hit.ReceiverId || f.Id == hit.OtherId) && f.Visual != null) f.Visual.PlaySquash();
            if (hit.ReceiverId == 0 && cameraRig != null) cameraRig.Shake(hit.Impulse);
        }

        private void OnHazard(HazardEvent e)
        {
            if (e.Kind == HazardEventKind.RingDropWarning && ringDrop != null) ringDrop.BeginWarning(e.Index);
        }

        private void OnDestroy()
        {
            if (bootstrap != null)
            {
                bootstrap.Clock.Ticked -= OnTick;
                bootstrap.Events.HazardRaised -= OnHazard;
            }
            localInput?.Dispose();
            Director?.Dispose();
        }
    }
}
