using System.IO;
using System.Linq;
using SquishyBump.Core;
using SquishyBump.Presentation;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Editor
{
    /// <summary>
    /// SB-76: the six fruit fighters become six animals. Slots, classes and GUIDs stay (MatchSetup.roster and the
    /// avatars keep working); only names, colours and the Visual/Body/Model GLB change. One-off, but re-runnable.
    /// </summary>
    public static class AnimalSwap
    {
        public const string ModelRoot = "Assets/_Project/Art/Characters";
        const string DataRoot = "Assets/_Project/Data";
        const string PrefabRoot = "Assets/_Project/Prefabs/Fighters";
        const string MaterialRoot = "Assets/_Project/Art/Materials/Fruits";
        const string PresentationProfilePath = DataRoot + "/Presentation/PresentationProfile.asset";

        /// <summary>Slot order: Heavy Bear + Pig, Normal Cat + Penguin, Light Chick + Frog.</summary>
        public static readonly string[] Animals = { "Bear", "Pig", "Cat", "Penguin", "Chick", "Frog" };
        static readonly string[] OldFruits = { "Watermelon", "Pineapple", "Apple", "Mangosteen", "Peach", "Blueberry" };

        // Extra rotation so every model stands upright and looks along +Z. The penguin mesh lies along -Z (its node has no scale).
        static Quaternion FacingFix(string animal) => animal == "Penguin" ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;

        [MenuItem("Squishy Bump/Swap To Animals")]
        public static void Swap()
        {
            for (var i = 0; i < Animals.Length; i++)
            {
                Move($"{PrefabRoot}/{OldFruits[i]}.prefab", $"{PrefabRoot}/{Animals[i]}.prefab");
                Move($"{DataRoot}/Fruits/{OldFruits[i]}.asset", $"{DataRoot}/Fruits/{Animals[i]}.asset");
                Move($"{MaterialRoot}/{OldFruits[i]}.mat", $"{MaterialRoot}/{Animals[i]}.mat");
            }
            foreach (var animal in Animals)
            {
                AttachModel($"{PrefabRoot}/{animal}.prefab", animal);
                ConfigureDefinition(animal);
            }
            ConfigurePresentation();
            AssetDatabase.DeleteAsset("Assets/_Project/Art/Fruits"); // the six fruit GLBs
            AssetDatabase.SaveAssets();
            Debug.Log("[AnimalSwap] done");
        }

        static void Move(string from, string to)
        {
            if (!File.Exists(from)) return;
            var error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error)) Debug.LogError($"[AnimalSwap] {error}");
        }

        /// <summary>Puts the animal GLB at Visual/Body/Model, fitted from renderer bounds: width = 2 × radius, feet at −radius.</summary>
        public static bool AttachModel(string prefabPath, string animal)
        {
            var glb = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelRoot}/{animal}.glb");
            if (glb == null) return false;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var body = root.transform.Find("Visual/Body");
                if (body == null) return false;
                var old = body.Find("Model");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(glb, body);
                instance.name = "Model";
                foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                var t = instance.transform;
                t.localPosition = Vector3.zero;
                t.localRotation = FacingFix(animal);
                t.localScale = Vector3.one;

                var radius = root.GetComponent<SphereCollider>().radius;
                var bounds = RendererBounds(instance);
                var scale = 2f * radius / bounds.size.x;
                var worldOffset = new Vector3(-bounds.center.x * scale, -radius - bounds.min.y * scale, -bounds.center.z * scale);
                t.localScale = Vector3.one * scale;
                t.localPosition = worldOffset / body.lossyScale.x; // Visual carries the sphere-mesh scale

                root.name = animal;
                var placeholder = body.GetComponent<MeshRenderer>();
                if (placeholder != null) placeholder.enabled = false;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static Bounds RendererBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        /// <summary>displayName = animal, colour = mean colour of the main texture of the biggest textured mesh.</summary>
        public static void ConfigureDefinition(string animal)
        {
            var definition = AssetDatabase.LoadAssetAtPath<FruitDefinition>($"{DataRoot}/Fruits/{animal}.asset");
            if (definition == null) return;
            var so = new SerializedObject(definition);
            so.FindProperty("displayName").stringValue = animal;
            var color = MainColor(AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelRoot}/{animal}.glb"));
            if (color.HasValue) so.FindProperty("color").colorValue = color.Value;
            so.ApplyModifiedPropertiesWithoutUndo();
            // The placeholder material of the fighter prefab carries the same colour (FighterTests reads it).
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{animal}.mat");
            if (material != null && color.HasValue) { material.color = color.Value; EditorUtility.SetDirty(material); }
            EditorUtility.SetDirty(definition);
        }

        static Color? MainColor(GameObject glb)
        {
            var best = glb.GetComponentsInChildren<Renderer>()
                .Select(r => (renderer: r, texture: TextureOf(r.sharedMaterial)))
                .Where(x => x.texture != null)
                .OrderByDescending(x => x.renderer.bounds.size.sqrMagnitude)
                .FirstOrDefault();
            if (best.texture == null) return null;

            var target = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Graphics.Blit(best.texture, target);
            RenderTexture.active = target;
            var copy = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            Color sum = default;
            foreach (var p in copy.GetPixels()) sum += p;
            Object.DestroyImmediate(copy);
            var mean = sum / (64 * 64);
            return new Color32((byte)Mathf.RoundToInt(mean.r * 255f), (byte)Mathf.RoundToInt(mean.g * 255f),
                (byte)Mathf.RoundToInt(mean.b * 255f), 255);
        }

        static Texture TextureOf(Material material)
        {
            if (material == null) return null;
            foreach (var property in new[] { "baseColorTexture", "_BaseMap", "_MainTex" })
                if (material.HasProperty(property) && material.GetTexture(property) != null) return material.GetTexture(property);
            return material.mainTexture;
        }

        /// <summary>Turns the upright mode on. Values live in the asset; the Scaffold never overwrites them.</summary>
        static void ConfigurePresentation()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PresentationProfile>(PresentationProfilePath);
            var so = new SerializedObject(profile);
            so.FindProperty("upright").boolValue = true;
            so.FindProperty("turnDegreesPerSecond").floatValue = 720f;
            so.FindProperty("leanMaxDegrees").floatValue = 14f;
            so.FindProperty("leanFullSpeed").floatValue = 6f;
            so.FindProperty("faceMinSpeed").floatValue = 0.3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }
    }
}
