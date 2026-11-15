using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Physics
{
    /// <summary>Per-fighter simulation state. Timers count 60 Hz ticks so the host stays deterministic.</summary>
    public struct MotorState
    {
        public Vector3 Velocity;
        public int DashCooldownTicks;
        public int DashStateTicks;
        public int StunTicks;
        public int JumpBufferTicks;
        public int DashBufferTicks;
        public bool AirDashUsed;
        public bool JumpHeld;
        public bool DashHeld;
    }

    /// <summary>The floor under the fighter this tick.</summary>
    public readonly struct GroundInfo
    {
        public GroundInfo(float friction, Vector3 surfaceVelocity)
        {
            Grounded = true;
            Friction = friction;
            SurfaceVelocity = surfaceVelocity;
        }

        public bool Grounded { get; }
        public float Friction { get; }
        public Vector3 SurfaceVelocity { get; }
    }

    /// <summary>
    /// Moves one fighter from <see cref="InputCommand"/> on the host (GDD 6.2, 4.4; PF01, PF03, PF04).
    /// Never reads devices. The caller (host loop) decides who may step it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class PlayerMotor : MonoBehaviour, IFighterMotor
    {
        [SerializeField] private BalanceProfile balance;
        [SerializeField] private ClassProfile fighterClass;
        [Tooltip("Layers that count as floor.")]
        [SerializeField] private LayerMask groundMask = UnityEngine.Physics.DefaultRaycastLayers;
        [Tooltip("How far below the sphere the floor is still felt, in metres.")]
        [SerializeField] private float groundProbeMeters = 0.05f;

        private Rigidbody body;
        private SphereCollider sphere;
        private MotorState state;

        public bool IsDashing => state.DashStateTicks > 0;
        public bool IsStunned => state.StunTicks > 0;
        public bool DashReady => state.DashCooldownTicks == 0;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
            // The traction rule owns all horizontal braking, so PhysX must add none of its own.
            body.freezeRotation = true;
            body.linearDamping = 0f;
            sphere.sharedMaterial = new PhysicsMaterial("Fighter")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
        }

        public void Configure(BalanceProfile balanceProfile, ClassProfile classProfile)
        {
            balance = balanceProfile;
            fighterClass = classProfile;
        }

        /// <summary>Locks input for the stun of a hit (PF06). Called by the collision resolver on the host.</summary>
        public void Stun(float seconds) =>
            state.StunTicks = Mathf.Max(state.StunTicks, Mathf.RoundToInt(seconds / Time.fixedDeltaTime));

        public void Step(InputCommand command, float fixedDeltaTime)
        {
            // A corrupt command steers nothing and presses nothing; held buttons stay held.
            if (!command.HasValidValues)
                command = new InputCommand(command.Sequence, Vector2.zero, state.JumpHeld, state.DashHeld, 0f);

            state.Velocity = body.linearVelocity;
            Tick(ref state, command, ProbeGround(), balance, fighterClass, fixedDeltaTime);
            body.linearVelocity = state.Velocity;
        }

        private GroundInfo ProbeGround()
        {
            var origin = body.position + sphere.center;
            if (!UnityEngine.Physics.Raycast(origin, Vector3.down, out var hit, sphere.radius + groundProbeMeters,
                    groundMask, QueryTriggerInteraction.Ignore))
                return default;

            // A floor says how slippery it is through its physics material (ice, GDD 6.2); bare floors use μ.
            var material = hit.collider.sharedMaterial;
            var friction = material != null ? material.dynamicFriction : balance.FloorFriction;
            var surface = hit.rigidbody != null ? hit.rigidbody.GetPointVelocity(hit.point) : Vector3.zero;
            return new GroundInfo(friction, surface);
        }

        /// <summary>One host tick: jump, dash, then movement (GDD 4.4). Pure, so EditMode tests can drive it.</summary>
        public static void Tick(ref MotorState s, InputCommand command, GroundInfo ground,
            BalanceProfile balance, ClassProfile fighter, float dt)
        {
            var stunned = s.StunTicks > 0;
            // A press counts on its own tick, or for the buffer window when it arrives during stun.
            var pressLife = stunned ? Mathf.RoundToInt(balance.StunInputBufferSeconds / dt) : 1;
            if (command.Jump && !s.JumpHeld) s.JumpBufferTicks = pressLife;
            if (command.Dash && !s.DashHeld) s.DashBufferTicks = pressLife;
            s.JumpHeld = command.Jump;
            s.DashHeld = command.Dash;
            if (ground.Grounded) s.AirDashUsed = false;

            var yaw = Quaternion.Euler(0f, command.YawDegrees, 0f);
            var steer = stunned ? Vector3.zero : yaw * new Vector3(command.Move.x, 0f, command.Move.y);

            if (!stunned && s.JumpBufferTicks > 0 && ground.Grounded)
            {
                s.Velocity.y = balance.JumpSpeed;
                s.JumpBufferTicks = 0;
            }

            // Host-side cooldown check: extra dash commands inside the cooldown are dropped.
            if (!stunned && s.DashBufferTicks > 0 && s.DashCooldownTicks == 0 && (ground.Grounded || !s.AirDashUsed))
            {
                var direction = steer.sqrMagnitude > 0f ? steer.normalized : yaw * Vector3.forward;
                s.Velocity += direction * balance.DashDeltaV(fighter);
                s.DashCooldownTicks = Mathf.RoundToInt(balance.DashCooldownSeconds / dt);
                s.DashStateTicks = Mathf.RoundToInt(balance.DashStateSeconds / dt);
                s.AirDashUsed = !ground.Grounded;
                s.DashBufferTicks = 0;
            }

            var maxAcceleration = ground.Grounded
                ? balance.MaxAcceleration(fighter, ground.Friction)
                : balance.MaxAcceleration(fighter, balance.FloorFriction) * balance.AirControl;
            var surface = ground.Grounded ? ground.SurfaceVelocity : Vector3.zero;
            var target = new Vector3(surface.x, 0f, surface.z) + fighter.MaxSpeed * steer;
            var horizontal = Traction(new Vector3(s.Velocity.x, 0f, s.Velocity.z), target, maxAcceleration * dt);
            s.Velocity = new Vector3(horizontal.x, s.Velocity.y, horizontal.z);

            if (s.DashCooldownTicks > 0) s.DashCooldownTicks--;
            if (s.DashStateTicks > 0) s.DashStateTicks--;
            if (s.StunTicks > 0) s.StunTicks--;
            if (s.JumpBufferTicks > 0) s.JumpBufferTicks--;
            if (s.DashBufferTicks > 0) s.DashBufferTicks--;
        }

        /// <summary>v ← v + clampLength(v_target − v, a_max·Δt) (GDD 6.2).</summary>
        public static Vector3 Traction(Vector3 velocity, Vector3 target, float maxDelta) =>
            velocity + Vector3.ClampMagnitude(target - velocity, maxDelta);
    }
}
