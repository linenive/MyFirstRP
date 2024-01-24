using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public partial class CameraRenderer
    {
        private ScriptableRenderContext context;

        private Camera camera;
        private CullingResults cullingResults;

        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
        private const string bufferName = "Render Camera";

        private CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        private Lighting lighting = new();

        public void Render(
            ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing,
            ShadowSettings shadowSettings)
        {
            this.context = context;
            this.camera = camera;

            this.PrepareBuffer();
            this.PrepareForSceneWindow();
            if (!this.Cull(shadowSettings.maxDistance))
            {
                return;
            }

            this.Setup();
            lighting.Setup(context, cullingResults, shadowSettings);
            this.DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
            this.DrawUnsupportedShaders();
            this.DrawGizmos();
            this.Submit();
        }

        private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
        {
            var sortingSettings = new SortingSettings(camera);
            var drawingSettings = new DrawingSettings(
                unlitShaderTagId, sortingSettings
            )
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing
            };
            drawingSettings.SetShaderPassName(1, litShaderTagId);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            this.context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings);

            this.context.DrawSkybox(camera);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            this.context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private bool Cull(float maxShadowDistance)
        {
            if (camera.TryGetCullingParameters(out var p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
                cullingResults = this.context.Cull(ref p);
                return true;
            }
            return false;
        }

        private void Setup()
        {
            this.context.SetupCameraProperties(camera);
            CameraClearFlags flags = camera.clearFlags;
            this.buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ?
                    camera.backgroundColor.linear : Color.clear);
            this.buffer.BeginSample((string)SampleName);
            this.ExecuteBuffer();
        }

        private void Submit()
        {
            this.buffer.EndSample((string)SampleName);
            this.ExecuteBuffer();
            this.context.Submit();
        }

        private void ExecuteBuffer()
        {
            this.context.ExecuteCommandBuffer(buffer);
            this.buffer.Clear();
        }
    }
}
