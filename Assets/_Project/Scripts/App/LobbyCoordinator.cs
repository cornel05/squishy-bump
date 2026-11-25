using System;
using System.Linq;
using SquishyBump.Core;
using SquishyBump.Net.Session;

namespace SquishyBump.App
{
    /// <summary>Nối chính sách LobbyRoom với session/clock/wire; adapter đi lại sẽ xác nhận claim trên host.</summary>
    public sealed class LobbyCoordinator : IDisposable
    {
        private readonly ISessionService session;
        private readonly IHostClock clock;
        private readonly ILobbyChannel channel;
        private readonly int tickRate;
        private uint revision;
        private bool disposed;
        public LobbyRoom Room { get; }
        public LobbyState State { get; private set; }
        public event Action<LobbyState> Changed, StartRequested;
        public LobbyCoordinator(ISessionService service, IHostClock hostClock, ILobbyChannel wire,
            int rate, int countdownTicks, MatchRule rule, LobbyState initialState = null)
        {
            if (rate <= 0) throw new ArgumentOutOfRangeException(nameof(rate));
            session = service; clock = hostClock; channel = wire; tickRate = rate;
            revision = initialState?.Revision ?? 0;
            if (session.CanSimulateGameplay)
            {
                Room = new LobbyRoom(0, 5, countdownTicks);
                Room.TryConfigure(0, rule, 5);
                foreach (var id in session.Peers) Room.TryAdd(id);
                State = Snapshot();
            }
            else State = initialState;
            session.PeerJoined += Add; session.PeerLeft += Remove; clock.Ticked += Tick;
            channel.ReadyReceived += OnReady; channel.StateReceived += OnState;
        }
        public bool ClaimFromHostPosition(ulong id, int fruit)
        {
            if (disposed || !session.CanSimulateGameplay) return false;
            if (Room != null && Room.Players.TryGetValue(id, out var current) && current.Fruit == fruit) return true;
            if (disposed || !session.CanSimulateGameplay || Room == null || !Room.TryClaim(id, fruit)) return false;
            Publish(); return true;
        }
        public void SetReady(bool ready)
        {
            if (disposed) return;
            if (session.CanSimulateGameplay) OnReady(0, ready);
            else channel.SendReady(ready);
        }
        public bool Configure(MatchRule rule, int finalLevel)
        {
            if (disposed || !session.CanSimulateGameplay) return false;
            if (Room != null && Room.Rule == rule && Room.FinalLevel == finalLevel) return session.CanSimulateGameplay;
            if (disposed || !session.CanSimulateGameplay || Room == null || !Room.TryConfigure(0, rule, finalLevel)) return false;
            Publish(); return true;
        }
        /// <summary>Host: capacity (2–6) and public/private; the session enforces the same capacity on new connections.</summary>
        public bool ConfigureRoom(int capacity, bool isPublic)
        {
            if (disposed || !session.CanSimulateGameplay || Room == null) return false;
            if (Room.Capacity == capacity && Room.Public == isPublic) return true;
            if (!Room.TryConfigureRoom(0, capacity, isPublic)) return false;
            session.Capacity = capacity; Publish(); return true;
        }
        private void OnReady(ulong id, bool ready)
        {
            if (Room != null && Room.Players.TryGetValue(id, out var current) && current.Ready == ready) return;
            if (!disposed && session.CanSimulateGameplay && Room != null && Room.TrySetReady(id, ready)) Publish();
        }
        private void Add(ulong id)
        { if (!disposed && session.CanSimulateGameplay && Room != null && Room.TryAdd(id)) Publish(); }
        private void Remove(ulong id)
        { if (!disposed && session.CanSimulateGameplay && Room != null && Room.TryRemove(id)) Publish(); }
        private void Tick(uint tick)
        {
            if (disposed || !session.CanSimulateGameplay || Room == null || Room.Started) return;
            var before = Room.RemainingTicks;
            var started = Room.Advance();
            if (started || (Room.RemainingTicks > 0 && (before == 0 || Room.RemainingTicks % tickRate == 0))) Publish();
            if (started) StartRequested?.Invoke(State);
        }
        private LobbyState Snapshot() => new(++revision, Room.Rule, Room.FinalLevel, Room.RemainingTicks, Room.Started,
            Room.Players.OrderBy(p => p.Key).Select(p => new LobbyPlayer(p.Key, p.Value.Fruit ?? -1, p.Value.Ready)),
            Room.Capacity, Room.Public);
        private void Publish()
        { State = Snapshot(); Changed?.Invoke(State); channel.Broadcast(State); }
        private void OnState(LobbyState state)
        {
            if (disposed || session.CanSimulateGameplay || (State != null && state.Revision <= State.Revision)) return;
            State = state; Changed?.Invoke(state);
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            session.PeerJoined -= Add; session.PeerLeft -= Remove; clock.Ticked -= Tick;
            channel.ReadyReceived -= OnReady; channel.StateReceived -= OnState;
        }
    }
}
