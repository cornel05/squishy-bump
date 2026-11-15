using System.Collections.Generic;
using SquishyBump.Core;
using SquishyBump.Physics;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>Gives hazards host-side access to fighters through Core's <see cref="IFighterPhysics"/> (ADR-0003).</summary>
    public sealed class FighterPhysicsAdapter : IFighterPhysics
    {
        private readonly CollisionResolver resolver;
        private readonly Dictionary<Collider, ulong> byCollider = new();
        private readonly Dictionary<ulong, Rigidbody> bodies = new();

        public FighterPhysicsAdapter(CollisionResolver resolver) => this.resolver = resolver;

        public void Add(ulong id, PlayerMotor motor)
        {
            byCollider[motor.GetComponent<SphereCollider>()] = id;
            bodies[id] = motor.GetComponent<Rigidbody>();
        }

        public void Remove(ulong id)
        {
            if (!bodies.Remove(id, out var body)) return;
            byCollider.Remove(body.GetComponent<SphereCollider>());
        }

        public bool TryGetFighter(Collider collider, out ulong fighterId) => byCollider.TryGetValue(collider, out fighterId);

        public int GetWobble(ulong fighterId) => resolver.GetWobble(fighterId);

        public void AddImpulse(ulong fighterId, Vector3 impulse)
        {
            if (bodies.TryGetValue(fighterId, out var body)) body.linearVelocity += impulse / body.mass;
        }

        public void AddWobble(ulong fighterId, int amount) => resolver.AddWobble(fighterId, amount);
    }
}
