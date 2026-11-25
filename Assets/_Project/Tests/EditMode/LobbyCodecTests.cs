using System;
using System.Linq;
using System.Collections.Generic;
using SquishyBump.App;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Net.Session;

namespace SquishyBump.Tests.EditMode
{
    public sealed class LobbyCodecTests
    {
        private sealed class Session : ISessionService
        {
            public bool IsHost = true;
            private readonly List<ulong> peers = new() { 1 };
            public SessionState State => IsHost ? SessionState.Hosting : SessionState.Joined;
            public bool CanSimulateGameplay => IsHost;
            public SessionEndReason LastEndReason => SessionEndReason.None;
            public IReadOnlyList<ulong> Peers => peers;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public event Action<ulong> PeerJoined, PeerLeft;
            public void Remove(ulong id) { peers.Remove(id); PeerLeft?.Invoke(id); }
            public void Add(ulong id) { peers.Add(id); PeerJoined?.Invoke(id); }
            public bool Host(SessionMode mode) => false;
            public bool Join(string address, ushort port = 7777, string password = null) => false;
            public int Capacity { get; set; } = 6;
            public string Password { get; set; }
            public void Leave() { }
        }
        private sealed class Clock : IHostClock
        {
            public uint Tick { get; private set; }
            public event Action<uint> Ticked;
            public void Advance(int count) { for (var i = 0; i < count; i++) Ticked?.Invoke(++Tick); }
        }
        private sealed class Channel : ILobbyChannel
        {
            public event Action<LobbyState> StateReceived;
            public event Action<ulong, bool> ReadyReceived;
            public int SentStates, SentReady;
            public void Broadcast(LobbyState state) { Assert.That(LobbyCodec.TryRead(LobbyCodec.Write(state), out _), Is.True); SentStates++; }
            public void SendReady(bool ready) => SentReady++;
            public void State(LobbyState state) => StateReceived?.Invoke(state);
            public void Ready(ulong sender, bool ready) => ReadyReceived?.Invoke(sender, ready);
        }
        [Test]
        public void Coordinator_BindsMembershipReadyCountdownAndStartsExactlyOnce()
        {
            var session = new Session(); var clock = new Clock(); var wire = new Channel();
            using var coordinator = new LobbyCoordinator(session, clock, wire, 60, 300, MatchRule.Rank);
            var starts = 0; coordinator.StartRequested += _ => starts++;
            clock.Advance(120); Assert.That(wire.SentStates, Is.Zero, "Không spam state mỗi tick khi chưa ready.");
            Assert.That(coordinator.ClaimFromHostPosition(0, 0), Is.True);
            Assert.That(coordinator.ClaimFromHostPosition(1, 1), Is.True);
            coordinator.SetReady(true); wire.Ready(1, true); clock.Advance(1);
            Assert.That(coordinator.State.RemainingTicks, Is.EqualTo(300));
            clock.Advance(299); Assert.That(starts, Is.Zero);
            clock.Advance(1); Assert.That(starts, Is.EqualTo(1));
            clock.Advance(120); Assert.That(starts, Is.EqualTo(1));
        }
        [Test]
        public void Coordinator_LeaveCancelsCountdownAndReleasesFruit_DisposeUnsubscribes()
        {
            var session = new Session(); var clock = new Clock(); var wire = new Channel();
            var coordinator = new LobbyCoordinator(session, clock, wire, 60, 300, MatchRule.Survival);
            coordinator.ClaimFromHostPosition(0, 0); coordinator.ClaimFromHostPosition(1, 1);
            coordinator.SetReady(true); wire.Ready(1, true); clock.Advance(1);
            session.Remove(1); Assert.That(coordinator.State.RemainingTicks, Is.Zero);
            session.Add(2); Assert.That(coordinator.ClaimFromHostPosition(2, 1), Is.True);
            var sent = wire.SentStates; coordinator.Dispose(); clock.Advance(301); wire.Ready(2, true);
            Assert.That(wire.SentStates, Is.EqualTo(sent));
        }
        [Test]
        public void ClientCoordinator_OnlyRequestsReadyAndIgnoresStaleState()
        {
            var session = new Session { IsHost = false }; var wire = new Channel(); var clock = new Clock();
            using var coordinator = new LobbyCoordinator(session, clock, wire, 60, 300, MatchRule.Rank);
            coordinator.SetReady(true); Assert.That(wire.SentReady, Is.EqualTo(1));
            Assert.That(coordinator.Configure(MatchRule.Rank, 5), Is.False);
            Assert.That(coordinator.ClaimFromHostPosition(1, 0), Is.False);
            wire.State(new LobbyState(2, MatchRule.Rank, 5, 0, false, new[] { new LobbyPlayer(0, -1, false) }));
            wire.State(new LobbyState(1, MatchRule.Survival, 1, 0, false, new[] { new LobbyPlayer(0, -1, false) }));
            Assert.That(coordinator.State.Rule, Is.EqualTo(MatchRule.Rank));
            clock.Advance(301); Assert.That(wire.SentStates, Is.Zero);
        }
        [TestCase(1)] [TestCase(2)] [TestCase(6)]
        public void State_RoundTripsEverySupportedRoomSize(int size)
        {
            var state = new LobbyState(10, MatchRule.Rank, 5, size == 1 ? 0 : 300, false,
                Enumerable.Range(0, size).Select(i => new LobbyPlayer((ulong)i, i, true)));
            var data = LobbyCodec.Write(state);
            Assert.That(data.Length, Is.EqualTo(15 + size * 10));
            Assert.That(LobbyCodec.TryRead(data, out var result), Is.True);
            Assert.That(result.Revision, Is.EqualTo(10)); Assert.That(result.Rule, Is.EqualTo(MatchRule.Rank));
            Assert.That(result.FinalLevel, Is.EqualTo(5)); Assert.That(result.RemainingTicks, Is.EqualTo(size == 1 ? 0 : 300));
            CollectionAssert.AreEqual(state.Players.Select(p => (p.Id, p.Fruit, p.Ready)), result.Players.Select(p => (p.Id, p.Fruit, p.Ready)));
        }
        [Test]
        public void UnknownOrTruncatedOrExtraBytes_RejectWholePacket()
        {
            var state = new LobbyState(1, MatchRule.Survival, 1, 0, false, new[] { new LobbyPlayer(0, -1, false) });
            var data = LobbyCodec.Write(state);
            Assert.That(LobbyCodec.TryRead(null, out _), Is.False);
            for (var i = 0; i < data.Length; i++) Assert.That(LobbyCodec.TryRead(data.Take(i).ToArray(), out _), Is.False);
            Assert.That(LobbyCodec.TryRead(data.Concat(new byte[1]).ToArray(), out _), Is.False);
            data[0] = 255; Assert.That(LobbyCodec.TryRead(data, out _), Is.False);
            data = LobbyCodec.Write(state); data[11] = 2; Assert.That(LobbyCodec.TryRead(data, out _), Is.False);
            data = LobbyCodec.Write(state); data[data.Length - 1] = 2; Assert.That(LobbyCodec.TryRead(data, out _), Is.False);
        }
        [Test]
        public void ConflictingFruitIdsAndReadyWithoutFruit_CannotSerialize()
        {
            foreach (var rows in new[] {
                new[] { new LobbyPlayer(0, 0, true), new LobbyPlayer(0, 1, true) },
                new[] { new LobbyPlayer(0, 0, true), new LobbyPlayer(1, 0, true) },
                new[] { new LobbyPlayer(0, -1, true) },
                new[] { new LobbyPlayer(0, 6, false) } })
                Assert.Throws<ArgumentException>(() => LobbyCodec.Write(new LobbyState(1, MatchRule.Rank, 5, 0, false, rows)));
        }
        [Test]
        public void CountdownRequiresTwoReadyPlayers_AndSourceListIsImmutable()
        {
            var rows = new[] { new LobbyPlayer(0, 0, true), new LobbyPlayer(1, 1, false) };
            Assert.Throws<ArgumentException>(() => LobbyCodec.Write(new LobbyState(1, MatchRule.Rank, 5, 300, false, rows)));
            rows[1] = new LobbyPlayer(1, 1, true);
            var state = new LobbyState(1, MatchRule.Rank, 5, 300, false, rows);
            rows[1] = new LobbyPlayer(1, 1, false);
            Assert.That(state.Players[1].Ready, Is.True);
            Assert.That(LobbyCodec.TryRead(LobbyCodec.Write(state), out _), Is.True);
        }
        [Test]
        public void RoomPolicy_ProducesCodecValidStateAcrossSelectionCountdownAndLeave()
        {
            var room = new LobbyRoom(0, 5, 300); uint revision = 0;
            void Check()
            {
                var snapshot = new LobbyState(++revision, room.Rule, room.FinalLevel, room.RemainingTicks, room.Started,
                    room.Players.Select(p => new LobbyPlayer(p.Key, p.Value.Fruit ?? -1, p.Value.Ready)));
                Assert.That(LobbyCodec.TryRead(LobbyCodec.Write(snapshot), out _), Is.True);
            }
            Check(); room.TryAdd(7); Check(); room.TryClaim(0, 1); room.TryClaim(7, 4); Check();
            room.TrySetReady(0, true); room.TrySetReady(7, true); Check(); room.Advance(); Check();
            room.TryRemove(7); Check(); room.TryAdd(8); room.TryClaim(8, 4); room.TrySetReady(8, true);
            for (var tick = 0; tick <= 300; tick++) { room.Advance(); Check(); }
            Assert.That(room.Started, Is.True);
        }

        [Test]
        public void RoundTrip_KeepsCapacityAndPublicFlag()
        {
            var state = new LobbyState(3, MatchRule.Survival, 2, 0, false,
                new[] { new LobbyPlayer(0, 1, false), new LobbyPlayer(1, -1, false) }, 3, false);

            Assert.That(LobbyCodec.TryRead(LobbyCodec.Write(state), out var read), Is.True);

            Assert.That((read.Capacity, read.Public), Is.EqualTo((3, false)));
        }

        [Test]
        public void TryRead_RejectsBadCapacityOrFlagBytes()
        {
            var state = new LobbyState(3, MatchRule.Survival, 2, 0, false,
                new[] { new LobbyPlayer(0, 1, false), new LobbyPlayer(1, -1, false) }, 4, true);
            var data = LobbyCodec.Write(state);

            foreach (var capacity in new byte[] { 0, 1, 7, 255 })
            {
                var bad = (byte[])data.Clone(); bad[13] = capacity;
                Assert.That(LobbyCodec.TryRead(bad, out _), Is.False, $"capacity {capacity}");
            }
            var below = (byte[])data.Clone(); below[13] = 1;
            Assert.That(LobbyCodec.TryRead(below, out _), Is.False, "below the two players inside");
            var flag = (byte[])data.Clone(); flag[14] = 2;
            Assert.That(LobbyCodec.TryRead(flag, out _), Is.False);
        }

        [Test]
        public void Write_RejectsMorePlayersThanCapacity()
        {
            var state = new LobbyState(1, MatchRule.Survival, 1, 0, false,
                new[] { new LobbyPlayer(0, -1, false), new LobbyPlayer(1, -1, false), new LobbyPlayer(2, -1, false) }, 2, true);

            Assert.Throws<ArgumentException>(() => LobbyCodec.Write(state));
        }
    }
}
