using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Lighting
    {
        const int maxDirLightCount = 4;

        static readonly int 
            dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
            dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
            dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
            dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

        static Vector4[] 
            dirLightColors = new Vector4[maxDirLightCount],
            dirLightDirections = new Vector4[maxDirLightCount],
            dirLightShadowData = new Vector4[maxDirLightCount];

        const string bufferName = "Lighting";

        CommandBuffer buffer = new()
        {
            name = bufferName
        };

        private CullingResults cullingResults;
        private Shadows shadows = new();

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            this.cullingResults = cullingResults;
            this.buffer.BeginSample(bufferName);
            this.shadows.Setup(context, cullingResults, shadowSettings);
            this.SetupLights();
            this.shadows.Render();
            this.buffer.EndSample(bufferName);
            context.ExecuteCommandBuffer(buffer);
            this.buffer.Clear();
        }

        private void SetupLights()
        {
            NativeArray<VisibleLight> visibleLights = this.cullingResults.visibleLights;

            int dirLightCount = 0;

            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];

                if (visibleLight.lightType == LightType.Directional)
                {
                    this.SetupDirectionalLight(dirLightCount++, ref visibleLight);

                    if (dirLightCount >= maxDirLightCount)
                    {
                        break;
                    }
                }

            }

            this.buffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
            this.buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            this.buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            this.buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }

        private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
        {
            dirLightColors[index] = visibleLight.finalColor;
            dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
        }
        
        public void Cleanup () {
            shadows.Cleanup();
        }
    }
}