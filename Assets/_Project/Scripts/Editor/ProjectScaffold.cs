using System.IO;
using System.Linq;
using SquishyBump.Core;
using SquishyBump.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SquishyBump.Editor
{
    /// <summary>
    /// Idempotent project scaffolding: placeholder scenes, build scene list and the
    /// Input Actions C# wrapper settings. Run from the menu or in batchmode:
    /// Unity -batchmode -quit -projectPath . -executeMethod SquishyBump.Editor.ProjectScaffold.Run
    /// </summary>
    public static class ProjectScaffold
    {
        const string SceneRoot = "Assets/_Project/Scenes";
        const string InputAsset = "Assets/_Project/Settings/Input/SquishyControls.inputactions";
        const string InputWrapper = "Assets/_Project/Scripts/Core/Input/SquishyControls.cs";

        const string DataRoot = "Assets/_Project/Data";
        const string FighterPrefabRoot = "Assets/_Project/Prefabs/Fighters";
        const string FruitMaterialRoot = "Assets/_Project/Art/Materials/Fruits";
        const string PlaceholderMaterial = DataRoot + "/TrainingRing/FruitPlaceholder.mat";
        static readonly string[] ClassNames = { "Heavy", "Normal", "Light" };
        static readonly string[] FruitNames = { "Watermelon", "Pineapple", "Apple", "Mangosteen", "Peach", "Blueberry" };

        // Build order: Boot first; sandboxes are never built.
        static readonly string[] BuildScenes =
        {
            "Boot", "MainMenu", "Lobby",
            "Levels/L1", "Levels/L2", "Levels/L3", "Levels/L4", "Levels/L5",
        };
        static readonly string[] SandboxScenes = { "Sandbox/Test_Duy", "Sandbox/Test_Nhat" };

        [MenuItem("Squishy Bump/Scaffold Project")]
        public static void Run()
        {
            foreach (var s in BuildScenes.Concat(SandboxScenes))
                EnsureScene($"{SceneRoot}/{s}.unity");

            EditorBuildSettings.scenes = BuildScenes
                .Select(s => new EditorBuildSettingsScene($"{SceneRoot}/{s}.unity", true))
                .ToArray();

            EnsureDataAssets();
            EnsureFighterPrefabs();
            ConfigureInputWrapper();
            // Project-wide actions = our asset (the template's default asset was removed).
            InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectScaffold] done");
        }

        static void EnsureScene(string path)
        {
            if (File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
        }

        // Creates missing assets only (Unity owns the GUIDs). Values are edited in the assets,
        // never here, and are guarded by BalanceProfileTests.
        static void EnsureDataAssets()
        {
            EnsureAsset<BalanceProfile>($"{DataRoot}/Balance/BalanceProfile.asset");
            foreach (var c in ClassNames) EnsureAsset<ClassProfile>($"{DataRoot}/Classes/{c}.asset");

            var sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            foreach (var f in FruitNames)
            {
                var so = new SerializedObject(EnsureAsset<FruitDefinition>($"{DataRoot}/Fruits/{f}.asset"));
                var mesh = so.FindProperty("mesh");
                if (mesh.objectReferenceValue != null) continue;
                mesh.objectReferenceValue = sphere;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Placeholder fighters (SB-11): sphere collider of the class radius, fruit-coloured sphere as visual child.
        // Existing prefabs are left alone, so hand edits and the later .glb visuals survive a re-run.
        static void EnsureFighterPrefabs()
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{DataRoot}/Balance/BalanceProfile.asset");
            var baseMaterial = AssetDatabase.LoadAssetAtPath<Material>(PlaceholderMaterial);
            EnsureFolder(FighterPrefabRoot);
            EnsureFolder(FruitMaterialRoot);

            foreach (var f in FruitNames)
            {
                var prefabPath = $"{FighterPrefabRoot}/{f}.prefab";
                if (File.Exists(prefabPath)) continue;
                var fruit = AssetDatabase.LoadAssetAtPath<FruitDefinition>($"{DataRoot}/Fruits/{f}.asset");

                var materialPath = $"{FruitMaterialRoot}/{f}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(baseMaterial) { color = fruit.Color };
                    AssetDatabase.CreateAsset(material, materialPath);
                }

                var root = new GameObject(f);
                var motor = root.AddComponent<PlayerMotor>(); // brings Rigidbody + SphereCollider
                var body = root.GetComponent<Rigidbody>();
                body.mass = fruit.Class.Mass;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                root.GetComponent<SphereCollider>().radius = fruit.Class.Radius;
                SetReferences(motor, ("balance", balance), ("fighterClass", fruit.Class));
                SetReferences(root.AddComponent<FighterProfileApplier>(), ("balance", balance), ("fruit", fruit));

                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = Vector3.one * (fruit.Class.Radius / fruit.Mesh.bounds.extents.x);
                visual.AddComponent<MeshFilter>().sharedMesh = fruit.Mesh;
                visual.AddComponent<MeshRenderer>().sharedMaterial = material;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
            }
        }

        static void SetReferences(Object target, params (string field, Object value)[] references)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in references) so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(Path.GetDirectoryName(path)!);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void ConfigureInputWrapper()
        {
            var importer = AssetImporter.GetAtPath(InputAsset);
            if (importer == null) { Debug.LogError($"[ProjectScaffold] missing {InputAsset}"); return; }

            // InputActionImporter is internal, so set its serialized fields directly.
            var so = new SerializedObject(importer);
            so.FindProperty("m_GenerateWrapperCode").boolValue = true;
            so.FindProperty("m_WrapperCodePath").stringValue = InputWrapper;
            so.FindProperty("m_WrapperClassName").stringValue = "SquishyControls";
            so.FindProperty("m_WrapperCodeNamespace").stringValue = "SquishyBump.Core.Input";
            if (so.ApplyModifiedPropertiesWithoutUndo()) importer.SaveAndReimport();
        }
    }
}
