using System.Collections.Generic;

namespace SquishyBump.Core
{
    /// <summary>One public LAN room seen on the network. Never carries the password.</summary>
    public readonly struct RoomInfo
    {
        public RoomInfo(string name, string address, int players, int capacity, MatchRule rule, bool hasPassword)
        { Name = name; Address = address; Players = players; Capacity = capacity; Rule = rule; HasPassword = hasPassword; }
        public string Name { get; }
        /// <summary>IPv4 of the sender as the socket saw it, never taken from the packet.</summary>
        public string Address { get; }
        public int Players { get; }
        public int Capacity { get; }
        public MatchRule Rule { get; }
        public bool HasPassword { get; }
        public bool Full => Players >= Capacity;
    }

    /// <summary>Find Server: public LAN rooms heard while listening. Private rooms are never listed (join by IP).</summary>
    public interface IRoomBrowser
    {
        IReadOnlyList<RoomInfo> Rooms { get; }
        /// <summary>Starts or stops listening; the list is empty when stopped.</summary>
        void Listen(bool on);
    }
}
