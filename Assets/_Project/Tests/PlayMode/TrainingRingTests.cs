using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SquishyBump.Match;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class TrainingRingTests
    {
        [UnityTest]
        public IEnumerator L1Scene_ProvidesFourBandsAndSixSpawns()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Levels/L1.unity");
            var arena = Object.FindAnyObjectByType<TrainingRingBounds>();
            Assert.That(arena, Is.Not.Null);
            Assert.That(arena.OuterRadius, Is.EqualTo(12f));
            Assert.That(arena.KillPlaneY, Is.EqualTo(-5f));
            Assert.That(arena.SpawnPoints.Count, Is.EqualTo(6));

            for (int i = 0; i < 6; i++)
            {
                var point = arena.SpawnPoints[i].position;
                var next = arena.SpawnPoints[(i + 1) % 6].position;
                Assert.That(point.y, Is.Zero);
                Assert.That(point.magnitude, Is.EqualTo(6f).Within(0.001f));
                Assert.That(Vector3.Angle(point, next), Is.EqualTo(60f).Within(0.01f));
            }

            var bands = new HashSet<Collider>();
            for (int i = 0; i < 4; i++)
            {
                Assert.That(Physics.Raycast(new Vector3(1.5f + i * 3f, 2f, 0f),
                    Vector3.down, out var hit, 3f), Is.True);
                Assert.That(hit.point.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(bands.Add(hit.collider), Is.True, "Mỗi vành cần có collider riêng.");
            }

            Assert.That(Physics.Raycast(new Vector3(12.1f, 2f, 0f), Vector3.down, 3f), Is.False);
        }

        [UnityTest]
        public IEnumerator HostSamples_ReportEachRingOutOnceUntilRespawn()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Levels/L1.unity");
            var arena = Object.FindAnyObjectByType<TrainingRingBounds>();
            Assert.That(arena, Is.Not.Null);
            var received = new List<(ulong, uint, RingOutReason)>();
            arena.RingOut += (fighter, tick, reason) => received.Add((fighter, tick, reason));

            arena.SampleOnHost(1, 1, new Vector3(12f, -5f, 0f));
            Assert.That(received, Is.Empty);
            arena.SampleOnHost(1, 2, new Vector3(12.01f, 0f, 0f));
            arena.SampleOnHost(1, 3, new Vector3(0f, -5.01f, 0f));
            arena.SampleOnHost(2, 4, new Vector3(0f, -5.01f, 0f));
            arena.ResetFighterOnHost(1);
            arena.SampleOnHost(1, 5, new Vector3(0f, -5.01f, 0f));

            CollectionAssert.AreEqual(new[]
            {
                (1UL, 2U, RingOutReason.OutsideArena),
                (2UL, 4U, RingOutReason.BelowKillPlane),
                (1UL, 5U, RingOutReason.BelowKillPlane),
            }, received);
        }
    }
}
