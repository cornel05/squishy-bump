using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Match;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-28: Rank respawn point of GDD 3.6 / LD13.</summary>
    public sealed class RespawnPickerTests
    {
        const float Dt = 1f / 60f;
        static BalanceProfile Balance =>
            AssetDatabase.LoadAssetAtPath<BalanceProfile>("Assets/_Project/Data/Balance/BalanceProfile.asset");

        static int Ticks(float seconds) => Mathf.RoundToInt(seconds / Dt);

        [Test]
        public void SafeRadius_FreshL1_IsArenaMinusMargin() =>
            Assert.That(RespawnPicker.SafeRadius(12f, 0, Balance, Dt, true), Is.EqualTo(10.5f));

        [Test]
        public void SafeRadius_BandDroppingWithinThreeSeconds_IsExcluded()
        {
            Assert.That(RespawnPicker.SafeRadius(12f, Ticks(41.9f), Balance, Dt, true), Is.EqualTo(10.5f));
            Assert.That(RespawnPicker.SafeRadius(12f, Ticks(42f), Balance, Dt, true), Is.EqualTo(7.5f));
            Assert.That(RespawnPicker.SafeRadius(12f, Ticks(80f), Balance, Dt, true), Is.EqualTo(4.5f));
        }

        [Test]
        public void SafeRadius_NoRingDrop_IgnoresSchedule() =>
            Assert.That(RespawnPicker.SafeRadius(9f, Ticks(140f), Balance, Dt, false), Is.EqualTo(7.5f));

        [Test]
        public void TryPick_StaysInsideSafeRadiusAndFarFromFruits()
        {
            var fruits = new[] { new Vector3(3f, 0f, 0f), new Vector3(-3f, 0f, 0f) };
            Assert.That(RespawnPicker.TryPick(Vector3.zero, 7.5f, fruits, null, out var p), Is.True);
            Assert.That(new Vector2(p.x, p.z).magnitude, Is.LessThanOrEqualTo(7.5f));
            // Furthest from both fruits inside the circle is on the z axis at the rim.
            Assert.That(Mathf.Abs(p.x), Is.LessThan(0.6f));
            Assert.That(Mathf.Abs(p.z), Is.GreaterThan(6.5f));
        }

        [Test]
        public void TryPick_NeverInsideActiveHazard()
        {
            var hazards = new[] { new HazardZone(Vector3.zero, 7f) };
            Assert.That(RespawnPicker.TryPick(Vector3.zero, 7.5f, null, hazards, out var p), Is.True);
            Assert.That(new Vector2(p.x, p.z).magnitude, Is.GreaterThanOrEqualTo(7f));
            Assert.That(RespawnPicker.TryPick(Vector3.zero, 6f, null, hazards, out _), Is.False);
        }

        [Test]
        public void TryPick_NoFloorLeft_Fails()
        {
            var r = RespawnPicker.SafeRadius(12f, Ticks(133f), Balance, Dt, true);
            Assert.That(RespawnPicker.TryPick(Vector3.zero, r, null, null, out _), Is.False);
        }

        [Test]
        public void TryPick_EmptyArena_PicksCentre()
        {
            Assert.That(RespawnPicker.TryPick(new Vector3(1f, 0f, 2f), 7.5f, null, null, out var p), Is.True);
            Assert.That(p, Is.EqualTo(new Vector3(1f, 0f, 2f)));
        }
    }
}
