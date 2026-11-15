using System.IO;
using System.Linq;
using SquishyBump.Core;
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

        static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetDatabase.Refresh();
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
