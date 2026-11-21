using System;
using System.Collections.Generic;

namespace SquishyBump.Core
{
    public enum SessionMode { SinglePlayer, Lan }
    public enum SessionState { Idle, Hosting, Joining, Joined, Leaving }
    public enum SessionEndReason { None, LocalLeave, ConnectFailed, HostLost, VersionMismatch, RoomFull, InvalidAddress, StartFailed, MatchInProgress }

    /// <summary>Host chỉ mở chỗ mới khi ở lobby, không cho peer mới chen vào roster đang thi đấu.</summary>
    public interface ISessionAdmission
    {
        bool AcceptingJoins { get; set; }
    }

    public interface ISessionService
    {
        SessionState State { get; }
        bool CanSimulateGameplay { get; }
        event Action<SessionState> StateChanged;

        /// <summary>Lý do phiên gần nhất về Idle. Được đặt trước khi StateChanged báo Idle.</summary>
        SessionEndReason LastEndReason { get; }

        /// <summary>Client từ xa đang kết nối, không tính host. Chỉ có dữ liệu trên host.</summary>
        IReadOnlyList<ulong> Peers { get; }
        event Action<ulong> PeerJoined;
        event Action<ulong> PeerLeft;

        // Trả true khi thao tác được khởi động. StateChanged báo kết quả kết nối.
        bool Host(SessionMode mode);
        bool Join(string address, ushort port = 7777);
        void Leave();
    }
}
