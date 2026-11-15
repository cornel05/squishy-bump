using System.Linq;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Match;
using SquishyBump.Presentation;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SquishyBump.Editor
{
    /// <summary>
    /// Rebuilds Duy's sandbox as a playable L1 host loop: H01 floor, six fruits, camera rig.
    /// Personal scene, so it is overwritten on every run. Menu or batchmode:
    /// -executeMethod SquishyBump.Editor.SandboxBuilder.BuildDuy
    /// </summary>
    public static class SandboxBuilder
    {
        const string Root = "Assets/_Project";
        const string Scene = Root + "/Scenes/Sandbox/Test_Duy.unity";
        static readonly string[] Roster = { "Apple", "Watermelon", "Blueberry", "Pineapple", "Peach", "Mangosteen" };

        [MenuItem("Squishy Bump/Build Duy Sandbox (L1 host loop)")]
        public static void BuildDuy()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var balance = AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{Root}/Data/Levels/L1.asset");

            var net = new GameObject("App", typeof(NetworkManager), typeof(UnityTransport));
            var bootstrap = net.AddComponent<GameplayBootstrap>();

            var floor = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Hazards/TrainingRingFloor.prefab"));

            var arena = new GameObject("Arena");
            var spawns = level.SpawnPoints.Select((p, i) =>
            {
                var t = new GameObject($"Spawn {i + 1}").transform;
                t.SetParent(arena.transform, false);
                t.position = p;
                return (Object)t;
            }).ToArray();
            var bounds = arena.AddComponent<TrainingRingBounds>();
            var so = new SerializedObject(bounds);
            so.FindProperty("outerRadius").floatValue = level.ArenaRadiusMeters;
            so.FindProperty("killPlaneY").floatValue = balance.KillPlaneY;
            SetArray(so.FindProperty("spawnPoints"), spawns);
            so.ApplyModifiedPropertiesWithoutUndo();

            var rig = new GameObject("CameraRig").AddComponent<CameraRig>();
            so = new SerializedObject(rig);
            so.FindProperty("profile").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PresentationProfile>($"{Root}/Data/Presentation/PresentationProfile.asset");
            so.FindProperty("outputCamera").objectReferenceValue = Camera.main;
            so.ApplyModifiedPropertiesWithoutUndo();

            var host = new GameObject("MatchHost").AddComponent<MatchHost>();
            so = new SerializedObject(host);
            so.FindProperty("bootstrap").objectReferenceValue = bootstrap;
            so.FindProperty("balance").objectReferenceValue = balance;
            so.FindProperty("bounds").objectReferenceValue = bounds;
            so.FindProperty("ringDrop").objectReferenceValue = floor.GetComponent<SquishyBump.Hazards.RingDrop>();
            so.FindProperty("cameraRig").objectReferenceValue = rig;
            SetArray(so.FindProperty("fighterPrefabs"), Roster
                .Select(f => (Object)AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/{f}.prefab")).ToArray());
            // The dev panel picks Single Player / Host LAN / Join, so two instances (Multiplayer Play Mode) can meet.
            so.FindProperty("autoHost").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            var panel = host.gameObject.AddComponent<DevSessionPanel>();
            so = new SerializedObject(panel);
            so.FindProperty("bootstrap").objectReferenceValue = bootstrap;
            so.FindProperty("match").objectReferenceValue = host;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, Scene);
            Debug.Log($"[SandboxBuilder] built {Scene}");
        }

        static void SetArray(SerializedProperty list, Object[] values)
        {
            list.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
