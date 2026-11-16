using System;
using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Hazards
{
    public enum CannonKind { Air, Fire }

    /// <summary>
    /// H04 air and H05 fire cannons on the rim (GDD 7.3, 6.5). Each cannon sweeps ±60° around its home direction at
    /// 30°/s and fires every period (5 s; 4 s on Chaos Core), staggered by 1.25 s per cannon. 1.0 s before a shot
    /// it locks its aim and glows; a reliable <see cref="HazardEventKind.CannonAim"/> event carries the aim point and
    /// the fire tick, so clients pose every shot from (fire tick, aim).
    /// Air: cone 15° / 10 m, 0.6 s of F = F₀(1 − d/L)·K(h), +1 Wobble once the gust's J reaches the hit threshold.
    /// Fire: lobbed at 45° to the fruit nearest its sweep line at 10–16 m (else a floor point 13 m out), integrated
    /// with fixed-step gravity; blast R 3 m, J = J₀(1 − d/R)·K(h) outward with 30% up, +2 Wobble; burning patch
    /// 2 m for 3 s, +1 Wobble per full second inside.
    /// </summary>
    public sealed class Cannons : MonoBehaviour, IHazard
    {
        [Serializable]
        public struct Mount
        {
            public CannonKind Kind;
            [Tooltip("Compass angle of the cannon from the centre: 0 = +z (north), 90 = +x (east).")]
            public float AngleDegrees;
        }

        [SerializeField] private BalanceProfile balance;
        [SerializeField] private HazardProfile hazards;
        [SerializeField] private Mount[] mounts = Array.Empty<Mount>();
        [Tooltip("Chaos Core uses the faster period.")]
        [SerializeField] private bool chaosPeriod;
        [SerializeField] private Color idleColor = new(0.35f, 0.35f, 0.4f);
        [SerializeField] private Color patchColor = new(1f, 0.25f, 0.05f, 0.7f);

        private sealed class Cannon
        {
            public Mount Mount;
            public Vector3 Muzzle;
            public Transform Barrel;
            public Renderer BarrelRenderer;
            public Renderer Cone, Marker, Patch, Ball;
            public int Shot;               // index of the next shot
            public int FireTick = -1;      // playing tick of the locked shot, −1 none
            public Vector3 Aim;            // floor point (fire) or end of the cone axis (air)
            // Host simulation of the current shot.
            public readonly Dictionary<ulong, float> Gust = new();
            public bool Flying;
            public Vector3 BallPosition, BallVelocity;
            public int PatchUntilTick = -1;
            public Vector3 PatchCentre;
            public readonly Dictionary<ulong, int> PatchTicks = new();
        }

        private readonly List<Cannon> cannons = new();
        private IGameEventSink sink;
        private IFighterPhysics fighters;
        private float period;

        public int Count => cannons.Count;
        public CannonKind Kind(int i) => cannons[i].Mount.Kind;
        public Vector3 Muzzle(int i) => cannons[i].Muzzle;
        public Vector3 Aim(int i) => cannons[i].Aim;
        public int LockedFireTick(int i) => cannons[i].FireTick;
        public bool BallFlying(int i) => cannons[i].Flying;
        public Vector3 BallPosition(int i) => cannons[i].BallPosition;

        /// <summary>For cannons built at runtime: call on an inactive object, before Awake.</summary>
        public void Configure(BalanceProfile balanceProfile, HazardProfile hazardProfile, Mount[] layout, bool chaos)
        {
            balance = balanceProfile;
            hazards = hazardProfile;
            mounts = layout;
            chaosPeriod = chaos;
        }

        private void Awake()
        {
            period = chaosPeriod ? hazards.CannonChaosPeriodSeconds : hazards.CannonPeriodSeconds;
            foreach (var m in mounts)
            {
                var c = new Cannon { Mount = m };
                c.Muzzle = transform.TransformPoint(Compass(m.AngleDegrees) * hazards.CannonRimRadius
                    + Vector3.up * hazards.CannonMuzzleHeight);
                var basePart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basePart.name = $"{m.Kind} cannon {m.AngleDegrees}°";
                basePart.transform.SetParent(transform, false);
                basePart.transform.position = c.Muzzle - Vector3.up * (hazards.CannonMuzzleHeight * 0.5f);
                basePart.transform.localScale = new Vector3(1.2f, hazards.CannonMuzzleHeight * 0.5f, 1.2f);
                var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(barrel.GetComponent<Collider>()); // visual only: bases are off the floor
                barrel.transform.SetParent(basePart.transform.parent, false);
                barrel.transform.position = c.Muzzle;
                barrel.transform.localScale = new Vector3(0.5f, 0.5f, 1.4f);
                c.Barrel = barrel.transform;
                c.BarrelRenderer = barrel.GetComponent<Renderer>();
                if (m.Kind == CannonKind.Air) c.Cone = Flat("Gust cone", ConeMesh(), TelegraphColors.AmberZone);
                else
                {
                    // Ring with a cross (GDD 12.5), red blinking at 2 Hz while the fireball flies.
                    c.Marker = Flat("Landing marker", AnnulusMesh.Build(hazards.BlastRadius * 0.85f, hazards.BlastRadius, 48), TelegraphColors.Red);
                    foreach (var yaw in new[] { 45f, -45f })
                    {
                        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        DestroyImmediate(bar.GetComponent<Collider>()); // visual only
                        bar.transform.SetParent(c.Marker.transform, false);
                        bar.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                        bar.transform.localScale = new Vector3(0.15f, 0.01f, hazards.BlastRadius * 1.7f);
                        bar.GetComponent<Renderer>().material.color = TelegraphColors.Red;
                    }
                    c.Patch = Flat("Burning patch", AnnulusMesh.Build(0f, hazards.PatchRadius, 48), patchColor);
                    var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    DestroyImmediate(ball.GetComponent<Collider>());
                    ball.name = "Fireball";
                    ball.transform.SetParent(transform, false);
                    ball.transform.localScale = Vector3.one * 0.6f;
                    c.Ball = ball.GetComponent<Renderer>();
                    c.Ball.material.color = patchColor;
                }
                cannons.Add(c);
            }
            ShowAt(0f);
        }

        private Renderer Flat(string name, Mesh mesh, Color color)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.material = new Material(Shader.Find("Sprites/Default")) { color = color };
            return r;
        }

        // Fan of the gust cone on the floor, apex at the origin, pointing +z.
        private Mesh ConeMesh()
        {
            const int segments = 16;
            var v = new Vector3[segments + 2];
            var tri = new int[segments * 3];
            for (var i = 0; i <= segments; i++)
            {
                var a = Mathf.Lerp(-hazards.AirConeHalfAngle, hazards.AirConeHalfAngle, i / (float)segments) * Mathf.Deg2Rad;
                v[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * hazards.AirRange;
            }
            for (var i = 0; i < segments; i++)
            {
                tri[i * 3] = 0;
                tri[i * 3 + 1] = i + 1;
                tri[i * 3 + 2] = i + 2;
            }
            var mesh = new Mesh { vertices = v, triangles = tri };
            mesh.RecalculateNormals();
            return mesh;
        }

        public void Bind(IGameEventSink eventSink, IFighterPhysics fighterPhysics)
        {
            sink = eventSink;
            fighters = fighterPhysics;
        }

        public void StepOnHost(int playingTicks, uint hostTick, float dt)
        {
            var lead = Mathf.RoundToInt(hazards.CannonTelegraphSeconds / dt);
            for (var i = 0; i < cannons.Count; i++)
            {
                var c = cannons[i];
                var next = FireTick(i, c.Shot, period, hazards.CannonStaggerSeconds, dt);
                if (c.FireTick != next && playingTicks >= next - lead)
                {
                    c.FireTick = next;
                    c.Aim = c.Mount.Kind == CannonKind.Air
                        ? c.Muzzle + SweepDirection(c, playingTicks * dt) * hazards.AirRange
                        : PickTarget(c, playingTicks * dt);
                    sink?.Publish(new HazardEvent(sink.NextEventId(), hostTick + (uint)(next - playingTicks),
                        HazardEventKind.CannonAim, i, c.Aim));
                }

                if (playingTicks == c.FireTick)
                {
                    c.Shot++;
                    sink?.Publish(new HazardEvent(sink.NextEventId(), hostTick, HazardEventKind.CannonFire, i, c.Aim));
                    if (c.Mount.Kind == CannonKind.Air) c.Gust.Clear();
                    else Launch(c);
                }

                if (fighters != null)
                {
                    if (c.Mount.Kind == CannonKind.Air) Gust(c, playingTicks, dt);
                    else Fireball(c, playingTicks, dt);
                }
            }
            ShowAt(playingTicks * dt);
        }

        /// <summary>Clients: a shot heard from the host (fire tick in playing ticks).</summary>
        public void ApplyAim(int cannon, Vector3 aim, int fireTick)
        {
            if (cannon < 0 || cannon >= cannons.Count) return;
            cannons[cannon].Aim = aim;
            cannons[cannon].FireTick = fireTick;
        }

        private void Gust(Cannon c, int tick, float dt)
        {
            var gustTicks = Mathf.RoundToInt(hazards.AirGustSeconds / dt);
            if (c.FireTick < 0 || tick < c.FireTick || tick >= c.FireTick + gustTicks) return;
            var axis = Flat(c.Aim - c.Muzzle).normalized;
            foreach (var id in fighters.FighterIds)
            {
                var to = Flat(fighters.GetPosition(id) - c.Muzzle);
                var d = to.magnitude;
                if (d > hazards.AirRange || Vector3.Angle(axis, to) > hazards.AirConeHalfAngle) continue;
                var j = GustForce(d, fighters.GetWobble(id), balance, hazards) * dt;
                fighters.AddImpulse(id, axis * j);
                c.Gust.TryGetValue(id, out var total);
                // +1 Wobble once this gust's impulse reaches the hit threshold.
                if (total < balance.HitThreshold && total + j >= balance.HitThreshold) fighters.AddWobble(id, 1);
                c.Gust[id] = total + j;
            }
        }

        private void Launch(Cannon c)
        {
            c.Flying = true;
            c.BallPosition = c.Muzzle;
            c.BallVelocity = LaunchVelocity(c.Muzzle, c.Aim, hazards.FireLaunchAngle, balance.Gravity);
        }

        private void Fireball(Cannon c, int tick, float dt)
        {
            if (c.Flying)
            {
                c.BallVelocity += Vector3.down * (balance.Gravity * dt);
                c.BallPosition += c.BallVelocity * dt;
                if (c.BallPosition.y <= c.Aim.y)
                {
                    c.Flying = false;
                    Blast(c.BallPosition);
                    c.PatchCentre = new Vector3(c.BallPosition.x, c.Aim.y, c.BallPosition.z);
                    c.PatchUntilTick = tick + Mathf.RoundToInt(hazards.PatchSeconds / dt);
                    c.PatchTicks.Clear();
                }
            }

            if (tick >= c.PatchUntilTick) return;
            var second = Mathf.RoundToInt(1f / dt);
            foreach (var id in fighters.FighterIds)
            {
                if (Flat(fighters.GetPosition(id) - c.PatchCentre).magnitude > hazards.PatchRadius)
                {
                    c.PatchTicks.Remove(id);
                    continue;
                }
                c.PatchTicks.TryGetValue(id, out var inside);
                c.PatchTicks[id] = ++inside;
                if (inside % second == 0) fighters.AddWobble(id, 1);
            }
        }

        private void Blast(Vector3 at)
        {
            foreach (var id in fighters.FighterIds)
            {
                var offset = Flat(fighters.GetPosition(id) - at);
                var d = offset.magnitude;
                if (d > hazards.BlastRadius) continue;
                fighters.AddImpulse(id, BlastImpulse(offset, fighters.GetWobble(id), balance, hazards));
                fighters.AddWobble(id, hazards.BlastWobble);
            }
        }

        private Vector3 PickTarget(Cannon c, float t)
        {
            var sweep = SweepDirection(c, t);
            var best = float.PositiveInfinity;
            var bestId = ulong.MaxValue;
            var target = c.Muzzle + sweep * hazards.FireFallbackRange;
            if (fighters != null)
                foreach (var id in fighters.FighterIds)
                {
                    var p = fighters.GetPosition(id);
                    var to = Flat(p - c.Muzzle);
                    if (to.magnitude < hazards.FireMinRange || to.magnitude > hazards.FireMaxRange) continue;
                    var offLine = Vector3.Cross(sweep, to).magnitude;
                    // Ties go to the lower fighter id, so the choice is deterministic on the host.
                    if (offLine < best || Mathf.Approximately(offLine, best) && id < bestId)
                    {
                        best = offLine;
                        bestId = id;
                        target = p;
                    }
                }
            return new Vector3(target.x, transform.position.y, target.z);
        }

        public void ShowAt(float t)
        {
            var dt = Time.fixedDeltaTime;
            foreach (var c in cannons)
            {
                var fireAt = c.FireTick * dt;
                var locked = c.FireTick >= 0 && t >= fireAt - hazards.CannonTelegraphSeconds;
                var telegraph = locked && t < fireAt;
                var gusting = c.Mount.Kind == CannonKind.Air && locked && t >= fireAt && t < fireAt + hazards.AirGustSeconds;
                var dir = locked && (telegraph || gusting) ? Flat(c.Aim - c.Muzzle).normalized : SweepDirection(c, t);
                if (c.Mount.Kind == CannonKind.Fire && locked && t >= fireAt) dir = Flat(c.Aim - c.Muzzle).normalized;
                c.Barrel.rotation = Quaternion.LookRotation(dir);
                c.BarrelRenderer.material.color = telegraph ? TelegraphColors.Amber : idleColor;

                if (c.Cone != null)
                {
                    c.Cone.enabled = telegraph || gusting;
                    c.Cone.transform.SetPositionAndRotation(new Vector3(c.Muzzle.x, transform.position.y + 0.02f, c.Muzzle.z),
                        Quaternion.LookRotation(dir));
                }
                if (c.Marker == null) continue;
                var flight = FlightSeconds(c.Muzzle, c.Aim, hazards.FireLaunchAngle, balance.Gravity);
                var inFlight = locked && t >= fireAt && t < fireAt + flight;
                var burning = locked && t >= fireAt + flight && t < fireAt + flight + hazards.PatchSeconds;
                var blinkOn = inFlight && RingDrop.BlinkOn(t - fireAt, hazards.TelegraphBlinkHz);
                c.Marker.enabled = blinkOn;
                foreach (var bar in c.Marker.GetComponentsInChildren<Renderer>())
                    if (bar != c.Marker) bar.enabled = blinkOn;
                c.Marker.transform.position = c.Aim + Vector3.up * 0.03f;
                c.Patch.enabled = burning;
                c.Patch.transform.position = c.Aim + Vector3.up * 0.02f;
                c.Ball.enabled = inFlight;
                if (inFlight)
                {
                    var v = LaunchVelocity(c.Muzzle, c.Aim, hazards.FireLaunchAngle, balance.Gravity);
                    var tau = t - fireAt;
                    c.Ball.transform.position = c.Muzzle + v * tau + 0.5f * balance.Gravity * tau * tau * Vector3.down;
                }
            }
        }

        private Vector3 SweepDirection(Cannon c, float t)
        {
            var home = -Compass(c.Mount.AngleDegrees);
            var yaw = SweepOffset(t, hazards);
            return Quaternion.Euler(0f, yaw, 0f) * home;
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);

        public static Vector3 Compass(float degrees)
        {
            var a = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        /// <summary>Triangle wave: 0 → +A → −A → 0 at the sweep rate (A = 60°, 30°/s: an 8 s cycle).</summary>
        public static float SweepOffset(float t, HazardProfile h)
        {
            var a = h.CannonSweepDegrees;
            var cycle = 4f * a / h.CannonSweepDegreesPerSecond;
            var x = Mathf.Repeat(t, cycle) / cycle; // 0..1
            return x < 0.25f ? 4f * a * x : x < 0.75f ? a - 4f * a * (x - 0.25f) : -a + 4f * a * (x - 0.75f);
        }

        /// <summary>Playing tick of shot <paramref name="shot"/> of cannon <paramref name="index"/>: one period, then staggered.</summary>
        public static int FireTick(int index, int shot, float period, float stagger, float dt) =>
            Mathf.RoundToInt((period * (shot + 1) + stagger * index) / dt);

        /// <summary>F(d) = F₀ · (1 − d/L) · K(h) (GDD 6.5).</summary>
        public static float GustForce(float distance, int wobble, BalanceProfile b, HazardProfile h) =>
            h.AirForce * Mathf.Max(0f, 1f - distance / h.AirRange) * b.Knockback(wobble);

        /// <summary>J(d) = J₀ · (1 − d/R_b) · K(h), outward with a 30% upward share (GDD 6.5).</summary>
        public static Vector3 BlastImpulse(Vector3 offsetFromBlast, int wobble, BalanceProfile b, HazardProfile h)
        {
            var flat = Flat(offsetFromBlast);
            var d = flat.magnitude;
            var outward = d > 1e-4f ? flat / d : Vector3.zero;
            var dir = (outward + Vector3.up * h.BlastUpShare).normalized;
            return dir * (h.BlastImpulse * Mathf.Max(0f, 1f - d / h.BlastRadius) * b.Knockback(wobble));
        }

        /// <summary>v₀ = √(g·d² / (2cos²θ·(d·tanθ − Δh))) toward the target (GDD 6.5).</summary>
        public static Vector3 LaunchVelocity(Vector3 from, Vector3 to, float angleDegrees, float gravity)
        {
            var flat = Flat(to - from);
            var d = flat.magnitude;
            var dh = to.y - from.y;
            var theta = angleDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(theta);
            var v0 = Mathf.Sqrt(gravity * d * d / (2f * cos * cos * (d * Mathf.Tan(theta) - dh)));
            return flat.normalized * (v0 * cos) + Vector3.up * (v0 * Mathf.Sin(theta));
        }

        /// <summary>t = d / (v₀·cosθ).</summary>
        public static float FlightSeconds(Vector3 from, Vector3 to, float angleDegrees, float gravity)
        {
            var v = LaunchVelocity(from, to, angleDegrees, gravity);
            var horizontal = Flat(v).magnitude;
            return horizontal > 0f ? Flat(to - from).magnitude / horizontal : 0f;
        }
    }
}
