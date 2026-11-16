using System;
using NUnit.Framework;
using SquishyBump.Hazards;
using SquishyBump.Presentation;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-49: the twelve SFX placeholders, bump feedback numbers and the telegraph palette (GDD 12.5, 12.7, 3.4).</summary>
    public sealed class AudioFeedbackTests
    {
        static PresentationProfile P =>
            AssetDatabase.LoadAssetAtPath<PresentationProfile>("Assets/_Project/Data/Presentation/PresentationProfile.asset");

        [Test]
        public void Profile_HasAllTwelveMonoPlaceholders()
        {
            var ids = (Sfx[])Enum.GetValues(typeof(Sfx));
            Assert.That(ids, Has.Length.EqualTo(12));
            foreach (var id in ids)
            {
                var clip = P.Clip(id);
                Assert.That(clip, Is.Not.Null, id.ToString());
                Assert.That((clip.channels, clip.frequency), Is.EqualTo((1, 44100)), id.ToString());
            }
            // The cannon charge fills the 1.0 s telegraph; the ring creak the 3 s warning.
            Assert.That(P.Clip(Sfx.Cannon).length, Is.GreaterThanOrEqualTo(1f));
            Assert.That(P.Clip(Sfx.RingDrop).length, Is.GreaterThanOrEqualTo(3f));
        }

        [Test]
        public void HitFeedback_HitStopFortyMillisecondsAndPitchRisesWithImpulse()
        {
            Assert.That(P.HitStopSeconds, Is.EqualTo(0.04f));
            Assert.That(P.WhooshDistance, Is.EqualTo(2f));
            var low = AudioDirector.SquishPitch(P.StrongHitImpulse, P);
            var mid = AudioDirector.SquishPitch((P.StrongHitImpulse + P.SquishTopImpulse) / 2f, P);
            var high = AudioDirector.SquishPitch(P.SquishTopImpulse * 2f, P);
            Assert.That(low, Is.LessThan(mid));
            Assert.That(mid, Is.LessThan(high));
            Assert.That(high, Is.EqualTo(P.SquishPitch.y), "capped");
        }

        [Test]
        public void TelegraphColors_AreTheGddHexValues()
        {
            Assert.That(ColorUtility.ToHtmlStringRGB(TelegraphColors.Amber), Is.EqualTo("FFB703"));
            Assert.That(ColorUtility.ToHtmlStringRGB(TelegraphColors.Red), Is.EqualTo("E63946"));
        }
    }
}
