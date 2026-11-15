using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Hazards
{
    /// <summary>
    /// L2 Moving Ground: H02 turntable (radius 4 m, 20°/s, reverses every 15 s with a 2 s ramp, amber arrows 2 s
    /// before) and H07 obstacle ring (4–6.5 m, turning against it at 15°/s, three boxes at 5.9 m). GDD 7.2, 6.5.
    /// Builds its own floor from <see cref="HazardProfile"/>. Both rings are kinematic and report
    /// v_surface = ω × r_c so the motor's traction rule carries fruits. A box touching a fruit gives
    /// J_b = 50 N·s · K(h) · normalize(t̂ + 0.5 r̂) and +1 Wobble, once per 0.5 s per fruit and box.
    /// </summary>
    public sealed class MovingGround : MonoBehaviour, IHazard
    {
        // Detection skin around a box: a fruit resting against it still counts as touching.
        private const float ContactSkinMeters = 0.05f;

        [SerializeField] private BalanceProfile balance;
        [SerializeField] private HazardProfile hazards;
        [SerializeField] private Material turntableMaterial;
        [SerializeField] private Material ringMaterial;
        [SerializeField] private Material boxMaterial;
        [SerializeField] private Color arrowColor = Color.white;
        [SerializeField] private Color arrowWarningColor = new(1f, 0.65f, 0f);

        private Rigidbody turntable;
        private Rigidbody ring;
        private readonly List<BoxCollider> boxes = new();
        private readonly List<Renderer> arrows = new();
        private readonly List<Transform> arrowRoots = new();
        private readonly Dictionary<(int box, ulong fighter), int> lastHit = new();
        private readonly Collider[] overlaps = new Collider[16];
        private IGameEventSink sink;
        private IFighterPhysics fighters;
        private int announcedReversal;

        public Rigidbody Turntable => turntable;
        public Rigidbody Ring => ring;
        public IReadOnlyList<BoxCollider> Boxes => boxes;
        public float Seconds { get; private set; }

        private void Awake()
        {
            turntable = BuildDisc("Turntable", 0f, hazards.TurntableRadius, turntableMaterial);
            ring = BuildDisc("Obstacle Ring", hazards.TurntableRadius, hazards.ObstacleRingOuterRadius, ringMaterial);
            turntable.gameObject.AddComponent<SurfaceMotion>().Init(this, true);
            ring.gameObject.AddComponent<SurfaceMotion>().Init(this, false);

            var size = hazards.ObstacleBoxSize;
            for (var i = 0; i < hazards.ObstacleBoxCount; i++)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = $"Box {i}";
                box.transform.SetParent(ring.transform, false);
                var angle = i * 360f / hazards.ObstacleBoxCount;
                var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                box.transform.localPosition = dir * hazards.ObstacleBoxRadius + Vector3.up * (size.y * 0.5f);
                box.transform.localRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 90f, 0f);
                box.transform.localScale = new Vector3(size.x, size.y, size.z);
                if (boxMaterial != null) box.GetComponent<Renderer>().sharedMaterial = boxMaterial;
                boxes.Add(box.GetComponent<BoxCollider>());
            }

            // Four arrows on the disc showing its turning direction.
            for (var i = 0; i < 4; i++)
            {
                var arrow = new GameObject($"Arrow {i}").transform;
                arrow.SetParent(turntable.transform, false);
                var dir = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward;
                arrow.localPosition = dir * (hazards.TurntableRadius * 0.6f) + Vector3.up * 0.01f;
                arrow.localRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, dir));
                arrowRoots.Add(arrow);
                AddArrowPart(arrow, new Vector3(0.25f, 0.02f, 1f), Vector3.zero);
                AddArrowPart(arrow, new Vector3(0.7f, 0.02f, 0.3f), Vector3.forward * 0.6f);
            }
            Pose(0f);
        }

        private void AddArrowPart(Transform arrow, Vector3 scale, Vector3 position)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(arrow, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            arrows.Add(part.GetComponent<Renderer>());
        }

        private Rigidbody BuildDisc(string name, float inner, float outer, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(Rigidbody));
            go.transform.SetParent(transform, false);
            var mesh = AnnulusMesh.Build(inner, outer, 64);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
            if (material != null) go.GetComponent<MeshRenderer>().sharedMaterial = material;
            var body = go.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            return body;
        }

        public void Bind(IGameEventSink eventSink, IFighterPhysics fighterPhysics)
        {
            sink = eventSink;
            fighters = fighterPhysics;
        }

        public void StepOnHost(int playingTicks, uint hostTick, float dt)
        {
            var t = playingTicks * dt;
            Pose(t);

            // Announce each reversal when its warning starts, carrying the tick the reversal begins.
            var next = announcedReversal + 1;
            var reversalTick = Mathf.RoundToInt(next * hazards.TurntableReversalSeconds / dt);
            if (sink != null && playingTicks >= reversalTick - Mathf.RoundToInt(hazards.TurntableWarningSeconds / dt))
            {
                announcedReversal = next;
                sink.Publish(new HazardEvent(sink.NextEventId(), hostTick + (uint)(reversalTick - playingTicks),
                    HazardEventKind.TurntableReversal, 0, default, next % 2 == 0 ? 1 : -1));
            }

            if (fighters != null) HitFighters(playingTicks, dt);
        }

        public void ShowAt(float playingSeconds) => Pose(playingSeconds);

        private void Pose(float t)
        {
            Seconds = t;
            var rotation = transform.rotation;
            turntable.MoveRotation(rotation * Quaternion.Euler(0f, TurntableAngle(t, hazards), 0f));
            ring.MoveRotation(rotation * Quaternion.Euler(0f, RingAngle(t, hazards), 0f));
            var warning = InReversalWarning(t, hazards);
            foreach (var a in arrows) a.material.color = warning ? arrowWarningColor : arrowColor;
            // Arrows point along the current turning direction.
            var backwards = TurntableDegreesPerSecond(t, hazards) < 0f;
            for (var i = 0; i < arrowRoots.Count; i++)
            {
                var dir = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward;
                var travel = Vector3.Cross(Vector3.up, dir) * (backwards ? -1f : 1f);
                arrowRoots[i].localRotation = Quaternion.LookRotation(travel);
            }
        }

        private void HitFighters(int tick, float dt)
        {
            var lockout = Mathf.RoundToInt(hazards.ObstacleRehitSeconds / dt);
            var omega = RingDegreesPerSecond(Seconds, hazards);
            for (var b = 0; b < boxes.Count; b++)
            {
                var box = boxes[b];
                var t = box.transform;
                var half = Vector3.Scale(t.lossyScale, box.size) * 0.5f + Vector3.one * ContactSkinMeters;
                var count = UnityEngine.Physics.OverlapBoxNonAlloc(t.position, half, overlaps, t.rotation, ~0,
                    QueryTriggerInteraction.Ignore);
                for (var i = 0; i < count; i++)
                {
                    if (!fighters.TryGetFighter(overlaps[i], out var id)) continue;
                    if (lastHit.TryGetValue((b, id), out var at) && tick - at < lockout) continue;
                    lastHit[(b, id)] = tick;
                    var radial = t.position - transform.position;
                    var j = BoxImpulse(radial, omega, fighters.GetWobble(id), balance, hazards);
                    fighters.AddImpulse(id, j);
                    fighters.AddWobble(id, hazards.ObstacleWobble);
                }
            }
        }

        /// <summary>J_b = J · K(h) · normalize(t̂ + k·r̂), t̂ the box's travel direction (GDD 6.5).</summary>
        public static Vector3 BoxImpulse(Vector3 radialFromCentre, float ringDegreesPerSecond, int wobble,
            BalanceProfile balance, HazardProfile hazards)
        {
            var r = new Vector3(radialFromCentre.x, 0f, radialFromCentre.z).normalized;
            // Positive yaw rate turns clockwise seen from above: travel = ŷ × r̂ · sign(ω) in Unity's left-handed frame.
            var travel = Vector3.Cross(Vector3.up, r) * Mathf.Sign(ringDegreesPerSecond);
            return hazards.ObstacleImpulse * balance.Knockback(wobble) *
                (travel + hazards.ObstacleRadialShare * r).normalized;
        }

        /// <summary>Turntable yaw rate: +ω, reversing every period with a linear ramp that starts at each reversal.</summary>
        public static float TurntableDegreesPerSecond(float t, HazardProfile h)
        {
            var w = h.TurntableDegreesPerSecond;
            var k = Mathf.FloorToInt(t / h.TurntableReversalSeconds);
            var sign = k % 2 == 0 ? 1f : -1f;
            var into = t - k * h.TurntableReversalSeconds;
            if (k == 0 || into >= h.TurntableRampSeconds) return sign * w;
            return Mathf.Lerp(-sign * w, sign * w, into / h.TurntableRampSeconds);
        }

        /// <summary>Integral of the yaw rate: the linear ramp from −ω to +ω adds no angle.</summary>
        public static float TurntableAngle(float t, HazardProfile h)
        {
            var w = h.TurntableDegreesPerSecond;
            var period = h.TurntableReversalSeconds;
            var ramp = h.TurntableRampSeconds;
            var k = Mathf.FloorToInt(t / period);
            var angle = 0f;
            for (var j = 0; j < k; j++) angle += (j % 2 == 0 ? 1f : -1f) * w * (j == 0 ? period : period - ramp);
            var into = t - k * period;
            var sign = k % 2 == 0 ? 1f : -1f;
            if (k == 0) return angle + w * into;
            if (into < ramp)
            {
                // ∫ of a line from −sign·w to +sign·w over [0, into].
                var x = into / ramp;
                return angle + sign * w * ramp * (x * x - x);
            }
            return angle + sign * w * (into - ramp);
        }

        public static float RingDegreesPerSecond(float t, HazardProfile h) =>
            -TurntableDegreesPerSecond(t, h) * h.ObstacleRingDegreesPerSecond / h.TurntableDegreesPerSecond;

        public static float RingAngle(float t, HazardProfile h) =>
            -TurntableAngle(t, h) * h.ObstacleRingDegreesPerSecond / h.TurntableDegreesPerSecond;

        public static bool InReversalWarning(float t, HazardProfile h)
        {
            var next = (Mathf.FloorToInt(t / h.TurntableReversalSeconds) + 1) * h.TurntableReversalSeconds;
            return next - t <= h.TurntableWarningSeconds;
        }

        /// <summary>v = ω × r_c with ω about +y in rad/s (GDD 6.5).</summary>
        public static Vector3 SurfaceVelocity(float degreesPerSecond, Vector3 offsetFromCentre)
        {
            var omega = Vector3.up * (degreesPerSecond * Mathf.Deg2Rad);
            var r = new Vector3(offsetFromCentre.x, 0f, offsetFromCentre.z);
            return Vector3.Cross(omega, r);
        }

        private sealed class SurfaceMotion : MonoBehaviour, ISurfaceMotion
        {
            private MovingGround owner;
            private bool isTurntable;

            public void Init(MovingGround ground, bool turntable)
            {
                owner = ground;
                isTurntable = turntable;
            }

            public Vector3 VelocityAt(Vector3 worldPoint)
            {
                var rate = isTurntable
                    ? TurntableDegreesPerSecond(owner.Seconds, owner.hazards)
                    : RingDegreesPerSecond(owner.Seconds, owner.hazards);
                return SurfaceVelocity(rate, worldPoint - owner.transform.position);
            }
        }
    }
}
