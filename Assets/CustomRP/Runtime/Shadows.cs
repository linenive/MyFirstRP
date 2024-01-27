using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Shadows
    {
        private static readonly int DirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        
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

        public void Render()
        {
            if (this.shadowedDirectionalLightCount > 0) {
                this.RenderDirectionalShadows();
            }
            else
            {
                // 셰도우 맵이 필요하지 않은 경우에는 1x1 텍스처를 사용하여 텍스처 누락을 막는다.
                buffer.GetTemporaryRT(
                    DirShadowAtlasId, 1, 1,
                    32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            }
        }

        private void RenderDirectionalShadows()
        {
            var atlasSize = (int) this.settings.directional.atlasSize;
            this.buffer.GetTemporaryRT(
                DirShadowAtlasId, atlasSize, atlasSize,
                // 텍스처 포맷의 결정. 정확한 형식은 대상 플랫폼에 따라 다르다.
                // 깊이 버퍼의 비트 수 : 가능한 한 높게 설정한다.
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            // 카메라 타깃 대신 이 텍스처로 렌더링하도록 지시한다.
            buffer.SetRenderTarget(
                DirShadowAtlasId,
                // 버퍼를 즉시 지울 것이므로 초기 상태는 신경 쓰지 않는다.
                RenderBufferLoadAction.DontCare, 
                // 텍스처에 그림자 데이터를 포함시킨다는 목적이므로 저장한다. 
                RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(true, false, Color.clear);
            buffer.BeginSample(BufferName);
            ExecuteBuffer();
            
            for (int i = 0; i < shadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadows(i, atlasSize);
            }
            
            buffer.EndSample(BufferName);
            ExecuteBuffer();
        }
        
        private void RenderDirectionalShadows(int index, int tileSize)
        {
            var light = shadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(
                this.cullingResults, light.visibleLightIndex,
                BatchCullingProjectionType.Orthographic);
            // 방향성 조명은 무한히 멀리 있다고 가정하므로, 
            // 거리를 사용하는 대신, 조명의 방향과 일치하는 뷰 / 투영 매트릭스를 파악하여
            // Clip Space 와 겹치도록 한다.
            this.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex,
                // 그림자 캐스케이드를 제어. (거리에 따라 절두체를 나누어 서로 다른 해상도로 보여준다)
                splitIndex: 0, splitCount: 1, splitRatio: Vector3.zero,
                tileSize, 
                // 지금 단계에서는 무시한다.
                shadowNearPlaneOffset: .0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData);
            shadowSettings.splitData = splitData;
            this.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            
        }
        
        public void Cleanup () {
            buffer.ReleaseTemporaryRT(DirShadowAtlasId);
            ExecuteBuffer();
        }
    }
}