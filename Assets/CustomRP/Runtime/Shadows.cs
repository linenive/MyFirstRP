using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Shadows
    {
        private const string BufferName = "Shadows";
        private const int MaxShadowedDirectionalLightCount = 4, maxCascades = 4;
        
        private static readonly int 
            DirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
            DirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
            CascadeCountId = Shader.PropertyToID("_CascadeCount"),
            CascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
            CascadeDataId = Shader.PropertyToID("_CascadeData"),
            ShadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
            ShadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
        
        static string[] directionalFilterKeywords = {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };
        
        static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades],
            cascadeData = new Vector4[maxCascades];
        
        private static readonly Matrix4x4[]
            DirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount * 4 * maxCascades];

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
            public float slopeScaleBias;
            public float nearPlaneOffset;
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
        
        /// <summary>
        /// WS -> Shadow Tile Space
        /// </summary>
        private static Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 lightMatrix, Vector2 tileOffset, int split) {
            // 반전된 Z 버퍼가 사용될 경우 Z 차원을 무효화한다.
            if (SystemInfo.usesReversedZBuffer) {
                lightMatrix.m20 = -lightMatrix.m20;
                lightMatrix.m21 = -lightMatrix.m21;
                lightMatrix.m22 = -lightMatrix.m22;
                lightMatrix.m23 = -lightMatrix.m23;
            }

            var scale = 1.0f / split;
            lightMatrix.m00 = (0.5f * (lightMatrix.m00 + lightMatrix.m30) + tileOffset.x * lightMatrix.m30) * scale;
            lightMatrix.m01 = (0.5f * (lightMatrix.m01 + lightMatrix.m31) + tileOffset.x * lightMatrix.m31) * scale;
            lightMatrix.m02 = (0.5f * (lightMatrix.m02 + lightMatrix.m32) + tileOffset.x * lightMatrix.m32) * scale;
            lightMatrix.m03 = (0.5f * (lightMatrix.m03 + lightMatrix.m33) + tileOffset.x * lightMatrix.m33) * scale;
            lightMatrix.m10 = (0.5f * (lightMatrix.m10 + lightMatrix.m30) + tileOffset.y * lightMatrix.m30) * scale;
            lightMatrix.m11 = (0.5f * (lightMatrix.m11 + lightMatrix.m31) + tileOffset.y * lightMatrix.m31) * scale;
            lightMatrix.m12 = (0.5f * (lightMatrix.m12 + lightMatrix.m32) + tileOffset.y * lightMatrix.m32) * scale;
            lightMatrix.m13 = (0.5f * (lightMatrix.m13 + lightMatrix.m33) + tileOffset.y * lightMatrix.m33) * scale;
            lightMatrix.m20 = 0.5f * (lightMatrix.m20 + lightMatrix.m30);
            lightMatrix.m21 = 0.5f * (lightMatrix.m21 + lightMatrix.m31);
            lightMatrix.m22 = 0.5f * (lightMatrix.m22 + lightMatrix.m32);
            lightMatrix.m23 = 0.5f * (lightMatrix.m23 + lightMatrix.m33);
            return lightMatrix;
        }

        public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (
                this.shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount &&
                light.shadows != LightShadows.None && light.shadowStrength > 0f && 
                this.cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                shadowedDirectionalLights[shadowedDirectionalLightCount] = new ShadowedDirectionalLight
                {
                    visibleLightIndex = visibleLightIndex,
                    slopeScaleBias = light.shadowBias,
                    nearPlaneOffset = light.shadowNearPlane
                };
                return new Vector3(
                    light.shadowStrength, 
                    settings.directional.cascadeCount * shadowedDirectionalLightCount++,
                    light.shadowNormalBias);
            }
            return Vector3.zero;
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
           
            // 그림자가 드리워진 조명이 두 개 이상이면 타일 크기를 절반으로 줄여 아틀라스를 네 타일로 분할해야 한다.
            var tiles = shadowedDirectionalLightCount * settings.directional.cascadeCount;
            var split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            var tileSize = atlasSize / split;
            
            for (var i = 0; i < shadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }
            
            this.buffer.SetGlobalInt(CascadeCountId, settings.directional.cascadeCount);
            this.buffer.SetGlobalVectorArray(
                CascadeCullingSpheresId, cascadeCullingSpheres
            );
            this.buffer.SetGlobalVectorArray(CascadeDataId, cascadeData);
            this.buffer.SetGlobalMatrixArray(DirShadowMatricesId, DirShadowMatrices);
            var f = 1f - settings.directional.cascadeFade;
            this.buffer.SetGlobalVector(
                ShadowDistanceFadeId,
                new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade,
                    1f / (1f - f * f))
            );
            this.SetKeywords();
            this.buffer.SetGlobalVector(
                ShadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize)
            );
            this.buffer.EndSample(BufferName);
            this.ExecuteBuffer();
        }
        
        private void SetKeywords () {
            var enabledIndex = (int)settings.directional.filter - 1;
            for (var i = 0; i < directionalFilterKeywords.Length; i++) {
                if (i == enabledIndex) {
                    buffer.EnableShaderKeyword(directionalFilterKeywords[i]);
                }
                else {
                    buffer.DisableShaderKeyword(directionalFilterKeywords[i]);
                }
            }
        }
        
        /// <summary>
        /// 타일의 인덱스를 기반으로 뷰포트를 설정한다.
        /// </summary>
        private Vector2 SetTileViewport (int index, int split, float tileSize) {
            var offset = new Vector2(index % split, index / split);
            this.buffer.SetViewport(new Rect(
                offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
            ));
            return offset;
        }
        
        private void RenderDirectionalShadows(int index, int split, int tileSize)
        {
            var light = shadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(
                this.cullingResults, light.visibleLightIndex,
                BatchCullingProjectionType.Orthographic);
            var cascadeCount = settings.directional.cascadeCount;
            var tileOffset = index * cascadeCount;
            var ratios = settings.directional.CascadeRatios;

            for (int i = 0; i < cascadeCount; i++)
            {
                // 방향성 조명은 무한히 멀리 있다고 가정하므로, 
                // 거리를 사용하는 대신, 조명의 방향과 일치하는 뷰 / 투영 매트릭스를 파악하여
                // Clip Space 와 겹치도록 한다.
                this.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    light.visibleLightIndex,
                    // 그림자 캐스케이드를 제어. (거리에 따라 절두체를 나누어 서로 다른 해상도로 보여준다)
                    splitIndex: i, splitCount: cascadeCount, splitRatio: ratios,
                    tileSize, 
                    // 지금 단계에서는 무시한다.
                    shadowNearPlaneOffset: light.nearPlaneOffset,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                    out ShadowSplitData splitData);
                shadowSettings.splitData = splitData;
                SetCascadeData(i, splitData.cullingSphere, tileSize);
                var tileIndex = tileOffset + i;
            
                DirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                    projectionMatrix * viewMatrix,
                    this.SetTileViewport(tileIndex, split, tileSize),
                    split);
                this.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                this.buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
                ExecuteBuffer();
                context.DrawShadows(ref shadowSettings);
                this.buffer.SetGlobalDepthBias(0f, 0f);
            }
        }
        
        private void SetCascadeData (int index, Vector4 cullingSphere, float tileSize) {
            var texelSize = 2f * cullingSphere.w / tileSize;
            float filterSize = texelSize * ((float)settings.directional.filter + 1f);
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[index] = cullingSphere;
            cascadeData[index] = new Vector4(
                1f / cullingSphere.w,
                filterSize * 1.4142136f
            );
        }
        
        public void Cleanup () {
            buffer.ReleaseTemporaryRT(DirShadowAtlasId);
            ExecuteBuffer();
        }
    }
}