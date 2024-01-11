using UnityEngine;

public class MeshBall : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    private Mesh mesh = default;

    [SerializeField]
    private Material material = default;

    private Matrix4x4[] matrices = new Matrix4x4[1023];
    private Vector4[] baseColors = new Vector4[1023];
    private float[] metallic = new float[1023];
    private float[] smoothness = new float[1023];
    private MaterialPropertyBlock block;

    private void Awake()
    {
        for (int i = 0; i < this.matrices.Length; i++)
        {
            this.matrices[i] = Matrix4x4.TRS(
              Random.insideUnitSphere * 10f, Quaternion.Euler(
                    Random.value * 360f, Random.value * 360f, Random.value * 360f
                ), Vector3.one * Random.Range(0.5f, 1.5f)
             );
            this.baseColors[i] =
              new Vector4(Random.value, Random.value, Random.value,
              Random.Range(0.5f, 1f));
            metallic[i] = Random.value < 0.25f ? 1f : 0f;
            smoothness[i] = Random.Range(0.05f, 0.95f);
        }
    }

    private void Update()
    {
        if (this.block == null)
        {
            this.block = new MaterialPropertyBlock();
            this.block.SetVectorArray(baseColorId, this.baseColors);
            block.SetFloatArray(metallicId, this.metallic);
            block.SetFloatArray(smoothnessId, this.smoothness);
        }
        Graphics.DrawMeshInstanced(this.mesh, 0, this.material, this.matrices, 1023, this.block);
    }
}