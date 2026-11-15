using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Hazards
{
    /// <summary>
    /// H03 pusher blocks (GDD 7.2, 6.5): two 1.5 × 1.0 × 0.8 m kinematic blocks on east–west lanes at z = ±1.2 m,
    /// docks at x = ±9.5 m. Block A starts west, B east. A block waits ≥ 2 s at its dock and departs (first at 5 s)
    /// only if the host predicts no H07 box meets it during the pass; its dock light turns amber 1 s before, and a
    /// <see cref="HazardEventKind.PusherDeparture"/> event carries the departure tick. 3 m/s with a 0.3 s ease at
    /// each end. A touched fruit gets 50 N·s · K(h) along the lane and +1 Wobble, once per 0.5 s.
    /// </summary>
    public sealed class PusherBlocks : MonoBehaviour, IHazard
    {
        private const float ContactSkinMeters = 0.05f;
        // Prediction samples the pass this often (seconds); not a balance value.
        private const float PredictionStepSeconds = 0.05f;

        [SerializeField] private BalanceProfile balance;
        [SerializeField] private HazardProfile hazards;
        [Tooltip("The H07 ring whose boxes a departure must not meet.")]
        [SerializeField] private MovingGround ground;
        [SerializeField] private Material blockMaterial;
        [SerializeField] private Color lightIdle = new(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color lightWarning = new(1f, 0.65f, 0f);

        private sealed class Block
        {
            public Rigidbody Body;
            public BoxCollider Collider;
            public Renderer[] Lights;
            public int Side;            // −1 west dock, +1 east dock (where it waits / where it left from)
            public int DockedAtTick;
            public int DepartTick = -1; // playing tick of the current or next departure
            public bool Moving;
        }

        private readonly List<Block> blocks = new();
        private readonly Dictionary<(int, ulong), int> lastHit = new();
        private readonly Collider[] overlaps = new Collider[16];
        private IGameEventSink sink;
        private IFighterPhysics fighters;

        public int Count => blocks.Count;
        public Vector3 BlockPosition(int i) => blocks[i].Body.position;
        public bool IsMoving(int i) => blocks[i].Moving;
        public int DepartTick(int i) => blocks[i].DepartTick;

        private void Awake()
        {
            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? -1 : 1;
                var z = i == 0 ? hazards.PusherLaneZ : -hazards.PusherLaneZ;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Pusher {(char)('A' + i)}";
                go.transform.SetParent(transform, false);
                go.transform.localScale = hazards.PusherSize;
                if (blockMaterial != null) go.GetComponent<Renderer>().sharedMaterial = blockMaterial;
                var body = go.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                var block = new Block
                {
                    Body = body, Collider = go.GetComponent<BoxCollider>(), Side = side,
                    Lights = new[] { DockLight(-1, z), DockLight(1, z) },
                };
                blocks.Add(block);
                go.transform.localPosition = new Vector3(side * hazards.PusherDockX, hazards.PusherSize.y * 0.5f, z);
            }
        }

        private Renderer DockLight(int side, float z)
        {
            var light = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(light.GetComponent<Collider>()); // visual only
            light.name = $"Dock light {(side < 0 ? "W" : "E")} {z:+0.0;-0.0}";
            light.transform.SetParent(transform, false);
            light.transform.localPosition = new Vector3(side * (hazards.PusherDockX + hazards.PusherSize.x), hazards.PusherSize.y, z);
            light.transform.localScale = new Vector3(0.4f, 0.1f, 0.4f);
            return light.GetComponent<Renderer>();
        }

        private void Place(Block b, float x, float z) =>
            b.Body.MovePosition(transform.TransformPoint(new Vector3(x, hazards.PusherSize.y * 0.5f, z)));

        public void Bind(IGameEventSink eventSink, IFighterPhysics fighterPhysics)
        {
            sink = eventSink;
            fighters = fighterPhysics;
        }

        public void StepOnHost(int playingTicks, uint hostTick, float dt)
        {
            for (var i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b.Moving && playingTicks - b.DepartTick >= Mathf.RoundToInt(PassSeconds(hazards) / dt))
                {
                    b.Moving = false;
                    b.Side = -b.Side;
                    b.DockedAtTick = playingTicks;
                    b.DepartTick = -1;
                }

                if (!b.Moving && b.DepartTick < 0)
                {
                    var lead = Mathf.RoundToInt(hazards.PusherLightSeconds / dt);
                    var depart = playingTicks + lead;
                    var earliest = Mathf.Max(Mathf.RoundToInt(hazards.PusherFirstDepartureSeconds / dt),
                        b.DockedAtTick + Mathf.RoundToInt(hazards.PusherDockWaitSeconds / dt));
                    if (depart >= earliest && PassIsClear(i, depart * dt))
                    {
                        b.DepartTick = depart;
                        sink?.Publish(new HazardEvent(sink.NextEventId(), hostTick + (uint)lead,
                            HazardEventKind.PusherDeparture, i, default, -b.Side));
                    }
                }

                if (!b.Moving && b.DepartTick >= 0 && playingTicks >= b.DepartTick) b.Moving = true;
                Pose(i, playingTicks * dt);
            }
            if (fighters != null) HitFighters(playingTicks, dt);
        }

        /// <summary>Clients: a departure heard from the host (playing tick of the departure).</summary>
        public void ApplyDeparture(int block, int departTick)
        {
            if (block < 0 || block >= blocks.Count) return;
            var b = blocks[block];
            if (b.DepartTick == departTick) return;
            // A new departure means the previous pass ended at the other dock.
            if (b.DepartTick >= 0) b.Side = -b.Side;
            b.DepartTick = departTick;
        }

        public void ShowAt(float playingSeconds)
        {
            for (var i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                b.Moving = b.DepartTick >= 0 && playingSeconds >= b.DepartTick * Time.fixedDeltaTime;
                Pose(i, playingSeconds);
            }
        }

        private void Pose(int i, float t)
        {
            var b = blocks[i];
            var z = i == 0 ? hazards.PusherLaneZ : -hazards.PusherLaneZ;
            var dt = Time.fixedDeltaTime;
            var x = b.Side * hazards.PusherDockX;
            if (b.DepartTick >= 0 && t >= b.DepartTick * dt)
                x = PassX(t - b.DepartTick * dt, b.Side, hazards);
            Place(b, x, z);
            var warning = b.DepartTick >= 0 && t < b.DepartTick * dt;
            for (var s = 0; s < 2; s++)
                b.Lights[s].material.color = warning && (s == 0 ? -1 : 1) == b.Side ? lightWarning : lightIdle;
        }

        private bool PassIsClear(int block, float departSeconds)
        {
            if (ground == null) return true;
            var z = block == 0 ? hazards.PusherLaneZ : -hazards.PusherLaneZ;
            var side = blocks[block].Side;
            var size = hazards.PusherSize;
            var box = hazards.ObstacleBoxSize;
            var reach = new Vector2(size.x, size.z).magnitude * 0.5f + new Vector2(box.x, box.z).magnitude * 0.5f;
            for (var tau = 0f; tau <= PassSeconds(hazards); tau += PredictionStepSeconds)
            {
                var p = new Vector2(PassX(tau, side, hazards), z);
                var ring = MovingGround.RingAngle(departSeconds + tau, hazards);
                for (var k = 0; k < hazards.ObstacleBoxCount; k++)
                {
                    var a = (ring + k * 360f / hazards.ObstacleBoxCount) * Mathf.Deg2Rad;
                    var q = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * hazards.ObstacleBoxRadius;
                    if ((p - q).magnitude < reach) return false;
                }
            }
            return true;
        }

        private void HitFighters(int tick, float dt)
        {
            var lockout = Mathf.RoundToInt(hazards.ObstacleRehitSeconds / dt);
            for (var i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (!b.Moving) continue;
                var t = b.Collider.transform;
                var half = Vector3.Scale(t.lossyScale, b.Collider.size) * 0.5f + Vector3.one * ContactSkinMeters;
                var count = UnityEngine.Physics.OverlapBoxNonAlloc(b.Body.position, half, overlaps, t.rotation, ~0,
                    QueryTriggerInteraction.Ignore);
                for (var c = 0; c < count; c++)
                {
                    if (!fighters.TryGetFighter(overlaps[c], out var id)) continue;
                    if (lastHit.TryGetValue((i, id), out var at) && tick - at < lockout) continue;
                    lastHit[(i, id)] = tick;
                    fighters.AddImpulse(id, BlockImpulse(-b.Side, fighters.GetWobble(id), balance, hazards));
                    fighters.AddWobble(id, hazards.ObstacleWobble);
                }
            }
        }

        /// <summary>J_b = 50 N·s · K(h) along the lane (GDD 6.5, H03: no radial term).</summary>
        public static Vector3 BlockImpulse(int travelSign, int wobble, BalanceProfile balance, HazardProfile hazards) =>
            Vector3.right * (travelSign * hazards.ObstacleImpulse * balance.Knockback(wobble));

        /// <summary>Duration of one dock-to-dock pass: two eases plus cruising the rest.</summary>
        public static float PassSeconds(HazardProfile h)
        {
            var easeDistance = h.PusherSpeed * h.PusherEaseSeconds * 0.5f;
            return 2f * h.PusherEaseSeconds + (2f * h.PusherDockX - 2f * easeDistance) / h.PusherSpeed;
        }

        /// <summary>x of a block <paramref name="tau"/> seconds into a pass that left the dock on <paramref name="fromSide"/>.</summary>
        public static float PassX(float tau, int fromSide, HazardProfile h)
        {
            var total = PassSeconds(h);
            var ease = h.PusherEaseSeconds;
            var v = h.PusherSpeed;
            var length = 2f * h.PusherDockX;
            tau = Mathf.Clamp(tau, 0f, total);
            float s;
            if (tau < ease) s = 0.5f * v / ease * tau * tau;
            else if (tau > total - ease)
            {
                var r = total - tau;
                s = length - 0.5f * v / ease * r * r;
            }
            else s = 0.5f * v * ease + v * (tau - ease);
            return fromSide * h.PusherDockX - fromSide * s;
        }
    }
}
