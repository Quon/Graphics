using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public enum IesLightType
    {
        Point,
        Spot,
    }

    public abstract class IesImporter : ScriptedImporter
    {
        public string FileFormatVersion;
        public string IesPhotometricType;
        public float  IesMaximumIntensity;
        public string IesMaximumIntensityUnit;

        // IES luminaire product information.
        public string Manufacturer;           // IES keyword MANUFAC
        public string LuminaireCatalogNumber; // IES keyword LUMCAT
        public string LuminaireDescription;   // IES keyword LUMINAIRE
        public string LampCatalogNumber;      // IES keyword LAMPCAT
        public string LampDescription;        // IES keyword LAMP

        public IesLightType PrefabLightType = IesLightType.Point;

        [Range(1f, 179f)]
        public float SpotAngle = 120f;
        [Range(32, 2048)]
        public int   SpotCookieSize = 512;
        public bool  ApplyLightAttenuation  = true;
        public bool  UseIesMaximumIntensity = true;

        public TextureImporterCompression CookieCompression = TextureImporterCompression.Uncompressed;

        [Range(-180f, 180f)]
        public float LightAimAxisRotation = -90f;

        protected abstract IesEngine CreateEngine();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var engine = CreateEngine();

            SetupIesEngineForRenderPipeline(engine);

            Texture cookieTexture      = null;
            Texture cylindricalTexture = null;

            string iesFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), ctx.assetPath);

            string errorMessage = engine.ReadFile(iesFilePath);

            if (string.IsNullOrEmpty(errorMessage))
            {
                FileFormatVersion      = engine.FileFormatVersion;
                IesPhotometricType     = engine.GetPhotometricType();
                Manufacturer           = engine.GetKeywordValue("MANUFAC");
                LuminaireCatalogNumber = engine.GetKeywordValue("LUMCAT");
                LuminaireDescription   = engine.GetKeywordValue("LUMINAIRE");
                LampCatalogNumber      = engine.GetKeywordValue("LAMPCAT");
                LampDescription        = engine.GetKeywordValue("LAMP");

                (IesMaximumIntensity, IesMaximumIntensityUnit) = engine.GetMaximumIntensity();

                string warningMessage;

                if (PrefabLightType == IesLightType.Point)
                {
                    (warningMessage, cookieTexture) = engine.GenerateCubeCookie(CookieCompression);
                }
                else // IesLightType.Spot
                {
                    (warningMessage, cookieTexture) = engine.Generate2DCookie(CookieCompression, SpotAngle, SpotCookieSize, ApplyLightAttenuation);
                }

                if (!string.IsNullOrEmpty(warningMessage))
                {
                    ctx.LogImportWarning($"Cannot properly generate IES cookie texture: {warningMessage}");
                }

                if (PrefabLightType == IesLightType.Point)
                {
                    (warningMessage, cylindricalTexture) = engine.GenerateCylindricalTexture(CookieCompression);

                    if (!string.IsNullOrEmpty(warningMessage))
                    {
                        ctx.LogImportWarning($"Cannot properly generate IES latitude-longitude texture: {warningMessage}");
                    }
                }
            }
            else
            {
                ctx.LogImportError($"Cannot read IES file '{iesFilePath}': {errorMessage}");
            }

            string iesFileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

            var lightObject = new GameObject(iesFileName);

            lightObject.transform.localEulerAngles = new Vector3(90f, 0f, LightAimAxisRotation);

            Light light = lightObject.AddComponent<Light>();
            light.type      = (PrefabLightType == IesLightType.Point) ? LightType.Point : LightType.Spot;
            light.intensity = 1f;  // would need a better intensity value formula
            light.range     = 10f; // would need a better range value formula
            light.spotAngle = SpotAngle;
            light.cookie    = cookieTexture;

            SetupRenderPipelinePrefabLight(engine, light);

            // The light object will be automatically converted into a prefab.
            ctx.AddObjectToAsset(iesFileName, lightObject);
            ctx.SetMainObject(lightObject);

            if (cookieTexture != null)
            {
                cookieTexture.name = iesFileName + "-Cookie";
                ctx.AddObjectToAsset(cookieTexture.name, cookieTexture);
            }

            if (cylindricalTexture != null)
            {
                cylindricalTexture.name = iesFileName + "-Cylindrical";
                ctx.AddObjectToAsset(cylindricalTexture.name, cylindricalTexture);

                // string filePath = Path.Combine(Path.GetDirectoryName(iesFilePath), cylindricalTexture.name + ".png");
                // byte[] bytes    = ((Texture2D)cylindricalTexture).EncodeToPNG();
                // File.WriteAllBytes(filePath, bytes);
            }
        }

        protected abstract void SetupIesEngineForRenderPipeline(IesEngine engine);
        //{
        //    Debug.LogError("IESImporter not specialized");
        //}

        protected abstract void SetupRenderPipelinePrefabLight(IesEngine engine, Light light);
        //{
        //    Debug.LogError("IESImporter not specialized");
        //}
    }
}
