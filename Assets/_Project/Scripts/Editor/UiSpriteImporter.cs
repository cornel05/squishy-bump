using UnityEditor;
using UnityEngine;

namespace SquishyBump.Editor
{
    /// <summary>Imports Art/UI sprites cleanly (only panels and slider bars get 9-slice borders) and sets the Sleeping font fallback.</summary>
    public sealed class UiSpriteImporter : AssetPostprocessor
    {
        const string Folder = "Assets/_Project/Art/UI/";

        // Left, bottom, right, top in source pixels (slice_kit art is 4x, import_uxui art is 1x).
        static Vector4? Border(string name) => name switch
        {
            "panel_box" => new Vector4(180, 180, 180, 180),
            "slider_track" or "slider_fill" => new Vector4(75, 0, 75, 0),
            "row_score" => new Vector4(24, 20, 0, 12),
            "plate" => new Vector4(24, 20, 24, 12),
            "hud_board" => new Vector4(24, 24, 24, 24),
            "btn_blank" => new Vector4(48, 0, 48, 0),
            _ => null,
        };

        // Sleeping has no Vietnamese diacritics: fall back to OS fonts for those glyphs ("điểm", "mạng").
        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith("/Sleeping.ttf")) return;
            var importer = (TrueTypeFontImporter)assetImporter;
            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.fontNames = new[] { "Segoe UI", "Arial" };
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spriteBorder = Border(System.IO.Path.GetFileNameWithoutExtension(assetPath)) ?? Vector4.zero;
        }
    }
}
