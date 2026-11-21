using UnityEngine;

namespace SquishyBump.Core
{
    public enum HazardEventKind
    {
        RingDropWarning = 1,
        IceTileWarning,
        CannonFire,
        TurntableReversal,
        PusherDeparture,
        CannonAim,
        IceTileFading,
        IceTileSolid,
    }

    /// <summary>Sự kiện hazard truyền tin cậy (GDD 10.4). HostTick là tick có hiệu lực.</summary>
    public readonly struct HazardEvent
    {
        public HazardEvent(uint eventId, uint hostTick, HazardEventKind kind, int index = 0,
            Vector3 aim = default, int direction = 0)
        {
            EventId = eventId;
            HostTick = hostTick;
            Kind = kind;
            Index = index;
            Aim = aim;
            Direction = direction;
        }

        public uint EventId { get; }
        public uint HostTick { get; }
        public HazardEventKind Kind { get; }
        /// <summary>Thứ tự của vòng, ô băng, pháo hoặc pusher trong level.</summary>
        public int Index { get; }
        /// <summary>Điểm ngắm của pháo trong toạ độ thế giới. Loại khác: zero.</summary>
        public Vector3 Aim { get; }
        /// <summary>Chiều quay mới của turntable (+1 hoặc -1). Loại khác: 0.</summary>
        public int Direction { get; }
    }
}
