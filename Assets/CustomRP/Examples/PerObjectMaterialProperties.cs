using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int cutoffId = Shader.PropertyToID("_Cutoff");
    private static int metalicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    private Color baseColor = Color.white;
    [SerializeField, Range(0f, 1f)]
    private float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;
    private static MaterialPropertyBlock block;

    private void Awake()
    {
        this.OnValidate();
    }

    private void OnValidate()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(baseColorId, this.baseColor);
        block.SetFloat(cutoffId, cutoff);
        block.SetFloat(metalicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
        this.GetComponent<Renderer>().SetPropertyBlock(block);
    }
}