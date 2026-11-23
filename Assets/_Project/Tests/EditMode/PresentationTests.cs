using NUnit.Framework;
using SquishyBump.Presentation;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-13: rolling, squash and camera numbers of GDD 6.2, 3.4 and 10.1.</summary>
    public sealed class PresentationTests
    {
        static PresentationProfile Profile =>
            AssetDatabase.LoadAssetAtPath<PresentationProfile>("Assets/_Project/Data/Presentation/PresentationProfile.asset");

        [Test]
        public void Profile_HoldsGddValues()
        {
            var p = Profile;
            Assert.That(p, Is.Not.Null);
            Assert.That(p.SquashScale, Is.EqualTo(new Vector2(1.15f, 0.85f)));
            Assert.That(p.SquashSeconds, Is.EqualTo(0.12f));
            Assert.That(p.CameraDistance, Is.EqualTo(9f));
            Assert.That(p.PitchMinDegrees, Is.EqualTo(15f));
            Assert.That(p.PitchMaxDegrees, Is.EqualTo(60f));
            Assert.That(p.PitchDefaultDegrees, Is.EqualTo(35f));
            Assert.That(p.LandingSpeed, Is.GreaterThan(0f));
            Assert.That(p.ShakePerImpulse, Is.GreaterThan(0f));
            Assert.That(p.Upright, Is.True, "animals stay upright");
            Assert.That(p.TurnDegreesPerSecond, Is.GreaterThan(0f));
            Assert.That(p.LeanMaxDegrees, Is.GreaterThan(0f));
            Assert.That(p.LeanFullSpeed, Is.GreaterThan(0f));
        }

        [Test]
        public void FacingYaw_TurnsTowardVelocityAtMostMaxStep()
        {
            Assert.That(RollVisual.FacingYaw(0f, Vector3.right * 4f, 0.5f, 30f), Is.EqualTo(30f).Within(1e-4f));
            Assert.That(RollVisual.FacingYaw(80f, Vector3.right * 4f, 0.5f, 30f), Is.EqualTo(90f).Within(1e-4f));
            Assert.That(RollVisual.FacingYaw(10f, Vector3.right * 0.1f, 0.5f, 30f), Is.EqualTo(10f), "too slow: keeps facing");
        }

        [Test]
        public void UprightRotation_LeansForwardWithSpeedAndKeepsYaw()
        {
            var rotation = RollVisual.UprightRotation(90f, 2f, 20f, 4f);

            Assert.That(rotation.eulerAngles.x, Is.EqualTo(10f).Within(1e-3f));
            Assert.That(rotation.eulerAngles.y, Is.EqualTo(90f).Within(1e-3f));
            Assert.That(rotation.eulerAngles.z, Is.EqualTo(0f).Within(1e-3f), "no roll");
            Assert.That(RollVisual.UprightRotation(0f, 99f, 20f, 4f).eulerAngles.x, Is.EqualTo(20f).Within(1e-3f), "lean is capped");
        }

        [Test]
        public void AngularVelocity_RollsWithoutSlipping()
        {
            var v = new Vector3(3f, -2f, 4f);
            const float r = 0.5f;
            var omega = RollVisual.AngularVelocity(v, r);
            // Contact point speed ω × (−r ŷ) cancels the horizontal velocity.
            var contact = v + Vector3.Cross(omega, -r * Vector3.up);
            Assert.That(contact.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(contact.z, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(omega.y, Is.EqualTo(0f), "vertical motion never spins the fruit");
        }

        [Test]
        public void SquashFactor_PeaksThenReturnsToOne()
        {
            var peak = new Vector2(1.15f, 0.85f);
            Assert.That(RollVisual.SquashFactor(0f, peak, 0.12f), Is.EqualTo(new Vector3(1.15f, 0.85f, 1.15f)));
            Assert.That(RollVisual.SquashFactor(0.12f, peak, 0.12f), Is.EqualTo(Vector3.one));
            Assert.That(RollVisual.SquashFactor(float.PositiveInfinity, peak, 0.12f), Is.EqualTo(Vector3.one));
            Assert.That(RollVisual.SquashFactor(0.06f, peak, 0.12f).y, Is.EqualTo(0.925f).Within(1e-5f));
        }

        [Test]
        public void ClampPitch_StaysBetween15And60()
        {
            Assert.That(CameraRig.ClampPitch(-90f, Profile), Is.EqualTo(15f));
            Assert.That(CameraRig.ClampPitch(90f, Profile), Is.EqualTo(60f));
            Assert.That(CameraRig.ClampPitch(35f, Profile), Is.EqualTo(35f));
        }
    }
}
