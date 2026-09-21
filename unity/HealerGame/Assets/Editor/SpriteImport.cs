using UnityEditor;

namespace Healer.EditorTools
{
    /// <summary>
    /// Import settings for pixel-art sprites (D-071) : anything under Resources/Sprites is imported crisp (point filtering, no
    /// compression, no mipmaps), so a rebuilt sprite sheet always looks the same and the .meta files stay reproducible.
    /// </summary>
    public sealed class SpriteImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Sprites/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = UnityEngine.FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
        }
    }
}
