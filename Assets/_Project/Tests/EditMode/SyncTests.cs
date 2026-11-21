using System.Collections.Generic;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Net.Sync;
using UnityEngine;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-21: wire formats, host input validation and the 100 ms interpolation buffer (GDD 10.4).</summary>
    public sealed class SyncTests
    {
        [Test]
        public void Admission_RejectsMidMatchAndAllowsLobbyAgainWithSpecificReason()
        {
            var payload = System.Text.Encoding.UTF8.GetBytes("build-4");
            Assert.That(SquishyBump.Net.Session.StubSessionService.CheckJoin(payload, "build-4", 2, SessionMode.Lan, false),
                Is.EqualTo(SessionEndReason.MatchInProgress));
            Assert.That(SquishyBump.Net.Session.StubSessionService.CheckJoin(payload, "build-4", 2, SessionMode.Lan, true),
                Is.EqualTo(SessionEndReason.None));
            Assert.That(SquishyBump.Net.Session.StubSessionService.ReadRejection("SB:MatchInProgress"),
                Is.EqualTo(SessionEndReason.MatchInProgress));
            Assert.That(SquishyBump.UI.Menus.MainMenu.ErrorText(SessionEndReason.MatchInProgress), Does.Contain("lobby"));
        }
        [TestCase(1, SessionEndReason.None)]
        [TestCase(5, SessionEndReason.None)]
        [TestCase(6, SessionEndReason.RoomFull)]
        public void LanApproval_EnforcesSixTotalPlayers(int connected, SessionEndReason expected)
        {
            var payload = System.Text.Encoding.UTF8.GetBytes("build-1");
            Assert.That(SquishyBump.Net.Session.StubSessionService.CheckJoin(payload, "build-1", connected, SessionMode.Lan),
                Is.EqualTo(expected));
        }

        [Test]
        public void LanApproval_RejectsWrongVersionMalformedPayloadAndSinglePlayer()
        {
            var check = new System.Func<byte[], string, int, SessionMode, SessionEndReason>(
                SquishyBump.Net.Session.StubSessionService.CheckJoin);
            Assert.That(check(null, "build-1", 1, SessionMode.Lan), Is.EqualTo(SessionEndReason.VersionMismatch));
            Assert.That(check(new byte[129], "build-1", 1, SessionMode.Lan), Is.EqualTo(SessionEndReason.VersionMismatch));
            Assert.That(check(System.Text.Encoding.UTF8.GetBytes("build-2"), "build-1", 1, SessionMode.Lan),
                Is.EqualTo(SessionEndReason.VersionMismatch));
            Assert.That(check(System.Text.Encoding.UTF8.GetBytes("build-1"), "build-1", 1, SessionMode.SinglePlayer),
                Is.EqualTo(SessionEndReason.RoomFull));
        }

        [TestCase("SB:VersionMismatch", SessionEndReason.VersionMismatch)]
        [TestCase("SB:RoomFull", SessionEndReason.RoomFull)]
        [TestCase("unknown", SessionEndReason.ConnectFailed)]
        public void LanRejection_MapsHostReasonToStableCode(string text, SessionEndReason expected) =>
            Assert.That(SquishyBump.Net.Session.StubSessionService.ReadRejection(text), Is.EqualTo(expected));

        static InputCommand Cmd(uint seq, float x = 0f) => new(seq, new Vector2(x, 0f), seq % 2 == 0, false, 90f);

        [Test]
        public void InputPacket_RoundTripsLastThreeCommands()
        {
            var bytes = SyncCodec.WriteInput(new[] { Cmd(1), Cmd(2, 0.5f), Cmd(3), Cmd(4, -1f) });
            var read = new List<InputCommand>();

            Assert.That(SyncCodec.TryReadInput(bytes, read), Is.True);
            Assert.That(read, Has.Count.EqualTo(3));
            Assert.That(read[0].Sequence, Is.EqualTo(2u));
            Assert.That(read[0].Move.x, Is.EqualTo(0.5f));
            Assert.That(read[2].Jump, Is.True);
            Assert.That(read[2].YawDegrees, Is.EqualTo(90f));
        }

        [Test]
        public void InputPacket_Malformed_IsRejected()
        {
            var read = new List<InputCommand>();
            var good = SyncCodec.WriteInput(new[] { Cmd(1) });
            Assert.That(SyncCodec.TryReadInput(null, read), Is.False);
            Assert.That(SyncCodec.TryReadInput(new byte[0], read), Is.False);
            Assert.That(SyncCodec.TryReadInput(good[..^1], read), Is.False, "short");

            var tooMany = (byte[])good.Clone();
            tooMany[0] = 4;
            Assert.That(SyncCodec.TryReadInput(tooMany, read), Is.False, "more than three commands");

            var badButtons = (byte[])good.Clone();
            badButtons[1 + 12] = 0xFF;
            Assert.That(SyncCodec.TryReadInput(badButtons, read), Is.False);
            Assert.That(read, Is.Empty);
        }

        [Test]
        public void InputPacket_OutOfRangeMove_IsClampedOnRead()
        {
            var raw = SyncCodec.WriteInput(new[] { new InputCommand(1, Vector2.zero, false, false, 0f) });
            System.BitConverter.GetBytes(50f).CopyTo(raw, 1 + 4);
            System.BitConverter.GetBytes(720f + 30f).CopyTo(raw, 1 + 13);
            var read = new List<InputCommand>();

            Assert.That(SyncCodec.TryReadInput(raw, read), Is.True);
            Assert.That(read[0].Move.magnitude, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(read[0].YawDegrees, Is.EqualTo(30f).Within(1e-3f));
        }

        [Test]
        public void Inbox_UsesEachSequenceOnceInOrder()
        {
            var inbox = new InputInbox();
            inbox.Offer(new[] { Cmd(1), Cmd(2), Cmd(3) });
            inbox.Offer(new[] { Cmd(2), Cmd(3), Cmd(4) });

            Assert.That(inbox.Pending, Is.EqualTo(4));
            Assert.That(inbox.Take().Sequence, Is.EqualTo(1u));
            Assert.That(inbox.Take().Sequence, Is.EqualTo(2u));
            Assert.That(inbox.LastProcessed, Is.EqualTo(2u));
        }

        [Test]
        public void Inbox_DropsNaNAndCapsFloods()
        {
            var inbox = new InputInbox();
            inbox.Offer(new[] { new InputCommand(1, new Vector2(float.NaN, 0f), false, false, 0f) });
            Assert.That(inbox.Pending, Is.Zero);

            var flood = new List<InputCommand>();
            for (uint i = 1; i <= 50; i++) flood.Add(Cmd(i));
            inbox.Offer(flood);
            Assert.That(inbox.Pending, Is.EqualTo(InputInbox.Capacity));
            Assert.That(inbox.Take().Sequence, Is.EqualTo(45u), "oldest are dropped");
        }

        [Test]
        public void Inbox_Starved_RepeatsButtonsWithoutSteering()
        {
            var inbox = new InputInbox();
            inbox.Offer(new[] { new InputCommand(7, Vector2.up, true, false, 45f) });
            inbox.Take();
            var idle = inbox.Take();

            Assert.That(idle.Move, Is.EqualTo(Vector2.zero));
            Assert.That(idle.Jump, Is.True);
            Assert.That(inbox.LastProcessed, Is.EqualTo(7u));
        }

        [Test]
        public void Snapshot_RoundTripsAndRejectsBadLength()
        {
            var f = new FighterSnapshot(3, 2, new Vector3(1, 2, 3), new Vector3(-4, 0, 5), 7, 1,
                FighterFlags.Dashing | FighterFlags.Stunned, 99);
            var bytes = SyncCodec.WriteSnapshot(new WorldSnapshot(1234, 600, new[] { f, f }));

            Assert.That(SyncCodec.TryReadSnapshot(bytes, out var s), Is.True);
            Assert.That(s.HostTick, Is.EqualTo(1234u));
            Assert.That(s.GoTick, Is.EqualTo(600u));
            Assert.That(s.Fighters.Count, Is.EqualTo(2));
            var r = s.Fighters[1];
            Assert.That((r.Id, r.Fruit, r.Position, r.Velocity, r.Wobble, r.Lives, r.Flags, r.LastInput),
                Is.EqualTo(((ushort)3, (byte)2, f.Position, f.Velocity, (byte)7, (byte)1,
                    FighterFlags.Dashing | FighterFlags.Stunned, 99u)));
            Assert.That(SyncCodec.TryReadSnapshot(bytes[..^1], out _), Is.False);
        }

        static WorldSnapshot At(uint tick, float x) =>
            new(tick, 0, new[] { new FighterSnapshot(1, 0, new Vector3(x, 0, 0), Vector3.zero, 0, 1, 0, 0) });

        [Test]
        public void Buffer_RendersHundredMillisecondsBehindAndInterpolates()
        {
            var buffer = new SnapshotBuffer(1f / 60f, 0.1f);
            buffer.Add(At(60, 0f), 10f);   // host 1.000 s
            buffer.Add(At(66, 1f), 10.1f); // host 1.100 s

            // Render time = 1.100 + 0 − 0.1 = 1.000 → first snapshot.
            Assert.That(buffer.TrySample(1, 10.1f, out var p, out _), Is.True);
            Assert.That(p.x, Is.EqualTo(0f).Within(1e-4f));
            // 50 ms later → halfway.
            Assert.That(buffer.TrySample(1, 10.15f, out p, out _), Is.True);
            Assert.That(p.x, Is.EqualTo(0.5f).Within(1e-3f));
        }

        [Test]
        public void Buffer_IgnoresStaleSnapshots()
        {
            var buffer = new SnapshotBuffer(1f / 60f, 0.1f);
            Assert.That(buffer.Add(At(10, 0f), 0f), Is.True);
            Assert.That(buffer.Add(At(8, 5f), 0.01f), Is.False);
            Assert.That(buffer.Newest.HostTick, Is.EqualTo(10u));
        }
    }
}
