using UnityEngine;

namespace SquishyBump.Core
{
    /// <summary>
    /// A level hazard (GDD 7). The host steps it once per playing tick; clients only show it at the host time
    /// they render. Implementations live in Hazards; App finds them in the level scene and wires them.
    /// </summary>
    public interface IHazard
    {
        /// <summary>Host only: where hazard events go and how fighters are pushed.</summary>
        void Bind(IGameEventSink sink, IFighterPhysics fighters);

        /// <summary>Host only. <paramref name="playingTicks"/> counts ticks since GO.</summary>
        void StepOnHost(int playingTicks, uint hostTick, float dt);

        /// <summary>Clients: pose the hazard for <paramref name="playingSeconds"/> since GO (deterministic parts only).</summary>
        void ShowAt(float playingSeconds);
    }

    /// <summary>Vùng đang tác động/cảnh báo cho bộ chọn điểm hồi sinh; không cấp quyền sửa hazard.</summary>
    public interface IHazardZones
    {
        System.Collections.Generic.IReadOnlyList<HazardZone> ThreatenedZones(int playingTick, float dt);
    }

    /// <summary>Host-side access to fighters for hazards, without a Physics dependency (ADR-0003).</summary>
    public interface IFighterPhysics
    {
        /// <summary>Fighters still in the arena.</summary>
        System.Collections.Generic.IReadOnlyCollection<ulong> FighterIds { get; }
        Vector3 GetPosition(ulong fighterId);
        bool TryGetFighter(Collider collider, out ulong fighterId);
        int GetWobble(ulong fighterId);
        /// <summary>Adds an impulse in N·s (Δv = J / m).</summary>
        void AddImpulse(ulong fighterId, Vector3 impulse);
        void AddWobble(ulong fighterId, int amount);
    }

    /// <summary>A moving floor that reports its surface velocity (GDD 6.5: v_surface = ω × r_c).</summary>
    public interface ISurfaceMotion
    {
        Vector3 VelocityAt(Vector3 worldPoint);
    }
}
