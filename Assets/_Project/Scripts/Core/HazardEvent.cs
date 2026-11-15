namespace SquishyBump.Core
{
    public enum HazardEventKind
    {
        RingDropWarning = 1,
        IceTileWarning,
        CannonFire,
        TurntableReversal,
        PusherDeparture,
    }

    /// <summary>Header của sự kiện truyền tin cậy. HostTick là tick có hiệu lực.</summary>
    public readonly struct HazardEvent
    {
        public HazardEvent(uint eventId, uint hostTick, HazardEventKind kind)
        {
            EventId = eventId;
            HostTick = hostTick;
            Kind = kind;
        }

        public uint EventId { get; }
        public uint HostTick { get; }
        public HazardEventKind Kind { get; }
    }
}
