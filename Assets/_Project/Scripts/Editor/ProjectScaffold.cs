using System.IO;
using System.Linq;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Physics;
using SquishyBump.Presentation;
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
        const string PresentationProfilePath = DataRoot + "/Presentation/PresentationProfile.asset";
        const string HazardProfilePath = DataRoot + "/Hazards/HazardProfile.asset";
        const string TrainingRingFloorPrefab = "Assets/_Project/Prefabs/Hazards/TrainingRingFloor.prefab";
        const string MatchSetupPath = DataRoot + "/MatchSetup.asset";
        const string PlaceholderMaterial = DataRoot + "/TrainingRing/FruitPlaceholder.mat";
        static readonly string[] ClassNames = { "Heavy", "Normal", "Light" };
        // SB-76: the six animals (slot order; same slots, classes and roster indices as the old fruits).
        static readonly string[] FruitNames = AnimalSwap.Animals;

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
            EnsureRollRigs();
            EnsureFruitModels();
            EnsureTrainingRingFloor();
            EnsureMatchSetup();
            EnsureSfxClips();
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
            EnsureAsset<PresentationProfile>(PresentationProfilePath);
            EnsureAsset<HazardProfile>(HazardProfilePath);
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

        // SB-13: Visual (RollVisual, squash) -> Body (mesh, rolls). Prefabs that already have the rig are skipped.
        static void EnsureRollRigs()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PresentationProfile>(PresentationProfilePath);
            foreach (var f in FruitNames)
            {
                var path = $"{FighterPrefabRoot}/{f}.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                var visual = root.transform.Find("Visual");
                if (visual != null && !visual.TryGetComponent<RollVisual>(out _))
                {
                    var body = new GameObject("Body");
                    body.transform.SetParent(visual, false);
                    body.AddComponent<MeshFilter>().sharedMesh = visual.GetComponent<MeshFilter>().sharedMesh;
                    body.AddComponent<MeshRenderer>().sharedMaterial = visual.GetComponent<MeshRenderer>().sharedMaterial;
                    Object.DestroyImmediate(visual.GetComponent<MeshRenderer>());
                    Object.DestroyImmediate(visual.GetComponent<MeshFilter>());
                    var roll = visual.gameObject.AddComponent<RollVisual>();
                    SetReferences(roll, ("profile", profile), ("body", body.transform));
                    var so = new SerializedObject(roll);
                    so.FindProperty("radius").floatValue = root.GetComponent<SphereCollider>().radius;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // SB-76: the animal .glb (Art/Characters) as Visual/Body/Model, fitted by AnimalSwap (width = 2 x radius, feet at
        // -radius). The placeholder renderer stays, switched off, so the radius is still visible in the Scene view.
        // Prefabs that already have a Model are skipped.
        static void EnsureFruitModels()
        {
            foreach (var f in FruitNames)
            {
                var path = $"{FighterPrefabRoot}/{f}.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                var hasModel = root.transform.Find("Visual/Body/Model") != null;
                PrefabUtility.UnloadPrefabContents(root);
                if (hasModel) continue;
                AnimalSwap.AttachModel(path, f);
                AnimalSwap.ConfigureDefinition(f);
            }
        }

        // SB-15: the L1 floor as one self-running H01 prefab. Bands come from the L1 band meshes, outermost first.
        static void EnsureTrainingRingFloor()
        {
            if (File.Exists(TrainingRingFloorPrefab)) return;
            EnsureFolder(Path.GetDirectoryName(TrainingRingFloorPrefab)!);
            var root = new GameObject("TrainingRingFloor");
            var bands = Enumerable.Range(1, 4)
                .Select(i => (mesh: AssetDatabase.LoadAssetAtPath<Mesh>($"{DataRoot}/TrainingRing/Band{i}.asset"),
                    material: AssetDatabase.LoadAssetAtPath<Material>($"{DataRoot}/TrainingRing/Band{i}.mat")))
                .OrderByDescending(b => b.mesh.bounds.extents.x)
                .Select((b, i) =>
                {
                    var go = new GameObject($"Band {i}");
                    go.transform.SetParent(root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = b.mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = b.material;
                    var collider = go.AddComponent<MeshCollider>();
                    collider.sharedMesh = b.mesh;
                    return (Object)collider;
                })
                .ToArray();

            var drop = root.AddComponent<RingDrop>();
            SetReferences(drop,
                ("balance", AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{DataRoot}/Balance/BalanceProfile.asset")),
                ("hazards", AssetDatabase.LoadAssetAtPath<HazardProfile>(HazardProfilePath)));
            var so = new SerializedObject(drop);
            var list = so.FindProperty("bands");
            list.arraySize = bands.Length;
            for (var i = 0; i < bands.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = bands[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, TrainingRingFloorPrefab);
            Object.DestroyImmediate(root);
        }

        /// <summary>Gán D1 vào MatchSetup hiện có mà không dựng lại toàn bộ prefab/scene.</summary>
        [MenuItem("Squishy Bump/Configure D1 Bots")]
        public static void ConfigureBotDefaults()
        {
            EnsureMatchSetup();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Tạo/nối cấu hình lobby bằng Editor API, không dựng lại toàn bộ prefab.</summary>
        [MenuItem("Squishy Bump/Configure Lobby Defaults")]
        public static void ConfigureLobbyDefaults()
        {
            EnsureMatchSetup();
            AssetDatabase.SaveAssets();
        }

        // SB-31: the scene flow's data asset, and the Boot bootstrap pointing at it. Fills only empty slots.
        static void EnsureMatchSetup()
        {
            var setup = EnsureAsset<MatchSetup>(MatchSetupPath);
            var so = new SerializedObject(setup);
            if (so.FindProperty("defaultBotProfile").objectReferenceValue == null)
            {
                so.FindProperty("defaultBotProfile").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<BotProfile>($"{DataRoot}/Bots/D1.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(setup);
            }
            var lobby = EnsureAsset<LobbyProfile>($"{DataRoot}/LobbyProfile.asset");
            var lobbyData = new SerializedObject(lobby);
            if (lobbyData.FindProperty("walkingClass").objectReferenceValue == null)
            {
                lobbyData.FindProperty("walkingClass").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ClassProfile>($"{DataRoot}/Classes/Normal.asset");
                lobbyData.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(lobby);
            }
            if (so.FindProperty("lobby").objectReferenceValue == null)
            {
                so.FindProperty("lobby").objectReferenceValue = lobby;
                so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(setup);
            }
            if (so.FindProperty("balance").objectReferenceValue == null)
            {
                so.FindProperty("balance").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{DataRoot}/Balance/BalanceProfile.asset");
                so.FindProperty("presentation").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<PresentationProfile>(PresentationProfilePath);
                var levels = so.FindProperty("levels");
                levels.arraySize = 5;
                for (var i = 0; i < 5; i++)
                    levels.GetArrayElementAtIndex(i).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{DataRoot}/Levels/L{i + 1}.asset");
                var roster = so.FindProperty("roster");
                roster.arraySize = FruitNames.Length;
                for (var i = 0; i < FruitNames.Length; i++)
                    roster.GetArrayElementAtIndex(i).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<GameObject>($"{FighterPrefabRoot}/{FruitNames[i]}.prefab");
                so.FindProperty("mainMenuScene").stringValue = $"{SceneRoot}/MainMenu.unity";
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(setup);
            }

            var boot = EditorSceneManager.OpenScene($"{SceneRoot}/Boot.unity", OpenSceneMode.Additive);
            var bootstrap = boot.GetRootGameObjects().Select(g => g.GetComponentInChildren<GameplayBootstrap>()).First(b => b != null);
            var bso = new SerializedObject(bootstrap);
            if (bso.FindProperty("setup").objectReferenceValue == null)
            {
                bso.FindProperty("setup").objectReferenceValue = setup;
                bso.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(boot);
            }
            if (EditorSceneManager.sceneCount > 1) EditorSceneManager.CloseScene(boot, true);
        }

        // SB-49: the twelve SFX placeholders (tools/audio/make_sfx.py) into PresentationProfile, in Sfx order.
        static void EnsureSfxClips()
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<PresentationProfile>(PresentationProfilePath));
            var list = so.FindProperty("sfx");
            var names = System.Enum.GetNames(typeof(Sfx));
            if (list.arraySize == names.Length) return;
            list.arraySize = names.Length;
            for (var i = 0; i < names.Length; i++)
            {
                var snake = System.Text.RegularExpressions.Regex.Replace(names[i], "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Audio/SFX/sfx_{snake}.wav");
                if (clip == null) Debug.LogError($"[ProjectScaffold] missing sfx_{snake}.wav");
                list.GetArrayElementAtIndex(i).objectReferenceValue = clip;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
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
