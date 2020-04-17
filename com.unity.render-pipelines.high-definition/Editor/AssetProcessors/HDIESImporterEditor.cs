using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEngine.Rendering.HighDefinition
{
    [CustomEditor(typeof(IesImporter))]
    public class HDIESImporterEditor : UnityEditor.Rendering.IesImporterEditor
    {
        override public void LayoutRenderPipelineUseIesMaximumIntensity()
        {
            // Before enabling this feature, more experimentation is needed with the addition of a Volume in the PreviewRenderUtility scene.

            // EditorGUILayout.PropertyField(m_UseIesMaximumIntensityProp, new GUIContent("Use IES Maximum Intensity"));
        }

        override public void SetupRenderPipelinePreviewCamera(Camera camera)
        {
            HDAdditionalCameraData hdCamera = camera.gameObject.AddComponent<HDAdditionalCameraData>();

            hdCamera.clearDepth     = true;
            hdCamera.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;

            hdCamera.GetType().GetProperty("isEditorCameraPreview", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hdCamera, true, null);
        }

        override public void SetupRenderPipelinePreviewLight(Light light)
        {
            HDLightTypeAndShape hdLightTypeAndShape = (light.type == LightType.Point) ? HDLightTypeAndShape.Point : HDLightTypeAndShape.ConeSpot;

            HDAdditionalLightData hdLight = GameObjectExtension.AddHDLight(light.gameObject, hdLightTypeAndShape);

            hdLight.SetIntensity(20000f, LightUnit.Lumen);

            hdLight.affectDiffuse     = true;
            hdLight.affectSpecular    = false;
            hdLight.affectsVolumetric = false;
        }

        override public void SetupRenderPipelinePreviewWallRenderer(MeshRenderer wallRenderer)
        {
            wallRenderer.material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat");
        }

        override public void SetupRenderPipelinePreviewFloorRenderer(MeshRenderer floorRenderer)
        {
            floorRenderer.material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat");
        }

        override public void SetupRenderPipelinePreviewLightIntensity(Light light)
        {
            // Before enabling this feature, more experimentation is needed with the addition of a Volume in the PreviewRenderUtility scene.

            // HDAdditionalLightData hdLight = light.GetComponent<HDAdditionalLightData>();
            //
            // if (m_UseIesMaximumIntensityProp.boolValue)
            // {
            //     LightUnit lightUnit = (m_IesMaximumIntensityUnitProp.stringValue == "Lumens") ? LightUnit.Lumen : LightUnit.Candela;
            //     hdLight.SetIntensity(m_IesMaximumIntensityProp.floatValue, lightUnit);
            // }
            // else
            // {
            //     hdLight.SetIntensity(20000f, LightUnit.Lumen);
            // }
        }
    }
}
