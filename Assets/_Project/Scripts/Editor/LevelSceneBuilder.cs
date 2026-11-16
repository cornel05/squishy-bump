using System.Linq;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SquishyBump.Editor
{
    /// <summary>
    /// Builds Duy's level scenes (L2, L3) from their LevelDefinition and the hazard components, which make their
    /// own geometry from HazardProfile at runtime. Re-running overwrites the scene. Batchmode:
    /// -executeMethod SquishyBump.Editor.LevelSceneBuilder.BuildL2
    /// </summary>
    public static class LevelSceneBuilder
    {
        const string Root = "Assets/_Project";

        [MenuItem("Squishy Bump/Build Level L2 (Moving Ground)")]
        public static void BuildL2() => Build(2, scene =>
        {
            var ground = new GameObject("Moving Ground").AddComponent<MovingGround>();
            Set(ground, ("balance", Balance), ("hazards", Hazards),
                ("turntableMaterial", Mat(1)), ("ringMaterial", Mat(2)), ("boxMaterial", Mat(4)));
            var pushers = new GameObject("Pusher Blocks").AddComponent<PusherBlocks>();
            Set(pushers, ("balance", Balance), ("hazards", Hazards), ("ground", ground), ("blockMaterial", Mat(3)));
        });

        [MenuItem("Squishy Bump/Build Level L3 (Cannon Court)")]
        public static void BuildL3() => Build(3, scene =>
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Floor r 9 m";
            // Unity's cylinder is 1 m wide and 2 m tall: flatten it to an 18 m disc whose top sits at y = 0.
            floor.transform.localScale = new Vector3(18f, 0.05f, 18f);
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.AddComponent<MeshCollider>();
            floor.GetComponent<Renderer>().sharedMaterial = Mat(1);
            var cannons = new GameObject("Cannons");
            cannons.SetActive(false);
            var c = cannons.AddComponent<Cannons>();
            // GDD 7.3: fire at 45° and 225°, air at 135° and 315°, staggered in this order.
            c.Configure(Balance, Hazards, new[]
            {
                new Cannons.Mount { Kind = CannonKind.Fire, AngleDegrees = 45f },
                new Cannons.Mount { Kind = CannonKind.Air, AngleDegrees = 135f },
                new Cannons.Mount { Kind = CannonKind.Fire, AngleDegrees = 225f },
                new Cannons.Mount { Kind = CannonKind.Air, AngleDegrees = 315f },
            }, false);
            cannons.SetActive(true);
        });

        static void Build(int number, System.Action<UnityEngine.SceneManagement.Scene> addHazards)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{Root}/Data/Levels/L{number}.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main.transform;
            cam.position = new Vector3(0f, 9f, -12f);
            cam.rotation = Quaternion.Euler(35f, 0f, 0f);

            addHazards(scene);

            var arena = new GameObject(level.ArenaName);
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
            so.FindProperty("killPlaneY").floatValue = Balance.KillPlaneY;
            var list = so.FindProperty("spawnPoints");
            list.arraySize = spawns.Length;
            for (var i = 0; i < spawns.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, level.ScenePath);
            Debug.Log($"[LevelSceneBuilder] built {level.ScenePath}");
        }

        static BalanceProfile Balance => AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");
        static HazardProfile Hazards => AssetDatabase.LoadAssetAtPath<HazardProfile>($"{Root}/Data/Hazards/HazardProfile.asset");
        static Material Mat(int band) => AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Data/TrainingRing/Band{band}.mat");

        static void Set(Object target, params (string field, Object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values) so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
