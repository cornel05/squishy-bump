using System;
using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Physics
{
    /// <summary>One fighter as the resolver sees it for a tick.</summary>
    public struct FighterBody
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Mass;
        public float Radius;
        public int Wobble;
        public bool Dashing;
    }

    /// <summary>A received impulse of at least the hit threshold (GDD 6.3). Indices point into the resolved array.</summary>
    public readonly struct BodyHit
    {
        public BodyHit(int receiver, int other, float impulse)
        {
            Receiver = receiver;
            Other = other;
            Impulse = impulse;
        }

        public int Receiver { get; }
        public int Other { get; }
        public float Impulse { get; }
    }

    /// <summary>A hit between two registered fighters, for score (last touch) and feedback.</summary>
    public readonly struct FighterHit
    {
        public FighterHit(ulong receiverId, ulong otherId, float impulse, int wobble)
        {
            ReceiverId = receiverId;
            OtherId = otherId;
            Impulse = impulse;
            Wobble = wobble;
        }

        public ulong ReceiverId { get; }
        public ulong OtherId { get; }
        public float Impulse { get; }
        /// <summary>Wobble of the receiver after the hit.</summary>
        public int Wobble { get; }
    }

    /// <summary>
    /// Fighter-to-fighter contacts on the host (GDD 6.3; PF05, PF06). PhysX never sees them:
    /// fighters sit on a layer that ignores itself, and every pair is tested here each tick.
    /// The host loop calls <see cref="Step"/> once per tick, after the motors.
    /// </summary>
    public sealed class CollisionResolver : MonoBehaviour
    {
        public const string FighterLayerName = "Fighter";

        [SerializeField] private BalanceProfile balance;

        private sealed class Entry
        {
            public ulong Id;
            public PlayerMotor Motor;
            public Rigidbody Body;
            public SphereCollider Sphere;
            public int Wobble;
            public int TicksSinceHit;
        }

        private readonly List<Entry> fighters = new List<Entry>();
        private readonly List<Entry> active = new List<Entry>();
        private readonly List<BodyHit> hits = new List<BodyHit>();
        private FighterBody[] bodies = Array.Empty<FighterBody>();

        public static int FighterLayer => LayerMask.NameToLayer(FighterLayerName);

        /// <summary>Raised on the host for every hit, after Wobble and stun were applied.</summary>
        public event Action<FighterHit> Hit;
        /// <summary>Fighter id and its new Wobble.</summary>
        public event Action<ulong, int> WobbleChanged;

        private void Awake() => UnityEngine.Physics.IgnoreLayerCollision(FighterLayer, FighterLayer, true);

        public void Configure(BalanceProfile balanceProfile) => balance = balanceProfile;

        public void Register(ulong fighterId, PlayerMotor motor)
        {
            Unregister(fighterId);
            motor.gameObject.layer = FighterLayer;
            fighters.Add(new Entry
            {
                Id = fighterId,
                Motor = motor,
                Body = motor.GetComponent<Rigidbody>(),
                Sphere = motor.GetComponent<SphereCollider>(),
            });
        }

        public void Unregister(ulong fighterId) => fighters.RemoveAll(f => f.Id == fighterId);

        public int GetWobble(ulong fighterId) => Find(fighterId)?.Wobble ?? 0;

        /// <summary>Wobble from a source other than a fighter hit, e.g. +2 for a fire blast (PF05).</summary>
        public void AddWobble(ulong fighterId, int amount)
        {
            var fighter = Find(fighterId);
            if (fighter == null) return;
            fighter.TicksSinceHit = 0;
            SetWobble(fighter, fighter.Wobble + amount);
        }

        /// <summary>Start of a match and every respawn (PF05).</summary>
        public void ResetWobble(ulong fighterId)
        {
            var fighter = Find(fighterId);
            if (fighter == null) return;
            fighter.TicksSinceHit = 0;
            SetWobble(fighter, 0);
        }

        public void Step(float fixedDeltaTime)
        {
            active.Clear();
            foreach (var fighter in fighters)
                if (fighter.Motor != null && fighter.Motor.isActiveAndEnabled) active.Add(fighter);
            if (bodies.Length < active.Count) bodies = new FighterBody[active.Count];

            var decayTicks = Mathf.RoundToInt(balance.WobbleDecaySeconds / fixedDeltaTime);
            for (var i = 0; i < active.Count; i++)
            {
                var fighter = active[i];
                if (++fighter.TicksSinceHit >= decayTicks)
                {
                    fighter.TicksSinceHit = 0;
                    SetWobble(fighter, fighter.Wobble - 1);
                }

                bodies[i] = new FighterBody
                {
                    Position = fighter.Body.position + fighter.Sphere.center,
                    Velocity = fighter.Body.linearVelocity,
                    Mass = fighter.Body.mass,
                    Radius = fighter.Sphere.radius,
                    Wobble = fighter.Wobble,
                    Dashing = fighter.Motor.IsDashing,
                };
            }

            hits.Clear();
            ResolveAll(bodies, active.Count, balance, hits);

            for (var i = 0; i < active.Count; i++)
            {
                active[i].Body.position = bodies[i].Position - active[i].Sphere.center;
                active[i].Body.linearVelocity = bodies[i].Velocity;
            }

            foreach (var hit in hits)
            {
                var receiver = active[hit.Receiver];
                receiver.TicksSinceHit = 0;
                SetWobble(receiver, receiver.Wobble + 1);
                receiver.Motor.Stun(balance.StunSeconds(receiver.Wobble));
                Hit?.Invoke(new FighterHit(receiver.Id, active[hit.Other].Id, hit.Impulse, receiver.Wobble));
            }
        }

        private Entry Find(ulong fighterId) => fighters.Find(f => f.Id == fighterId);

        private void SetWobble(Entry fighter, int value)
        {
            value = Mathf.Clamp(value, 0, balance.WobbleMax);
            if (value == fighter.Wobble) return;
            fighter.Wobble = value;
            WobbleChanged?.Invoke(fighter.Id, value);
        }

        /// <summary>Tests every pair once, in index order (15 pairs for six fighters). Pure.</summary>
        public static void ResolveAll(FighterBody[] bodies, int count, BalanceProfile balance, List<BodyHit> hits)
        {
            for (var a = 0; a < count; a++)
            for (var b = a + 1; b < count; b++)
            {
                if (!Resolve(ref bodies[a], ref bodies[b], balance, out var impulseA, out var impulseB)) continue;
                if (impulseA >= balance.HitThreshold) hits.Add(new BodyHit(a, b, impulseA));
                if (impulseB >= balance.HitThreshold) hits.Add(new BodyHit(b, a, impulseB));
            }
        }

        /// <summary>
        /// One pair (GDD 6.3): j = −(1 + e)(v_rel · n) / (1/m_A + 1/m_B), each side receives
        /// j · K(h_i) · b_i, then 80% of the overlap beyond the slop is removed by inverse mass.
        /// Returns false when the spheres do not touch.
        /// </summary>
        public static bool Resolve(ref FighterBody a, ref FighterBody b, BalanceProfile balance,
            out float impulseA, out float impulseB)
        {
            impulseA = impulseB = 0f;
            var offset = b.Position - a.Position;
            var distance = offset.magnitude;
            var penetration = a.Radius + b.Radius - distance;
            if (penetration <= 0f) return false;

            // Same centre: any fixed direction separates them deterministically.
            var normal = distance > 0f ? offset / distance : Vector3.right;
            var inverseA = 1f / a.Mass;
            var inverseB = 1f / b.Mass;
            var inverseSum = inverseA + inverseB;

            var approach = Vector3.Dot(b.Velocity - a.Velocity, normal);
            if (approach < 0f)
            {
                var impulse = -(1f + balance.Restitution) * approach / inverseSum;
                impulseA = impulse * balance.Knockback(a.Wobble) * (b.Dashing ? balance.DashBonus : 1f);
                impulseB = impulse * balance.Knockback(b.Wobble) * (a.Dashing ? balance.DashBonus : 1f);
                a.Velocity -= impulseA * inverseA * normal;
                b.Velocity += impulseB * inverseB * normal;
            }

            var correction = Mathf.Max(penetration - balance.PenetrationSlopMeters, 0f)
                             * balance.PenetrationCorrection / inverseSum;
            a.Position -= correction * inverseA * normal;
            b.Position += correction * inverseB * normal;
            return true;
        }
    }
}
