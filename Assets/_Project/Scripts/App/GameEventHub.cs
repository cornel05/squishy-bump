using System;
using SquishyBump.Core;

namespace SquishyBump.App
{
    /// <summary>
    /// Kênh event của một phiên. Host gọi Publish; phần đồng bộ mạng gọi Deliver phía client.
    /// EventId tăng dần trong phiên, nên bản có ID không lớn hơn ID cuối là bản lặp và bị bỏ.
    /// </summary>
    public sealed class GameEventHub : IGameEvents, IGameEventSink
    {
        private readonly Func<bool> hasHostAuthority;
        private uint nextEventId;
        private uint lastHazardId;
        private uint lastMatchId;

        public GameEventHub(Func<bool> hasHostAuthority)
        {
            this.hasHostAuthority = hasHostAuthority ?? throw new ArgumentNullException(nameof(hasHostAuthority));
        }

        public event Action<HazardEvent> HazardRaised;
        public event Action<MatchEvent> MatchRaised;

        public uint NextEventId() => ++nextEventId;

        public void Publish(HazardEvent gameEvent)
        {
            if (hasHostAuthority())
                Deliver(gameEvent);
        }

        public void Publish(MatchEvent gameEvent)
        {
            if (hasHostAuthority())
                Deliver(gameEvent);
        }

        public void Deliver(HazardEvent gameEvent)
        {
            if (gameEvent.EventId <= lastHazardId)
                return;
            lastHazardId = gameEvent.EventId;
            HazardRaised?.Invoke(gameEvent);
        }

        public void Deliver(MatchEvent gameEvent)
        {
            if (gameEvent.EventId <= lastMatchId)
                return;
            lastMatchId = gameEvent.EventId;
            MatchRaised?.Invoke(gameEvent);
        }

        public void Reset() => nextEventId = lastHazardId = lastMatchId = 0;
    }
}
