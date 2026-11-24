using System;
using System.Collections.Generic;

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
        SessionResult,
    }

    /// <summary>Một dòng của kết quả trận hoặc bảng điểm phiên.</summary>
    public readonly struct MatchStanding
    {
        public MatchStanding(ulong fighterId, int rank, int points, int lives = 0, int survivalTicks = 0)
        {
            FighterId = fighterId;
            Rank = rank;
            Points = points;
            Lives = lives;
            SurvivalTicks = survivalTicks;
        }

        public ulong FighterId { get; }
        public int Rank { get; }
        public int Points { get; }
        public int Lives { get; }
        public int SurvivalTicks { get; }
    }

    /// <summary>
    /// Sự kiện trận truyền tin cậy (GDD 10.4), có EventId để bỏ bản nhận trùng.
    /// Dùng các hàm tạo tĩnh để điền đúng payload cho từng loại.
    /// </summary>
    public readonly struct MatchEvent
    {
        /// <summary>Giá trị của FighterId hoặc CreditedFighterId khi không có fighter nào.</summary>
        public const ulong NoFighter = ulong.MaxValue;

        private readonly IReadOnlyList<MatchStanding> standings;

        public MatchEvent(uint eventId, uint hostTick, MatchEventKind kind)
            : this(eventId, hostTick, kind, NoFighter, NoFighter, 0, null)
        {
        }

        private MatchEvent(uint eventId, uint hostTick, MatchEventKind kind, ulong fighterId,
            ulong creditedFighterId, int value, IReadOnlyList<MatchStanding> standings)
        {
            EventId = eventId;
            HostTick = hostTick;
            Kind = kind;
            FighterId = fighterId;
            CreditedFighterId = creditedFighterId;
            Value = value;
            // Payload không đổi khi caller tái sử dụng danh sách sau Publish.
            this.standings = standings == null ? null : Array.AsReadOnly(new List<MatchStanding>(standings).ToArray());
        }

        public uint EventId { get; }
        public uint HostTick { get; }
        public MatchEventKind Kind { get; }
        public ulong FighterId { get; }
        /// <summary>Fighter được tính điểm ring-out theo luật chạm cuối, hoặc NoFighter.</summary>
        public ulong CreditedFighterId { get; }
        /// <summary>Countdown: số giây còn lại. Elimination: thứ hạng. Loại khác: 0.</summary>
        public int Value { get; }
        // Không bao giờ null, kể cả với default(MatchEvent).
        public IReadOnlyList<MatchStanding> Standings => standings ?? Array.Empty<MatchStanding>();

        public static MatchEvent Countdown(uint eventId, uint hostTick, int secondsRemaining) =>
            new MatchEvent(eventId, hostTick, MatchEventKind.Countdown, NoFighter, NoFighter, secondsRemaining, null);

        public static MatchEvent RingOut(uint eventId, uint hostTick, ulong fighterId, ulong creditedFighterId = NoFighter) =>
            new MatchEvent(eventId, hostTick, MatchEventKind.RingOut, fighterId, creditedFighterId, 0, null);

        public static MatchEvent Respawn(uint eventId, uint hostTick, ulong fighterId) =>
            new MatchEvent(eventId, hostTick, MatchEventKind.Respawn, fighterId, NoFighter, 0, null);

        public static MatchEvent Elimination(uint eventId, uint hostTick, ulong fighterId, int rank) =>
            new MatchEvent(eventId, hostTick, MatchEventKind.Elimination, fighterId, NoFighter, rank, null);

        public static MatchEvent MatchResult(uint eventId, uint hostTick, IReadOnlyList<MatchStanding> standings) =>
            new MatchEvent(eventId, hostTick, MatchEventKind.MatchResult, NoFighter, NoFighter, 0, standings);

        public static MatchEvent Scoreboard(uint eventId, uint hostTick, IReadOnlyList<MatchStanding> standings) =>
            new MatchEvent(eventId, hostTick, MatchEventKind.Scoreboard, NoFighter, NoFighter, 0, standings);

        public static MatchEvent SessionResult(uint eventId, uint hostTick, IReadOnlyList<MatchStanding> standings) =>
            new MatchEvent(eventId, hostTick, MatchEventKind.SessionResult, NoFighter, NoFighter, 0, standings);
    }
}
