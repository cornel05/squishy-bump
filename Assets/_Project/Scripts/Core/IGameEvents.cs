using System;

namespace SquishyBump.Core
{
    /// <summary>Phía nhận: UI, Presentation và module gameplay đăng ký ở đây, trên cả host lẫn client.</summary>
    public interface IGameEvents
    {
        event Action<HazardEvent> HazardRaised;
        event Action<MatchEvent> MatchRaised;
    }

    /// <summary>Phía phát: chỉ code chạy trên host mới publish. Lấy EventId từ NextEventId.</summary>
    public interface IGameEventSink
    {
        uint NextEventId();
        void Publish(HazardEvent gameEvent);
        void Publish(MatchEvent gameEvent);
    }
}
