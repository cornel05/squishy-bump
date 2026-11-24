using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Net.Session;

namespace SquishyBump.Tests.EditMode
{
    /// <summary>SB-77: the LAN room beacon is parsed strictly and rooms expire; it never carries a password.</summary>
    public sealed class LanDiscoveryTests
    {
        private static RoomInfo Room(string name = "Duy", string address = "192.168.1.5", int players = 2, int capacity = 4) =>
            new(name, address, players, capacity, MatchRule.Rank, true);

        [Test]
        public void EncodeDecode_RoundTripsARoom()
        {
            var data = LanDiscovery.Encode("Duy's PC", 2, 4, MatchRule.Rank, true);

            Assert.That(LanDiscovery.TryDecode(data, "192.168.1.5", out var room), Is.True);

            Assert.That((room.Name, room.Address, room.Players, room.Capacity, room.Rule, room.HasPassword),
                Is.EqualTo(("Duy's PC", "192.168.1.5", 2, 4, MatchRule.Rank, true)));
        }

        [Test]
        public void Decode_TakesTheAddressFromTheSocket_NotThePacket() =>
            Assert.That(LanDiscovery.TryDecode(LanDiscovery.Encode("10.0.0.99", 1, 2, MatchRule.Survival, false), "192.168.1.7", out var room) && room.Address == "192.168.1.7", Is.True);

        [Test]
        public void Encode_CutsLongNamesAtTheLimit()
        {
            var data = LanDiscovery.Encode(new string('x', 100), 1, 6, MatchRule.Survival, false);

            Assert.That(LanDiscovery.TryDecode(data, "1.2.3.4", out var room), Is.True);
            Assert.That(room.Name.Length, Is.EqualTo(LanDiscovery.MaxNameBytes));
        }

        [Test]
        public void Decode_StripsControlCharacters_AndFallsBackToTheAddressWhenNameIsEmpty()
        {
            LanDiscovery.TryDecode(LanDiscovery.Encode("a\nb\tc", 1, 6, MatchRule.Survival, false), "1.2.3.4", out var cleaned);
            LanDiscovery.TryDecode(LanDiscovery.Encode("", 1, 6, MatchRule.Survival, false), "1.2.3.4", out var unnamed);

            Assert.That(cleaned.Name, Is.EqualTo("abc"));
            Assert.That(unnamed.Name, Is.EqualTo("1.2.3.4"));
        }

        [Test]
        public void Decode_RejectsMalformedPackets()
        {
            var good = LanDiscovery.Encode("Duy", 2, 4, MatchRule.Rank, false);
            Assert.That(LanDiscovery.TryDecode(null, "1.2.3.4", out _), Is.False);
            Assert.That(LanDiscovery.TryDecode(good, "", out _), Is.False);
            for (var i = 0; i < good.Length; i++)
                Assert.That(LanDiscovery.TryDecode(good.Take(i).ToArray(), "1.2.3.4", out _), Is.False, $"truncated to {i}");
            Assert.That(LanDiscovery.TryDecode(good.Concat(new byte[] { 0 }).ToArray(), "1.2.3.4", out _), Is.False, "trailing byte");

            void Corrupt(int index, byte value, string why)
            {
                var bad = (byte[])good.Clone(); bad[index] = value;
                Assert.That(LanDiscovery.TryDecode(bad, "1.2.3.4", out _), Is.False, why);
            }
            Corrupt(0, 0, "magic");
            Corrupt(4, 0, "no players");
            Corrupt(4, 5, "more players than capacity");
            Corrupt(5, 1, "capacity below two");
            Corrupt(5, 7, "capacity above six");
            Corrupt(6, 9, "unknown rule");
            Corrupt(7, 2, "password flag");
            Corrupt(8, 200, "name length");
        }

        [Test]
        public void Rooms_ExpireWhenNotHeardForAFewSeconds_AndSortByName()
        {
            var discovery = new LanDiscovery();
            discovery.Hear(Room("Zed", "10.0.0.2"), 0);
            discovery.Hear(Room("Amy", "10.0.0.1"), 1000);

            Assert.That(discovery.Snapshot(2000).Select(r => r.Name), Is.EqualTo(new[] { "Amy", "Zed" }));
            Assert.That(discovery.Snapshot(LanDiscovery.ExpireMilliseconds + 500).Select(r => r.Name), Is.EqualTo(new[] { "Amy" }));
            Assert.That(discovery.Snapshot(LanDiscovery.ExpireMilliseconds + 1500), Is.Empty);
        }

        [Test]
        public void Hear_UpdatesTheSameHostInsteadOfListingItTwice()
        {
            var discovery = new LanDiscovery();
            discovery.Hear(Room("Duy", "10.0.0.1", 1), 0);
            discovery.Hear(Room("Duy", "10.0.0.1", 3), 500);

            var rooms = discovery.Snapshot(600);

            Assert.That(rooms.Count, Is.EqualTo(1));
            Assert.That(rooms[0].Players, Is.EqualTo(3));
        }

        [Test]
        public void Beacon_NeverContainsAPasswordOnlyAFlag()
        {
            var data = LanDiscovery.Encode("Duy", 2, 4, MatchRule.Rank, true);

            Assert.That(data.Length, Is.EqualTo(9 + 3), "header + name only");
        }
    }
}
