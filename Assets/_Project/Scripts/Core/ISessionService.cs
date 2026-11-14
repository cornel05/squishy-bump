using System;

namespace SquishyBump.Core
{
    public enum SessionMode { SinglePlayer, Lan }
    public enum SessionState { Idle, Hosting, Joining, Joined, Leaving }

    public interface ISessionService
    {
        SessionState State { get; }
        bool CanSimulateGameplay { get; }
        event Action<SessionState> StateChanged;

        // Trả true khi thao tác được khởi động. StateChanged báo kết quả kết nối.
        bool Host(SessionMode mode);
        bool Join(string address, ushort port);
        void Leave();
    }
}
