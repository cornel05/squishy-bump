using System.Linq;
using NUnit.Framework;
using SquishyBump.Core;
using SquishyBump.Net.Session;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquishyBump.Tests.EditMode
{
    public sealed class LobbyRoomTests
    {
        [Test]
        public void FruitClaim_IsExclusiveAndReleasedWhenPlayerLeaves()
        {
            var room = new LobbyRoom(0, 5, 300);
            Assert.That(room.TryAdd(1), Is.True);
            Assert.That(room.TryAdd(1), Is.False);
            Assert.That(room.TrySetReady(1, true), Is.False);
            Assert.That(room.TryClaim(1, 0), Is.True);
            Assert.That(room.TryClaim(0, 0), Is.False);
            Assert.That(room.TryClaim(2, 1), Is.False);
            Assert.That(room.TryClaim(0, 6), Is.False);
            Assert.That(room.TryRemove(1), Is.True);
            Assert.That(room.TryClaim(0, 0), Is.True);
            for (ulong peer = 1; peer <= 5; peer++) Assert.That(room.TryAdd(peer), Is.True);
            Assert.That(room.TryAdd(6), Is.False);
        }

        [Test]
        public void AllReadyAtFiveSeconds_StartsOnceAndLocksRoom()
        {
            const int countdown = 5 * 60;
            var room = new LobbyRoom(0, 5, countdown);
            room.TryClaim(0, 0);
            room.TrySetReady(0, true);
            Assert.That(room.Advance(), Is.False);
            Assert.That(room.RemainingTicks, Is.Zero);
            room.TryAdd(1);
            room.TryClaim(1, 1);
            room.TrySetReady(1, true);
            Assert.That(room.Advance(), Is.False);
            Assert.That(room.RemainingTicks, Is.EqualTo(countdown));
            for (int i = 1; i < countdown; i++) Assert.That(room.Advance(), Is.False);
            Assert.That(room.Advance(), Is.True);
            Assert.That(room.Advance(), Is.False);
            Assert.That(room.TryAdd(2), Is.False);
            Assert.That(room.TryClaim(1, 2), Is.False);
        }

        [Test]
        public void HostChangesAndUnready_CancelCountdown()
        {
            var room = new LobbyRoom(0, 5, 300);
            room.TryAdd(1);
            room.TryClaim(0, 0);
            room.TryClaim(1, 1);
            room.TrySetReady(0, true);
            room.TrySetReady(1, true);
            room.Advance();

            Assert.That(room.TryConfigure(1, MatchRule.Rank, 5), Is.False);
            Assert.That(room.TryConfigure(0, MatchRule.Rank, 6), Is.False);
            Assert.That(room.TryConfigure(0, MatchRule.Rank, 5), Is.True);
            Assert.That(room.Rule, Is.EqualTo(MatchRule.Rank));
            Assert.That(room.FinalLevel, Is.EqualTo(5));
            Assert.That(room.RemainingTicks, Is.Zero);
            room.Advance();
            Assert.That(room.TrySetReady(1, false), Is.True);
            Assert.That(room.Advance(), Is.False);
            Assert.That(room.RemainingTicks, Is.Zero);
        }

        [Test]
        public void RoomOptions_DefaultToSixPublicAndAreHostOnly()
        {
            var room = new LobbyRoom(0, 5, 300);
            Assert.That(room.Capacity, Is.EqualTo(6));
            Assert.That(room.Public, Is.True);
            room.TryAdd(1);

            Assert.That(room.TryConfigureRoom(1, 3, false), Is.False, "clients cannot change the room");
            Assert.That(room.TryConfigureRoom(0, 3, false), Is.True);
            Assert.That(room.Capacity, Is.EqualTo(3));
            Assert.That(room.Public, Is.False);
        }

        [TestCase(1), TestCase(7), TestCase(0), TestCase(-1)]
        public void RoomOptions_RejectCapacityOutsideTwoToSix(int capacity) =>
            Assert.That(new LobbyRoom(0, 5, 300).TryConfigureRoom(0, capacity, true), Is.False);

        [Test]
        public void Capacity_CannotDropBelowThePeopleInside_AndFullRoomRefusesNewcomers()
        {
            var room = new LobbyRoom(0, 5, 300);
            room.TryAdd(1); room.TryAdd(2);

            Assert.That(room.TryConfigureRoom(0, 2, true), Is.False, "3 are inside");
            Assert.That(room.TryConfigureRoom(0, 3, true), Is.True);
            Assert.That(room.TryAdd(3), Is.False, "capacity 3 is full");
            Assert.That(room.TryConfigureRoom(0, 4, true), Is.True);
            Assert.That(room.TryAdd(3), Is.True);
        }

        [Test]
        public void RoomOptions_CancelCountdown()
        {
            var room = new LobbyRoom(0, 5, 5);
            room.TryAdd(1); room.TryClaim(0, 0); room.TryClaim(1, 1);
            room.TrySetReady(0, true); room.TrySetReady(1, true);
            room.Advance();
            Assert.That(room.RemainingTicks, Is.GreaterThan(0));

            room.TryConfigureRoom(0, 4, true);

            Assert.That(room.RemainingTicks, Is.Zero);
        }

        [Test]
        public void LobbyScene_ContainsSixPickVolumesDeskAndBoards()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lobby.unity", OpenSceneMode.Additive);
            try
            {
                var layout = scene.GetRootGameObjects().Single(go => go.name == "Store aisle").transform;
                var stands = layout.Find("Fruit stands");
                Assert.That(stands.childCount, Is.EqualTo(6));
                for (int i = 0; i < stands.childCount; i++)
                    Assert.That(stands.GetChild(i).Find("Pick volume").GetComponent<BoxCollider>().isTrigger, Is.True);
                foreach (var name in new[] { "Floor", "Host desk", "Info board", "Controls board" })
                    Assert.That(layout.Find(name), Is.Not.Null, name);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
