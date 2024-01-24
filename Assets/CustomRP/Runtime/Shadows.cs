using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Shadows
    {
        private const string BufferName = "Shadows";
        private const int MaxShadowedDirectionalLightCount = 1;

        private readonly CommandBuffer buffer = new CommandBuffer
        {
            name = BufferName
        };

        private ScriptableRenderContext context;

        private CullingResults cullingResults;

        private ShadowSettings settings;
        
        private int shadowedDirectionalLightCount;

        private struct ShadowedDirectionalLight {
            public int visibleLightIndex;
        }

        private ShadowedDirectionalLight[] shadowedDirectionalLights =
            new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];

        public void Setup(
            ScriptableRenderContext context, CullingResults cullingResults,
            ShadowSettings settings)
        {
            this.context = context;
            this.cullingResults = cullingResults;
            this.settings = settings;
            this.shadowedDirectionalLightCount = 0;
        }

        private void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (
                this.shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount &&
                light.shadows != LightShadows.None && light.shadowStrength > 0f && 
                this.cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                shadowedDirectionalLights[shadowedDirectionalLightCount++] = new ShadowedDirectionalLight
                {
                    visibleLightIndex = visibleLightIndex
                };
            }
        }
    }
}