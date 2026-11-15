using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Net.Sync;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-23: hazard events cross the wire intact and clients line the telegraph up with the host tick.</summary>
    public sealed class HazardSyncTests
    {
        const float Dt = 1f / 60f;

        [Test]
        public void HazardEvent_RoundTripsWithHostTick()
        {
            var e = new HazardEvent(42, 2700, HazardEventKind.CannonFire, 3, new Vector3(1f, 0f, -2f), -1);
            Assert.That(SyncCodec.TryReadHazard(SyncCodec.WriteHazard(e), out var r), Is.True);
            Assert.That((r.EventId, r.HostTick, r.Kind, r.Index, r.Aim, r.Direction),
                Is.EqualTo((42u, 2700u, HazardEventKind.CannonFire, 3, e.Aim, -1)));
        }

        [Test]
        public void HazardEvent_Malformed_IsRejected()
        {
            var bytes = SyncCodec.WriteHazard(new HazardEvent(1, 10, HazardEventKind.RingDropWarning));
            Assert.That(SyncCodec.TryReadHazard(bytes[..^1], out _), Is.False);
            bytes[8] = 200;
            Assert.That(SyncCodec.TryReadHazard(bytes, out _), Is.False, "unknown kind");
        }

        [Test]
        public void TelegraphElapsed_MatchesHostStage()
        {
            // Drop at tick 2700 after a 3 s lead: warning began at host time 42 s.
            Assert.That(RingDrop.TelegraphElapsed(2700, 3f, 42f, Dt), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(RingDrop.TelegraphElapsed(2700, 3f, 44.5f, Dt), Is.EqualTo(2.5f).Within(1e-4f));
            // A client rendering 0.1 s behind starts the telegraph 0.1 s later.
            Assert.That(RingDrop.TelegraphElapsed(2700, 3f, 41.9f, Dt), Is.EqualTo(-0.1f).Within(1e-4f));
        }
    }
}
