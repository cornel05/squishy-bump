using System.IO;
using System.Linq;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SquishyBump.Editor
{
    /// <summary>Tạo L4/L5 bằng Editor API; không ghi tay YAML/GUID. Không sửa scene L2/L3.</summary>
    public static class IceSceneBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ProfilePath = Root + "/Data/Hazards/IceProfile.asset";
        [MenuItem("Squishy Bump/Build Level L4 (Frost Ring)")]
        public static void BuildL4() => Build(4);

        [MenuItem("Squishy Bump/Build Level L5 (Chaos Core)")]
        public static void BuildL5() => Build(5);

        private static void Build(int number)
        {
            var profile = AssetDatabase.LoadAssetAtPath<IceProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<IceProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var balance = AssetDatabase.LoadAssetAtPath<BalanceProfile>(Root + "/Data/Balance/BalanceProfile.asset");
            var hazards = AssetDatabase.LoadAssetAtPath<HazardProfile>(Root + "/Data/Hazards/HazardProfile.asset");
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{Root}/Data/Levels/L{number}.asset");
            if (balance == null || hazards == null || level == null) throw new InvalidDataException($"Thiếu dữ liệu sàn L{number}.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 9, -12), Quaternion.Euler(35, 0, 0));
            var ice = new GameObject(number == 5 ? "H06 Chaos Core" : "H06 Frost Ring"); ice.SetActive(false);
            ice.AddComponent<IceTiles>().Configure(profile, balance, hazards,
                AssetDatabase.LoadAssetAtPath<Material>(Root + "/Data/TrainingRing/Band1.mat"), number == 5);
            ice.SetActive(true);
            if (number == 5)
            {
                var cannonObject = new GameObject("Chaos Cannons"); cannonObject.SetActive(false);
                cannonObject.AddComponent<Cannons>().Configure(balance, hazards, new[]
                {
                    new Cannons.Mount { Kind = CannonKind.Air, AngleDegrees = 270f },
                    new Cannons.Mount { Kind = CannonKind.Fire, AngleDegrees = 90f },
                }, true);
                cannonObject.SetActive(true);
            }
            var arena = new GameObject(level.ArenaName);
            var spawns = level.SpawnPoints.Select((p, i) =>
            {
                var t = new GameObject($"Spawn {i + 1}").transform;
                t.SetParent(arena.transform, false); t.position = p; return t;
            }).ToArray();
            var so = new SerializedObject(arena.AddComponent<TrainingRingBounds>());
            so.FindProperty("outerRadius").floatValue = level.ArenaRadiusMeters;
            so.FindProperty("killPlaneY").floatValue = balance.KillPlaneY;
            var list = so.FindProperty("spawnPoints"); list.arraySize = spawns.Length;
            for (var i = 0; i < spawns.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, level.ScenePath);
            Debug.Log($"[IceSceneBuilder] built {level.ScenePath}");
        }
    }
}
