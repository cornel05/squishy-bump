namespace SquishyBump.Core
{
    public enum MatchEventKind
    {
        Countdown = 1,
        RingOut,
        Respawn,
        Elimination,
        MatchResult,
        Scoreboard,
    }

    /// <summary>Header của sự kiện truyền tin cậy, có EventId để bỏ bản nhận trùng.</summary>
    public readonly struct MatchEvent
    {
        public MatchEvent(uint eventId, uint hostTick, MatchEventKind kind)
        {
            EventId = eventId;
            HostTick = hostTick;
            Kind = kind;
        }

        public uint EventId { get; }
        public uint HostTick { get; }
        public MatchEventKind Kind { get; }
    }
}
