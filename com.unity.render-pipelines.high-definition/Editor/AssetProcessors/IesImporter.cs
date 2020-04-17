using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [UnityEditor.Experimental.AssetImporters.ScriptedImporter(1, "ies")]
    public class IesImporter : UnityEditor.Rendering.IesImporter
    {
        protected override UnityEditor.Rendering.IesEngine CreateEngine()
        {
            UnityEngine.Rendering.HighDefinition.IesEngine iesEngine = new UnityEngine.Rendering.HighDefinition.IesEngine();
            return iesEngine as UnityEditor.Rendering.IesEngine;
        }

        protected override void SetupIesEngineForRenderPipeline(UnityEditor.Rendering.IesEngine engine)
        {
            engine.TextureGenerationType = TextureImporterType.Default;
        }

        protected override void SetupRenderPipelinePrefabLight(UnityEditor.Rendering.IesEngine engine, Light light)
        {
            HDLightTypeAndShape hdLightTypeAndShape = (light.type == LightType.Point) ? HDLightTypeAndShape.Point : HDLightTypeAndShape.ConeSpot;

            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, hdLightTypeAndShape);

            if (UseIesMaximumIntensity)
            {
                LightUnit lightUnit = (IesMaximumIntensityUnit == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
                hdLight.SetIntensity(IesMaximumIntensity, lightUnit);
            }
        }
    }
}
