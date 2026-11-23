#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;
using SquishyBump.Presentation;
using SquishyBump.UI.Menus;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    /// <summary>
    /// SB-21 LAN bug: the host's Esc menu does not pause a LAN match, so a hit-stop that lands while it is
    /// open must still give time back; before the fix the host stayed at timeScale 0 and both windows froze.
    /// </summary>
    public sealed class HitStopTests
    {
        const string Root = "Assets/_Project";
        GameObject hostObject;

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            if (hostObject != null) Object.Destroy(hostObject);
            foreach (var app in Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None))
            {
                app.Session.Leave();
                Object.Destroy(app.gameObject);
            }
            var l1 = SceneManager.GetSceneByPath($"{Root}/Scenes/Levels/L1.unity");
            if (l1.isLoaded) yield return SceneManager.UnloadSceneAsync(l1);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HitStop_WhileLanPauseMenuOpen_RestoresTime()
        {
            // Arrange: a host match with the LAN Esc menu (does not pause) open.
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Levels/L1.unity", LoadSceneMode.Additive);
            var app = Object.FindAnyObjectByType<GameplayBootstrap>();
            app.Store = new LocalSave(System.IO.Path.Combine(Application.temporaryCachePath, "sb-tests"));

            hostObject = new GameObject("MatchHost");
            hostObject.SetActive(false);
            var host = hostObject.AddComponent<MatchHost>();
            var so = new SerializedObject(host);
            so.FindProperty("bootstrap").objectReferenceValue = app;
            so.FindProperty("balance").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");
            so.FindProperty("bounds").objectReferenceValue = Object.FindAnyObjectByType<TrainingRingBounds>();
            var prefabs = so.FindProperty("fighterPrefabs");
            prefabs.arraySize = 2;
            prefabs.GetArrayElementAtIndex(0).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/Cat.prefab");
            prefabs.GetArrayElementAtIndex(1).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/Bear.prefab");
            so.ApplyModifiedPropertiesWithoutUndo();
            var profile = AssetDatabase.LoadAssetAtPath<PresentationProfile>($"{Root}/Data/Presentation/PresentationProfile.asset");
            var audio = hostObject.AddComponent<AudioDirector>();
            audio.Configure(profile);
            host.AttachAudio(audio);
            host.ConfigureFeedback(profile.HitStopSeconds, profile.WhooshDistance);
            hostObject.SetActive(true);
            yield return null;

            var pause = hostObject.AddComponent<PauseMenu>();
            pause.Init(app.Settings, false);
            host.AttachPause(pause, false, () => { });
            pause.SetOpen(true);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "the LAN Esc menu does not pause");

            // Act: the hit-stop a confirmed hit on the local fruit starts.
            var hitStop = typeof(MatchHost).GetMethod("HitStop", BindingFlags.Instance | BindingFlags.NonPublic);
            host.StartCoroutine((IEnumerator)hitStop.Invoke(host, null));
            Assert.That(Time.timeScale, Is.EqualTo(0f), "hit-stop freezes time");
            yield return new WaitForSecondsRealtime(profile.HitStopSeconds + 0.2f);

            // Assert
            Assert.That(Time.timeScale, Is.EqualTo(1f), "hit-stop gave time back with the LAN menu open");
        }
    }
}
#endif
