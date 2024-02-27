using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int
        baseColorId = Shader.PropertyToID("_BaseColor"),
        cutoffId = Shader.PropertyToID("_Cutoff"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoothness"),
        emissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField]
    private Color baseColor = Color.white;
    [SerializeField, Range(0f, 1f)]
    private float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;
    [SerializeField, ColorUsage(false, true)]
    Color emissionColor = Color.black;
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
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
        block.SetColor(emissionColorId, emissionColor);
        this.GetComponent<Renderer>().SetPropertyBlock(block);
    }
}