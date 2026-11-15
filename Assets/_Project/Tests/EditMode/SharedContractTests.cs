using NUnit.Framework;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    public sealed class SharedContractTests
    {
        // Regression: sai số float32 làm sqrMagnitude của vector đơn vị vượt 1 (ví dụ góc 38 độ).
        [Test]
        public void UnitMoveAtAnyAngle_IsValid()
        {
            for (var degrees = 0; degrees < 360; degrees++)
            {
                var radians = degrees * Mathf.Deg2Rad;
                var command = new InputCommand(1, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)), false, false, 0f);
                Assert.That(command.HasValidValues, Is.True, $"góc {degrees}");
            }
        }

        [Test]
        public void OversizedMove_ClampedToLengthOne()
        {
            var command = new InputCommand(1, new Vector2(3f, 4f), false, false, 0f);

            Assert.That(command.HasValidValues, Is.True);
            Assert.That(command.Move.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(command.Move.x, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void NonFiniteInput_IsInvalid()
        {
            Assert.That(new InputCommand(1, new Vector2(float.NaN, 0f), false, false, 0f).HasValidValues, Is.False);
            Assert.That(new InputCommand(1, new Vector2(0f, float.PositiveInfinity), false, false, 0f).HasValidValues, Is.False);
            Assert.That(new InputCommand(1, Vector2.zero, false, false, float.NaN).HasValidValues, Is.False);
            Assert.That(new InputCommand(1, Vector2.zero, false, false, float.NegativeInfinity).HasValidValues, Is.False);
        }

        [Test]
        public void Yaw_WrappedTo0To360()
        {
            Assert.That(new InputCommand(1, Vector2.zero, false, false, -90f).YawDegrees, Is.EqualTo(270f).Within(0.0001f));
            Assert.That(new InputCommand(1, Vector2.zero, false, false, 725f).YawDegrees, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void MatchEventFactories_CarryPayload()
        {
            var standings = new[] { new MatchStanding(4, 1, 3), new MatchStanding(2, 2, 1) };

            var ringOut = MatchEvent.RingOut(7, 120, 2, 4);
            var hazardFall = MatchEvent.RingOut(8, 121, 3);
            var elimination = MatchEvent.Elimination(9, 122, 2, 5);
            var result = MatchEvent.MatchResult(10, 123, standings);

            Assert.That((ringOut.Kind, ringOut.EventId, ringOut.HostTick, ringOut.FighterId, ringOut.CreditedFighterId),
                Is.EqualTo((MatchEventKind.RingOut, 7u, 120u, 2ul, 4ul)));
            Assert.That(hazardFall.CreditedFighterId, Is.EqualTo(MatchEvent.NoFighter));
            Assert.That((elimination.FighterId, elimination.Value), Is.EqualTo((2ul, 5)));
            Assert.That(result.Standings, Is.EqualTo(standings));
            Assert.That(ringOut.Standings, Is.Empty);
            Assert.That(default(MatchEvent).Standings, Is.Empty);
        }

        [Test]
        public void HazardEvent_CarriesCannonAim()
        {
            var aim = new Vector3(1f, 0f, -2f);

            var cannon = new HazardEvent(3, 600, HazardEventKind.CannonAim, 1, aim);
            var turntable = new HazardEvent(4, 660, HazardEventKind.TurntableReversal, direction: -1);

            Assert.That((cannon.Index, cannon.Aim, cannon.HostTick), Is.EqualTo((1, aim, 600u)));
            Assert.That(turntable.Direction, Is.EqualTo(-1));
        }
    }
}
