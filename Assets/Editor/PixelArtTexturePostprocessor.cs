using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MousePlatformer
{
    public sealed class PixelArtTexturePostprocessor : AssetPostprocessor
    {
        private const string RootFolder = "Assets/Art/Pixel Art";
        private const string TilesFolder = "Assets/Art/Tiles";

        private void OnPreprocessTexture()
        {
            if (!IsInPixelArtFolder(assetPath)) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width <= 0 || height <= 0) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 32f;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.anisoLevel = 0;
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize,
                Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(width, height)), 32, 16384));

            // Reimports must preserve manual sprite slicing and existing Sprite references.
            if (importer.importSettingsMissing)
            {
                importer.spriteImportMode = width != height || width > 64 || height > 64
                    ? SpriteImportMode.Multiple
                    : SpriteImportMode.Single;
            }

            foreach (string platform in new[] { "Standalone", "Android", "iPhone", "WebGL", "Server" })
            {
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
                if (!settings.overridden) continue;
                settings.textureCompression = TextureImporterCompression.Uncompressed;
                settings.crunchedCompression = false;
                settings.format = TextureImporterFormat.RGBA32;
                settings.maxTextureSize = importer.maxTextureSize;
                importer.SetPlatformTextureSettings(settings);
            }
        }

        private static bool IsInPixelArtFolder(string path)
        {
            return path.StartsWith(RootFolder + "/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(TilesFolder + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            HashSet<string> textures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in movedAssets)
            {
                if (!IsInPixelArtFolder(path)
                    && !string.Equals(path, RootFolder, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(path, TilesFolder, StringComparison.OrdinalIgnoreCase)) continue;
                if (AssetDatabase.IsValidFolder(path))
                {
                    string[] identifiers = AssetDatabase.FindAssets("t:Texture2D", new string[] { path });
                    foreach (string identifier in identifiers)
                        textures.Add(AssetDatabase.GUIDToAssetPath(identifier));
                }
                else if (AssetImporter.GetAtPath(path) is TextureImporter)
                {
                    textures.Add(path);
                }
            }

            // Moving an existing texture preserves its importer, so explicitly reapply the folder rules.
            foreach (string path in textures)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
