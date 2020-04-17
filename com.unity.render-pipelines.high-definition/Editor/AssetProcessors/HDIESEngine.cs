using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEngine.Rendering.HighDefinition
{
    // Photometric type coordinate system references:
    // https://www.ies.org/product/approved-method-guide-to-goniometer-measurements-and-types-and-photometric-coordinate-systems/
    // https://support.agi32.com/support/solutions/articles/22000209748-type-a-type-b-and-type-c-photometry
    public class HDIESEngine : UnityEditor.Rendering.IesEngine
    {

        // k_MinTextureSize should be 32, but using a larger value to minimize Unity's issue with cubemap cookies made from low-resolution latitude-longitude images.
        // When used, such a cubemap cookie North-South axis is visually tilted compared to its point light Y axis.
        // In other words, when the light Y rotation is modified, the cookie highlights and shadows wriggles on the floor and walls.
        const int k_MinTextureSize = 256; // power of two >= 32
        const int k_MaxTextureSize = 2048; // power of two <= 2048

        const int k_CylindricalTextureHeight = 256;                            // for 180 latitudinal degrees
        const int k_CylindricalTextureWidth  = 2*k_CylindricalTextureHeight; // for 360 longitudinal degrees

        public override (string, Texture) GenerateCubeCookie(UnityEditor.TextureImporterCompression compression)
        {
            int width  = Mathf.NextPowerOfTwo(Mathf.Clamp(m_IesReader.GetMinHorizontalSampleCount(), k_MinTextureSize, k_MaxTextureSize)); // for 360 longitudinal degrees
            int height = Mathf.NextPowerOfTwo(Mathf.Clamp(m_IesReader.GetMinVerticalSampleCount(), k_MinTextureSize, k_MaxTextureSize)); // for 180 latitudinal degrees

            NativeArray<Color32> colorBuffer;

            switch (m_IesReader.PhotometricType)
            {
                case 3: // type A
                    colorBuffer = BuildTypeACylindricalTexture(width, height);
                    break;
                case 2: // type B
                    colorBuffer = BuildTypeBCylindricalTexture(width, height);
                    break;
                default: // type C
                    colorBuffer = BuildTypeCCylindricalTexture(width, height);
                    break;
            }

            return GenerateTexture(m_TextureGenerationType, UnityEditor.TextureImporterShape.TextureCube, compression, width, height, colorBuffer);
        }

        // Gnomonic projection reference:
        // http://speleotrove.com/pangazer/gnomonic_projection.html
        public override (string, Texture) Generate2DCookie(UnityEditor.TextureImporterCompression compression, float coneAngle, int textureSize, bool applyLightAttenuation)
        {
            NativeArray<Color32> colorBuffer;

            switch (m_IesReader.PhotometricType)
            {
                case 3: // type A
                    colorBuffer = BuildTypeAGnomonicTexture(coneAngle, textureSize, applyLightAttenuation);
                    break;
                case 2: // type B
                    colorBuffer = BuildTypeBGnomonicTexture(coneAngle, textureSize, applyLightAttenuation);
                    break;
                default: // type C
                    colorBuffer = BuildTypeCGnomonicTexture(coneAngle, textureSize, applyLightAttenuation);
                    break;
            }

            return GenerateTexture(m_TextureGenerationType, UnityEditor.TextureImporterShape.Texture2D, compression, textureSize, textureSize, colorBuffer);
        }

        public override (string, Texture) GenerateCylindricalTexture(UnityEditor.TextureImporterCompression compression)
        {
            int width = k_CylindricalTextureWidth;  // for 360 longitudinal degrees
            int height = k_CylindricalTextureHeight; // for 180 latitudinal degrees

            NativeArray<Color32> colorBuffer;

            switch (m_IesReader.PhotometricType)
            {
                case 3: // type A
                    colorBuffer = BuildTypeACylindricalTexture(width, height);
                    break;
                case 2: // type B
                    colorBuffer = BuildTypeBCylindricalTexture(width, height);
                    break;
                default: // type C
                    colorBuffer = BuildTypeCCylindricalTexture(width, height);
                    break;
            }

            return GenerateTexture(UnityEditor.TextureImporterType.Default, UnityEditor.TextureImporterShape.Texture2D, compression, width, height, colorBuffer);
        }

        public override (string, Texture) GenerateTexture(UnityEditor.TextureImporterType type, UnityEditor.TextureImporterShape shape, UnityEditor.TextureImporterCompression compression, int width, int height, NativeArray<Color32> colorBuffer)
        {
            // Default values set by the TextureGenerationSettings constructor can be found in this file on GitHub:
            // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/AssetPipeline/TextureGenerator.bindings.cs

            var settings = new TextureGenerationSettings(type);

            SourceTextureInformation textureInfo = settings.sourceTextureInformation;
            textureInfo.containsAlpha = true;
            textureInfo.height = height;
            textureInfo.width = width;

            UnityEditor.TextureImporterSettings textureImporterSettings = settings.textureImporterSettings;
            textureImporterSettings.alphaSource = UnityEditor.TextureImporterAlphaSource.FromInput;
            textureImporterSettings.aniso = 0;
            textureImporterSettings.borderMipmap = (textureImporterSettings.textureType == UnityEditor.TextureImporterType.Cookie);
            textureImporterSettings.filterMode = FilterMode.Bilinear;
            textureImporterSettings.generateCubemap = UnityEditor.TextureImporterGenerateCubemap.Cylindrical;
            textureImporterSettings.mipmapEnabled = false;
            textureImporterSettings.npotScale = UnityEditor.TextureImporterNPOTScale.None;
            textureImporterSettings.readable = true;
            textureImporterSettings.sRGBTexture = false;
            textureImporterSettings.textureShape = shape;
            textureImporterSettings.wrapMode = textureImporterSettings.wrapModeU = textureImporterSettings.wrapModeV = textureImporterSettings.wrapModeW = TextureWrapMode.Clamp;

            UnityEditor.TextureImporterPlatformSettings platformSettings = settings.platformSettings;
            platformSettings.maxTextureSize = 2048;
            platformSettings.resizeAlgorithm = UnityEditor.TextureResizeAlgorithm.Bilinear;
            platformSettings.textureCompression = compression;

            TextureGenerationOutput output = TextureGenerator.GenerateTexture(settings, colorBuffer);

            if (output.importWarnings.Length > 0)
            {
                Debug.LogWarning("Cannot properly generate IES texture:\n" + string.Join("\n", output.importWarnings));
            }

            return (output.importInspectorWarnings, output.texture);
        }
    }
}
