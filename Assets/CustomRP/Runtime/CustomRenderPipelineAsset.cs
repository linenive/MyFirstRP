using CustomRP.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
  [SerializeField]
  private bool useDynamicBatching = false, useGPUInstancing = true, useSRPBatcher = true;

  [SerializeField]
  ShadowSettings shadows = default;

  protected override RenderPipeline CreatePipeline()
  {
    return new CustomRenderPipeline(
      useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows);
  }
}
