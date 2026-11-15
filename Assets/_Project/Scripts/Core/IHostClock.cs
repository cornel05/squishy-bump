using System;

namespace SquishyBump.Core
{
    /// <summary>
    /// Đồng hồ mô phỏng của host: 60 Hz, một tick mỗi FixedUpdate khi đang host (GDD 10.4, 10.5).
    /// Tick bắt đầu từ 1 trong mỗi phiên và là 0 khi không host.
    /// </summary>
    public interface IHostClock
    {
        uint Tick { get; }
        /// <summary>Phát trên host sau khi Tick tăng. Tham số là tick vừa mở.</summary>
        event Action<uint> Ticked;
    }
}
