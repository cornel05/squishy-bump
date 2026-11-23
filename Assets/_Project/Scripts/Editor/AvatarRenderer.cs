using System.IO;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Editor
{
    /// <summary>
    /// SB-76: renders each animal prefab's model (3/4 view, orthographic) onto a white rounded tile and overwrites
    /// Art/UI/Resources/UI/avatar_0..5.png in slot order, so HUD, results and lobby avatars follow the slot.
    /// </summary>
    public static class AvatarRenderer
    {
        const int Size = 256;
        const float CornerRadius = 0.12f; // of the tile width
        const float Margin = 1.12f;
        const string Folder = "Assets/_Project/Art/UI/Resources/UI";
        const string PrefabRoot = "Assets/_Project/Prefabs/Fighters";

        [MenuItem("Squishy Bump/Render Avatars")]
        public static void RenderAll()
        {
            for (var i = 0; i < AnimalSwap.Animals.Length; i++)
                File.WriteAllBytes($"{Folder}/avatar_{i}.png", Render(AnimalSwap.Animals[i]).EncodeToPNG());
            AssetDatabase.Refresh();
            Debug.Log("[AvatarRenderer] avatars written");
        }

        static Texture2D Render(string animal)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{animal}.prefab");
            var preview = new PreviewRenderUtility();
            try
            {
                var source = prefab.transform.Find("Visual/Body/Model");
                var model = Object.Instantiate(source.gameObject);
                model.transform.SetPositionAndRotation(Vector3.zero, source.rotation);
                model.transform.localScale = source.lossyScale;
                preview.AddSingleGO(model);
                var bounds = AnimalSwap.RendererBounds(model);

                var camera = preview.camera;
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(1f, 1f, 1f, 0f);
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 50f;
                camera.orthographicSize = bounds.extents.magnitude * Margin * 0.82f;
                // In front of the model (it faces +Z), a little to the side and above.
                camera.transform.position = bounds.center + Quaternion.Euler(-20f, 30f, 0f) * Vector3.forward * 10f;
                camera.transform.LookAt(bounds.center);
                preview.lights[0].intensity = 1.2f;
                preview.lights[0].transform.rotation = Quaternion.Euler(40f, 210f, 0f);
                preview.ambientColor = new Color(0.55f, 0.55f, 0.55f);

                preview.BeginStaticPreview(new Rect(0, 0, Size, Size));
                camera.Render();
                var rendered = preview.EndStaticPreview();
                return Compose(rendered);
            }
            finally { preview.Cleanup(); }
        }

        // White rounded tile with the render alpha-blended on top; corners outside the tile stay transparent.
        static Texture2D Compose(Texture2D render)
        {
            var tile = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var radius = CornerRadius * Size;
            for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                {
                    var dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (Size - radius), 0f);
                    var dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (Size - radius), 0f);
                    var coverage = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    var over = render.GetPixel(x, y);
                    var color = Color.Lerp(Color.white, new Color(over.r, over.g, over.b, 1f), over.a);
                    tile.SetPixel(x, y, new Color(color.r, color.g, color.b, coverage));
                }
            Object.DestroyImmediate(render);
            tile.Apply();
            return tile;
        }
    }
}
