using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-10: the pure tick of PlayerMotor against GDD 6.2 and 4.4, using the real balance assets.</summary>
    public sealed class PlayerMotorTests
    {
        const float Dt = 1f / 60f;
        const string Data = "Assets/_Project/Data";

        static readonly BalanceProfile Balance =
            AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Data}/Balance/BalanceProfile.asset");
        static ClassProfile Class(string name) =>
            AssetDatabase.LoadAssetAtPath<ClassProfile>($"{Data}/Classes/{name}.asset");

        static GroundInfo Floor => new GroundInfo(Balance.FloorFriction, Vector3.zero);
        static GroundInfo Ice => new GroundInfo(Balance.IceFriction, Vector3.zero);
        static GroundInfo Air => default;

        static InputCommand Command(Vector2 move = default, bool jump = false, bool dash = false, float yaw = 0f) =>
            new InputCommand(0, move, jump, dash, yaw);

        static void Tick(ref MotorState state, InputCommand command, GroundInfo ground, string fighter = "Normal") =>
            PlayerMotor.Tick(ref state, command, ground, Balance, Class(fighter), Dt);

        [TestCase("Heavy", 9.6f)]
        [TestCase("Normal", 12f)]
        [TestCase("Light", 14.4f)]
        public void Move_OnFloor_AcceleratesByClassTraction(string fighter, float maxAcceleration)
        {
            var state = new MotorState();

            Tick(ref state, Command(Vector2.up), Floor, fighter);

            Assert.That(state.Velocity.z, Is.EqualTo(maxAcceleration * Dt).Within(1e-4f));
            Assert.That(state.Velocity.x, Is.Zero.Within(1e-5f));
        }

        [Test]
        public void Move_OnIce_AcceleratesAtHalfTheFloorRate()
        {
            var floor = new MotorState();
            var ice = new MotorState();

            Tick(ref floor, Command(Vector2.up), Floor);
            Tick(ref ice, Command(Vector2.up), Ice);

            Assert.That(ice.Velocity.z, Is.EqualTo(floor.Velocity.z / 2f).Within(1e-5f));
        }

        [Test]
        public void Move_HeldForOneSecond_SettlesAtMaxSpeed()
        {
            var state = new MotorState();

            for (var i = 0; i < 60; i++) Tick(ref state, Command(Vector2.up), Floor);

            Assert.That(state.Velocity.magnitude, Is.EqualTo(Class("Normal").MaxSpeed).Within(1e-3f));
        }

        [Test]
        public void Move_WithYaw_IsCameraRelative()
        {
            var state = new MotorState();

            Tick(ref state, Command(Vector2.up, yaw: 90f), Floor);

            Assert.That(state.Velocity.x, Is.GreaterThan(0f));
            Assert.That(state.Velocity.z, Is.Zero.Within(1e-5f));
        }

        [Test]
        public void Move_InAir_UsesThirtyPercentControl()
        {
            var ground = new MotorState();
            var air = new MotorState();

            Tick(ref ground, Command(Vector2.up), Floor);
            Tick(ref air, Command(Vector2.up), Air);

            Assert.That(air.Velocity.z, Is.EqualTo(ground.Velocity.z * Balance.AirControl).Within(1e-5f));
        }

        [Test]
        public void NoInput_BrakesTowardSurfaceVelocity()
        {
            var state = new MotorState { Velocity = new Vector3(3f, 0f, 0f) };
            var turntable = new GroundInfo(Balance.FloorFriction, new Vector3(0f, 0f, 1f));

            for (var i = 0; i < 60; i++) Tick(ref state, Command(), turntable);

            Assert.That(state.Velocity.x, Is.Zero.Within(1e-4f));
            Assert.That(state.Velocity.z, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Traction_ClampsVelocityChange()
        {
            var result = PlayerMotor.Traction(Vector3.zero, new Vector3(6f, 0f, 0f), 0.2f);

            Assert.That(result, Is.EqualTo(new Vector3(0.2f, 0f, 0f)));
        }

        [Test]
        public void Jump_OnGround_SetsLaunchSpeedOncePerPress()
        {
            var state = new MotorState();

            Tick(ref state, Command(jump: true), Floor);
            Assert.That(state.Velocity.y, Is.EqualTo(6.93f).Within(0.005f));

            // Still held on the next grounded tick: no second launch.
            state.Velocity.y = 0f;
            Tick(ref state, Command(jump: true), Floor);
            Assert.That(state.Velocity.y, Is.Zero);
        }

        [Test]
        public void Jump_InAir_IsIgnored()
        {
            var state = new MotorState();

            Tick(ref state, Command(jump: true), Air);

            Assert.That(state.Velocity.y, Is.Zero);
        }

        [TestCase("Heavy", 5.3f)]
        [TestCase("Normal", 8.0f)]
        [TestCase("Light", 11.4f)]
        public void Dash_AddsImpulseOverMass(string fighter, float deltaV)
        {
            var state = new MotorState();

            Tick(ref state, Command(dash: true), Air, fighter);

            // Airborne and no steering: the dash goes along the yaw, and air braking this tick is small.
            Assert.That(state.Velocity.z, Is.EqualTo(deltaV).Within(0.1f));
            Assert.That(state.DashStateTicks, Is.EqualTo(Mathf.RoundToInt(Balance.DashStateSeconds / Dt) - 1));
        }

        [Test]
        public void Dash_DuringCooldown_IsRejectedUntilThreeSeconds()
        {
            var state = new MotorState();
            var cooldownTicks = Mathf.RoundToInt(Balance.DashCooldownSeconds / Dt);
            Tick(ref state, Command(dash: true), Floor);

            // A client spamming presses on every other tick gains nothing.
            for (var i = 1; i < cooldownTicks; i++)
            {
                state.Velocity = Vector3.zero;
                Tick(ref state, Command(dash: i % 2 == 0), Floor);
                Assert.That(state.Velocity, Is.EqualTo(Vector3.zero), $"tick {i}");
            }

            Tick(ref state, Command(), Floor);
            Tick(ref state, Command(dash: true), Floor);
            Assert.That(state.Velocity.z, Is.GreaterThan(0f));
        }

        [Test]
        public void Dash_InAir_IsAllowedOncePerAirtime()
        {
            var state = new MotorState();
            Tick(ref state, Command(dash: true), Air);
            state.DashCooldownTicks = 0;
            Tick(ref state, Command(), Air);
            state.Velocity = Vector3.zero;

            Tick(ref state, Command(dash: true), Air);
            Assert.That(state.Velocity, Is.EqualTo(Vector3.zero));

            Tick(ref state, Command(), Floor);
            Tick(ref state, Command(dash: true), Air);
            Assert.That(state.Velocity.z, Is.GreaterThan(0f));
        }

        [Test]
        public void JumpDashMove_InOneTick_AllApplyInOrder()
        {
            var state = new MotorState();
            var normal = Class("Normal");

            Tick(ref state, Command(Vector2.right, jump: true, dash: true), Floor);

            // Jump first (still grounded for the dash), dash along the steer, then traction pulls
            // the overspeed back toward v_max by one tick of a_max.
            Assert.That(state.Velocity.y, Is.EqualTo(Balance.JumpSpeed));
            var expected = Balance.DashDeltaV(normal) - Balance.MaxAcceleration(normal, Balance.FloorFriction) * Dt;
            Assert.That(state.Velocity.x, Is.EqualTo(expected).Within(1e-4f));
            Assert.That(state.AirDashUsed, Is.False);
        }

        [Test]
        public void Stun_IgnoresInputAndStillBrakes()
        {
            var state = new MotorState { StunTicks = 10, Velocity = new Vector3(2f, 0f, 0f) };

            Tick(ref state, Command(Vector2.right, jump: true, dash: true), Floor);

            Assert.That(state.Velocity.y, Is.Zero);
            Assert.That(state.Velocity.x, Is.LessThan(2f));
            Assert.That(state.DashCooldownTicks, Is.Zero);
        }

        [Test]
        public void PressDuringStun_FiresWhenStunEndsInsideBuffer()
        {
            var state = new MotorState { StunTicks = 3 };

            Tick(ref state, Command(jump: true), Floor);
            for (var i = 0; i < 2; i++) Tick(ref state, Command(jump: true), Floor);
            Assert.That(state.Velocity.y, Is.Zero);

            Tick(ref state, Command(jump: true), Floor);
            Assert.That(state.Velocity.y, Is.EqualTo(Balance.JumpSpeed));
        }

        [Test]
        public void PressDuringStun_ExpiresAfterBufferWindow()
        {
            var bufferTicks = Mathf.RoundToInt(Balance.StunInputBufferSeconds / Dt);
            var state = new MotorState { StunTicks = bufferTicks + 2 };

            Tick(ref state, Command(dash: true), Floor);
            for (var i = 0; i < bufferTicks + 3; i++) Tick(ref state, Command(dash: true), Floor);

            Assert.That(state.Velocity, Is.EqualTo(Vector3.zero));
            Assert.That(state.DashCooldownTicks, Is.Zero);
        }
    }
}
