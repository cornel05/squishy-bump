#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>SB-15: the host loop on L1 — spawn, countdown, motors + resolver each tick, ring-out ends the match.</summary>
    public sealed class MatchHostTests
    {
        const string Root = "Assets/_Project";
        GameObject hostObject;

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1f;
            if (hostObject != null) Object.Destroy(hostObject);
            var app = Object.FindAnyObjectByType<GameplayBootstrap>();
            if (app != null) app.Session.Leave();
            // L1's floor must not stay under later tests.
            var l1 = SceneManager.GetSceneByPath($"{Root}/Scenes/Levels/L1.unity");
            if (l1.isLoaded) yield return SceneManager.UnloadSceneAsync(l1);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DuySandbox_HostsAndSpawnsSixFruits()
        {
            // Sandboxes are not in the build list.
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                $"{Root}/Scenes/Sandbox/Test_Duy.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var host = Object.FindAnyObjectByType<MatchHost>();
            hostObject = host.gameObject;
            var app = Object.FindAnyObjectByType<GameplayBootstrap>();
            Assert.That(host.Started, Is.False, "sandbox waits for the dev panel");
            Assert.That(app.Session.Host(Core.SessionMode.SinglePlayer), Is.True);
            host.StartMatch();
            for (var i = 0; i < 10; i++) yield return new WaitForFixedUpdate();

            Assert.That(host.Fighters, Has.Count.EqualTo(6));
            // Six fighters fit the ~200-byte snapshot of GDD 10.4.
            Assert.That(SquishyBump.Net.Sync.SyncCodec.WriteSnapshot(host.Snapshot(app.Clock.Tick)).Length, Is.EqualTo(209));
            Assert.That(host.Director.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(app.Clock.Tick, Is.GreaterThan(5u));
        }

        [UnityTest]
        public IEnumerator L1_RingOutOfOneOfTwo_ConfirmsResult()
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Levels/L1.unity", LoadSceneMode.Additive);
            var app = Object.FindAnyObjectByType<GameplayBootstrap>();
            var bounds = Object.FindAnyObjectByType<TrainingRingBounds>();

            hostObject = new GameObject("MatchHost");
            hostObject.SetActive(false);
            var host = hostObject.AddComponent<MatchHost>();
            var so = new SerializedObject(host);
            so.FindProperty("bootstrap").objectReferenceValue = app;
            so.FindProperty("balance").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");
            so.FindProperty("bounds").objectReferenceValue = bounds;
            var prefabs = so.FindProperty("fighterPrefabs");
            prefabs.arraySize = 2;
            prefabs.GetArrayElementAtIndex(0).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/Apple.prefab");
            prefabs.GetArrayElementAtIndex(1).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/Watermelon.prefab");
            so.ApplyModifiedPropertiesWithoutUndo();
            hostObject.SetActive(true);
            yield return null;

            Assert.That(app.Session.State, Is.EqualTo(Core.SessionState.Hosting));
            Assert.That(host.Fighters, Has.Count.EqualTo(2));
            Assert.That(host.Fighters.All(f => f.Motor.gameObject.layer == CollisionResolver.FighterLayer));
            Assert.That(Vector3.Distance(host.Fighters[1].Motor.transform.position, bounds.SpawnPoints[1].position),
                Is.LessThan(1f), "spawned at the level's spawn point");

            Time.timeScale = 10f;
            var deadline = Time.realtimeSinceStartup + 10f;
            while (host.Director.Phase != MatchPhase.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.Director.Phase, Is.EqualTo(MatchPhase.Playing), "countdown ran on host ticks");

            host.Fighters[1].Motor.GetComponent<Rigidbody>().position = new Vector3(0f, bounds.KillPlaneY - 1f, 0f);
            deadline = Time.realtimeSinceStartup + 10f;
            while (host.Director.Phase != MatchPhase.Result && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.That(host.Director.Phase, Is.EqualTo(MatchPhase.Result));
            Assert.That(host.Director.Placements[0].FighterId, Is.EqualTo(0UL));
            Assert.That(host.Fighters[1].Out, Is.True);
        }
    }
}
#endif
